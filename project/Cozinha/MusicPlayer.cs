using System.Runtime.InteropServices;
using System.Text;

namespace Cozinha;

static class MusicPlayer
{
    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    static extern int mciSendString(string command, StringBuilder? returnStr, int returnLength, IntPtr callback);

    const string Alias = "bgmusic";
    static bool _loaded;

    public static void Play(string fileName)
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", fileName);
        if (!File.Exists(path)) return;

        mciSendString($"open \"{path}\" type mpegvideo alias {Alias}", null, 0, IntPtr.Zero);
        mciSendString($"play {Alias} repeat", null, 0, IntPtr.Zero);
        _loaded = true;
    }

    public static void Stop()
    {
        if (!_loaded) return;
        mciSendString($"stop {Alias}", null, 0, IntPtr.Zero);
        mciSendString($"close {Alias}", null, 0, IntPtr.Zero);
        _loaded = false;
    }
}
