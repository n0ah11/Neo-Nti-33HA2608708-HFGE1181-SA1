using UnityEngine;
using UnityEngine.EventSystems;

/// SA1 addition: buttons grow slightly on hover/press so menus feel responsive (works with touch too).
public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private float target = 1f;

    private void OnEnable() { target = 1f; transform.localScale = Vector3.one; }
    private void Update()
    {
        float s = Mathf.Lerp(transform.localScale.x, target, 15f * Time.unscaledDeltaTime);
        transform.localScale = new Vector3(s, s, 1f);
    }
    public void OnPointerEnter(PointerEventData e) { target = 1.08f; }
    public void OnPointerExit(PointerEventData e) { target = 1f; }
    public void OnPointerDown(PointerEventData e) { target = 0.94f; }
    public void OnPointerUp(PointerEventData e) { target = 1.08f; }
}
