using System.Drawing.Drawing2D;

namespace Cozinha;

public enum ParticleKind { Fire, Smoke, Bubble, Spark, ErrorPuff, Confetti }

// Lightweight particle. Life runs from MaxLife down to 0; most visuals fade
// or shrink as a function of Life/MaxLife.
public class Particle
{
    public float X, Y;
    public float VX, VY;
    public float Life;
    public float MaxLife;
    public float Size;
    public float Seed;        // per-particle phase for wobble
    public float Angle;       // rotation in radians (confetti)
    public float AngularV;    // rotation speed rad/s
    public Color Tint;        // used by confetti
    public ParticleKind Kind;

    public float Frac => MaxLife <= 0 ? 0 : Life / MaxLife; // 1 → 0 over lifetime
}

// Owns every live particle and the emitters. Driven by a single ~60fps tick in
// Form1. Pure simulation + GDI+ drawing; never touches game rules.
public class ParticleSystem
{
    readonly List<Particle> _ps = new();
    readonly Random _rng = new();
    float _t; // global clock for wobble

    public int Count => _ps.Count;

    public void Update(float dt)
    {
        _t += dt;
        for (int i = _ps.Count - 1; i >= 0; i--)
        {
            var p = _ps[i];
            p.Life -= dt;
            if (p.Life <= 0f) { _ps.RemoveAt(i); continue; }

            p.X += p.VX * dt;
            p.Y += p.VY * dt;

            switch (p.Kind)
            {
                case ParticleKind.Fire:
                    p.VY -= 60f * dt;            // accelerates upward as it gets hot
                    p.X += (float)Math.Sin((_t + p.Seed) * 9f) * 12f * dt;
                    break;
                case ParticleKind.Smoke:
                    p.VY -= 8f * dt;
                    p.X += (float)Math.Sin((_t + p.Seed) * 2.5f) * 10f * dt;
                    p.Size += 18f * dt;          // billows out
                    break;
                case ParticleKind.Bubble:
                    p.VY -= 28f * dt;            // buoyancy
                    p.X += (float)Math.Sin((_t + p.Seed) * 7f) * 22f * dt;
                    break;
                case ParticleKind.Spark:
                    p.VY += 260f * dt;           // gravity
                    p.VX *= 0.96f;
                    break;
                case ParticleKind.ErrorPuff:
                    p.VX *= 0.93f;
                    p.VY = p.VY * 0.93f + 40f * dt;
                    break;
                case ParticleKind.Confetti:
                    p.VY += 320f * dt;           // gravity
                    p.VX += (float)Math.Sin((_t + p.Seed) * 3.5f) * 18f * dt; // drift
                    p.VX *= 0.995f;
                    p.Angle += p.AngularV * dt;
                    break;
            }
        }
    }

    public void Draw(Graphics g)
    {
        var oldMode = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        foreach (var p in _ps)
        {
            switch (p.Kind)
            {
                case ParticleKind.Fire:   DrawFire(g, p);   break;
                case ParticleKind.Smoke:  DrawSmoke(g, p);  break;
                case ParticleKind.Bubble: DrawBubble(g, p); break;
                case ParticleKind.Spark:  DrawSpark(g, p);  break;
                case ParticleKind.ErrorPuff: DrawErrorPuff(g, p); break;
                case ParticleKind.Confetti:  DrawConfetti(g, p);  break;
            }
        }
        g.SmoothingMode = oldMode;
    }

    public void Clear() => _ps.Clear();

    // ── Emitters ─────────────────────────────────────────────────────────────

    public void EmitFire(float x, float y, int n)
    {
        for (int i = 0; i < n; i++)
        {
            _ps.Add(new Particle
            {
                Kind = ParticleKind.Fire,
                X = x + Rand(-9, 9),
                Y = y + Rand(-3, 3),
                VX = Rand(-14, 14),
                VY = Rand(-70, -40),
                Life = Rand(0.35f, 0.7f),
                MaxLife = 0.7f,
                Size = Rand(9, 16),
                Seed = Rand(0, 100),
            });
        }
        // occasional wisp of smoke off the top of the flame
        if (_rng.NextDouble() < 0.25)
            _ps.Add(new Particle
            {
                Kind = ParticleKind.Smoke,
                X = x + Rand(-5, 5), Y = y - 28,
                VX = Rand(-6, 6), VY = Rand(-26, -16),
                Life = Rand(0.8f, 1.4f), MaxLife = 1.4f,
                Size = Rand(7, 12), Seed = Rand(0, 100),
            });
    }

