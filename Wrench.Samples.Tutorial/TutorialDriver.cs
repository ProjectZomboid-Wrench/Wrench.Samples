using Wrench;

namespace Wrench.Samples.Tutorial;

/// <summary>
/// Replaces the functions of PZ's tutorial driver (media/lua/client/Tutorial/Tutorial1.lua) with C# versions,
/// leaving its data and the Lua tutorial steps in place. PZ's own code keeps calling <c>Tutorial1.*</c> by name, so
/// patching the table is enough to redirect it.
/// </summary>
public sealed class TutorialDriver : ModInstance
{
    private static readonly (int X, int Y, int Z)[] BoardedWindows =
    [
        (162, 157, 0), (158, 160, 0), (160, 152, 0), (154, 151, 0), (151, 151, 0), (153, 162, 0), (149, 162, 0),
        (147, 159, 0), (148, 151, 0), (147, 154, 1), (149, 151, 1), (154, 151, 1), (131, 179, 0), (129, 183, 0),
    ];

    private static readonly string[] Steps =
    [
        "WelcomeStep", "WalkToAdjacent", "InventoryLootingStep", "InventoryUseStep",
        "FightStep", "SneakStep", "BandageStep", "ShotgunStep",
    ];

    private IDisposable? _ticking;

    public override void OnLoad()
    {
        PatchLua(Patch);
        RegisterLuaEvent(LuaEvents.OnMainMenuEnter, StopTicking);
    }

    private void Patch()
    {
        if (Lua.GetGlobal("Tutorial1") is null)
            return;

        Lua.Replace("Tutorial1.PreloadInit", PreloadInit);
        Lua.Replace("Tutorial1.AddPlayer", AddPlayer);
        Lua.Replace("Tutorial1.closeBothInv", CloseBothInv);
        Lua.Replace("Tutorial1.closeLoot", CloseLoot);
        Lua.Replace("Tutorial1.closeInv", CloseInv);
        Lua.Replace("Tutorial1.createInventoryContextMenu", CreateInventoryContextMenu);
        Lua.Replace("Tutorial1.createWorldContextMenuFromContext", CreateWorldContextMenuFromContext);
        Lua.Replace("Tutorial1.createWorldContextMenu", CreateWorldContextMenu);
        Lua.Replace("Tutorial1.doubleClickInventory", DoubleClickInventory);
        Lua.Replace("Tutorial1.Init", Init);
        Lua.Replace("Tutorial1.openDoor", OpenDoor);
        Lua.Replace("Tutorial1.collapseInv", CollapseInv);
        Lua.Replace("Tutorial1.lockDoor", LockDoor);
        Lua.Replace("Tutorial1.unlockDoor", UnlockDoor);
        Lua.Replace("Tutorial1.addBarricade", AddBarricade);
        Lua.Replace("Tutorial1.addMetalBarricade", AddMetalBarricade);
        Lua.Replace("Tutorial1.FillContainers", FillContainers);
        Lua.Replace("Tutorial1.SpawnZombies", (double count) => { });
        Lua.Replace("Tutorial1.Render", () => { });
        Lua.Replace("Tutorial1.Tick", Tick);
        Lua.Replace("Tutorial1.removeFences", RemoveFences);
        Lua.Replace("Tutorial1.replaceFence", ReplaceFence);
        Log.Info("Tutorial1 driver patched");
    }

    private dynamic Tutorial => Global("Tutorial1");

    private void PreloadInit()
    {
        var desc = Global("SurvivorFactory").Dot.CreateSurvivor(Global("SurvivorType").Neutral, true);
        var world = Lua.Call("getWorld")!;
        world.setLuaPlayerDesc(desc);
        world.getLuaTraits().clear();

        var sandbox = Global("SandboxVars");
        sandbox.Alarm = 1;
        sandbox.ZombieLore.ThumpNoChasing = true;
        sandbox.ZombieLore.DisableFakeDead = 3;
        sandbox.ZombieLore.Strength = 1;
        sandbox.ThumpOnConstruction = true;
        sandbox.FirearmJamMultiplier = 0;

        Lua.Call("getCore")!.setAutoZoom(0, false);
        Log.Info("PreloadInit");
    }

