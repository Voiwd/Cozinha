namespace Cozinha;

public static class AssetManager
{
    public static Bitmap? Background { get; private set; }
    public static Bitmap? Estante    { get; private set; }
    public static Bitmap? Mesa       { get; private set; }
    public static Bitmap? Walter     { get; private set; }

    public static void Load()
    {
        Background = LoadAsset("background.png", keyWhite: false);
        Estante    = LoadAsset("estante.png",    keyWhite: true);
        Mesa       = LoadAsset("mesa.png",       keyWhite: true);
        Walter     = LoadAsset("walter.png",     keyWhite: false); // real alpha channel
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
