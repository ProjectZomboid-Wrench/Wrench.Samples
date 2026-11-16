using Wrench;

namespace Wrench.Samples.ReallyCDDAy;

/// <summary>
/// PZ's "A Really CD DA" challenge (media/lua/client/Challenge/AReallyCDDAy.lua) ported to C#, registered under its
/// own id so it appears alongside the original.
/// </summary>
public sealed class ReallyCDDAy : ModInstance
{
    private const string ChallengeId = "AReallyCDDAyDotnet";

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
        var challenge = Lua.NewTable();
        challenge["id"] = ChallengeId;
        challenge["OnInitWorld"] = Lua.Function(OnInitWorld);
        challenge["AddPlayer"] = Lua.Function((double playerNum, GameObject player) => AddPlayer(player));
        challenge["RemovePlayer"] = Lua.Function((GameObject? player) => { });
        challenge["Render"] = Lua.Function(() => { });
        challenge["completionText"] = "Survive a night to unlock next challenge.";
        challenge["image"] = "media/lua/client/Challenge/a_really_cdday.png";
        challenge["video"] = "a_really_cdday.bik";
        challenge["gameMode"] = "A Really CD DA";
        challenge["world"] = "Muldraugh, KY";
        challenge["x"] = 10816;
        challenge["y"] = 9413;
        challenge["z"] = 0;
        challenge["spawns"] = new[]
        {
            Spawn(10788, 9985, 0), Spawn(10693, 9745, 0), Spawn(10770, 9688, 0), Spawn(10746, 9413, 1),
            Spawn(11791, 6855, 0), Spawn(11604, 6877, 1), Spawn(11496, 6700, 1), Spawn(11415, 6665, 0),
            Spawn(11218, 6791, 1), Spawn(11217, 6797, 1), Spawn(10639, 6800, 1), Spawn(10839, 12153, 1),
            Spawn(11673, 6810, 1),
        };
        challenge["hourOfDay"] = 7;

        Lua.Call("addChallenge", challenge);
        // addChallenge looks the name up as a translation key, which only exists for the built-in id.
        challenge["name"] = "A Really CD DA (.NET)";
        challenge["description"] = Lua.Call("getText", "Challenge_AReallyCDDAy_desc");
    }

    private static Dictionary<string, object?> Spawn(int x, int y, int z) => new() { ["x"] = x, ["y"] = y, ["z"] = z };

    private void OnInitWorld()
    {
        var sandbox = Lua.GetGlobal("SandboxVars")!;
        sandbox.Zombies = 1;
        sandbox.Distribution = 1;
        sandbox.DayLength = 3;
        sandbox.StartMonth = 12;
        sandbox.StartTime = 2;
        sandbox.WaterShutModifier = -1;
        sandbox.ElecShutModifier = -1;
        sandbox.LootItemRemovalList = "";
        sandbox.Temperature = 3;
        sandbox.Rain = 3;
        sandbox.ErosionSpeed = 1;
        sandbox.Farming = 3;
        sandbox.NatureAbundance = 5;
        sandbox.PlantResilience = 3;
        sandbox.PlantAbundance = 3;
        sandbox.Alarm = 3;
        sandbox.LockedHouses = 3;
        sandbox.FoodRotSpeed = 3;
        sandbox.FridgeFactor = 3;
        sandbox.LootRespawn = 1;
        sandbox.StatsDecrease = 3;
        sandbox.StarterKit = false;
        sandbox.TimeSinceApo = 13;
        sandbox.MultiHitZombies = false;

        sandbox.MultiplierConfig = new Dictionary<string, object?>
        {
            ["XPMultiplierGlobal"] = 1,
            ["XPMultiplierGlobalToggle"] = true,
        };

        sandbox.ZombieConfig.PopulationMultiplier = Lua.GetGlobal("ZombiePopulationMultiplier")!.Insane;

        _active = true;
        Log.Info("Challenge world initialised");
    }

    private void OnGameStart()
    {
        if (!_active)
            return;

        var player = Lua.Call("getPlayer")!;
        if (player.getHoursSurvived() > 0)
            return;

        var square = player.getCurrentSquare();
        if (square is null)
            return;
        var room = square.getRoom();
        if (room is null)
            return;
        var building = room.getBuilding();
        if (building is null)
            return;

        var badRooms = Lua.GetGlobal("ArrayList")!.Dot.@new();
        badRooms.add("kitchen");
        badRooms.add("garage");
        var tile = building.getRandomRoomExcluding(badRooms).getRandomSquare();
        Lua.GetGlobal("IsoFireManager")!.Dot.explode(Lua.Call("getCell"), tile, 100000);
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
