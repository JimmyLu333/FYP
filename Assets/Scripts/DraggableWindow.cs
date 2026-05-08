using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("拖动目标（整个窗口）")]
    public RectTransform windowToDrag;

    private Vector2 offset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (windowToDrag == null) return;

        windowToDrag.SetAsLastSibling();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            windowToDrag,
            eventData.position,
            eventData.pressEventCamera,
            out offset
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (windowToDrag == null) return;

        RectTransform parentRect = windowToDrag.parent as RectTransform;
        if (parentRect == null) return;

        Vector2 localPointerPos;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPointerPos))
        {
            windowToDrag.localPosition = localPointerPos - offset;
        }
    }
}