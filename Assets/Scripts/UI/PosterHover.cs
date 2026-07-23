using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PosterHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float restRotation;

    Vector2 restPos;
    RectTransform rt;
    Image img;
    bool hover;
    bool ready;

    void Awake()
    {
        rt = (RectTransform)transform;
        img = GetComponent<Image>();
    }

    void Start()
    {
        restPos = rt.anchoredPosition;
        ready = true;
    }

    public void OnPointerEnter(PointerEventData e) => hover = true;
    public void OnPointerExit(PointerEventData e) => hover = false;

    void Update()
    {
        if (!ready)
        {
            return;
        }
        float k = Time.unscaledDeltaTime * 12f;
        rt.localRotation = Quaternion.Lerp(rt.localRotation, Quaternion.Euler(0f, 0f, hover ? 0f : restRotation), k);
        rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, hover ? restPos + new Vector2(0f, 22f) : restPos, k);
        img.color = Color.Lerp(img.color, hover ? new Color(1f, 0.97f, 0.86f) : UIFactory.Parchment, k);
    }
}
