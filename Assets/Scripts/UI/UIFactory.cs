using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

public static class UIFactory
{
    public static readonly Color Parchment = new Color(0.90f, 0.82f, 0.66f);
    public static readonly Color Ink = new Color(0.16f, 0.10f, 0.06f);
    public static readonly Color Rust = new Color(0.62f, 0.24f, 0.13f);
    public static readonly Color Dim = new Color(0f, 0f, 0f, 0.72f);

    static TMP_FontAsset western;
    static bool fontTried;

    static TMP_FontAsset Font()
    {
        if (!fontTried)
        {
            fontTried = true;
            western = Resources.Load<TMP_FontAsset>("Fonts/Western SDF");
        }
        return western;
    }

    public static Canvas CreateOverlayCanvas(string name, int sortOrder, Transform owner = null)
    {
        EnsureEventSystem();
        var go = new GameObject(name);
        if (owner != null)
        {
            go.transform.SetParent(owner, false);
        }
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
        Object.DontDestroyOnLoad(go);
    }

    public static Image FullScreenPanel(Transform parent, Color color)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        Stretch(img.rectTransform);
        return img;
    }

    public static TMP_Text Label(Transform parent, string text, float size, Color color,
                                 Vector2 anchoredPos, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 6f;
        if (Font() != null)
        {
            t.font = Font();
        }
        if (size >= 70f)
        {
            t.outlineColor = Ink;
            t.outlineWidth = 0.18f;
        }

        var rt = t.rectTransform;
        rt.sizeDelta = new Vector2(1500, size * 1.6f);
        rt.anchoredPosition = anchoredPos;
        return t;
    }

    public static Button MenuButton(Transform parent, string label, Vector2 anchoredPos, System.Action onClick, float width = 520f)
    {
        var go = new GameObject("Button_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = Parchment;
        var rt = img.rectTransform;
        rt.sizeDelta = new Vector2(width, 92);
        rt.anchoredPosition = anchoredPos;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Parchment;
        colors.highlightedColor = new Color(1f, 0.94f, 0.78f);
        colors.pressedColor = Rust;
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick?.Invoke());
        go.AddComponent<UISfx>();
        go.AddComponent<HoverScale>();

        var t = Label(go.transform, label, 40, Ink, Vector2.zero);
        Stretch(t.rectTransform);
        t.margin = new Vector4(20, 4, 20, 4);
        return btn;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static TMP_Text Readout(Transform parent)
    {
        var t = Label(parent, "", 32, Rust, new Vector2(680, 470), TextAlignmentOptions.Right);
        t.rectTransform.sizeDelta = new Vector2(480, 50);
        return t;
    }

    static RectTransform Row(Transform parent, string name, Vector2 pos, out TMP_Text label, string labelText)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(780, 60);
        rt.anchoredPosition = pos;
        label = Label(rt, labelText, 34, Parchment, new Vector2(-190, 0), TextAlignmentOptions.Left);
        label.rectTransform.sizeDelta = new Vector2(340, 50);
        return rt;
    }

    static Image Child(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    public static UnityEngine.UI.Slider Slider(Transform parent, string label, float value, Vector2 pos, System.Action<float> onChange)
    {
        var row = Row(parent, "Slider_" + label, pos, out _, label);

        var bar = new GameObject("Bar");
        bar.transform.SetParent(row, false);
        var barRt = bar.AddComponent<RectTransform>();
        barRt.sizeDelta = new Vector2(340, 22);
        barRt.anchoredPosition = new Vector2(200, 0);
        var slider = bar.AddComponent<UnityEngine.UI.Slider>();

        var bg = Child(barRt, "Background", new Color(0f, 0f, 0f, 0.5f));
        Stretch(bg.rectTransform);

        var fillArea = new GameObject("Fill Area").AddComponent<RectTransform>();
        fillArea.SetParent(barRt, false);
        Stretch(fillArea);
        var fill = Child(fillArea, "Fill", Rust);
        fill.rectTransform.anchorMin = new Vector2(0, 0);
        fill.rectTransform.anchorMax = new Vector2(0, 1);
        fill.rectTransform.sizeDelta = new Vector2(12, 0);

        var handleArea = new GameObject("Handle Slide Area").AddComponent<RectTransform>();
        handleArea.SetParent(barRt, false);
        Stretch(handleArea);
        var handle = Child(handleArea, "Handle", Parchment);
        handle.rectTransform.sizeDelta = new Vector2(26, 30);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;
        slider.onValueChanged.AddListener(v => onChange?.Invoke(v));
        return slider;
    }

    public static UnityEngine.UI.Toggle Toggle(Transform parent, string label, bool value, Vector2 pos, System.Action<bool> onChange)
    {
        var row = Row(parent, "Toggle_" + label, pos, out _, label);

        var box = Child(row, "Box", new Color(0f, 0f, 0f, 0.5f));
        box.rectTransform.sizeDelta = new Vector2(44, 44);
        box.rectTransform.anchoredPosition = new Vector2(340, 0);

        var check = Child(box.rectTransform, "Check", Rust);
        check.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        check.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        check.rectTransform.sizeDelta = new Vector2(28, 28);

        var toggle = box.gameObject.AddComponent<UnityEngine.UI.Toggle>();
        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.isOn = value;
        toggle.onValueChanged.AddListener(v => onChange?.Invoke(v));
        return toggle;
    }
}
