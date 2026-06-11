using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Cozinha;

// All coordinates assume 800x600 client area.
public static class Renderer
{
    // ── Scene constants ──────────────────────────────────────────────────────

    // Shelf top edges in game canvas
    const int Shelf1Top = 80;
    const int Shelf2Top = 200;
    // The shelf strip lives at roughly y=355 in the 600px estante.png
    static readonly Rectangle EstanteSrcRect = new(0, 375, 800, 50);

    // Walter: upper body visible on the right, lower half behind the mesa.
    // >>> AJUSTE A IMAGEM DA CABEÇA AQUI: posição (x, y) e tamanho (largura, altura). <<<
    public static readonly Rectangle WalterDest = new(600, 135, 160, 160);

    // >>> AJUSTE O CORPO DO WALTER AQUI: posição (x, y) e tamanho (largura, altura). <<<
    public static readonly Rectangle WalterBodyDest = new(540, 250, 280, 280);

    // >>> AJUSTE O CHAPÉU AQUI: posição (x, y) e tamanho (largura, altura). <<<
    public static readonly Rectangle HatDest = new(605, 110, 150, 90);

    // Table top surface y
    const int TableTop = 315;
    const int TableBottom = 460;

    // ── Game UI zones ────────────────────────────────────────────────────────

    static readonly Rectangle StepZone   = new(5,   195, 150, 120);
    static readonly Rectangle InfoZone   = new(610, 195, 185, 120);
    static readonly Rectangle ActionZone = new(0,   462, 800, 138);

    // Beaker and burner on table surface
    const int BeakerX = 230, BeakerY = 240, BeakerW = 55, BeakerH = 65;
    const int BurnerX = 215, BurnerY = 295;

    // ── Entry point ──────────────────────────────────────────────────────────

    public static void DrawAll(Graphics g, GameState state, List<Ingredient> ingredients, Point mouse)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        DrawScene(g, ingredients, state, mouse);
        // DrawBeaker(g, state);
        // DrawBurner(g, state.IsHeated);

