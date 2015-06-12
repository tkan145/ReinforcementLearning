﻿module Compute

open MathNet.Numerics.Distributions

//-------------------------------------------------------------------------------------------------

let tasks = 2000
let steps = 1000
let n = 10

//-------------------------------------------------------------------------------------------------

type State =
    { ActionValuesActual : double[]
      ActionValuesApprox : double[]
      KthSelectionCounts : int[] }

type Value =
    { Reward : double
      OptimalActionTaken : bool }

type Result =
    { AverageReward : double
      OptimalAction : double }

//-------------------------------------------------------------------------------------------------

let selectOptimalAction actionValues =
    actionValues
    |> Array.mapi (fun i x -> i, x)
    |> Array.maxBy snd
    |> fst

let selectAction epsilon random state =
    match (Sample.continuousUniform 0.0 1.0 random) with
    | x when x < epsilon
        -> Sample.discreteUniform 0 (n - 1) random
    | _ -> selectOptimalAction state.ActionValuesApprox

let executeOneStep epsilon random state =

    let actionOptimal = selectOptimalAction state.ActionValuesActual
    let action = selectAction epsilon random state
    let reward = state.ActionValuesActual.[action] + (Sample.normal 0.0 1.0 random)
    let approx = state.ActionValuesApprox.[action]
    let k = state.KthSelectionCounts.[action]

    state.ActionValuesApprox.[action] <- approx + ((reward - approx) / double k)
    state.KthSelectionCounts.[action] <- k + 1

    { Reward = reward; OptimalActionTaken = (action = actionOptimal) }, state

let executeOneTask epsilon random _ =

    let state =
        { ActionValuesActual = Array.init n (fun i -> Sample.normal 0.0 1.0 random)
          ActionValuesApprox = Array.create n 0.0
          KthSelectionCounts = Array.create n 1 }

    state
    |> Seq.unfold (executeOneStep epsilon random >> Some)
    |> Seq.take steps
    |> Seq.toArray

let computeAverageResults (valuesOfTasks : Value[][]) =

    let average step selection =
        valuesOfTasks |> Seq.averageBy (fun values -> selection values.[step])

    let averageResult step =
        { AverageReward = average step (fun x -> x.Reward)
          OptimalAction = average step (fun x -> if x.OptimalActionTaken then 1.0 else 0.0) }

    Array.init steps averageResult

let computeResults epsilon random =
    executeOneTask epsilon random
    |> Array.init tasks
    |> computeAverageResults
