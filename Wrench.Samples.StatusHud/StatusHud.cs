using Zomboid.Java;
using Zomboid.Java.Ui;
using Zomboid.Lua;
using Zomboid.Lua.Options;

namespace Wrench.Samples.StatusHud;

/// <summary>
/// A small panel in the top-left corner with the time, the outside temperature and the player's carried weight.
/// Toggled by a key that can be rebound in the game's Mod Options (default: H).
/// </summary>
public sealed class StatusHud : ModInstance
{
    private StatusPanel? _panel;

    public override void OnLoad()
    {
        RegisterLuaEvent(LuaEvents.OnGameStart, Show);
        RegisterLuaEvent(LuaEvents.OnMainMenuEnter, Hide);
        RegisterLuaEvent(LuaEvents.OnKeyPressed, args =>
        {
            if (_panel is { IsReleased: false } && args.Get<int>(0) == HudOptions.Toggle.Value)
                _panel.SetVisible(!_panel.IsVisible());
        });
    }

    private void Show()
    {
        Hide();
        _panel = new StatusPanel(0);
        _panel.Initialise();
        _panel.AddToUIManager();
    }

    // A Lua reload releases the panel along with the rest of the old Lua state; it's only removed by hand otherwise.
    private void Hide()
    {
        if (_panel is { IsReleased: false })
            _panel.RemoveFromUIManager();
        _panel?.Dispose();
        _panel = null;
    }
}

internal sealed class StatusPanel : ISPanel
{
    private const double Line = 18;

    public StatusPanel(int player) : base(20, 120, 190, 3 * Line + 10) => Set("playerNum", player);

    private int PlayerNumber => Get<int>("playerNum");

    public override void Render()
    {
        base.Render();
        if (Globals.GetSpecificPlayer(PlayerNumber) is not { } player)
            return;
        var time = Globals.GetGameTime();
        var weight = player.Inventory.GetCapacityWeight();
        Text(0, $"{time.GetHour():00}:{time.GetMinutes():00}");
        Text(1, $"{Globals.GetClimateManager().GetTemperature():0.0} °C");
        Text(2, $"{weight:0.0} / {player.MaxWeight:0} kg");
    }

    private void Text(int row, string text) => DrawText(text, 8, 5 + row * Line, 1, 1, 1, 1, UIFont.Small);
}

internal sealed class HudOptions() : ModOptions("UI_StatusHud_Title")
{
    // Keyboard.KEY_H
    public static readonly KeyBind Toggle = new("UI_StatusHud_Toggle", 35);
}
