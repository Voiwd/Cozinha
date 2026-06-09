namespace Cozinha;

public static class AssetManager
{
    public static Bitmap? Background { get; private set; }
    public static Bitmap? Estante    { get; private set; }
    public static Bitmap? Mesa       { get; private set; }
    public static Bitmap? Walter     { get; private set; }
    public static Bitmap? Beaker     { get; private set; }

    static Dictionary<string, Bitmap?> IngredientAssets { get; } = new();

    public static void Load()
    {
        Background = LoadAsset("background.png", keyWhite: false);
        Estante    = LoadAsset("estante.png",    keyWhite: true);
        Mesa       = LoadAsset("mesa.png",       keyWhite: true);
        Walter     = LoadAsset("walter.png",     keyWhite: false); // real alpha channel
        Beaker     = LoadAsset("bequer.png",     keyWhite: true);

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
        IngredientAssets[name] = LoadAsset(name, keyWhite: true); // Most compounds have white keying
    }

    static Bitmap? LoadAsset(string name, bool keyWhite)
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", name);
        if (!File.Exists(path)) return null;
        var bmp = new Bitmap(path);
        if (keyWhite) bmp.MakeTransparent(Color.White);
        return bmp;
    }
}
