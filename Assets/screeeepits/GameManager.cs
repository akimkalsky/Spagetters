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

        steps--;
        UpdateUI();

        if (steps <= 0)
        {
            Debug.Log("Out of steps!");
            // TODO: Lose game
        }
    }

    public void AddSteps(int amount)
    {
        steps += amount;
        UpdateUI();
    }

    void UpdateUI()
    {
        stepText.text = $"Steps: {steps}";
    }
}