    public void EmitBubble(float x, float y, int n, Color tint)
    {
        for (int i = 0; i < n; i++)
            _ps.Add(new Particle
            {
                Kind = ParticleKind.Bubble,
                X = x + Rand(-28, 28), Y = y + Rand(-2, 4),
                VX = Rand(-22, 22), VY = Rand(-55, -28),
                Life = Rand(0.7f, 1.3f), MaxLife = 1.3f,
                Size = Rand(8, 18), Seed = Rand(0, 100),
                Tint = tint,
            });
    }

    public void EmitStepComplete(float cx, float cy)
    {
        // confetti burst upward from the beaker
        for (int i = 0; i < 55; i++)
        {
            double a = -Math.PI / 2 + ((_rng.NextDouble() - 0.5) * Math.PI * 1.4);
            float speed = Rand(180, 480);
            _ps.Add(new Particle
            {
                Kind = ParticleKind.Confetti,
                X = cx + Rand(-18, 18), Y = cy,
                VX = (float)Math.Cos(a) * speed,
                VY = (float)Math.Sin(a) * speed,
                Life = Rand(1.2f, 2.2f), MaxLife = 2.2f,
                Size = Rand(6, 12), Seed = Rand(0, 100),
                Angle = Rand(0, 6.28f),
                AngularV = Rand(-8f, 8f),
                Tint = ConfettiColor(),
            });
        }
    }

    public void EmitVictoryBorders(int screenW, int screenH)
    {
        // top edge — shoot downward
        for (int i = 0; i < 120; i++)
        {
            float x = Rand(0, screenW);
            _ps.Add(MakeConfetti(x, Rand(-10, 0),
                Rand(-60, 60), Rand(80, 320),
                Rand(1.5f, 3.0f)));
        }
        // left edge — shoot right
        for (int i = 0; i < 60; i++)
        {
            float y = Rand(0, screenH * 0.6f);
            _ps.Add(MakeConfetti(Rand(-10, 0), y,
                Rand(120, 340), Rand(-200, 50),
                Rand(1.5f, 3.0f)));
        }
        // right edge — shoot left
        for (int i = 0; i < 60; i++)
        {
            float y = Rand(0, screenH * 0.6f);
            _ps.Add(MakeConfetti(screenW + Rand(0, 10), y,
                Rand(-340, -120), Rand(-200, 50),
                Rand(1.5f, 3.0f)));
        }
        // bottom corners — shoot inward-up
        for (int i = 0; i < 40; i++)
        {
            _ps.Add(MakeConfetti(Rand(0, 80), screenH + Rand(0, 10),
                Rand(60, 260), Rand(-450, -200), Rand(1.5f, 3.0f)));
            _ps.Add(MakeConfetti(screenW - Rand(0, 80), screenH + Rand(0, 10),
                Rand(-260, -60), Rand(-450, -200), Rand(1.5f, 3.0f)));
        }
    }

    Particle MakeConfetti(float x, float y, float vx, float vy, float life) => new Particle
    {
        Kind = ParticleKind.Confetti,
        X = x, Y = y, VX = vx, VY = vy,
        Life = life, MaxLife = life,
        Size = Rand(7, 14), Seed = Rand(0, 100),
        Angle = Rand(0, 6.28f),
        AngularV = Rand(-10f, 10f),
        Tint = ConfettiColor(),
    };

    static readonly Color[] _palette =
    [
        Color.FromArgb(255, 255, 80,  80),   // vermelho
        Color.FromArgb(255, 255, 200, 40),   // amarelo
        Color.FromArgb(255, 60,  220, 120),  // verde
        Color.FromArgb(255, 80,  160, 255),  // azul
        Color.FromArgb(255, 220, 80,  255),  // roxo
        Color.FromArgb(255, 255, 140, 40),   // laranja
        Color.FromArgb(255, 255, 255, 255),  // branco
    ];

    Color ConfettiColor() => _palette[_rng.Next(_palette.Length)];

    public void EmitMix(float x, float y)
    {
        // a couple of sparks flinging up, plus a curl of smoke
        for (int i = 0; i < 2; i++)
            _ps.Add(new Particle
            {
                Kind = ParticleKind.Spark,
                X = x + Rand(-12, 12), Y = y,
                VX = Rand(-90, 90), VY = Rand(-150, -70),
                Life = Rand(0.35f, 0.6f), MaxLife = 0.6f,
                Size = Rand(2, 4), Seed = Rand(0, 100),
            });
        if (_rng.NextDouble() < 0.4)
            _ps.Add(new Particle
            {
                Kind = ParticleKind.Smoke,
                X = x + Rand(-10, 10), Y = y - 6,
                VX = Rand(-8, 8), VY = Rand(-30, -18),
                Life = Rand(0.7f, 1.2f), MaxLife = 1.2f,
                Size = Rand(6, 11), Seed = Rand(0, 100),
            });
    }

