namespace Cozinha;

public static class AssetManager
{
    public static Bitmap? Background { get; private set; }
    public static Bitmap? Estante    { get; private set; }
    public static Bitmap? Mesa       { get; private set; }
    public static Bitmap? Walter     { get; private set; }

    public static void Load()
    {
        Background = LoadAsset("background.png", makeTransparent: false);
        Estante    = LoadAsset("estante.png",    makeTransparent: true);
        Mesa       = LoadAsset("mesa.png",       makeTransparent: true);
        Walter     = LoadAsset("walter.png",     makeTransparent: true);
    }

    static Bitmap? LoadAsset(string name, bool makeTransparent)
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", name);
        if (!File.Exists(path)) return null;
        var bmp = new Bitmap(path);
        if (makeTransparent) bmp.MakeTransparent(Color.White);
        return bmp;
    }
}
