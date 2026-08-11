using UnityEngine;
using UnityEngine.EventSystems;

namespace SixDaysRemaining.Debugging
{
    public class DebugDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public RectTransform Target;

        private Vector2 pointerOffset;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Target == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Target,
                eventData.position,
                eventData.pressEventCamera,
                out pointerOffset);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null || Target.parent == null)
            {
                return;
            }

            RectTransform parentRect = Target.parent as RectTransform;
            if (parentRect == null)
            {
                return;
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPoint))
            {
                return;
            }

            Target.anchoredPosition = localPoint - pointerOffset;
        }
    }
}
