using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    Canvas canvas;

    void Start()
    {
        Build();
        GameFlow.Instance.StateChanged += OnState;
        OnState(GameFlow.Instance.State);
    }

    void OnDestroy()
    {
        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.StateChanged -= OnState;
        }
    }

    void OnState(GameState s) => canvas.gameObject.SetActive(s == GameState.Paused);

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null)
        {
            return;
        }
        var g = GameFlow.Instance;
        if (kb.escapeKey.wasPressedThisFrame && (g.State == GameState.Duel || g.State == GameState.Paused))
        {
            g.TogglePause();
        }
    }

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("PauseCanvas", 200, transform);
        UIFactory.FullScreenPanel(canvas.transform, UIFactory.Dim);
        UIFactory.Label(canvas.transform, "PAUSED", 110, UIFactory.Parchment, new Vector2(0, 260));
        UIFactory.MenuButton(canvas.transform, "RESUME", new Vector2(0, 90),
                             () => GameFlow.Instance.TogglePause());
        UIFactory.MenuButton(canvas.transform, "SETTINGS", new Vector2(0, -20),
                             () => SettingsController.Instance.Open());
        UIFactory.MenuButton(canvas.transform, "MAIN MENU", new Vector2(0, -130),
                             () => GameFlow.Instance.ToMainMenu());
        UIFactory.MenuButton(canvas.transform, "QUIT", new Vector2(0, -240),
                             () => GameFlow.Instance.Quit());
    }
}