    private void AddPlayer(GameObject player)
    {
        if (Lua.Truthy(Lua.Call("getCore")!.isDedicated()))
            return;
        ((dynamic)player).setDir(Global("IsoDirections").SE);
    }

    private void CloseBothInv()
    {
        Tutorial.Dot.closeLoot();
        Tutorial.Dot.closeInv();
        Lua.Call("getPlayer")!.setBannedAttacking(false);
    }

    private void CloseLoot() => Collapse(Lua.Call("getPlayerLoot", 0)!);

    private void CloseInv() => Collapse(Lua.Call("getPlayerInventory", 0)!);

    private void Collapse(dynamic panel)
    {
        panel.blink = false;
        panel.inventoryPane.highlightItem = null;
        if (Lua.Truthy(Global("JoypadState").players[1]))
        {
            panel.setVisible(false);
            Lua.Call("setJoypadFocus", 0, null);
        }
        else
        {
            panel.collapse();
            panel.isCollapsed = true;
            panel.setMaxDrawHeight(Lua.Call("getPlayerInventory", 0)!.titleBarHeight());
        }
    }

    private object? CreateInventoryContextMenu(double player, object? isInPlayerInventory, GameObject items, double x, double y)
    {
        var tutorial = Tutorial;
        var paneMenu = Global("ISInventoryPaneContextMenu");
        var context = Global("ISContextMenu").Dot.get(player, x, y);
        context.blinkOption = paneMenu.blinkOption;

        dynamic? testItem = null;
        foreach (var v in items.Sequence())
        {
            if (!InstanceOf(v, "InventoryItem"))
                testItem = v.items[1];
        }
        if (testItem is null)
        {
            Log.Debug($"inventory menu: no test item among {items.Sequence().Count()} entries");
            return context;
        }

        if (Lua.Truthy(tutorial.contextMenuEat) && testItem.getType() == "DeadRat")
        {
            var eatOption = context.addOption(Text("ContextMenu_Eat"), new object[] { testItem });
            var subMenuEat = context.getNew(context);
            context.addSubMenu(eatOption, subMenuEat);
            subMenuEat.addOption(Text("ContextMenu_Eat_All"), new object[] { testItem }, paneMenu.onEatItems, 1, player);
            subMenuEat.addOption(Text("ContextMenu_Eat_Half"), new object[] { testItem }, paneMenu.onEatItems, 0.5, player);
            subMenuEat.addOption(Text("ContextMenu_Eat_Quarter"), new object[] { testItem }, paneMenu.onEatItems, 0.25, player);
        }
        if (Lua.Truthy(tutorial.contextMenuEquipPrimary) && InstanceOf(testItem, "HandWeapon"))
            context.addOption(Text("ContextMenu_Equip_Primary"), new object[] { testItem }, paneMenu.OnPrimaryWeapon, player);
        if (Lua.Truthy(tutorial.contextMenuEquip2Hand) && InstanceOf(testItem, "HandWeapon"))
            context.addOption(Text("ContextMenu_Equip_Two_Hands"), new object[] { testItem }, paneMenu.OnTwoHandsEquip, player);
        if (Lua.Truthy(tutorial.contextMenuEquipBag) && InstanceOf(testItem, "InventoryContainer")
            && testItem.canBeEquipped() == Global("ItemBodyLocation").BACK
            && !Lua.Truthy(Lua.Call("getPlayer")!.isEquipped(testItem)))
            context.addOption(Text("ContextMenu_Equip_on_your_Back"), new object[] { testItem }, paneMenu.onWearItems, player);
        if (Lua.Truthy(tutorial.contextMenuWear) && InstanceOf(testItem, "Clothing"))
        {
            var character = Lua.Call("getSpecificPlayer", player)!;
            if (Lua.Truthy(character.isEquipped(testItem)))
                context.addOption(Text("ContextMenu_Unequip"), new object[] { testItem }, paneMenu.onUnEquip, player);
            else
                paneMenu.Dot.doWearClothingMenu(player, testItem, items, context);

            if (Lua.Truthy(testItem.getClothingItemExtraOption()))
                paneMenu.Dot.doClothingItemExtraMenu(context, testItem, character);
        }
        Log.Debug($"inventory menu: item={testItem.getType()} eat={tutorial.contextMenuEat} primary={tutorial.contextMenuEquipPrimary} " +
                  $"wear={tutorial.contextMenuWear} -> {context.numOptions} option(s)");
        return context;
    }

