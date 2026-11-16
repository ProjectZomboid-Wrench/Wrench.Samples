using Wrench;
using Zomboid.Characters;
using Zomboid.Entity.Components.Crafting.Recipe;
using Zomboid.Inventory;
using Zomboid.Inventory.Types;

namespace Wrench.Samples.GourmetDishes;

/// <summary>
/// The OnCreate for the mod's craftRecipes (<c>OnCreate = GourmetDishes.OnCreate.MakePlateOfRice</c>): splits the
/// cooked rice's nutrition across the plates made, names them after it and gives back the emptied pot. The age comes
/// through the recipe's InheritFoodAge.
/// </summary>
public sealed class GourmetDishes : ModInstance
{
    private const string PlateType = "Base.Plate";
    private const float PlatingHappiness = 5;
    private static readonly string[] NameNoise = ["Fresh", "Stale", "Cooked", "(", ")"];

    public override void OnLoad() =>
        PatchLua(() => Lua.Define("GourmetDishes.OnCreate.MakePlateOfRice",
            (CraftRecipeData craftData, IsoGameCharacter character) => Serve(craftData, character, "Plate")));

    private void Serve(CraftRecipeData craftData, IsoGameCharacter? character, string container)
    {
        var meal = craftData.GetAllConsumedItems().FirstOrDefault(item => item.GetFullType() != PlateType)?.As<Food>();
        if (meal is null)
        {
            Log.Warn("No meal among the consumed items; leaving the plates as scripted");
            return;
        }

        var dishes = craftData.GetAllCreatedItems().Select(item => item.As<Food>()).ToList();
        var name = $"{container} of {MealName(meal.GetDisplayName())}";
        foreach (var dish in dishes)
        {
            dish.BaseHunger = meal.BaseHunger / dishes.Count;
            dish.HungChange = meal.BaseHunger / dishes.Count;
            dish.ThirstChange = meal.GetThirstChangeUnmodified() / dishes.Count;
            dish.BoredomChange = meal.GetBoredomChangeUnmodified() / dishes.Count;
            dish.UnhappyChange = meal.UnhappyChange - PlatingHappiness;
            dish.Carbohydrates = meal.Carbohydrates / dishes.Count;
            dish.Lipids = meal.Lipids / dishes.Count;
            dish.Proteins = meal.Proteins / dishes.Count;
            dish.Calories = meal.Calories / dishes.Count;
            dish.SetCooked(meal.IsCooked());
            dish.Name = name;
            dish.SetCustomName(true);
        }
        ReturnCookware(meal, character);
        Log.Debug($"Served {meal.GetFullType()} onto {dishes.Count} {container.ToLowerInvariant()}(s) as \"{name}\"");
    }

    // PZ only puts back a consumed food's ReplaceOnUse for partial uses; ItemCount inputs are taken whole, pot and all.
    private static void ReturnCookware(Food meal, IsoGameCharacter? character)
    {
        var cookware = meal.GetReplaceOnUseFullType();
        if (character is null || string.IsNullOrEmpty(cookware))
            return;
        var returned = character.Inventory.AddItem(cookware);
        if (returned is not null)
            returned.Condition = meal.Condition;
    }

    private static string MealName(string displayName)
    {
        foreach (var word in NameNoise)
            displayName = displayName.Replace(word, "");
        return string.Join(' ', displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
