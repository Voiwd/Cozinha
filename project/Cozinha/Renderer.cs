using System.Drawing.Drawing2D;

namespace Cozinha;

// All coordinates assume 800x600 client area.
public static class Renderer
{
    // Zone rectangles
    static readonly Rectangle ShelfZone    = new(0,   0,   800, 115);
    static readonly Rectangle StepZone     = new(0,   115, 160, 270);
    static readonly Rectangle CharZone     = new(160, 115, 460, 270);
    static readonly Rectangle InfoZone     = new(620, 115, 180, 270);
    static readonly Rectangle ActionZone   = new(0,   385, 800, 215);

    public static void DrawAll(Graphics g, GameState state, List<Ingredient> ingredients, Point mouse)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        DrawShelf(g, ingredients, state, mouse);
        DrawStepPanel(g, state);
        DrawCharZone(g, state);
        DrawInfoPanel(g, state);
        DrawActionBar(g, state, mouse);

        if (state.Phase == GamePhase.WrongOrder)
            DrawErrorFlash(g, state.LastFeedbackMessage);
        else if (state.Phase == GamePhase.Success)
            DrawSuccessOverlay(g);
    }

    // ── Shelf ────────────────────────────────────────────────────────────────

    static void DrawShelf(Graphics g, List<Ingredient> ingredients, GameState state, Point mouse)
    {
        // Background
        using var bg = new SolidBrush(Color.FromArgb(55, 35, 15));
        g.FillRectangle(bg, ShelfZone);

        // Shelf plank
        using var plank = new SolidBrush(Color.FromArgb(90, 60, 25));
        g.FillRectangle(plank, new Rectangle(0, 100, 800, 15));

        // Next-step ingredient id
        string? nextIngId = null;
        if (state.Phase == GamePhase.Playing && state.CurrentStep < GameState.Recipe.Length)
        {
            var step = GameState.Recipe[state.CurrentStep];
            if (step.Type == StepType.AddIngredient) nextIngId = step.Id;
        }

        foreach (var ing in ingredients)
        {
            bool hovered = ing.Bounds.Contains(mouse);
            bool isNext  = ing.Id == nextIngId;
            DrawBottle(g, ing, isNext, hovered);
        }
    }

    static void DrawBottle(Graphics g, Ingredient ing, bool isNext, bool hovered)
    {
        var r = ing.Bounds;

        // PLACEHOLDER: colored rectangle representing a bottle
        // TODO: replace with sprite asset
        Color fill = hovered ? Lighten(ing.BottleColor, 30) : ing.BottleColor;
        using var body = new SolidBrush(fill);
        g.FillRectangle(body, r);

        // Liquid fill (lower 40%)
        int liqH = (int)(r.Height * 0.4f);
        using var liq = new SolidBrush(ing.LiquidColor);
        g.FillRectangle(liq, new Rectangle(r.X + 3, r.Bottom - liqH - 3, r.Width - 6, liqH));

        // Border
        using var border = new Pen(isNext ? Color.Gold : Color.FromArgb(80, 80, 80), isNext ? 2.5f : 1f);
        g.DrawRectangle(border, r);

        // Formula label
        using var font = new Font("Consolas", 7f, FontStyle.Bold);
        using var txt  = new SolidBrush(Color.Black);
        var sz = g.MeasureString(ing.Formula, font);
        g.DrawString(ing.Formula, font, txt,
            r.X + (r.Width - sz.Width) / 2f,
            r.Y + 5);

        // "PLACEHOLDER" tag in small gray
        using var tagFont = new Font("Arial", 5.5f);
        using var tagBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
        g.DrawString("[asset]", tagFont, tagBrush, r.X + 2, r.Bottom - 13);
    }

    // ── Step panel ──────────────────────────────────────────────────────────

    static void DrawStepPanel(Graphics g, GameState state)
    {
        using var bg = new SolidBrush(Color.FromArgb(20, 20, 35));
        g.FillRectangle(bg, StepZone);

        using var titleFont = new Font("Arial", 9f, FontStyle.Bold);
        using var white = new SolidBrush(Color.White);
        g.DrawString("RECEITA", titleFont, white, StepZone.X + 10, StepZone.Y + 8);

        using var stepFont  = new Font("Arial", 8f);
        using var doneColor = new SolidBrush(Color.FromArgb(100, 200, 100));
        using var dimColor  = new SolidBrush(Color.FromArgb(100, 100, 100));
        using var goldBar   = new SolidBrush(Color.Gold);

        int y = StepZone.Y + 28;
        for (int i = 0; i < GameState.Recipe.Length; i++)
        {
            var step = GameState.Recipe[i];
            string prefix = i < state.CurrentStep ? "✓" : $"{i + 1}.";
            string label  = step.DisplayName.Length > 14 ? step.DisplayName[..14] : step.DisplayName;
            string line   = $"{prefix} {label}";

            if (i == state.CurrentStep && state.Phase == GamePhase.Playing)
            {
                g.FillRectangle(goldBar, new Rectangle(StepZone.X, y, 3, 18));
                g.DrawString(line, stepFont, white, StepZone.X + 8, y);
            }
            else if (i < state.CurrentStep)
            {
                g.DrawString(line, stepFont, doneColor, StepZone.X + 8, y);
            }
            else
            {
                g.DrawString(line, stepFont, dimColor, StepZone.X + 8, y);
            }

            y += 33;
        }
    }

    // ── Character zone ───────────────────────────────────────────────────────

    static void DrawCharZone(Graphics g, GameState state)
    {
        // Background (dark lab)
        using var bg = new SolidBrush(Color.FromArgb(30, 30, 50));
        g.FillRectangle(bg, CharZone);

        DrawWalter(g, state.WalterExpression);
        DrawBeaker(g, state);
        DrawBurner(g, state.IsHeated);

        // Table surface
        using var table = new SolidBrush(Color.FromArgb(100, 70, 40));
        g.FillRectangle(table, new Rectangle(CharZone.X, CharZone.Bottom - 30, CharZone.Width, 30));
        using var edge = new Pen(Color.FromArgb(70, 45, 20), 2);
        g.DrawLine(edge, CharZone.X, CharZone.Bottom - 30, CharZone.Right, CharZone.Bottom - 30);
    }

    static void DrawWalter(Graphics g, int expression)
    {
        // PLACEHOLDER: Walter White represented as labeled rectangles
        // TODO: replace with sprite asset

        int cx = 390; // center x within char zone

        // Body (lab coat)
        using var coat = new SolidBrush(Color.WhiteSmoke);
        g.FillRectangle(coat, new Rectangle(cx - 35, 210, 70, 95));
        using var coatBorder = new Pen(Color.LightGray, 1);
        g.DrawRectangle(coatBorder, new Rectangle(cx - 35, 210, 70, 95));

        // Head
        Color skinColor = Color.FromArgb(220, 180, 140);
        using var skin = new SolidBrush(skinColor);
        g.FillEllipse(skin, cx - 28, 145, 56, 62);
        using var headBorder = new Pen(Color.FromArgb(180, 140, 100), 1.5f);
        g.DrawEllipse(headBorder, cx - 28, 145, 56, 62);

        // Glasses (two small rects — placeholder for sprite)
        using var glassesPen = new Pen(Color.Black, 1.5f);
        g.DrawRectangle(glassesPen, cx - 24, 166, 18, 11);
        g.DrawRectangle(glassesPen, cx + 4,  166, 18, 11);
        g.DrawLine(glassesPen, cx - 6, 171, cx + 4, 171);

        // Goatee
        using var goatee = new SolidBrush(Color.FromArgb(60, 40, 30));
        g.FillEllipse(goatee, cx - 8, 192, 16, 10);

        // Mouth by expression
        using var mouth = new Pen(Color.FromArgb(120, 60, 60), 2);
        switch (expression)
        {
            case 1: // feliz
                g.DrawArc(mouth, cx - 12, 182, 24, 14, 0, 180);
                break;
            case 2: // irritado
            {
                g.DrawArc(mouth, cx - 12, 186, 24, 14, 180, 180);
                using var brow = new Pen(Color.FromArgb(60, 40, 30), 2);
                g.DrawLine(brow, cx - 22, 162, cx - 8,  166);
                g.DrawLine(brow, cx + 8,  166, cx + 22, 162);
                break;
            }
            default: // neutro
                g.DrawLine(mouth, cx - 10, 186, cx + 10, 186);
                break;
        }

        // [asset] tag
        using var tagFont  = new Font("Arial", 6f);
        using var tagBrush = new SolidBrush(Color.FromArgb(140, Color.Yellow));
        g.DrawString("[walter — asset]", tagFont, tagBrush, cx - 40, 310);
    }

    static void DrawBeaker(Graphics g, GameState state)
    {
        int bx = 510, by = 215, bw = 50, bh = 70;

        // Heat glow
        if (state.IsHeated)
        {
            using var glow = new SolidBrush(Color.FromArgb(60, 255, 120, 0));
            g.FillEllipse(glow, bx - 8, by + bh - 10, bw + 16, 20);
        }

        // Beaker body — trapezoid placeholder
        Point[] trapezoid =
        {
            new(bx,      by),
            new(bx + bw, by),
            new(bx + bw - 5, by + bh),
            new(bx + 5,  by + bh),
        };
        using var glass = new SolidBrush(Color.FromArgb(160, 200, 220, 240));
        g.FillPolygon(glass, trapezoid);

        // Liquid inside
        int liquidSteps = state.BeakerContents.Count;
        if (liquidSteps > 0)
        {
            float fraction = Math.Min(liquidSteps / 4f, 1f);
            int liqH = (int)(bh * fraction);
            Color liqColor = liquidSteps >= 4
                ? Color.FromArgb(180, 100, 200, 100)
                : Color.FromArgb(160, 100, 160, 220);
            using var liq = new SolidBrush(liqColor);
            int liqY = by + bh - liqH - 3;
            g.FillRectangle(liq, new Rectangle(bx + 5, liqY, bw - 10, liqH));
        }

        // Beaker outline
        using var outline = new Pen(Color.FromArgb(100, 150, 180), 1.5f);
        g.DrawPolygon(outline, trapezoid);

        // Lip
        g.DrawRectangle(outline, new Rectangle(bx - 3, by - 5, bw + 6, 6));

        // [asset] label
        using var tagFont  = new Font("Arial", 6f);
        using var tagBrush = new SolidBrush(Color.FromArgb(140, Color.Cyan));
        g.DrawString("[beaker]", tagFont, tagBrush, bx - 2, by + bh + 4);
    }

    static void DrawBurner(Graphics g, bool lit)
    {
        int bx = 500, by = 290;

        // Stand — placeholder rectangle
        using var stand = new SolidBrush(Color.FromArgb(80, 80, 90));
        g.FillRectangle(stand, new Rectangle(bx, by, 70, 12));
        g.FillRectangle(stand, new Rectangle(bx + 28, by + 12, 14, 25));

        if (lit)
        {
            // Flame — simple colored ellipses
            using var flameOuter = new SolidBrush(Color.FromArgb(200, 255, 140, 0));
            using var flameInner = new SolidBrush(Color.FromArgb(220, 255, 220, 0));
            g.FillEllipse(flameOuter, bx + 27, by - 18, 16, 22);
            g.FillEllipse(flameInner, bx + 30, by - 12, 10, 14);
        }

        using var tagFont  = new Font("Arial", 6f);
        using var tagBrush = new SolidBrush(Color.FromArgb(140, Color.Orange));
        g.DrawString("[burner]", tagFont, tagBrush, bx + 4, by + 14);
    }

    // ── Info panel ───────────────────────────────────────────────────────────

    static void DrawInfoPanel(Graphics g, GameState state)
    {
        using var bg = new SolidBrush(Color.FromArgb(15, 25, 15));
        g.FillRectangle(bg, InfoZone);

        using var titleFont   = new Font("Arial", 9f, FontStyle.Bold);
        using var formulaFont = new Font("Consolas", 13f, FontStyle.Bold);
        using var factFont    = new Font("Arial", 8f);
        using var white  = new SolidBrush(Color.White);
        using var green  = new SolidBrush(Color.LimeGreen);
        using var dimGray = new SolidBrush(Color.FromArgb(160, 160, 160));

        int x = InfoZone.X + 8;
        int y = InfoZone.Y + 8;

        g.DrawString("COMPOSTO", titleFont, white, x, y);
        y += 20;

        if (state.CurrentStep < GameState.Recipe.Length)
        {
            var step = GameState.Recipe[state.CurrentStep];

            // Name
            using var nameFont = new Font("Arial", 8f, FontStyle.Bold);
            DrawWrapped(g, step.DisplayName, nameFont, white, x, y, InfoZone.Width - 12);
            y += 30;

            // Formula
            g.DrawString(step.ChemFormula, formulaFont, green, x, y);
            y += 30;

            // Fact
            DrawWrapped(g, step.EducationalFact, factFont, dimGray, x, y, InfoZone.Width - 12);
        }
        else
        {
            g.DrawString("Receita\nconcluída!", titleFont, green, x, y + 10);
        }
    }

    // ── Action bar ───────────────────────────────────────────────────────────

    static void DrawActionBar(Graphics g, GameState state, Point mouse)
    {
        using var bg = new SolidBrush(Color.FromArgb(35, 35, 35));
        g.FillRectangle(bg, ActionZone);

        foreach (var kv in HitTester.ButtonBounds)
        {
            bool hovered  = kv.Value.Contains(mouse);
            bool enabled  = IsButtonEnabled(kv.Key, state);
            DrawButton(g, kv.Key, kv.Value, enabled, hovered);
        }

        // Ingredient name hint at bottom
        using var hintFont  = new Font("Arial", 8f, FontStyle.Italic);
        using var hintBrush = new SolidBrush(Color.FromArgb(160, 160, 160));
        g.DrawString("Clique nos compostos na ordem certa para seguir a receita.", hintFont, hintBrush, 10, 555);
    }

    static bool IsButtonEnabled(string id, GameState state)
    {
        if (id == "RESET") return true;
        if (state.Phase != GamePhase.Playing) return false;
        if (state.CurrentStep >= GameState.Recipe.Length) return false;
        var step = GameState.Recipe[state.CurrentStep];
        return step.Type == StepType.PerformAction && step.Id == id;
    }

    static void DrawButton(Graphics g, string label, Rectangle r, bool enabled, bool hovered)
    {
        Color baseColor = label switch
        {
            "HEAT"  => Color.OrangeRed,
            "MIX"   => Color.SteelBlue,
            "SERVE" => Color.MediumSeaGreen,
            _       => Color.DimGray,
        };

        Color fill = enabled
            ? (hovered ? Lighten(baseColor, 40) : baseColor)
            : Color.FromArgb(70, 70, 70);

        using var brush = new SolidBrush(fill);
        g.FillRectangle(brush, r);

        using var pen = new Pen(enabled ? Color.White : Color.FromArgb(100, 100, 100), 1.5f);
        g.DrawRectangle(pen, r);

        using var font  = new Font("Arial", 9f, FontStyle.Bold);
        using var white = new SolidBrush(enabled ? Color.White : Color.FromArgb(120, 120, 120));
        var sz = g.MeasureString(label, font);
        g.DrawString(label, font, white,
            r.X + (r.Width  - sz.Width)  / 2f,
            r.Y + (r.Height - sz.Height) / 2f);
    }

    // ── Overlays ─────────────────────────────────────────────────────────────

    static void DrawErrorFlash(Graphics g, string message)
    {
        using var overlay = new SolidBrush(Color.FromArgb(80, 200, 0, 0));
        g.FillRectangle(overlay, new Rectangle(0, 0, 800, 600));

        using var font   = new Font("Arial", 14f, FontStyle.Bold);
        using var shadow = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
        using var white  = new SolidBrush(Color.White);

        var sz = g.MeasureString(message, font);
        float tx = (800 - sz.Width) / 2f;
        float ty = 270;
        g.DrawString(message, font, shadow, tx + 2, ty + 2);
        g.DrawString(message, font, white, tx, ty);
    }

    static void DrawSuccessOverlay(Graphics g)
    {
        using var overlay = new SolidBrush(Color.FromArgb(200, 0, 50, 0));
        g.FillRectangle(overlay, new Rectangle(0, 0, 800, 600));

        // Border
        using var borderPen = new Pen(Color.Gold, 3);
        g.DrawRectangle(borderPen, new Rectangle(60, 120, 680, 300));

        using var titleFont = new Font("Arial", 26f, FontStyle.Bold);
        using var subFont   = new Font("Arial", 16f);
        using var hintFont  = new Font("Arial", 10f, FontStyle.Italic);
        using var gold      = new SolidBrush(Color.Gold);
        using var white     = new SolidBrush(Color.White);
        using var gray      = new SolidBrush(Color.FromArgb(200, 200, 200));

        string title = "Heisenberg aprovaria!";
        var tsz = g.MeasureString(title, titleFont);
        g.DrawString(title, titleFont, gold, (800 - tsz.Width) / 2f, 175);

        string purity = "Pureza: 99.1%";
        var psz = g.MeasureString(purity, subFont);
        g.DrawString(purity, subFont, white, (800 - psz.Width) / 2f, 240);

        string hint = "Pressione R para jogar novamente";
        var hsz = g.MeasureString(hint, hintFont);
        g.DrawString(hint, hintFont, gray, (800 - hsz.Width) / 2f, 360);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static Color Lighten(Color c, int amount)
        => Color.FromArgb(c.A,
            Math.Min(255, c.R + amount),
            Math.Min(255, c.G + amount),
            Math.Min(255, c.B + amount));

    static void DrawWrapped(Graphics g, string text, Font font, Brush brush, int x, int y, int maxWidth)
    {
        var fmt = new StringFormat();
        g.DrawString(text, font, brush, new RectangleF(x, y, maxWidth, 200), fmt);
    }
}