    private object? CreateWorldContextMenuFromContext(GameObject context, GameObject worldObjects)
    {
        var tutorial = Tutorial;
        var menu = (dynamic)context;
        var worldMenu = Global("ISWorldObjectContextMenu");
        var character = Lua.Call("getPlayer")!;
        menu.blinkOption = Global("ISInventoryPaneContextMenu").blinkOption;

        var items = new List<dynamic>();
        foreach (var v in worldObjects.Sequence())
        {
            var objects = v.getSquare().getObjects();
            for (var i = 0; i < objects.size(); i++)
                items.Add(objects.get(i));
        }

        var bottle = character.getInventory().FindAndReturn("EmptyJar");
        foreach (var item in items)
        {
            var v = item;
            if (bottle is not null)
            {
                if (Lua.Truthy(tutorial.contextMenuFillBottle) && Lua.Truthy(v.hasWater()))
                {
                    menu.addOption(Text("ContextMenu_Fill") + " " + bottle.getName(), worldObjects, worldMenu.onTakeWater,
                        v, null, bottle, character.getPlayerNum());
                    break;
                }
            }
            // Same precedence as the Lua: (fill-curtain flag and curtain) or (window with curtains).
            if ((Lua.Truthy(tutorial.contextMenuOpenCurtain) && InstanceOf(v, "IsoCurtain"))
                || (InstanceOf(v, "IsoWindow") && Lua.Truthy(v.HasCurtains())))
            {
                if (InstanceOf(v, "IsoWindow"))
                    v = v.HasCurtains();
                var text = Lua.Truthy(v.IsOpen()) ? Text("ContextMenu_Close_curtains") : Text("ContextMenu_Open_curtains");
                if (!Lua.Truthy(menu.getOptionFromName(text)))
                    menu.addOption(text, worldObjects, worldMenu.onOpenCloseCurtain, v, character.getPlayerNum());
                break;
            }
        }
        Log.Debug($"world menu: {items.Count} object(s), bottle={bottle is not null} fill={tutorial.contextMenuFillBottle} " +
                  $"curtain={tutorial.contextMenuOpenCurtain} -> {menu.numOptions} option(s)");
        return context;
    }

    private object? CreateWorldContextMenu(double player, GameObject worldObjects, double x, double y)
    {
        var context = Global("ISContextMenu").Dot.get(player, x, y);
        return Tutorial.Dot.createWorldContextMenuFromContext(context, worldObjects);
    }

    // True when the double-click was handled here; false lets ISInventoryPane handle it.
    private bool DoubleClickInventory(GameObject inventoryPane, double x, double y, object? mouseOverOption)
    {
        var tutorial = Tutorial;
        var pane = (dynamic)inventoryPane;
        var steps = tutorial.steps;
        if (steps is null || Lua.Truthy(steps.isEmpty()))
            return false;
        if (!Lua.Truthy(pane.parent.onCharacter))
            return false;
        var item = mouseOverOption is null ? null : pane.items[mouseOverOption];
        if (item is null)
            return false;
        if (x < pane.column2)
            return false;

        GameObject actualItems = Global("ISInventoryPane").Dot.getActualItems(new object[] { item });
        foreach (var actual in actualItems.Sequence())
        {
            if (InstanceOf(actual, "HandWeapon") || !Lua.Truthy(tutorial.contextMenuEquipPrimary))
                return true;
        }
        return false;
    }

