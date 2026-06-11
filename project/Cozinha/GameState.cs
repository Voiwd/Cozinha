namespace Cozinha;

public enum GamePhase { Playing, WrongOrder, Success }
public enum StepType { AddIngredient, PerformAction }

public class RecipeStep
{
    public StepType Type;
    public string Id = "";
    public string DisplayName = "";
    public string ChemFormula = "";
    public string EducationalFact = "";
}

public class GameState
{
    public GamePhase Phase { get; private set; } = GamePhase.Playing;
    public int CurrentStep { get; private set; } = 0;
    public int WalterExpression { get; private set; } = 0; // 0=neutro 1=feliz 2=irritado
    public bool IsHeated { get; private set; } = false;
    public List<string> BeakerContents { get; } = new();

    // ── Bico de Bunsen ────────────────────────────────────────────────────────
    // Combustível finito: o bico só acende enquanto houver gás. Se o jogador
    // deixar ligado à toa, o tanque esvazia e não dá pra acender de novo (até
    // dar Reset).
    const float FuelBurnRate = 7f; // unidades por segundo → ~14s de chama total
    public bool BurnerOn { get; private set; }
    public float Fuel { get; private set; } = 100f;
    public bool BurnerEmpty => Fuel <= 0f;

    // Liga/desliga. Devolve true se a ação teve efeito (pra disparar feedback).
    public bool ToggleBurner()
    {
        if (BurnerOn) { BurnerOn = false; return true; }
        if (BurnerEmpty) return false; // sem gás, não acende
        BurnerOn = true;
        return true;
    }

    public void TickBurner(float dt)
    {
        if (!BurnerOn) return;
        Fuel -= dt * FuelBurnRate;
        if (Fuel <= 0f) { Fuel = 0f; BurnerOn = false; } // acabou o gás
    }
    // Colors poured in via drag-and-drop. Separate from BeakerContents because
    // dragging isn't tied to the recipe order yet.
    public List<Color> BeakerFill { get; } = new();
    public string LastFeedbackMessage { get; private set; } = "";

    // Beaker is draggable now, so its position is mutable state.
    public const int BeakerW = 180, BeakerH = 140;
    public static readonly PointF BeakerHome = new(360, 270);
    public PointF BeakerPos = BeakerHome;
    public Rectangle BeakerRect => new((int)BeakerPos.X, (int)BeakerPos.Y, BeakerW, BeakerH);

    public void PourIntoBeaker(Color liquid) => BeakerFill.Add(liquid);

    public static readonly RecipeStep[] Recipe =
    {
        new() { Type = StepType.AddIngredient, Id = "NaOH",   DisplayName = "Hidróxido de Sódio", ChemFormula = "NaOH",   EducationalFact = "Base forte, dissolve\nmatéria orgânica." },
        new() { Type = StepType.AddIngredient, Id = "CH3NH2", DisplayName = "Metilamina",         ChemFormula = "CH3NH2", EducationalFact = "Amina de odor forte.\nPrecursora química." },
        new() { Type = StepType.PerformAction, Id = "HEAT",   DisplayName = "Aquecer",            ChemFormula = "Δ",      EducationalFact = "Calor acelera reações\nquímicas (cinética)." },
        new() { Type = StepType.AddIngredient, Id = "RedP",   DisplayName = "Fósforo Vermelho",   ChemFormula = "P4",     EducationalFact = "Alótropo estável do\nfósforo. Redutor." },
        new() { Type = StepType.AddIngredient, Id = "I2",     DisplayName = "Iodo",               ChemFormula = "I2",     EducationalFact = "Halogênio sólido.\nOxidante moderado." },
        new() { Type = StepType.PerformAction, Id = "MIX",    DisplayName = "Misturar",           ChemFormula = "~",      EducationalFact = "Homogeneização garante\nreação uniforme." },
        new() { Type = StepType.PerformAction, Id = "SERVE",  DisplayName = "Servir",             ChemFormula = "->",     EducationalFact = "Produto final obtido\ncom 99.1% de pureza." },
    };

    // Passo atual da receita (null quando já concluiu tudo).
    public RecipeStep? Current => CurrentStep < Recipe.Length ? Recipe[CurrentStep] : null;

    // Devolve true se o ingrediente foi aceito (pra só então despejar a cor).
    public bool TryIngredient(string id)
    {
        if (Phase != GamePhase.Playing) return false;
        var step = Recipe[CurrentStep];
        if (step.Type != StepType.AddIngredient)
        {
            SetWrong("Precisa realizar uma ação primeiro!");
            return false;
        }
        if (step.Id == id)
        {
            BeakerContents.Add(id);
            CurrentStep++;
            WalterExpression = 0;
            CheckComplete();
            return true;
        }
        SetWrong("Ingrediente errado! Walter não aprovaria...");
        return false;
    }

    // Chamado quando o jogador acende o bico. Só avança se aquecer for o passo
    // atual — fora disso o bico é um objeto livre (não pune, não progride).
    public void OnBurnerLit()
    {
        if (Phase != GamePhase.Playing) return;
        if (Current is { Type: StepType.PerformAction, Id: "HEAT" })
        {
            IsHeated = true;
            CurrentStep++;
            WalterExpression = 0;
            CheckComplete();
        }
    }

    // Chamado quando o béquer chega a 100% de mistura. Mesma ideia: só conta
    // quando misturar é o passo atual.
    public void OnMixed()
    {
        if (Phase != GamePhase.Playing) return;
        if (Current is { Type: StepType.PerformAction, Id: "MIX" })
        {
            CurrentStep++;
            WalterExpression = 0;
            CheckComplete();
        }
    }

    public void TryAction(string actionId)
    {
        if (Phase != GamePhase.Playing) return;
        var step = Recipe[CurrentStep];
        if (step.Type != StepType.PerformAction || step.Id != actionId)
        {
            SetWrong("Ação errada agora!");
            return;
        }
        if (actionId == "HEAT") IsHeated = true;
        CurrentStep++;
        WalterExpression = 0;
        CheckComplete();
    }

    public void RecoverFromWrongOrder()
    {
        if (Phase != GamePhase.WrongOrder) return;
        Phase = GamePhase.Playing;
        WalterExpression = 0;
        LastFeedbackMessage = "";
    }

    public void Reset()
    {
        Phase = GamePhase.Playing;
        CurrentStep = 0;
        WalterExpression = 0;
        IsHeated = false;
        BurnerOn = false;
        Fuel = 100f;
        BeakerContents.Clear();
        BeakerFill.Clear();
        BeakerPos = BeakerHome;
        LastFeedbackMessage = "";
    }

    private void CheckComplete()
    {
        if (CurrentStep == Recipe.Length)
        {
            Phase = GamePhase.Success;
            WalterExpression = 1;
        }
    }

    private void SetWrong(string msg)
    {
        Phase = GamePhase.WrongOrder;
        WalterExpression = 2;
        LastFeedbackMessage = msg;
    }
}
