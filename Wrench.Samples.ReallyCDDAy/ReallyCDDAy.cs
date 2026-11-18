using Wrench;
using Wrench.Constants;
using Zomboid.Java;
using Zomboid.Java.Iso.Objects;
using Zomboid.Lua;
using Zomboid.Lua.Umbrella;

namespace Wrench.Samples.ReallyCDDAy;

/// <summary>
/// PZ's "A Really CD DA" challenge (media/lua/client/Challenge/AReallyCDDAy.lua) ported to C#, registered under its
/// own id so it appears alongside the original.
/// </summary>
public sealed class ReallyCDDAy : ModInstance
{
    private const string ChallengeId = "AReallyCDDAyDotnet";

    private static readonly (int X, int Y, int Z)[] Spawns =
    [
        (10788, 9985, 0), (10693, 9745, 0), (10770, 9688, 0), (10746, 9413, 1), (11791, 6855, 0), (11604, 6877, 1),
        (11496, 6700, 1), (11415, 6665, 0), (11218, 6791, 1), (11217, 6797, 1), (10639, 6800, 1), (10839, 12153, 1),
        (11673, 6810, 1),
    ];

    private bool _active;

    public override void OnLoad()
    {
        RegisterLuaEvent(LuaEvents.OnChallengeQuery, AddChallenge);
        RegisterLuaEvent(LuaEvents.OnGameStart, OnGameStart);
        // The Lua version adds its OnGameStart handler from OnInitWorld; a flag gives the same effect without
        // leaking into later games started from the same session.
        RegisterLuaEvent(LuaEvents.OnMainMenuEnter, () => _active = false);
    }

    private void AddChallenge()
    {
        var challenge = new ZomboidChallenge
        {
            Id = ChallengeId,
            OnInitWorld = Lua.Function(OnInitWorld),
            AddPlayer = Lua.Function((double _, GameObject player) => AddPlayer(player)),
            RemovePlayer = Lua.Function((GameObject? _) => { }),
            Render = Lua.Function(() => { }),
            CompletionText = "Survive a night to unlock next challenge.",
            Image = "media/lua/client/Challenge/a_really_cdday.png",
            Video = "a_really_cdday.bik",
            GameMode = "A Really CD DA",
            World = ZomboidMaps.MuldraughKy,
            X = 10816,
            Y = 9413,
            Z = 0
        };

        var spawns = new LuaSequence<ZomboidChallengeSpawn>();
        foreach (var (x, y, z) in Spawns)
        {
            var spawn = new ZomboidChallengeSpawn(x, y, z);
            spawns.Add(spawn);
        }

        challenge.Spawns = spawns;
        challenge.HourOfDay = 7;

        LuaGlobals.AddChallenge(challenge);
        // addChallenge looks the name up as a translation key, which only exists for the built-in id.
        challenge.Name = "A Really CD DA (.NET)";
        challenge.Description = Globals.GetText("Challenge_AReallyCDDAy_desc");
    }

    private void OnInitWorld()
    {
        var sandbox = LuaGlobals.SandboxVars;
        sandbox.Zombies = SandboxValues.Zombies.Insane;
        sandbox.Distribution = SandboxValues.Distribution.UrbanFocused;
        sandbox.DayLength = SandboxValues.DayLength._1Hour;
        sandbox.StartMonth = SandboxValues.StartMonth.December;
        sandbox.StartTime = SandboxValues.StartTime._9AM;
        sandbox.WaterShutModifier = SandboxValues.WaterShutModifier.Instant;
        sandbox.ElecShutModifier = SandboxValues.ElecShutModifier.Instant;
        sandbox.LootItemRemovalList = "";
        sandbox.Temperature = SandboxValues.Temperature.Normal;
        sandbox.Rain = SandboxValues.Rain.Normal;
        sandbox.ErosionSpeed = SandboxValues.ErosionSpeed.VeryFast;
        sandbox.Farming = SandboxValues.Farming.Normal;
        sandbox.NatureAbundance = SandboxValues.NatureAbundance.VeryAbundant;
        sandbox.PlantResilience = SandboxValues.PlantResilience.Normal;
        sandbox.PlantAbundance = SandboxValues.PlantAbundance.Normal;
        sandbox.Alarm = SandboxValues.Alarm.Rare;
        sandbox.LockedHouses = SandboxValues.LockedHouses.Rare;
        sandbox.FoodRotSpeed = SandboxValues.FoodRotSpeed.Normal;
        sandbox.FridgeFactor = SandboxValues.FridgeFactor.Normal;
        sandbox.StatsDecrease = SandboxValues.StatsDecrease.Normal;
        sandbox.StarterKit = false;
        sandbox.TimeSinceApo = SandboxValues.TimeSinceApo._12Months;
        sandbox.MultiHitZombies = false;

        // The Lua original still sets B41's LootRespawn and XPMultiplierGlobal*, which B42 no longer reads.
        var multipliers = new SandboxVarsMultiplierConfig();
        multipliers.Global = 1;
        multipliers.GlobalToggle = true;
        sandbox.MultiplierConfig = multipliers;

        sandbox.ZombieConfig.PopulationMultiplier = ZombiePopulationMultiplier.Insane;

        _active = true;
        Log.Info("Challenge world initialised");
    }

    private void OnGameStart()
    {
        if (!_active)
            return;

        var player = Globals.GetPlayer();
        if (player.GetHoursSurvived() > 0)
            return;

        var building = player.CurrentSquare?.Room?.GetBuilding();
        if (building is null)
            return;

        var badRooms = new JavaList<string>();
        badRooms.Add("kitchen");
        badRooms.Add("garage");
        var tile = building.GetRandomRoomExcluding(badRooms).GetRandomSquare();
        IsoFireManager.Explode(Globals.GetCell(), tile, 100000);
        Log.Info("Lit the fire");
    }

    private void AddPlayer(dynamic player)
    {
        if (player.getHoursSurvived() > 0)
            return;

        var characterStat = Lua.GetGlobal("CharacterStat")!;
        var bodyPartType = Lua.GetGlobal("BodyPartType")!;
        var maxWetness = characterStat.WETNESS.getMaximumValue();

        player.getStats().set(characterStat.INTOXICATION, 100);

        player.getInventory().clear();
        player.clearWornItems();
        player.getStats().set(characterStat.WETNESS, maxWetness);
        var bodyDamage = player.getBodyDamage();
        var bodyParts = bodyDamage.getBodyParts();
        for (var i = 0; i < bodyParts.size(); i++)
            bodyParts.get(i).setWetness(maxWetness);
        bodyDamage.setCatchACold(0.0);
        bodyDamage.setHasACold(true);
        bodyDamage.setColdStrength(50.0);
        bodyDamage.setTimeToSneezeOrCough(0);
        player.setClothingItem_Feet(null);
        player.setClothingItem_Legs(null);
        player.setClothingItem_Torso(null);
        bodyDamage.getBodyPart(bodyPartType.Groin).generateDeepShardWound();
        Log.Info("Player set up for the challenge");
    }
}
