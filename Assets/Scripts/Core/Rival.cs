using UnityEngine;

public class Rival
{
    public int Index;
    public string Name;
    public int Bounty;
    public float Reaction;
    public int Seed;
    public bool Defeated;

    // Arcade mode
    public string Tagline;
    public string Taunt;
    public string Gloat;
    public string DeathLine;
    public string CowardLine;

    // Story mode
    public Sprite Portrait;
    public DialogueLine[] IntroDialogue;
}
