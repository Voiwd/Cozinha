namespace Cozinha;

public class Ingredient
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public string Formula { get; init; } = "";
    public Color BottleColor { get; init; }
    public Color LiquidColor { get; init; }
    public Rectangle Bounds { get; set; }
    public bool IsHovered { get; set; }
}

public static class IngredientFactory
{
    public static List<Ingredient> CreateAll()
    {
        var defs = new (string id, string label, string formula, Color bottle, Color liquid)[]
        {
            ("NaOH",   "Hidróxido de Sódio", "NaOH",   Color.WhiteSmoke,              Color.FromArgb(220, 220, 200)),
            ("CH3NH2", "Metilamina",          "CH3NH2", Color.Gold,                    Color.FromArgb(200, 200, 50)),
            ("RedP",   "Fósforo Vermelho",    "P4",     Color.Firebrick,               Color.FromArgb(180, 40,  40)),
            ("I2",     "Iodo",                "I2",     Color.DarkViolet,              Color.FromArgb(120, 0,   150)),
            ("H2SO4",  "Ácido Sulfúrico",     "H2SO4",  Color.LightGray,               Color.FromArgb(200, 200, 200)),
            ("HCl",    "Ácido Clorídrico",    "HCl",    Color.LightGreen,              Color.FromArgb(180, 230, 180)),
        };

        const int startX = 40;
        const int spacing = 120;
        const int bottleY = 15;
        const int bottleW = 70;
        const int bottleH = 85;

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
                Bounds      = new Rectangle(startX + i * spacing, bottleY, bottleW, bottleH),
            });
        }
        return list;
    }
}
