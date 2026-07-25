using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public bool playerSpeaks;

    [TextArea]
    public string text;
}