    private void Init()
    {
        var tutorial = Tutorial;
        tutorial.steps = Global("LuaList").@new();

        var core = Lua.Call("getCore")!;
        var keys = new[] { "Forward", "Left", "Backward", "Right" }
            .Select(key => (object?)Lua.Call("getKeyName", core.getKey(key)))
            .ToArray();
        tutorial.moveKeys = Text("IGUI_Tutorial1_MovementKeys", keys);

        var climate = Lua.Call("getClimateManager")!;
        if (Lua.Truthy(climate.getWeatherPeriod().isRunning()))
            climate.stopWeatherAndThunder();
        var fog = climate.getClimateFloat(5);
        fog.setEnableOverride(true);
        fog.setOverride(0, 1);

        SetUpCharacter(Lua.Call("getPlayer")!);

        foreach (var (x, y, z) in BoardedWindows)
            tutorial.Dot.addBarricade(x, y, z);
        var bandageStep = Global("BandageStep");
        bandageStep.barricadedWindow1 = tutorial.Dot.addMetalBarricade(179, 147, 0);
        bandageStep.curtain = bandageStep.barricadedWindow1.HasCurtains();
        bandageStep.curtain.ToggleDoorSilent();
        tutorial.Dot.addMetalBarricade(179, 150, 0);
        tutorial.Dot.addMetalBarricade(177, 153, 0);
        tutorial.Dot.addBarricade(195, 194, 0);
        tutorial.Dot.addBarricade(197, 199, 0);

        tutorial.Dot.lockDoor(131, 181, 0);
        Global("SneakStep").fenceGate = tutorial.Dot.lockDoor(169, 145, 0);
        var fightStep = Global("FightStep");
        fightStep.lockedDoor = tutorial.Dot.lockDoor(163, 153, 0);
        fightStep.lockedDoor2 = tutorial.Dot.lockDoor(164, 153, 0);
        bandageStep.lockedDoor = tutorial.Dot.lockDoor(176, 153, 0);
        tutorial.Dot.lockDoor(193, 194, 0);
        tutorial.Dot.lockDoor(106, 128, 0);

        tutorial.Dot.removeFences();

        var directions = Global("IsoDirections");
        Lua.Call("addVehicleDebug", "Base.PickupBurnt", directions.E, -1, Lua.Call("getSquare", 164, 160, 0));
        Lua.Call("addVehicleDebug", "Base.CarNormalBurnt", directions.NE, -1, Lua.Call("getSquare", 168, 160, 0));
        Lua.Call("addVehicleDebug", "Base.TaxiBurnt", directions.N, -1, Lua.Call("getSquare", 169, 155, 0));

        tutorial.marker = Lua.Call("getWorldMarkers");

        var joypadState = Global("JoypadState");
        if (Lua.Truthy(joypadState.players[1]))
        {
            tutorial.joypad = true;
            foreach (var flag in new[] { "disableGrab", "disableInvInteraction", "disableYInventory", "disableControllerPrompt",
                         "disableMovement", "disableClimbOver", "disableSmashWindow", "disableReload" })
                joypadState[flag] = true;
            var wheel = Global("ISBackButtonWheel");
            foreach (var flag in new[] { "disablePlayerInfo", "disableCrafting", "disableTime", "disableMoveable",
                         "disableZoomOut", "disableZoomIn", "disableScavenge" })
                wheel[flag] = true;
        }

        Lua.Call("setZoomLevels", 1.75, 1.5, 1.25, 1.0);

        foreach (var step in Steps)
            tutorial.steps.add(Global(step).@new());
        tutorial.steps.get(0).begin();

        var survivalGuide = Global("SurvivalGuide");
        survivalGuide.blockSurvivalGuide = true;
        var player = Lua.Call("getPlayer")!;
        player.setCanShout(false);
        player.setAllowSprint(false);
        core.setCollideZombies(false);
        Global("ISReloadWeaponAction").disableReloading = true;
        if (survivalGuide.instance is not null)
            survivalGuide.instance.setVisible(false);

        // The Lua does Events.OnTick.Add(Tutorial1.Tick), but Events.*.Add silently ignores anything that isn't a Lua
        // closure, including .NET functions; Wrench's own subscription has no such restriction.
        StopTicking();
        _ticking = RegisterLuaEvent(LuaEvents.OnTick, () => Tutorial.Dot.Tick());
        Log.Info("Init");
    }

