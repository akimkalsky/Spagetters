using UnityEngine;

[System.Serializable]
public class StoryEncounter
{
    public Rival rival;

    public string[] introDialogue;

    public Sprite rivalPortrait;

    public Sprite playerPortrait;

    public bool defeated;
}