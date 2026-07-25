using UnityEngine;

public class JobStation : MonoBehaviour
{
    public string minigameKey = "TargetRange";
    public string title = "TARGET RANGE";
    public int interactDistance = 4;

    void Start()
    {
        if (GameSettings.StoryMode)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.AddComponent<NpcNameTag>().Init(title);

        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.isTrigger = true;
        }
    }
}
