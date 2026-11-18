namespace Wrench.Samples.FSharp

open Wrench
open Zomboid.Java
open Zomboid.Java.Characters

type private Milestone =
    | Quiet
    | Round of int
    | Century of int

type KillTally() =
    inherit ModInstance()

    let mutable kills = 0

    let milestone n =
        match n with
        | n when n % 100 = 0 -> Century n
        | n when n % 25 = 0 -> Round n
        | _ -> Quiet

    let onZombieDead () =
        kills <- kills + 1
        let player = Globals.GetPlayer()
        match milestone kills, player with
        | _, null -> ()
        | Quiet, _ -> ()
        | Round n, p -> HaloTextHelper.AddGoodText(p, $"{n} zombies down")
        | Century n, p -> HaloTextHelper.AddGoodText(p, $"{n} zombies down. Take a breather.")

    override this.OnLoad() =
        this.RegisterLuaEvent(LuaEvents.OnGameStart, fun () -> kills <- 0) |> ignore
        this.RegisterLuaEvent(LuaEvents.OnZombieDead, onZombieDead) |> ignore
