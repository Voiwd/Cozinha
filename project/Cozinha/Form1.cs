namespace Cozinha;

public partial class Form1 : Form
{
    private readonly GameState _state;
    private readonly List<Ingredient> _ingredients;
    private Point _mousePos;
    private readonly System.Windows.Forms.Timer _feedbackTimer;

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

        MusicPlayer.Play("YTDown_YouTube_Breaking-Bad-Intro_Media_F1HNuAE9WdU_007_128k.mp3");
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        MusicPlayer.Stop();
        base.OnFormClosed(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Renderer.DrawAll(e.Graphics, _state, _ingredients, _mousePos);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _mousePos = e.Location;

        foreach (var ing in _ingredients)
            ing.IsHovered = ing.Bounds.Contains(e.Location);

        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left) return;
        if (_state.Phase == GamePhase.WrongOrder) return;

        // DEBUG: clicar no Walter alterna o rosto (normal -> feliz -> triste).
        if (Renderer.WalterDest.Contains(e.Location) && e.Y < 385)
        {
            _state.DebugCycleFace();
            Invalidate();
            return;
        }

        string? ingId = HitTester.HitIngredient(e.Location, _ingredients);
        if (ingId != null)
        {
            _state.TryIngredient(ingId);
            if (_state.Phase == GamePhase.WrongOrder) _feedbackTimer.Start();
            Invalidate();
            return;
        }

        string? action = HitTester.HitActionButton(e.Location);
        if (action == "RESET")
        {
            _state.Reset();
            Invalidate();
            return;
        }
        if (action != null)
        {
            _state.TryAction(action);
            if (_state.Phase == GamePhase.WrongOrder) _feedbackTimer.Start();
            Invalidate();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.R)
        {
            _state.Reset();
            Invalidate();
        }
        else if (e.KeyCode == Keys.F)
        {
            // DEBUG: tecla F também alterna o rosto do Walter.
            _state.DebugCycleFace();
            Invalidate();
        }
    }
}