    public void EmitErrorBurst(float cx, float cy)
    {
        for (int i = 0; i < 40; i++)
        {
            double a = _rng.NextDouble() * Math.PI * 2;
            float speed = Rand(120, 340);
            _ps.Add(new Particle
            {
                Kind = ParticleKind.ErrorPuff,
                X = cx, Y = cy,
                VX = (float)Math.Cos(a) * speed,
                VY = (float)Math.Sin(a) * speed,
                Life = Rand(0.4f, 0.8f), MaxLife = 0.8f,
                Size = Rand(4, 10), Seed = Rand(0, 100),
            });
        }
    }

    // ── Drawing per kind ─────────────────────────────────────────────────────

    static void DrawFire(Graphics g, Particle p)
    {
        float f = p.Frac;                 // 1 (hot/young) → 0 (cooled)
        Color c = f > 0.6f ? Lerp(Color.FromArgb(255, 255, 230, 120), Color.FromArgb(255, 255, 170, 40), (1 - f) / 0.4f)
                : f > 0.3f ? Lerp(Color.FromArgb(255, 255, 170, 40), Color.FromArgb(255, 230, 70, 20), (0.6f - f) / 0.3f)
                : Lerp(Color.FromArgb(230, 230, 70, 20), Color.FromArgb(0, 120, 30, 20), (0.3f - f) / 0.3f);
        float s = p.Size * (0.5f + f * 0.7f);
        using var b = new SolidBrush(c);
        g.FillEllipse(b, p.X - s / 2, p.Y - s / 2, s, s);
    }

    static void DrawSmoke(Graphics g, Particle p)
    {
        int a = (int)(90 * p.Frac);
        using var b = new SolidBrush(Color.FromArgb(Math.Max(0, a), 110, 110, 110));
        g.FillEllipse(b, p.X - p.Size / 2, p.Y - p.Size / 2, p.Size, p.Size);
    }

    static void DrawBubble(Graphics g, Particle p)
    {
        int a = (int)(200 * p.Frac);
        var c = p.Tint == default ? Color.FromArgb(235, 245, 255) : p.Tint;
        using var pen = new Pen(Color.FromArgb(Math.Max(0, a), c.R, c.G, c.B), 2f);
        g.DrawEllipse(pen, p.X - p.Size / 2, p.Y - p.Size / 2, p.Size, p.Size);
        // inner glow tinted
        using var fill = new SolidBrush(Color.FromArgb(Math.Max(0, a / 4), c.R, c.G, c.B));
        g.FillEllipse(fill, p.X - p.Size / 2, p.Y - p.Size / 2, p.Size, p.Size);
        // specular highlight
        using var hi = new SolidBrush(Color.FromArgb(Math.Max(0, a / 2), 255, 255, 255));
        g.FillEllipse(hi, p.X - p.Size / 4, p.Y - p.Size / 3, p.Size / 3, p.Size / 3);
    }

    static void DrawSpark(Graphics g, Particle p)
    {
        int a = (int)(255 * p.Frac);
        using var b = new SolidBrush(Color.FromArgb(Math.Max(0, a), 255, 220, 120));
        g.FillEllipse(b, p.X - p.Size / 2, p.Y - p.Size / 2, p.Size, p.Size);
    }

    static void DrawErrorPuff(Graphics g, Particle p)
    {
        int a = (int)(220 * p.Frac);
        float s = p.Size * (0.6f + p.Frac * 0.6f);
        using var b = new SolidBrush(Color.FromArgb(Math.Max(0, a), 255, 60, 40));
        g.FillEllipse(b, p.X - s / 2, p.Y - s / 2, s, s);
    }

    static void DrawConfetti(Graphics g, Particle p)
    {
        int alpha = (int)(255 * Math.Min(1f, p.Frac * 3f)); // fade only near end of life
        if (alpha <= 0) return;
        var c = Color.FromArgb(Math.Max(0, alpha), p.Tint.R, p.Tint.G, p.Tint.B);

        float w = p.Size;
        float h = p.Size * 0.45f;

        var old = g.Transform;
        g.TranslateTransform(p.X, p.Y);
        g.RotateTransform(p.Angle * 57.2958f);  // rad → deg

        using var brush = new SolidBrush(c);
        g.FillRectangle(brush, -w / 2, -h / 2, w, h);

        g.Transform = old;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    float Rand(float lo, float hi) => lo + (float)_rng.NextDouble() * (hi - lo);

    static Color Lerp(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb(
            (int)(a.A + (b.A - a.A) * t),
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }
}
