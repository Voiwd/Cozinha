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

    // Cabeças do Walter por expressão (0=normal 1=feliz 2=triste).
    static Dictionary<int, Bitmap?> WalterHeads { get; } = new();

    public static void Load()
    {
        Background = LoadAsset("background.png", keyWhite: false);
        Estante    = LoadAsset("estante.png",    keyWhite: true);
        Mesa       = LoadAsset("mesa.png",       keyWhite: true);
        Walter     = LoadAsset("walter.png",     keyWhite: false); // real alpha channel

        // Cabeças trocáveis conforme a reação do Walter
        WalterHeads[0] = LoadAsset("Walter Normal.png", keyWhite: false);
        WalterHeads[1] = LoadAsset("walter feliz.png",  keyWhite: false);
        WalterHeads[2] = LoadAsset("Walter Triste.png", keyWhite: false);
        Beaker     = LoadAsset("bequer.png",     keyWhite: true);
        Burner     = LoadAsset("bicobunsen.png", keyWhite: true);

        // Load ingredient compound images
        LoadIngredientAsset("NaOH.png");
        LoadIngredientAsset("Metilamina.png");
        LoadIngredientAsset("Fósforo vermelho.png");
        LoadIngredientAsset("Iodo.png");
        LoadIngredientAsset("Acido Sulfurico.png");
        LoadIngredientAsset("Hcl.png");
    }

    // Retorna a cabeça correspondente à expressão; cai para a normal se faltar.
    public static Bitmap? GetWalterHead(int expression)
    {
        if (WalterHeads.TryGetValue(expression, out var bmp) && bmp != null) return bmp;
        return WalterHeads.TryGetValue(0, out var normal) ? normal : null;
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
