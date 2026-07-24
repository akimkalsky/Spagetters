using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    public float fadeSeconds = 0.45f;
    const float DoorWidth = 1100f;

    RectTransform left, right;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Build();
    }

    void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        left = Door("LeftDoor", new Vector2(1f, 0.5f));
        right = Door("RightDoor", new Vector2(0f, 0.5f));
        SetClosed(0f);
    }

    RectTransform Door(string name, Vector2 pivot)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.20f, 0.12f, 0.07f);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = pivot;
        rt.sizeDelta = new Vector2(DoorWidth, 1400f);

        var plank = new GameObject("Plank").AddComponent<Image>();
        plank.transform.SetParent(go.transform, false);
        plank.color = new Color(0.32f, 0.20f, 0.11f);
        var prt = plank.rectTransform;
        prt.anchorMin = new Vector2(pivot.x, 0f);
        prt.anchorMax = new Vector2(pivot.x, 1f);
        prt.pivot = pivot;
        prt.sizeDelta = new Vector2(24f, 0f);
        return rt;
    }

    void SetClosed(float t)
    {
        float lx = Mathf.Lerp(-DoorWidth, 0f, t);
        left.anchoredPosition = new Vector2(lx, 0f);
        right.anchoredPosition = new Vector2(-lx, 0f);
        bool block = t > 0.5f;
        left.GetComponent<Image>().raycastTarget = block;
        right.GetComponent<Image>().raycastTarget = block;
    }

    public Coroutine FadeOut() => StartCoroutine(Slide(0f, 1f));
    public Coroutine FadeIn() => StartCoroutine(Slide(1f, 0f));

    IEnumerator Slide(float from, float to)
    {
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            SetClosed(Mathf.SmoothStep(from, to, t / fadeSeconds));
            yield return null;
        }
        SetClosed(to);
    }
}
