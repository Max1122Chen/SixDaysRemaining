using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 悬停描边高亮：给按钮 Image 挂 Outline，鼠标进入时描边淡入、移出后淡出。
    /// 用于战斗界面「开始战斗」等按钮的选中态反馈，不依赖场景手工配置。
    /// </summary>
    public class HoverOutline : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField]
        private Outline outline;

        [SerializeField]
        private Button button;

        [SerializeField]
        private Color outlineColor = new Color(1f, 0.84f, 0.35f, 1f);

        [SerializeField]
        private float fadeSpeed = 10f;

        private float currentAlpha;
        private float targetAlpha;

        public static HoverOutline Attach(Button button, Color color)
        {
            if (button == null)
            {
                return null;
            }

            Image image = button.GetComponent<Image>();
            if (image == null)
            {
                return null;
            }

            Outline outline = image.GetComponent<Outline>();
            if (outline == null)
            {
                outline = image.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(color.r, color.g, color.b, 0f);
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = true;

            HoverOutline hover = image.GetComponent<HoverOutline>();
            if (hover == null)
            {
                hover = image.gameObject.AddComponent<HoverOutline>();
            }

            hover.outline = outline;
            hover.outlineColor = color;
            hover.button = button;
            return hover;
        }

        private void OnEnable()
        {
            if (outline != null)
            {
                outline.enabled = true;
                ApplyAlpha();
            }
        }

        private void Update()
        {
            if (outline == null)
            {
                return;
            }

            currentAlpha = Mathf.MoveTowards(
                currentAlpha,
                targetAlpha,
                Time.unscaledDeltaTime * fadeSpeed);

            ApplyAlpha();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null && !button.interactable)
            {
                targetAlpha = 0f;
                return;
            }

            targetAlpha = 1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetAlpha = 0f;
        }

        private void ApplyAlpha()
        {
            Color c = outline.effectColor;
            c.a = currentAlpha;
            outline.effectColor = c;
        }
    }
}
