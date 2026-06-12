namespace Cozinha;

public static class HitTester
{
    public static readonly Dictionary<string, Rectangle> ButtonBounds = new()
    {
        ["HEAT"]  = new Rectangle(150, 472, 110, 48),
        ["MIX"]   = new Rectangle(295, 472, 110, 48),
        ["SERVE"] = new Rectangle(440, 472, 110, 48),
        ["RESET"] = new Rectangle(622, 477, 80,  38),
    };

    // "On" button (bico de Bunsen) — círculo desenhado em (170,452) r=20,
    // centralizado sob o bico junto com a barra de gás.
    public static readonly Rectangle OnButton = new(150, 432, 40, 40);

    // "Servir" — círculo em (400,555) r=24. Entrega o produto final.
    public static readonly Rectangle ServeButton = new(374, 529, 52, 52);

    // "Recomeçar" — retângulo em (700,540) 100x40.
    public static readonly Rectangle ResetButton = new(700, 540, 100, 40);

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
