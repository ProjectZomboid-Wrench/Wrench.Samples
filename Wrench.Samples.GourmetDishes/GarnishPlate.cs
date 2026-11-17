using Zomboid;
using Zomboid.Characters;
using Zomboid.Inventory.Types;
using Zomboid.TimedActions;

namespace Wrench.Samples.GourmetDishes;

/// <summary>Garnishes a plate of food: a short crafting animation, after which the dish cheers its eater up more.</summary>
internal sealed class GarnishPlate : TimedAction
{
    public const string Text = "ContextMenu_GourmetDishes_Garnish";
    private const float Happiness = 5;
    private const string GarnishedKey = "GourmetDishesGarnished";

    public GarnishPlate(IsoGameCharacter character, Food plate) : base(character)
    {
        MaxTime = 150;
        Set("plate", plate);
    }

    private Food Plate => Get<Food>("plate");

    public static bool IsGarnished(Food plate) => plate.GetModData()[GarnishedKey] is true;

    public override bool IsValid() => Character.Inventory.Contains(Plate) && !IsGarnished(Plate);

    public override void Start()
    {
        Plate.JobType = Globals.GetText(Text);
        Plate.JobDelta = 0;
        SetActionAnim("Craft");
    }

    public override void Update() => Plate.JobDelta = JobDelta;

    public override void Stop()
    {
        Plate.JobDelta = 0;
        base.Stop();
    }

    public override void Perform()
    {
        Plate.JobDelta = 0;
        base.Perform();
    }

    public override bool Complete()
    {
        var plate = Plate;
        plate.UnhappyChange -= Happiness;
        plate.Name = $"Garnished {plate.Name}";
        plate.GetModData()[GarnishedKey] = true;
        return true;
    }
}
