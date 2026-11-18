using Zomboid.Java;
using Zomboid.Java.Characters;
using Zomboid.Java.Iso;
using Zomboid.Java.Iso.Objects;
using Zomboid.Lua;
using Zomboid.Lua.ContextMenus;

namespace Wrench.Samples.RoomLights;

/// <summary>Adds "Turn on/off all lights in room" to the world context menu of any square inside a room.</summary>
public sealed class RoomLights : ModInstance
{
    public override void OnLoad()
    {
    }
}

internal sealed class RoomLightsMenu : WorldMenuFiller
{
    private const string TurnOn = "ContextMenu_RoomLights_TurnOn";
    private const string TurnOff = "ContextMenu_RoomLights_TurnOff";

    public override void Fill(WorldMenu menu)
    {
        if (menu.Objects.FirstOrDefault()?.GetSquare() is not { } square || Switches(square) is not { Count: > 0 } switches)
            return;
        if (switches.Any(s => !s.IsActivated()))
            menu.AddOption(Globals.GetText(TurnOn), click => Toggle(click.Player, click.Arg<IsoGridSquare>(0), on: true), square);
        if (switches.Any(s => s.IsActivated()))
            menu.AddOption(Globals.GetText(TurnOff), click => Toggle(click.Player, click.Arg<IsoGridSquare>(0), on: false), square);
    }

    private static List<IsoLightSwitch> Switches(IsoGridSquare square) =>
        square.Room is { } room && room.HasLightSwitches() ? room.GetLightSwitches().Where(s => s.CanSwitchLight()).ToList() : [];

    // Nearest switch first, walking onto each switch's own square: it's inside the room, so the wall is never in the way.
    private static void Toggle(IsoPlayer player, IsoGridSquare square, bool on)
    {
        var remaining = Switches(square).Where(s => s.IsActivated() != on).ToList();
        var at = player.CurrentSquare;
        while (remaining.Count > 0)
        {
            var next = remaining.MinBy(s => s.GetSquare().DistToProper(at))!;
            remaining.Remove(next);
            if (next.GetSquare() != at)
                ISTimedActionQueue.Add(new ISWalkToTimedAction(player, next.GetSquare()));
            ISTimedActionQueue.Add(new ISToggleLightAction(player, next));
            at = next.GetSquare();
        }
    }
}
