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
    public string LastFeedbackMessage { get; private set; } = "";

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

    public void TryIngredient(string id)
    {
        if (Phase != GamePhase.Playing) return;
        var step = Recipe[CurrentStep];
        if (step.Type != StepType.AddIngredient)
        {
            SetWrong("Precisa realizar uma ação primeiro!");
            return;
        }
        if (step.Id == id)
        {
            BeakerContents.Add(id);
            CurrentStep++;
            WalterExpression = 0;
            CheckComplete();
        }
        else
        {
            SetWrong("Ingrediente errado! Walter não aprovaria...");
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
        BeakerContents.Clear();
        LastFeedbackMessage = "";
    }

    // DEBUG: alterna a expressão (0 normal -> 1 feliz -> 2 triste -> 0) para testar os rostos.
    public void DebugCycleFace()
    {
        WalterExpression = (WalterExpression + 1) % 3;
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
