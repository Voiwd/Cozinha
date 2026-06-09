namespace Cozinha;

// Handles picking up a compound and dragging it around. Decoupled from the
// recipe/gameloop on purpose — it just moves a sprite and reports where it was
// dropped. The form decides what a drop *means*.
public class DragController
{
    public Ingredient? Held { get; private set; }
    public PointF Pos;            // top-left of the dragged sprite, in canvas px
    public bool Returning { get; private set; }

    Point _grab;                  // cursor offset inside the sprite when grabbed
    PointF _home;                 // shelf slot to lerp back to

    public bool Active => Held != null;

    public void Begin(Ingredient ing, Point cursor)
    {
        Held = ing;
        _home = ing.Bounds.Location;
        _grab = new Point(cursor.X - ing.Bounds.X, cursor.Y - ing.Bounds.Y);
        Pos = ing.Bounds.Location;
        Returning = false;
    }

    public void MoveTo(Point cursor)
    {
        if (Held == null || Returning) return;
        Pos = new PointF(cursor.X - _grab.X, cursor.Y - _grab.Y);
    }

    // Drop on a target rect. Returns the dropped ingredient if it landed on the
    // target (caller consumes it), otherwise starts the lerp-back and returns null.
    public Ingredient? Drop(Rectangle target)
    {
        if (Held == null) return null;
        if (CurrentRect().IntersectsWith(target))
        {
            var dropped = Held;
            Held = null;
            Returning = false;
            return dropped;
        }
        Returning = true; // missed — glide home
        return null;
    }

    // Advance the return animation one frame. False once it's home (and cleared).
    public bool TickReturn()
    {
        if (Held == null || !Returning) return false;
        // small lerp so it doesn't snap, but doesn't drag on forever either
        Pos = new PointF(Pos.X + (_home.X - Pos.X) * 0.25f,
                         Pos.Y + (_home.Y - Pos.Y) * 0.25f);
        if (Math.Abs(Pos.X - _home.X) < 0.6f && Math.Abs(Pos.Y - _home.Y) < 0.6f)
        {
            Held = null;
            Returning = false;
            return false;
        }
        return true;
    }

    public Rectangle CurrentRect()
    {
        if (Held == null) return Rectangle.Empty;
        var b = Held.Bounds;
        return new Rectangle((int)Pos.X, (int)Pos.Y, b.Width, b.Height);
    }

    // Vertical anchor used for z-ordering against Walter.
    public int CenterY => (int)Pos.Y + (Held?.Bounds.Height ?? 0) / 2;
}
