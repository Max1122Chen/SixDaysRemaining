using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 庇护所顶部弹幕：从右侧横向滚过并在左侧消失，用于死亡/逃离公告。
    /// </summary>
    public class ShelterBulletBanner : MonoBehaviour
    {
        private const float Speed = 340f;
        private const float StartX = 1180f;
        private const float EndX = -1180f;

        private RectTransform rect;

        public static ShelterBulletBanner Spawn(Transform parent, string message, Color color, int index)
        {
            GameObject go = new GameObject("Bullet_" + index);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(540f, 48f);
            rt.anchoredPosition = new Vector2(StartX, 376f - index * 52f);

            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.62f);
            bg.raycastTarget = false;

            TextMeshProUGUI label = UiFactory.CreateText(
                go.transform,
                "Label",
                message,
                22,
                new Vector2(12f, 0f),
                new Vector2(516f, 40f),
                TextAlignmentOptions.Left,
                color);
            label.raycastTarget = false;

            ShelterBulletBanner banner = go.AddComponent<ShelterBulletBanner>();
            banner.rect = rt;
            return banner;
        }

        private void Update()
        {
            if (rect == null)
            {
                return;
            }

            Vector2 pos = rect.anchoredPosition;
            pos.x -= Speed * Time.unscaledDeltaTime;
            rect.anchoredPosition = pos;
            if (pos.x < EndX)
            {
                Destroy(gameObject);
            }
        }
    }
}
