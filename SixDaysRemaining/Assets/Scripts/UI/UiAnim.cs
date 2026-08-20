using System.Collections;
using UnityEngine;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 轻量动画工具：不依赖 DOTween，先满足“基础动画反馈”的原型需求。
    /// </summary>
    public static class UiAnim
    {
        private static readonly AnimationCurve Ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public static IEnumerator Move(RectTransform rt, Vector2 to, float duration)
        {
            if (rt == null)
            {
                yield break;
            }

            Vector2 from = rt.anchoredPosition;
            float t = 0f;
            while (t < duration)
            {
                if (rt == null)
                {
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                float k = duration <= 0f ? 1f : Ease.Evaluate(Mathf.Clamp01(t / duration));
                rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
                yield return null;
            }

            if (rt == null)
            {
                yield break;
            }

            rt.anchoredPosition = to;
        }

        public static IEnumerator MoveAndResize(RectTransform rt, Vector2 toPos, Vector2 toSize, float duration)
        {
            if (rt == null)
            {
                yield break;
            }

            Vector2 fromPos = rt.anchoredPosition;
            Vector2 fromSize = rt.sizeDelta;
            float t = 0f;
            while (t < duration)
            {
                if (rt == null)
                {
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                float k = duration <= 0f ? 1f : Ease.Evaluate(Mathf.Clamp01(t / duration));
                rt.anchoredPosition = Vector2.LerpUnclamped(fromPos, toPos, k);
                rt.sizeDelta = Vector2.LerpUnclamped(fromSize, toSize, k);
                yield return null;
            }

            if (rt == null)
            {
                yield break;
            }

            rt.anchoredPosition = toPos;
            rt.sizeDelta = toSize;
        }

        public static IEnumerator Scale(RectTransform rt, Vector3 to, float duration)
        {
            if (rt == null)
            {
                yield break;
            }

            Vector3 from = rt.localScale;
            float t = 0f;
            while (t < duration)
            {
                if (rt == null)
                {
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                float k = duration <= 0f ? 1f : Ease.Evaluate(Mathf.Clamp01(t / duration));
                rt.localScale = Vector3.LerpUnclamped(from, to, k);
                yield return null;
            }

            if (rt == null)
            {
                yield break;
            }

            rt.localScale = to;
        }

        public static IEnumerator Rotate(RectTransform rt, float toAngleDeg, float duration)
        {
            if (rt == null)
            {
                yield break;
            }

            Quaternion from = rt.localRotation;
            Quaternion to = Quaternion.Euler(0f, 0f, toAngleDeg);
            float t = 0f;
            while (t < duration)
            {
                if (rt == null)
                {
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                float k = duration <= 0f ? 1f : Ease.Evaluate(Mathf.Clamp01(t / duration));
                rt.localRotation = Quaternion.SlerpUnclamped(from, to, k);
                yield return null;
            }

            if (rt == null)
            {
                yield break;
            }

            rt.localRotation = to;
        }

        public static IEnumerator Fade(CanvasGroup group, float to, float duration)
        {
            if (group == null)
            {
                yield break;
            }

            float from = group.alpha;
            float t = 0f;
            while (t < duration)
            {
                if (group == null)
                {
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                float k = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
                group.alpha = Mathf.Lerp(from, to, k);
                yield return null;
            }

            if (group == null)
            {
                yield break;
            }

            group.alpha = to;
        }

        public static IEnumerator Shake(RectTransform rt, float duration, float amount)
        {
            if (rt == null)
            {
                yield break;
            }

            Vector2 origin = rt.anchoredPosition;
            float t = 0f;
            while (t < duration)
            {
                if (rt == null)
                {
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                float k = t / duration;
                rt.anchoredPosition = origin + (Random.insideUnitCircle * amount * (1f - k));
                yield return null;
            }

            if (rt == null)
            {
                yield break;
            }

            rt.anchoredPosition = origin;
        }
    }
}
