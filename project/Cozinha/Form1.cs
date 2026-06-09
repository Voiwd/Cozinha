namespace Cozinha;

public partial class Form1 : Form
{
    private readonly GameState _state;
    private readonly List<Ingredient> _ingredients;
    private Point _mousePos;
    private readonly System.Windows.Forms.Timer _feedbackTimer;
    private readonly DragController _drag = new();
    private readonly System.Windows.Forms.Timer _dragTimer; // return-to-shelf lerp

    public Form1()
    {
        InitializeComponent();
        DoubleBuffered = true;

        AssetManager.Load();
        _state       = new GameState();
        _ingredients = IngredientFactory.CreateAll();
        _mousePos    = Point.Empty;

        _feedbackTimer = new System.Windows.Forms.Timer { Interval = 1500 };
        _feedbackTimer.Tick += (_, _) =>
        {
            _feedbackTimer.Stop();
            _state.RecoverFromWrongOrder();
            Invalidate();
        };

        _dragTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60fps
        _dragTimer.Tick += (_, _) =>
        {
            if (!_drag.TickReturn()) _dragTimer.Stop();
            Invalidate();
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Renderer.DrawAll(e.Graphics, _state, _ingredients, _mousePos, _drag);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _mousePos = e.Location;

        if (_drag.Active)
        {
            _drag.MoveTo(e.Location);
            Invalidate();
            return;
        }

        foreach (var ing in _ingredients)
            ing.IsHovered = ing.Bounds.Contains(e.Location);

        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || _drag.Active) return;

        string? ingId = HitTester.HitIngredient(e.Location, _ingredients);
        if (ingId == null) return;

        var ing = _ingredients.First(i => i.Id == ingId);
        _dragTimer.Stop();          // cancel any in-flight return
        _drag.Begin(ing, e.Location);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left || !_drag.Active) return;

        var dropped = _drag.Drop(Renderer.BeakerBounds);
        if (dropped != null)
            _state.PourIntoBeaker(dropped.LiquidColor);
        else
            _dragTimer.Start();     // missed → glide home
        Invalidate();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.R)
        {
            _state.Reset();
            Invalidate();
        }
    }
}
