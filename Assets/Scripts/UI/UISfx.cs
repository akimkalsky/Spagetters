using UnityEngine;
using UnityEngine.EventSystems;

public class UISfx : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISelectHandler
{
    public string moveSfx = "ui_move";
    public string clickSfx = "ui_confirm";

    public void OnPointerEnter(PointerEventData e) => AudioManager.Instance?.PlaySfx(moveSfx, 0.5f);
    public void OnSelect(BaseEventData e) => AudioManager.Instance?.PlaySfx(moveSfx, 0.5f);
    public void OnPointerClick(PointerEventData e) => AudioManager.Instance?.PlaySfx(clickSfx, 0.6f);
}
