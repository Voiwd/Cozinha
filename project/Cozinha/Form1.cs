namespace Cozinha;

public partial class Form1 : Form
{
    private readonly GameState _state;
    private readonly List<Ingredient> _ingredients;
    private Point _mousePos;
    private readonly System.Windows.Forms.Timer _feedbackTimer;
    private readonly DragController _drag = new();
    private readonly System.Windows.Forms.Timer _dragTimer; // return-to-shelf lerp

    private readonly ShakeMixer _mixer = new();
    private readonly System.Windows.Forms.Timer _shakeTimer;
    private bool _beakerHeld;
    private bool _beakerReturning;
    private Point _beakerGrab;     // cursor offset inside the beaker when grabbed
    private Point _lastShakeMouse; // for per-move dx

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
            bool busy = _drag.TickReturn();
            if (_beakerReturning) busy |= TickBeakerReturn();
            if (!busy) _dragTimer.Stop();
            Invalidate();
        };

        _shakeTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _shakeTimer.Tick += (_, _) =>
        {
            _mixer.Tick();
            if (!_beakerHeld && _mixer.Idle) _shakeTimer.Stop(); // fully faded
            Invalidate();
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Renderer.DrawAll(e.Graphics, _state, _ingredients, _mousePos, _drag, _mixer.Mixing, _mixer.Percent);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _mousePos = e.Location;

        if (_beakerHeld)
        {
            _state.BeakerPos = new PointF(e.X - _beakerGrab.X, e.Y - _beakerGrab.Y);
            _mixer.Feed(e.X - _lastShakeMouse.X);
            _lastShakeMouse = e.Location;
            Invalidate();
            return;
        }

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
        if (e.Button != MouseButtons.Left || _drag.Active || _beakerHeld) return;

        // Beaker takes priority over the bottles behind it.
        if (_state.BeakerRect.Contains(e.Location))
        {
            _beakerHeld = true;
            _beakerReturning = false; // grabbed mid-glide → cancel the return
            _beakerGrab = new Point(e.X - (int)_state.BeakerPos.X, e.Y - (int)_state.BeakerPos.Y);
            _lastShakeMouse = e.Location;
            _shakeTimer.Start();
            Invalidate();
            return;
        }

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
        if (e.Button != MouseButtons.Left) return;

        if (_beakerHeld)
        {
            _beakerHeld = false;
            _beakerReturning = true; // glide back to its spot on the table
            _dragTimer.Start();
            Invalidate();
            return;
        }

        if (!_drag.Active) return;

        var dropped = _drag.Drop(_state.BeakerRect);
        if (dropped != null)
            _state.PourIntoBeaker(dropped.LiquidColor);
        else
            _dragTimer.Start();     // missed → glide home
        Invalidate();
    }

    // Lerp the beaker back to its home spot. Returns false once it's there.
    private bool TickBeakerReturn()
    {
        var home = GameState.BeakerHome;
        var p = _state.BeakerPos;
        p = new PointF(p.X + (home.X - p.X) * 0.25f, p.Y + (home.Y - p.Y) * 0.25f);
        if (Math.Abs(p.X - home.X) < 0.6f && Math.Abs(p.Y - home.Y) < 0.6f)
        {
            _state.BeakerPos = home;
            _beakerReturning = false;
            return false;
        }
        _state.BeakerPos = p;
        return true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.R)
        {
            _state.Reset();
            _mixer.Reset();
            Invalidate();
        }
    }
}
