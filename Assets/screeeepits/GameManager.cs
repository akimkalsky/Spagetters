using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int steps = 30;

    public TMP_Text stepText;

    private void Awake()
    {
        Instance = this;
        UpdateUI();
    }

    public void UseStep()
    {
        if (steps <= 0)
            return;

        SetSteps(steps - 1);
    }

    public void AddSteps(int amount)
    {
        SetSteps(steps + amount);
    }

    public void ResetTo(int value)
    {
        steps = Mathf.Max(0, value);
        UpdateUI();
    }

    void SetSteps(int value)
    {
        bool hadSteps = steps > 0;
        steps = Mathf.Max(0, value);
        UpdateUI();

        if (steps <= 0 && hadSteps)
        {
            GameEvents.RaiseOutOfSteps();
        }
    }

    void UpdateUI()
    {
        if (stepText != null)
        {
            stepText.text = $"Paces: {steps}";
        }
    }
}
