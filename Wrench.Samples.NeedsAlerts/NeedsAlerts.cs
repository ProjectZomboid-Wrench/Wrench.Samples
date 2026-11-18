using Zomboid.Java;
using Zomboid.Java.Characters;
using Zomboid.Lua.Options;

namespace Wrench.Samples.NeedsAlerts;

/// <summary>
/// Floats a one-time reminder over the player when thirst, hunger or tiredness reaches its second moodle level.
/// Each need can be switched off in the game's Mod Options.
/// </summary>
public sealed class NeedsAlerts : ModInstance
{
    // Thresholds of the second moodle level ("Thirsty", "Hungry", "Tired"). Stats are looked up per check: Java
    // wrappers only live as long as the call they came from.
    private static readonly Need[] Needs =
    [
        new("thirst", () => CharacterStat.THIRST, 0.25f, NeedsOptions.Thirst, "IGUI_NeedsAlerts_Thirsty"),
        new("hunger", () => CharacterStat.HUNGER, 0.25f, NeedsOptions.Hunger, "IGUI_NeedsAlerts_Hungry"),
        new("fatigue", () => CharacterStat.FATIGUE, 0.7f, NeedsOptions.Fatigue, "IGUI_NeedsAlerts_Tired"),
    ];

    private readonly HashSet<(int Player, string Need)> _alerted = [];

    public override void OnLoad() => RegisterLuaEvent(LuaEvents.EveryTenMinutes, Check);

    private void Check()
    {
        for (var i = 0; i < Globals.GetNumActivePlayers(); i++)
        {
            var player = Globals.GetSpecificPlayer(i);
            if (player is null || player.IsDead())
                continue;
            foreach (var need in Needs)
            {
                var key = (i, need.Id);
                if (player.GetStats().Get(need.Stat()) < need.Threshold)
                    _alerted.Remove(key);
                else if (need.Enabled.Value && _alerted.Add(key))
                    HaloTextHelper.AddBadText(player, Globals.GetText(need.Text));
            }
        }
    }

    private sealed record Need(string Id, Func<CharacterStat> Stat, float Threshold, TickBox Enabled, string Text);
}

internal sealed class NeedsOptions() : ModOptions("UI_NeedsAlerts_Title")
{
    public static readonly TickBox Thirst = new("UI_NeedsAlerts_thirst", true);
    public static readonly TickBox Hunger = new("UI_NeedsAlerts_hunger", true);
    public static readonly TickBox Fatigue = new("UI_NeedsAlerts_fatigue", true);
}
