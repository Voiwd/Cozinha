namespace Cozinha;

public static class AssetManager
{
    public static Bitmap? Background { get; private set; }
    public static Bitmap? Estante    { get; private set; }
    public static Bitmap? Mesa       { get; private set; }
    public static Bitmap? Walter     { get; private set; }

    public static void Load()
    {
        // estante.png, mesa.png and walter.png already carry their own alpha
        // channel, so we keep it as-is instead of keying out a color.
        Background = LoadAsset("background.png");
        Estante    = LoadAsset("estante.png");
        Mesa       = LoadAsset("mesa.png");
        Walter     = LoadAsset("walter.png");
    }

    static Bitmap? LoadAsset(string name)
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", name);
        return File.Exists(path) ? new Bitmap(path) : null;
    }
}
