# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Workflow Rules

- **Perguntar antes de assumir:** Se faltar contexto ou o requisito for ambíguo, perguntar ao usuário antes de implementar.
- **Rodar e testar sempre:** Após qualquer mudança, buildar e executar o projeto para confirmar que funciona. Não reportar a tarefa como concluída sem ter rodado.
- **Commitar automaticamente ao finalizar:** Quando uma tarefa estiver pronta e testada, criar um commit descritivo sem precisar de instrução adicional do usuário.

## Build & Run

```powershell
# Build
dotnet build project\Cozinha\Cozinha.sln

# Run
dotnet run --project project\Cozinha\Cozinha.csproj

# Build release
dotnet publish project\Cozinha\Cozinha.csproj -c Release
```

The project targets `net8.0-windows` (WinForms) and requires Windows.

## Architecture

This is a single-screen WinForms game (800×600 fixed client area). There is no external dependency — everything is drawn with GDI+.

**Data flow:**
1. `Form1` owns `GameState` and `List<Ingredient>`. All user input (mouse click/move, keyboard) is handled in `Form1` and forwarded to `GameState` methods.
2. `GameState` is the only mutable model. It exposes `Phase`, `CurrentStep`, `BeakerContents`, `IsHeated`, `WalterExpression`, and `LastFeedbackMessage`. It also holds the static `Recipe` array (7 ordered steps).
3. After any state change, `Form1` calls `Invalidate()`, which triggers `OnPaint` → `Renderer.DrawAll`.
4. `Renderer` is a pure static class — it reads state and renders everything; it never writes state.
5. `HitTester` is a pure static class — it maps pixel coordinates to ingredient IDs or action button IDs.

**Game loop:**
- The player must click ingredients and action buttons in the exact order defined by `GameState.Recipe`.
- A wrong click transitions `Phase` to `WrongOrder` (red flash + Walter angry face), then a 1500 ms timer fires `RecoverFromWrongOrder()` to resume.
- Completing all 7 steps transitions to `Success` (green overlay). Press `R` or click RESET to restart.

**Layout zones (all hard-coded to 800×600):**

| Zone | Rect | Content |
|------|------|---------|
| Shelf | `(0,0,800,115)` | Clickable ingredient bottles |
| StepPanel | `(0,115,160,270)` | Recipe checklist |
| CharZone | `(160,115,460,270)` | Walter White + beaker + burner |
| InfoPanel | `(620,115,180,270)` | Current step compound info |
| ActionBar | `(0,385,800,215)` | HEAT / MIX / SERVE / RESET buttons |

**Placeholder graphics:** All visual elements are drawn with GDI+ primitives (rectangles, ellipses, polygons). `[asset]` tags are rendered in small text where sprite assets would eventually go — search for `TODO: replace with sprite asset` comments in `Renderer.cs`.

**Adding a new ingredient:** Add an entry to `IngredientFactory.CreateAll()` in `Ingredient.cs`, and if it should be part of the recipe, add a `RecipeStep` to `GameState.Recipe`.