        if (state.Phase == GamePhase.WrongOrder)
            DrawErrorFlash(g, state.LastFeedbackMessage);
        else if (state.Phase == GamePhase.Success)
            DrawSuccessOverlay(g);
    }

    // ── Scene ────────────────────────────────────────────────────────────────

    static void DrawScene(Graphics g, List<Ingredient> ingredients, GameState state, Point mouse)
    {
        // 1. Background
        if (AssetManager.Background != null)
            g.DrawImage(AssetManager.Background, 0, 0, 800, 600);
        else
            DrawFallbackBackground(g);

        // 2. Shelves (on the wall, behind Walter)
        DrawShelves(g);

        // 3. Ingredients sitting on shelves (behind Walter)
        DrawIngredients(g, ingredients, state, mouse);

        // 4. Walter (in front of wall and shelves, behind table)
        DrawWalter(g, state.WalterExpression);

        // 5. Table (covers Walter's lower half)
        DrawTable(g);

        // 6. Beaker and burner on table (on top of everything)
        DrawBurner(g);
        DrawBeaker(g);

        // 7. UI Buttons
        DrawButtons(g, state.Phase);
    }

    static void DrawFallbackBackground(Graphics g)
    {
        using var brush = new SolidBrush(Color.FromArgb(55, 20, 75));
        g.FillRectangle(brush, 0, 0, 800, 600);
    }

    static void DrawShelves(Graphics g)
    {
        // Draw the shelf strip (bottom slice of estante.png) at two positions.
        if (AssetManager.Estante != null)
        {
            var dst1 = new Rectangle(0, Shelf1Top, 800, EstanteSrcRect.Height);
            var dst2 = new Rectangle(0, Shelf2Top, 800, EstanteSrcRect.Height);
            g.DrawImage(AssetManager.Estante, dst1, EstanteSrcRect, GraphicsUnit.Pixel);
            g.DrawImage(AssetManager.Estante, dst2, EstanteSrcRect, GraphicsUnit.Pixel);
        }
        else
        {
            DrawFallbackShelf(g, Shelf1Top);
            DrawFallbackShelf(g, Shelf2Top);
        }
    }

    static void DrawFallbackShelf(Graphics g, int y)
    {
        using var brush = new SolidBrush(Color.FromArgb(180, 140, 50));
        g.FillRoundedRectangle(brush, new Rectangle(15, y, 770, 22), 6);
        using var pen = new Pen(Color.FromArgb(120, 85, 25), 2);
        g.DrawRoundedRectangle(pen, new Rectangle(15, y, 770, 22), 6);
    }

    static void DrawWalter(Graphics g, int expression)
    {
        // Corpo primeiro (atrás), depois a cabeça por cima.
        if (AssetManager.WalterBody != null)
            g.DrawImage(AssetManager.WalterBody, WalterBodyDest);

        // Troca a "cabeça"/expressão conforme o estado; volta para walter.png se faltar.
        var head = AssetManager.GetWalterHead(expression) ?? AssetManager.Walter;
        if (head != null)
        {
            g.DrawImage(head, WalterDest);
            // Chapéu por cima da cabeça.
            if (AssetManager.Hat != null)
                g.DrawImage(AssetManager.Hat, HatDest);
            return;
        }

        // GDI+ fallback
        int cx = 510;
        using var coat = new SolidBrush(Color.WhiteSmoke);
        g.FillRectangle(coat, new Rectangle(cx - 35, 210, 70, 95));

        using var skin = new SolidBrush(Color.FromArgb(220, 180, 140));
        g.FillEllipse(skin, cx - 28, 145, 56, 62);

        using var glassesPen = new Pen(Color.Black, 1.5f);
        g.DrawRectangle(glassesPen, cx - 24, 166, 18, 11);
        g.DrawRectangle(glassesPen, cx + 4,  166, 18, 11);
        g.DrawLine(glassesPen, cx - 6, 171, cx + 4, 171);

        using var goatee = new SolidBrush(Color.FromArgb(60, 40, 30));
        g.FillEllipse(goatee, cx - 8, 192, 16, 10);

        using var mouth = new Pen(Color.FromArgb(120, 60, 60), 2);
        switch (expression)
        {
            case 1:
                g.DrawArc(mouth, cx - 12, 182, 24, 14, 0, 180);
                break;
            case 2:
            {
                g.DrawArc(mouth, cx - 12, 186, 24, 14, 180, 180);
                using var brow = new Pen(Color.FromArgb(60, 40, 30), 2);
                g.DrawLine(brow, cx - 22, 162, cx - 8,  166);
                g.DrawLine(brow, cx + 8,  166, cx + 22, 162);
                break;
            }
            default:
                g.DrawLine(mouth, cx - 10, 186, cx + 10, 186);
                break;
        }
    }

    static void DrawTable(Graphics g)
    {
        // mesa.png is also a full 800x600 overlay; it covers Walter's lower half.
        if (AssetManager.Mesa != null)
        {
            g.DrawImage(AssetManager.Mesa, 0, 0, 800, 600);
        }
        else
        {
            DrawFallbackTable(g);
        }
    }

    static void DrawFallbackTable(Graphics g)
    {
        using var top = new SolidBrush(Color.FromArgb(190, 145, 65));
        g.FillRoundedRectangle(top, new Rectangle(0, TableTop, 800, 20), 6);
        using var body = new SolidBrush(Color.FromArgb(170, 130, 55));
        g.FillRectangle(body, new Rectangle(0, TableTop + 20, 800, TableBottom - TableTop - 20));
        using var pen = new Pen(Color.FromArgb(120, 85, 30), 2);
        g.DrawRoundedRectangle(pen, new Rectangle(1, TableTop, 798, TableBottom - TableTop), 6);
    }

    static void DrawBeaker(Graphics g)
    {
        // Beaker positioned in the center of the table, where reactions happen
        if (AssetManager.Beaker != null)
        {
            const int beakerW = 180;
            const int beakerH = 140;
            const int beakerX = 360; // center of 800px canvas
            const int beakerY = 270; // on the table surface
            g.DrawImage(AssetManager.Beaker, new Rectangle(beakerX, beakerY, beakerW, beakerH));
        }
    }

    static void DrawBurner(Graphics g)
    {
        // Bunsen burner positioned on the left side of the table
        if (AssetManager.Burner != null)
        {
            const int burnerW = 100;
            const int burnerH = 120;
            const int burnerX = 120; // left side of table
            const int burnerY = 280; // on the table surface
            g.DrawImage(AssetManager.Burner, new Rectangle(burnerX, burnerY, burnerW, burnerH));
        }
    }

    static void DrawButtons(Graphics g, GamePhase phase)
    {
        // "On" button - red circle, below the burner
        DrawCircleButton(g, 200, 435, 20, "On", Color.Red, Color.White);

        // "Ok" button - green circle, center bottom
        DrawCircleButton(g, 400, 555, 20, "Ok", Color.LimeGreen, Color.Black);

        // "Recomeçar" button - blue rectangle, bottom right
        DrawRectButton(g, 700, 540, 100, 40, "Recomeçar", Color.RoyalBlue, Color.White);
    }

    static void DrawCircleButton(Graphics g, int cx, int cy, int radius, string label, Color bgColor, Color textColor)
    {
        // Draw filled circle background
        using var bgBrush = new SolidBrush(bgColor);
        g.FillEllipse(bgBrush, cx - radius, cy - radius, radius * 2, radius * 2);

        // Draw border
        using var borderPen = new Pen(Color.FromArgb(40, 40, 40), 2f);
        g.DrawEllipse(borderPen, cx - radius, cy - radius, radius * 2, radius * 2);

        // Draw text
        using var font = new Font("Arial", 10f, FontStyle.Bold);
        using var textBrush = new SolidBrush(textColor);
        var textSize = g.MeasureString(label, font);
        g.DrawString(label, font, textBrush,
            cx - textSize.Width / 2f,
            cy - textSize.Height / 2f);
    }

    static void DrawRectButton(Graphics g, int x, int y, int w, int h, string label, Color bgColor, Color textColor)
    {
        const int cornerRadius = 6;

        // Draw rounded rectangle background
        using var bgBrush = new SolidBrush(bgColor);
        g.FillRoundedRectangle(bgBrush, new Rectangle(x, y, w, h), cornerRadius);

        // Draw border
        using var borderPen = new Pen(Color.FromArgb(40, 40, 40), 1.5f);
        g.DrawRoundedRectangle(borderPen, new Rectangle(x, y, w, h), cornerRadius);

        // Draw text
        using var font = new Font("Arial", 9f, FontStyle.Bold);
        using var textBrush = new SolidBrush(textColor);
        var textSize = g.MeasureString(label, font);
        g.DrawString(label, font, textBrush,
            x + (w - textSize.Width) / 2f,
            y + (h - textSize.Height) / 2f);
    }

    // ── Ingredients ──────────────────────────────────────────────────────────

    static void DrawIngredients(Graphics g, List<Ingredient> ingredients, GameState state, Point mouse)
    {
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

        // Try to draw ingredient asset image
        if (!string.IsNullOrEmpty(ing.AssetName))
        {
            var asset = AssetManager.GetIngredientAsset(ing.AssetName);
            if (asset != null)
            {
                // Draw image with slight opacity if hovered
                var oldState = g.Save();
                if (hovered)
                {
                    var cm = new ColorMatrix();
                    cm.Matrix33 = 0.85f;
                    var ia = new ImageAttributes();
                    ia.SetColorMatrix(cm);
                    g.DrawImage(asset, r, 0, 0, asset.Width, asset.Height, GraphicsUnit.Pixel, ia);
                }
                else
                {
                    g.DrawImage(asset, r);
                }
                g.Restore(oldState);

                // Draw highlight border if next ingredient
                if (isNext)
                {
                    using var border = new Pen(Color.Gold, 2.5f);
                    g.DrawRectangle(border, r);
                }
                return;
            }
        }

        // Fallback: draw GDI+ bottle
        Color fill = hovered ? Lighten(ing.BottleColor, 30) : ing.BottleColor;
        using var body = new SolidBrush(fill);
        g.FillRectangle(body, r);

        int liqH = (int)(r.Height * 0.4f);
        using var liq = new SolidBrush(ing.LiquidColor);
        g.FillRectangle(liq, new Rectangle(r.X + 3, r.Bottom - liqH - 3, r.Width - 6, liqH));

        using var border2 = new Pen(isNext ? Color.Gold : Color.FromArgb(80, 80, 80), isNext ? 2.5f : 1f);
        g.DrawRectangle(border2, r);

        using var font = new Font("Consolas", 7f, FontStyle.Bold);
        using var txt  = new SolidBrush(Color.Black);
        var sz = g.MeasureString(ing.Formula, font);
        g.DrawString(ing.Formula, font, txt,
            r.X + (r.Width - sz.Width) / 2f,
            r.Y + 5);
    }

    // ── Step panel ──────────────────────────────────────────────────────────

    static void DrawStepPanel(Graphics g, GameState state)
    {
        using var bg = new SolidBrush(Color.FromArgb(170, 10, 10, 25));
        g.FillRectangle(bg, StepZone);

        using var titleFont = new Font("Arial", 9f, FontStyle.Bold);
        using var white = new SolidBrush(Color.White);
        g.DrawString("RECEITA", titleFont, white, StepZone.X + 8, StepZone.Y + 6);

        using var stepFont  = new Font("Arial", 7.5f);
        using var doneColor = new SolidBrush(Color.FromArgb(100, 200, 100));
        using var dimColor  = new SolidBrush(Color.FromArgb(100, 100, 100));
        using var goldBar   = new SolidBrush(Color.Gold);

        int y = StepZone.Y + 24;
        for (int i = 0; i < GameState.Recipe.Length; i++)
        {
            var step = GameState.Recipe[i];
            string prefix = i < state.CurrentStep ? "✓" : $"{i + 1}.";
            string label  = step.DisplayName.Length > 13 ? step.DisplayName[..13] : step.DisplayName;
            string line   = $"{prefix} {label}";

            if (i == state.CurrentStep && state.Phase == GamePhase.Playing)
            {
                g.FillRectangle(goldBar, new Rectangle(StepZone.X, y, 3, 16));
                g.DrawString(line, stepFont, white, StepZone.X + 7, y);
            }
            else if (i < state.CurrentStep)
                g.DrawString(line, stepFont, doneColor, StepZone.X + 7, y);
            else
                g.DrawString(line, stepFont, dimColor, StepZone.X + 7, y);

            y += 14;
        }
    }

    // ── Beaker & Burner ───────────────────────────────────────────────────────

    static void DrawBeaker(Graphics g, GameState state)
    {
        int bx = BeakerX, by = BeakerY, bw = BeakerW, bh = BeakerH;

        if (state.IsHeated)
        {
            using var glow = new SolidBrush(Color.FromArgb(60, 255, 120, 0));
            g.FillEllipse(glow, bx - 8, by + bh - 10, bw + 16, 20);
        }

        Point[] trap =
        {
            new(bx,      by),
            new(bx + bw, by),
            new(bx + bw - 5, by + bh),
            new(bx + 5,  by + bh),
        };
        using var glass = new SolidBrush(Color.FromArgb(160, 200, 220, 240));
        g.FillPolygon(glass, trap);

        int liquidSteps = state.BeakerContents.Count;
        if (liquidSteps > 0)
        {
            float fraction = Math.Min(liquidSteps / 4f, 1f);
            int liqH = (int)(bh * fraction);
            Color liqColor = liquidSteps >= 4
                ? Color.FromArgb(180, 100, 200, 100)
                : Color.FromArgb(160, 100, 160, 220);
            using var liq = new SolidBrush(liqColor);
            g.FillRectangle(liq, new Rectangle(bx + 5, by + bh - liqH - 3, bw - 10, liqH));
        }

        using var outline = new Pen(Color.FromArgb(100, 150, 180), 1.5f);
        g.DrawPolygon(outline, trap);
        g.DrawRectangle(outline, new Rectangle(bx - 3, by - 5, bw + 6, 6));
    }

    static void DrawBurner(Graphics g, bool lit)
    {
        int bx = BurnerX, by = BurnerY;

        using var stand = new SolidBrush(Color.FromArgb(80, 80, 90));
        g.FillRectangle(stand, new Rectangle(bx, by, 70, 12));
        g.FillRectangle(stand, new Rectangle(bx + 28, by + 12, 14, 25));

        if (lit)
        {
            using var flameOuter = new SolidBrush(Color.FromArgb(200, 255, 140, 0));
            using var flameInner = new SolidBrush(Color.FromArgb(220, 255, 220, 0));
            g.FillEllipse(flameOuter, bx + 27, by - 18, 16, 22);
            g.FillEllipse(flameInner, bx + 30, by - 12, 10, 14);
        }
    }

    // ── Info panel ───────────────────────────────────────────────────────────

    static void DrawInfoPanel(Graphics g, GameState state)
    {
        using var bg = new SolidBrush(Color.FromArgb(170, 8, 20, 8));
        g.FillRectangle(bg, InfoZone);

        using var titleFont   = new Font("Arial", 9f, FontStyle.Bold);
        using var formulaFont = new Font("Consolas", 11f, FontStyle.Bold);
        using var factFont    = new Font("Arial", 7.5f);
        using var white  = new SolidBrush(Color.White);
        using var green  = new SolidBrush(Color.LimeGreen);
        using var dim    = new SolidBrush(Color.FromArgb(160, 160, 160));

        int x = InfoZone.X + 7, y = InfoZone.Y + 6;

        g.DrawString("COMPOSTO", titleFont, white, x, y);
        y += 18;

        if (state.CurrentStep < GameState.Recipe.Length)
        {
            var step = GameState.Recipe[state.CurrentStep];
            using var nameFont = new Font("Arial", 8f, FontStyle.Bold);
            DrawWrapped(g, step.DisplayName, nameFont, white, x, y, InfoZone.Width - 10);
            y += 26;
            g.DrawString(step.ChemFormula, formulaFont, green, x, y);
            y += 26;
            DrawWrapped(g, step.EducationalFact, factFont, dim, x, y, InfoZone.Width - 10);
        }
        else
        {
            g.DrawString("Receita\nconcluída!", titleFont, green, x, y + 8);
        }
    }

    // ── Action bar ───────────────────────────────────────────────────────────

    static void DrawActionBar(Graphics g, GameState state, Point mouse)
    {
        using var bg = new SolidBrush(Color.FromArgb(200, 25, 25, 25));
        g.FillRectangle(bg, ActionZone);

        foreach (var kv in HitTester.ButtonBounds)
        {
            bool hovered = kv.Value.Contains(mouse);
            bool enabled = IsButtonEnabled(kv.Key, state);
            DrawButton(g, kv.Key, kv.Value, enabled, hovered);
        }

        using var hintFont  = new Font("Arial", 7.5f, FontStyle.Italic);
        using var hintBrush = new SolidBrush(Color.FromArgb(140, 140, 140));
        g.DrawString("Clique nos compostos na ordem certa para seguir a receita.", hintFont, hintBrush, 8, 575);
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
        g.FillRoundedRectangle(brush, r, 8);

        using var pen = new Pen(enabled ? Color.White : Color.FromArgb(100, 100, 100), 1.5f);
        g.DrawRoundedRectangle(pen, r, 8);

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
        float tx = (800 - sz.Width) / 2f, ty = 270;
        g.DrawString(message, font, shadow, tx + 2, ty + 2);
        g.DrawString(message, font, white, tx, ty);
    }

    static void DrawSuccessOverlay(Graphics g)
    {
        using var overlay = new SolidBrush(Color.FromArgb(200, 0, 50, 0));
        g.FillRectangle(overlay, new Rectangle(0, 0, 800, 600));

        using var borderPen = new Pen(Color.Gold, 3);
        g.DrawRectangle(borderPen, new Rectangle(60, 120, 680, 300));

        using var titleFont = new Font("Arial", 26f, FontStyle.Bold);
        using var subFont   = new Font("Arial", 16f);
        using var hintFont  = new Font("Arial", 10f, FontStyle.Italic);
        using var gold  = new SolidBrush(Color.Gold);
        using var white = new SolidBrush(Color.White);
        using var gray  = new SolidBrush(Color.FromArgb(200, 200, 200));

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
        => g.DrawString(text, font, brush, new RectangleF(x, y, maxWidth, 200));
}

// Extension methods for rounded rectangles
public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle r, int radius)
    {
        using var path = RoundedRect(r, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle r, int radius)
    {
        using var path = RoundedRect(r, radius);
        g.DrawPath(pen, path);
    }

    static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle r, int rad)
    {
        int d = rad * 2;
        var p = new System.Drawing.Drawing2D.GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
