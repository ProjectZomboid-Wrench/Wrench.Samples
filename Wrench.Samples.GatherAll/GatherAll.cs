using Zomboid.Java;
using Zomboid.Java.Characters;
using Zomboid.Java.Inventory;
using Zomboid.Lua;
using Zomboid.Lua.ContextMenus;

namespace Wrench.Samples.GatherAll;

/// <summary>
/// Adds "Gather all &lt;item&gt; (n)" to the inventory context menu: every item of the clicked type in the containers
/// the loot window shows is moved into the player's main inventory.
/// </summary>
public sealed class GatherAll : ModInstance
{
    public override void OnLoad()
    {
    }
}

internal sealed class GatherAllMenu : InventoryMenuFiller
{
    private const string Gather = "ContextMenu_GatherAll_Gather";

    public override void Fill(InventoryMenu menu)
    {
        if (menu.Items is not [var clicked, ..])
            return;
        var count = Nearby(menu.PlayerNumber, clicked.GetFullType()).Count;
        if (count == 0)
            return;
        menu.AddOption(Globals.GetText(Gather, clicked.GetDisplayName(), count),
            click => Take(click.Player, click.Arg<string>(0)), clicked.GetFullType());
    }

    private static void Take(IsoPlayer player, string fullType)
    {
        foreach (var item in Nearby(player.GetPlayerNum(), fullType))
            ISTimedActionQueue.Add(new ISInventoryTransferAction(player, item, item.Container, player.Inventory));
    }

    private static List<InventoryItem> Nearby(int player, string fullType)
    {
        if (LuaGlobals.GetPlayerLoot(player) is not { } loot)
            return [];
        return loot.Containers
            .SelectMany(container => container.Items)
            .Where(item => item.GetFullType() == fullType)
            .ToList();
    }
}