    private void SetUpCharacter(dynamic character)
    {
        var inventory = character.getInventory();
        var visual = character.getHumanVisual();
        character.setFemale(true);
        character.getDescriptor().setFemale(true);
        character.getDescriptor().setForename("Jane");
        character.getDescriptor().setSurname("Doe");

        character.clearWornItems();
        inventory.clear();
        visual.setSkinTextureIndex(2);

        var immutableColor = Global("ImmutableColor");
        visual.setHairModel("Longcurly");
        visual.setHairColor(immutableColor.Dot.@new(0.83, 0.67, 0.27, 1));
        character.resetModel();

        var shirt = inventory.AddItem("Base.Tshirt_DefaultTEXTURE_TINT");
        shirt.getVisual().setTint(immutableColor.Dot.@new(1, 1, 1, 1));
        Wear(character, shirt);
        var shorts = inventory.AddItem("Base.Shorts_ShortDenim");
        shorts.getVisual().setTextureChoice(2);
        Wear(character, shorts);
        Wear(character, inventory.AddItem("Base.Socks_Ankle"));
        Wear(character, inventory.AddItem("Base.Shoes_BlueTrainers"));

        character.setInvincible(true);
        character.setUnlimitedCarry(true);
        character.setUnlimitedEndurance(true);
    }

    private static void Wear(dynamic character, dynamic clothing)
    {
        clothing.synchWithVisual();
        character.setWornItem(clothing.getBodyLocation(), clothing);
    }

    private void OpenDoor(int x, int y, int z, object? north)
    {
        var facing = Global("GridSquareEdgeFacingDirection");
        var direction = Lua.Truthy(north) ? facing.NORTH_SOUTH : facing.EAST_WEST;
        var square = Lua.Call("getSquare", x, y, z);
        if (square is null || square.getDoor(direction) is null)
            return;
        var door = square.getDoor(direction);
        door.setLocked(false);
        door.setLockedByKey(false);
        door.ToggleDoorSilent();
    }

    private static void CollapseInv(GameObject inventory)
    {
        var inv = (dynamic)inventory;
        inv.collapse();
        inv.isCollapsed = true;
        inv.setMaxDrawHeight(inv.titleBarHeight());
    }

    private object? LockDoor(int x, int y, int z)
    {
        var square = Lua.Call("getSquare", x, y, z);
        if (square is null)
        {
            Log.Warn($"Tutorial1.lockDoor() square is null @ {x},{y},{z}");
            return null;
        }
        return SetDoorLocked(square, locked: true);
    }

    private object? UnlockDoor(int x, int y, int z) => SetDoorLocked(Lua.Call("getSquare", x, y, z)!, locked: false);

    private object? SetDoorLocked(dynamic square, bool locked)
    {
        var objects = square.getObjects();
        for (var i = 0; i < objects.size(); i++)
        {
            var obj = objects.get(i);
            if (InstanceOf(obj, "IsoDoor"))
            {
                obj.setLockedByKey(locked);
                obj.setLocked(locked);
                return obj;
            }
        }
        return null;
    }

    private void AddBarricade(int x, int y, int z)
    {
        var square = Lua.Call("getSquare", x, y, z);
        if (square is null)
            return;
        var objects = square.getObjects();
        for (var i = 0; i < objects.size(); i++)
        {
            var obj = objects.get(i);
            if (InstanceOf(obj, "IsoWindow"))
            {
                // 3 or 4 barricades make building interiors too dark (SPIF-1071).
                obj.addBarricadesDebug(2, false);
                break;
            }
            if (InstanceOf(obj, "IsoDoor"))
            {
                obj.addRandomBarricades();
                break;
            }
        }
    }

