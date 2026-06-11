using System.Drawing.Drawing2D;

namespace Cozinha;

public static class AssetManager
{
    public static Bitmap? Background   { get; private set; }
    public static Bitmap? Estante      { get; private set; }
    public static Bitmap? Mesa         { get; private set; }
    public static Bitmap? Walter       { get; private set; }
    public static Bitmap? Beaker       { get; private set; }
    public static Bitmap? Burner       { get; private set; }

    static Dictionary<string, Bitmap?> IngredientAssets { get; } = new();

    public static void Load()
    {
        // Os PNGs vêm em 1080x1080, mas são desenhados bem menores. Pré-escalar
        // no load deixa o DrawImage de cada frame ser um blit direto em vez de
        // reamostrar a bitmap gigante a cada movimento de mouse.
        Background = LoadAsset("background.png", keyWhite: false);               // já 800x600
        Estante    = LoadAsset("estante.png",    keyWhite: true);                // pequena
        Mesa       = LoadAsset("mesa.png",       keyWhite: true);                // já 800x600
        Walter     = LoadAsset("walter.png",     keyWhite: false, 360, 360);     // real alpha channel
        Beaker     = LoadAsset("bequer.png",     keyWhite: true,  180, 140);
        Burner     = LoadAsset("bicobunsen.png", keyWhite: true,  100, 120);

        // Load ingredient compound images
        LoadIngredientAsset("NaOH.png");
        LoadIngredientAsset("Metilamina.png");
        LoadIngredientAsset("Fósforo vermelho.png");
        LoadIngredientAsset("Iodo.png");
        LoadIngredientAsset("Acido Sulfurico.png");
        LoadIngredientAsset("Hcl.png");
    }

    public static Bitmap? GetIngredientAsset(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return null;
        if (IngredientAssets.TryGetValue(assetName, out var bmp)) return bmp;
        return null;
    }

    static void LoadIngredientAsset(string name)
    {
        // Frascos são desenhados a 100x100 (ver Ingredient.Bounds).
        IngredientAssets[name] = LoadAsset(name, keyWhite: true, 100, 100);
    }

    static Bitmap? LoadAsset(string name, bool keyWhite, int targetW = 0, int targetH = 0)
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", name);
        if (!File.Exists(path)) return null;
        var bmp = new Bitmap(path);
        if (keyWhite) bmp.MakeTransparent(Color.White);
        if (targetW > 0 && targetH > 0 && (bmp.Width != targetW || bmp.Height != targetH))
            bmp = ScaleTo(bmp, targetW, targetH);
        return bmp;
    }

    // Reamostra uma vez, com qualidade alta, num bitmap ARGB do tamanho final.
    static Bitmap ScaleTo(Bitmap src, int w, int h)
    {
        var dst = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(dst))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode   = PixelOffsetMode.HighQuality;
            g.DrawImage(src, new Rectangle(0, 0, w, h));
        }
        src.Dispose();
        return dst;
    }
}
