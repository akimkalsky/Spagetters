using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class Typewriter : MonoBehaviour
{
    public float charsPerSecond = 34f;
    public string clickSfx = "type";

    TMP_Text tmp;
    Coroutine run;

    void Awake() => tmp = GetComponent<TMP_Text>();

    public void Play(string text)
    {
        if (tmp == null)
        {
            tmp = GetComponent<TMP_Text>();
        }
        if (run != null)
        {
            StopCoroutine(run);
        }
        tmp.text = text ?? "";
        tmp.ForceMeshUpdate();
        run = StartCoroutine(Reveal(tmp.textInfo.characterCount));
    }

    IEnumerator Reveal(int total)
    {
        tmp.maxVisibleCharacters = 0;
        float shown = 0f;
        while (shown < total)
        {
            shown += charsPerSecond * Time.unscaledDeltaTime;
            int n = Mathf.Min(total, Mathf.FloorToInt(shown));
            if (n != tmp.maxVisibleCharacters)
            {
                if (n % 2 == 0 && !string.IsNullOrEmpty(clickSfx))
                {
                    AudioManager.Instance?.PlaySfx(clickSfx, 0.2f);
                }
                tmp.maxVisibleCharacters = n;
            }
            yield return null;
        }
        tmp.maxVisibleCharacters = total;
        run = null;
    }
}
