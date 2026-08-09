using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SixDaysRemaining.UI {
    /// <summary>
    /// 艺术字按钮果冻效果：
    /// 1. Idle 上下浮动
    /// 2. Hover Sprite Alpha Glow
    /// 3. Press 弹性缩放
    /// </summary>
    public class JuicyButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler {

        [Header("Idle Float")]
        [SerializeField]
        private float idleAmplitude = 4f;

        [SerializeField]
        private float idleSpeed = 1.6f;

        [SerializeField]
        private float idlePhase = 0f;



        [Header("Sprite Glow")]
        [SerializeField]
        private float hoverGlowStrength = 1.2f;

        [SerializeField]
        private float glowFadeSpeed = 8f;



        [Header("Elastic Press")]
        [SerializeField]
        private float squashAmount = 0.12f;

        [SerializeField]
        private float squashDuration = 0.08f;

        [SerializeField]
        private float springDuration = 0.42f;



        private RectTransform rect;

        private Button button;

        private Vector2 basePosition;


        private Material glowMaterial;


        private Coroutine pressRoutine;


        private bool pressed;


        private float currentGlow;


        private float targetGlow;



        public static JuicyButton Attach(Button target) {
            if (target == null) {
                return null;
            }


            JuicyButton juicy =
                target.GetComponent<JuicyButton>();


            if (juicy == null) {
                juicy =
                    target.gameObject.AddComponent<JuicyButton>();
            }


            return juicy;
        }



        public JuicyButton SetIdle(
            float amplitude,
            float speed,
            float phase) {
            idleAmplitude = amplitude;
            idleSpeed = speed;
            idlePhase = phase;

            return this;
        }



        public JuicyButton SetGlow(
            Color color,
            float strength,
            float unused) {
            hoverGlowStrength = strength;

            if (glowMaterial != null) {
                glowMaterial.SetColor(
                    "_GlowColor",
                    color
                );
            }

            return this;
        }



        public JuicyButton SetSquash(
            float amount,
            float squashTime,
            float springTime) {
            squashAmount = amount;
            squashDuration = squashTime;
            springDuration = springTime;

            return this;
        }



        private void Awake() {
            rect =
                GetComponent<RectTransform>();

            button =
                GetComponent<Button>();


            Image img =
                GetComponent<Image>();


            if (img != null && img.material != null) {
                glowMaterial =
                    new Material(img.material);


                img.material =
                    glowMaterial;


                glowMaterial.SetFloat(
                    "_GlowStrength",
                    0f
                );
            }
        }



        private void OnEnable() {
            if (rect == null) {
                rect =
                    GetComponent<RectTransform>();
            }


            basePosition =
                rect.anchoredPosition;


            rect.localScale =
                Vector3.one;


            pressed = false;


            currentGlow = 0;

            targetGlow = 0;
        }



        private void Update() {

            if (rect == null)
                return;



            // Idle浮动

            if (!pressed) {

                float offset =
                    Mathf.Sin(
                        Time.unscaledTime *
                        idleSpeed +
                        idlePhase
                    )
                    *
                    idleAmplitude;



                Vector2 pos =
                    basePosition;


                pos.y += offset;


                rect.anchoredPosition =
                    pos;
            }



            // Glow平滑过渡

            if (glowMaterial != null) {

                currentGlow =
                    Mathf.Lerp(
                        currentGlow,
                        targetGlow,
                        Time.unscaledDeltaTime *
                        glowFadeSpeed
                    );


                glowMaterial.SetFloat(
                    "_GlowStrength",
                    currentGlow
                );
            }

        }



        public void OnPointerEnter(
            PointerEventData eventData) {

            if (!CanReact())
                return;


            targetGlow =
                hoverGlowStrength;
        }



        public void OnPointerExit(
            PointerEventData eventData) {

            targetGlow = 0;



            if (pressed) {
                pressed = false;

                SpringBack();
            }
        }



        public void OnPointerDown(
            PointerEventData eventData) {

            if (!CanReact())
                return;


            pressed = true;


            if (pressRoutine != null) {
                StopCoroutine(pressRoutine);
            }


            pressRoutine =
                StartCoroutine(
                    PressRoutine()
                );
        }



        public void OnPointerUp(
            PointerEventData eventData) {

            if (!pressed)
                return;


            pressed = false;


            SpringBack();
        }



        private bool CanReact() {
            return button == null ||
                   button.interactable;
        }



        private IEnumerator PressRoutine() {

            Vector3 from =
                rect.localScale;



            Vector3 squash =
                new Vector3(
                    1f + squashAmount,
                    1f - squashAmount,
                    1f
                );



            float t = 0;



            while (
                t < squashDuration &&
                pressed) {

                t +=
                    Time.unscaledDeltaTime;


                float k =
                    Mathf.Clamp01(
                        t / squashDuration
                    );


                rect.localScale =
                    Vector3.LerpUnclamped(
                        from,
                        squash,
                        EaseOutQuad(k)
                    );


                yield return null;
            }



            if (pressed) {
                rect.localScale =
                    squash;
            }


            pressRoutine = null;
        }



        private void SpringBack() {

            if (pressRoutine != null) {
                StopCoroutine(
                    pressRoutine
                );

                pressRoutine = null;
            }



            pressRoutine =
                StartCoroutine(
                    SpringRoutine()
                );
        }



        private IEnumerator SpringRoutine() {

            Vector3 from =
                rect.localScale;



            float t = 0;



            while (
                t < springDuration) {

                t +=
                    Time.unscaledDeltaTime;



                float k =
                    Mathf.Clamp01(
                        t / springDuration
                    );


                rect.localScale =
                    Vector3.LerpUnclamped(
                        from,
                        Vector3.one,
                        BackOut(k)
                    );


                yield return null;
            }



            rect.localScale =
                Vector3.one;


            pressRoutine = null;
        }



        private static float EaseOutQuad(float t) {
            return 1f -
                   (1f - t) *
                   (1f - t);
        }



        private static float BackOut(float t) {

            float c1 =
                1.70158f;


            float c3 =
                c1 + 1f;


            t -= 1f;


            return
                1f +
                c3 * t * t * t +
                c1 * t * t;
        }

    }
}