    private object? AddMetalBarricade(int x, int y, int z)
    {
        var square = Lua.Call("getSquare", x, y, z);
        if (square is null)
            return null;
        var objects = square.getObjects();
        for (var i = 0; i < objects.size(); i++)
        {
            var obj = objects.get(i);
            if (InstanceOf(obj, "IsoWindow"))
            {
                obj.addBarricadesDebug(Lua.Call("ZombRand", 2, 5), true);
                obj.setPermaLocked(true);
                return obj;
            }
        }
        return null;
    }

    private void FillContainers()
    {
        var tutorial = Tutorial;
        var cell = Lua.Call("getCell")!;
        GameObject positions = tutorial.cratePositions;
        foreach (var position in positions.Sequence())
        {
            string type = position[1];
            string container = position[2];
            var square = cell.getGridSquare(position[3], position[4], position[5]);
            if (square is null)
                continue;
            var objects = square.getObjects();
            for (var i = 0; i < objects.size(); i++)
            {
                var obj = objects.get(i);
                var c = obj.getContainer();
                if (c is null)
                    continue;
                if (c.getType() != container)
                    continue;
                c.emptyIt();
                if (type == "lootingStuff")
                {
                    tutorial.DeadMouseContainer = obj;
                    var rat = c.AddItem("Base.DeadRat");
                    rat.setAge(17);
                    tutorial.DeadMouse = rat;
                    c.AddItem("Base.EmptyJar");
                    c.AddItem("Base.Dung_Rat");
                }
                c.setExplored(true);
            }
        }
    }

    private void StopTicking()
    {
        _ticking?.Dispose();
        _ticking = null;
    }

    private void Tick()
    {
        if (Lua.Call("getPlayer") is null)
            return;
        var tutorial = Tutorial;
        var steps = tutorial.steps;
        if (steps is null)
            return;
        if (Lua.Truthy(steps.isEmpty()))
            return;
        if (Lua.Truthy(Global("JoypadState").players[1]))
            tutorial.joypad = true;

        var current = steps.get(0);
        if (!Lua.Truthy(current.isComplete()))
            return;
        current.finish();
        steps.removeAt(0);
        if (!Lua.Truthy(steps.isEmpty()))
        {
            var next = steps.get(0);
            Log.Debug($"step complete; beginning {next.Type ?? "next step"}");
            next.begin();
        }
    }

    private void RemoveFences()
    {
        var tutorial = Tutorial;
        for (var x = 178; x <= 187; x++)
            tutorial.Dot.replaceFence(Lua.Call("getSquare", x, 153, 0), true);
        for (var y = 143; y <= 152; y++)
            tutorial.Dot.replaceFence(Lua.Call("getSquare", 188, y, 0), false);
    }

    private void ReplaceFence(GameObject? square, bool north)
    {
        if (square is null)
            return;
        var sq = (dynamic)square;
        var wall = Global("IsoObjectType").wall;
        var objects = sq.getObjects();
        for (var i = 0; i < objects.size(); i++)
        {
            var obj = objects.get(i);
            if (obj.getType() != wall)
                continue;
            var sprite = obj.getSprite().getName();
            var fence = Global("IsoThumpable").Dot.@new(Lua.Call("getCell"), sq, sprite, north,
                Global("ISWoodenWall").@new(sprite, sprite, null));
            sq.AddTileObject(fence);
            sq.RemoveTileObject(obj);
            return;
        }
    }

    private dynamic Global(string name) => Lua.GetGlobal(name) ?? throw new LuaException($"global '{name}' is nil");

    private bool InstanceOf(object? value, string type) => Lua.Truthy(Lua.Call("instanceof", value, type));

    private string Text(string key, params object?[] args) => (string)Lua.Call("getText", [key, .. args])!;
}
