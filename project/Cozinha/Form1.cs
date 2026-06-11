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

    private readonly ParticleSystem _particles = new();
    private readonly System.Windows.Forms.Timer _animTimer; // particle sim @ ~60fps
    private GamePhase _lastPhase = GamePhase.Playing;       // edge-detect WrongOrder
    private int _lastStep;                                  // detect step changes

    // Bocal do bico de Bunsen (de onde sai a chama) e topo do béquer.
    private static readonly PointF BurnerNozzle = new(170, 292);
    // Zona da chama: área acima do bocal onde o béquer precisa estar para aquecer.
    private static readonly Rectangle BurnerFlameZone = new(125, 228, 90, 65);

    private float _heatAccum;          // segundos com béquer sobre a chama
    private const float HeatRequired = 3f; // tempo necessário para aquecer
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

        _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animTimer.Tick += (_, _) => StepParticles();
        _animTimer.Start();
    }

    // One frame of particle simulation: burn fuel, fire emitters off the current
    // state, advance every particle, and only repaint when something's live.
    private void StepParticles()
    {
        const float dt = 0.016f;
        _state.TickBurner(dt);

        // Mudou de passo → zera a mistura, pra "chacoalhar antes da hora" não
        // contar quando enfim chegar no passo de misturar.
        if (_state.CurrentStep != _lastStep)
        {
            if (_state.CurrentStep > _lastStep && _lastStep >= 0)
            {
                var r = _state.BeakerRect;
                _particles.EmitStepComplete(r.X + r.Width / 2f, r.Y + r.Height / 2f);
            }
            _mixer.Reset();
            _lastStep = _state.CurrentStep;
        }

        // Misturar terminou (béquer chacoalhado até 100% no passo certo).
        if (_state.Current is { Type: StepType.PerformAction, Id: "MIX" } && _mixer.Percent >= 100)
            _state.OnMixed();

        // Chama saindo do bocal enquanto o bico estiver aceso.
        if (_state.BurnerOn)
            _particles.EmitFire(BurnerNozzle.X, BurnerNozzle.Y, 3);

        // Béquer sobre a chama com bico aceso → acumula calor e avança quando atingir o tempo.
        bool beakerOverFlame = _state.BurnerOn && BurnerFlameZone.IntersectsWith(_state.BeakerRect);
        if (beakerOverFlame && _state.Current is { Type: StepType.PerformAction, Id: "HEAT" })
        {
            _heatAccum += dt;
            if (_heatAccum >= HeatRequired)
                _state.OnBurnerLit();
        }
        else if (!beakerOverFlame)
        {
            _heatAccum = 0f;
        }

        // Bolhas sobem só quando béquer está sobre a chama.
        if (beakerOverFlame && _state.BeakerFill.Count > 0)
        {
            var r = _state.BeakerRect;
            _particles.EmitBubble(r.X + r.Width / 2f, r.Y + r.Height * 0.50f, 2);
        }

        // Faíscas/fumaça ao misturar (chacoalhando o béquer).
        if (_mixer.Mixing)
        {
            var r = _state.BeakerRect;
            _particles.EmitMix(r.X + r.Width / 2f, r.Y + r.Height * 0.42f);
        }

        // Confete de vitória ao completar a receita.
        if (_state.Phase == GamePhase.Success && _lastPhase != GamePhase.Success)
            _particles.EmitVictoryBorders(ClientSize.Width, ClientSize.Height);

        // Explosão de partículas + timer de recuperação quando entra em erro.
        if (_state.Phase == GamePhase.WrongOrder && _lastPhase != GamePhase.WrongOrder)
        {
            _particles.EmitErrorBurst(400, 300);
            _feedbackTimer.Stop();
            _feedbackTimer.Start();
        }
        _lastPhase = _state.Phase;

        _particles.Update(dt);

        bool live = _particles.Count > 0 || _state.BurnerOn || _state.IsHeated || _mixer.Mixing;
        if (live) Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Renderer.DrawAll(e.Graphics, _state, _ingredients, _mousePos, _drag, _mixer.Mixing, _mixer.Percent);
        _particles.Draw(e.Graphics);
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

        // Botão "On" do bico de Bunsen — apenas acende/apaga, não avança o passo.
        // O avanço ocorre no tick quando o béquer estiver sobre a chama.
        if (HitTester.OnButton.Contains(e.Location))
        {
            _state.ToggleBurner();
            Invalidate();
            return;
        }

        // Botão "Servir" → entrega o produto (passo final).
        if (HitTester.ServeButton.Contains(e.Location))
        {
            _state.TryAction("SERVE");
            Invalidate();
            return;
        }

        // Botão "Recomeçar".
        if (HitTester.ResetButton.Contains(e.Location))
        {
            _state.Reset();
            _mixer.Reset();
            _heatAccum = 0f;
            Invalidate();
            return;
        }

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
        {
            // Só despeja a cor se for o ingrediente certo da vez.
            if (_state.TryIngredient(dropped.Id))
                _state.PourIntoBeaker(dropped.LiquidColor);
        }
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
            _heatAccum = 0f;
            Invalidate();
        }
    }
}
