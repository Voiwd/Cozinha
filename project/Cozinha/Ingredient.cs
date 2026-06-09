namespace Cozinha;

public class Ingredient
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public string Formula { get; init; } = "";
    public Color BottleColor { get; init; }
    public Color LiquidColor { get; init; }
    public string? AssetName { get; init; } // e.g., "NaOH.png"
    public Rectangle Bounds { get; set; }
    public bool IsHovered { get; set; }
}

public static class IngredientFactory
{
    public static List<Ingredient> CreateAll()
    {
        var defs = new (string id, string label, string formula, Color bottle, Color liquid, string assetName)[]
        {
            ("NaOH",   "Hidróxido de Sódio", "NaOH",   Color.WhiteSmoke,              Color.FromArgb(220, 220, 200), "NaOH.png"),
            ("CH3NH2", "Metilamina",          "CH3NH2", Color.Gold,                    Color.FromArgb(200, 200, 50),  "Metilamina.png"),
            ("RedP",   "Fósforo Vermelho",    "P4",     Color.Firebrick,               Color.FromArgb(180, 40,  40),  "Fósforo vermelho.png"),
            ("I2",     "Iodo",                "I2",     Color.DarkViolet,              Color.FromArgb(120, 0,   150), "Iodo.png"),
            ("H2SO4",  "Ácido Sulfúrico",     "H2SO4",  Color.LightGray,               Color.FromArgb(200, 200, 200), "Acido Sulfurico.png"),
            ("HCl",    "Ácido Clorídrico",    "HCl",    Color.LightGreen,              Color.FromArgb(180, 230, 180), "Hcl.png"),
        };

        // 3 bottles per shelf; shelf surfaces at y=80 and y=200 (adjusted to match renderer positions)
        const int bottleW = 80;
        const int bottleH = 80;
        int[] xs    = { 65, 295, 525 };
        int[] ys    = { 80 - bottleH, 80 - bottleH, 80 - bottleH,
                        200 - bottleH, 200 - bottleH, 200 - bottleH };
        int[] xsFull = { xs[0], xs[1], xs[2], xs[0], xs[1], xs[2] };

        var list = new List<Ingredient>();
        for (int i = 0; i < defs.Length; i++)
        {
            var d = defs[i];
            list.Add(new Ingredient
            {
                Id          = d.id,
                Label       = d.label,
                Formula     = d.formula,
                BottleColor = d.bottle,
                LiquidColor = d.liquid,
                AssetName   = d.assetName,
                Bounds      = new Rectangle(xsFull[i], ys[i], bottleW, bottleH),
            });
        }
        return list;
    }
}
