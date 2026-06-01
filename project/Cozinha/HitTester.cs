namespace Cozinha;

public static class HitTester
{
    public static readonly Dictionary<string, Rectangle> ButtonBounds = new()
    {
        ["HEAT"]  = new Rectangle(170, 390, 110, 50),
        ["MIX"]   = new Rectangle(310, 390, 110, 50),
        ["SERVE"] = new Rectangle(450, 390, 110, 50),
        ["RESET"] = new Rectangle(620, 400, 80,  35),
    };

    public static string? HitIngredient(Point p, List<Ingredient> ingredients)
    {
        foreach (var ing in ingredients)
            if (ing.Bounds.Contains(p)) return ing.Id;
        return null;
    }

    public static string? HitActionButton(Point p)
    {
        foreach (var kv in ButtonBounds)
            if (kv.Value.Contains(p)) return kv.Key;
        return null;
    }
}
