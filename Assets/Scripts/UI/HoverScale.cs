using UnityEngine;
using UnityEngine.EventSystems;

public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public float hover = 1.06f;
    Vector3 target = Vector3.one;
    RectTransform rt;

    void Awake() => rt = transform as RectTransform;

    void Update() => rt.localScale = Vector3.Lerp(rt.localScale, target, Time.unscaledDeltaTime * 12f);

    public void OnPointerEnter(PointerEventData e) => target = Vector3.one * hover;
    public void OnPointerExit(PointerEventData e) => target = Vector3.one;
    public void OnSelect(BaseEventData e) => target = Vector3.one * hover;
    public void OnDeselect(BaseEventData e) => target = Vector3.one;
}
