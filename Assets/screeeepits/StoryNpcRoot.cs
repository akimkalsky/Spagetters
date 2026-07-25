using UnityEngine;

public class StoryNpcRoot : MonoBehaviour
{
    void Awake()
    {
        gameObject.SetActive(GameSettings.StoryMode);
    }
}