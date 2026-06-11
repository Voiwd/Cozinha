namespace Cozinha;

// Turns mouse-shaking (while holding the beaker) into a 0-100 progress value.
// The on-screen "Misturando... X%" label is a debug stand-in — the real mixing
// step will hook into this later. Each horizontal flick-back counts as a shake;
// energy decays when you stop, so the label fades on its own.
public class ShakeMixer
{
    public float Progress { get; private set; }
    public bool Mixing { get; private set; }

    int _lastDir;
    float _energy;

    public bool Idle => !Mixing && _energy <= 0f;
    public int Percent => (int)Progress;

    public void Feed(int dx)
    {
        const int dead = 3; // ignore sub-pixel jitter
        int dir = dx > dead ? 1 : dx < -dead ? -1 : 0;
        if (dir == 0) return;
        if (_lastDir != 0 && dir != _lastDir)
            _energy = Math.Min(_energy + 1.4f, 5f); // reversal = one shake
        _lastDir = dir;
    }

    // call ~60fps
    public void Tick()
    {
        _energy = Math.Max(0f, _energy - 0.18f);
        Mixing = _energy > 1.2f;
        if (Mixing) Progress = Math.Min(100f, Progress + 1.1f);
    }

    public void Reset()
    {
        Progress = 0f;
        _energy = 0f;
        _lastDir = 0;
        Mixing = false;
    }
}
