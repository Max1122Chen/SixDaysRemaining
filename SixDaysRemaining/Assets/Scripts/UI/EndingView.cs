using SixDaysRemaining.Gameplay;
using SixDaysRemaining.App;
using SixDaysRemaining.Combat.Cards;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 结局展示：按 <see cref="GameState.endingId"/> 查 EndingContent 文案。
    /// </summary>
    public class EndingView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI endingText;

        [SerializeField]
        private TextMeshProUGUI dayText;

        [SerializeField]
        private Image backgroundImage;

        [SerializeField]
        private Image blackScreen;

        [SerializeField]
        private TextMeshProUGUI summaryText;

        [SerializeField]
        private Button menuButton;

        [Tooltip("背景图淡入时长（秒）")]
        [SerializeField]
        private float fadeDuration = 1f;

        [Tooltip("黑屏（含“第六天”）完整停留时长（秒）")]
        [SerializeField]
        private float blackHoldDuration = 1.5f;

        [Tooltip("背景图完全显示后停留的时长（秒）")]
        [SerializeField]
        private float holdDuration = 2f;

        [Tooltip("结局文字淡入时长（秒）")]
        [SerializeField]
        private float textFadeDuration = 1f;

        private AppFlowController flow;
        private Coroutine sequenceRoutine;
        private string playedEndingId;

        public static EndingView Build(Transform parent, AppFlowController flow)
        {
            GameObject panel = UiFactory.CreatePanel(parent, "EndingScreen", Color.black);
            EndingView view = panel.AddComponent<EndingView>();

            // 黑屏层放最底层，Txt_Day 作为它的子节点；背景图随后直接盖在黑屏上。
            GameObject blackGo = UiFactory.CreateImage(panel.transform, "BlackScreen", Vector2.zero, Vector2.zero, Color.black).gameObject;
            UiFactory.Stretch(blackGo.GetComponent<RectTransform>());
            view.blackScreen = blackGo.GetComponent<Image>();
            view.dayText = UiFactory.CreateText(blackGo.transform, "Txt_Day", "第六天", 44, new Vector2(0f, 260f), new Vector2(400f, 70f), TextAlignmentOptions.Center, Color.white);
            blackGo.SetActive(false);

            GameObject bgGo = UiFactory.CreateImage(panel.transform, "Img_Bg", Vector2.zero, Vector2.zero, Color.white).gameObject;
            UiFactory.Stretch(bgGo.GetComponent<RectTransform>());
            view.backgroundImage = bgGo.GetComponent<Image>();

            UiFactory.CreateText(panel.transform, "Txt_Title", "终局", 56, new Vector2(0f, 200f), new Vector2(600f, 80f), TextAlignmentOptions.Center, Color.white);
            view.endingText = UiFactory.CreateText(panel.transform, "Txt_Ending", "", 24, new Vector2(0f, 80f), new Vector2(800f, 120f), TextAlignmentOptions.Top);
            view.summaryText = UiFactory.CreateText(panel.transform, "Txt_Summary", "", 20, new Vector2(0f, -40f), new Vector2(800f, 60f), TextAlignmentOptions.Center);
            view.menuButton = UiFactory.CreateButton(panel.transform, "Btn_Menu", "返回主菜单", null, new Vector2(0f, -200f), new Vector2(220f, 56f), null, 22);

            view.Wire(flow);
            return view;
        }

        public void Wire(AppFlowController appFlow)
        {
            flow = appFlow;
            if (menuButton != null && flow != null)
            {
                menuButton.onClick.RemoveAllListeners();
                menuButton.onClick.AddListener(flow.OnBackToMenu);
            }
        }

        private void Awake()
        {
            // 场景里的 BlackScreen 默认关闭；只有结局开场动画期间才启用，避免运行时遮挡其他界面。
            if (blackScreen != null)
            {
                blackScreen.gameObject.SetActive(false);
            }
        }

        public void Refresh()
        {
            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            string endingId = gi != null && gi.Gameplay != null && gi.Gameplay.State != null
                ? gi.Gameplay.State.endingId
                : null;

            if (string.IsNullOrEmpty(endingId)
                && gi != null && gi.Gameplay != null && gi.Gameplay.State != null
                && gi.Gameplay.State.corruption >= CorruptedRules.FuseThreshold)
            {
                endingId = EndingIds.G;
            }

            if (endingText != null)
            {
                string body = EndingEvaluator.ResolveDisplayText(endingId);
                string criteria = EndingEvaluator.ResolveCriteriaText(endingId);
                if (!string.IsNullOrEmpty(criteria))
                {
                    body += "\n\n【达成条件】\n" + criteria;
                }

                endingText.text = body;
            }

            if (dayText != null)
            {
                dayText.text = "第六天";
            }

            if (summaryText != null)
            {
                if (gi != null && gi.Gameplay != null && gi.Gameplay.State != null)
                {
                    GameState state = gi.Gameplay.State;
                    summaryText.text = "第 " + state.day + " 天 · 腐蚀 " + state.corruption
                        + (string.IsNullOrEmpty(endingId) ? "" : " · " + endingId);
                }
                else
                {
                    summaryText.text = string.Empty;
                }
            }

            PlaySequence(endingId);
        }

        private void OnDisable()
        {
            // 面板隐藏后重置，下次再进入结局屏时重新播放入场动画。
            playedEndingId = null;
            if (blackScreen != null)
            {
                blackScreen.gameObject.SetActive(false);
            }
        }

        private void PlaySequence(string endingId)
        {
            if (string.Equals(playedEndingId, endingId, System.StringComparison.Ordinal))
            {
                return;
            }

            playedEndingId = endingId;
            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
            }

            sequenceRoutine = StartCoroutine(SequenceRoutine(endingId));
        }

        private IEnumerator SequenceRoutine(string endingId)
        {
            // 1. 黑屏：面板自身背景先透明，让底层 BlackScreen 的纯黑与 Txt_Day 透出来；背景图与结局文字先隐藏。
            Image panelImage = GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.CrossFadeAlpha(0f, 0f, true);
            }

            if (blackScreen != null)
            {
                blackScreen.gameObject.SetActive(true);
                blackScreen.color = Color.black;
                blackScreen.raycastTarget = true;
            }

            if (backgroundImage != null)
            {
                backgroundImage.sprite = LoadEndingSprite(endingId);
                backgroundImage.CrossFadeAlpha(0f, 0f, true);
            }

            if (endingText != null)
            {
                endingText.CrossFadeAlpha(0f, 0f, true);
            }

            FadeButton(0f, 0f);
            yield return null;

            // 黑屏完整展示 blackHoldDuration 秒后，背景图才开始淡入。
            yield return new WaitForSecondsRealtime(blackHoldDuration);

            // 2. 背景图直接在黑屏上方淡入，黑屏保持不动；图片盖满后隐藏 BlackScreen（连同 Txt_Day 一起被盖住）。
            if (backgroundImage != null)
            {
                backgroundImage.CrossFadeAlpha(1f, fadeDuration, true);
            }

            yield return new WaitForSecondsRealtime(fadeDuration);
            if (blackScreen != null)
            {
                blackScreen.gameObject.SetActive(false);
            }

            // 图片完整展示 holdDuration 秒。
            yield return new WaitForSecondsRealtime(holdDuration);

            // 3. 结局文字淡入，叠加在背景图上；返回按钮随文字一起出现。
            if (endingText != null)
            {
                endingText.CrossFadeAlpha(1f, textFadeDuration, true);
            }

            FadeButton(1f, textFadeDuration);
        }

        private void FadeButton(float alpha, float duration)
        {
            if (menuButton == null)
            {
                return;
            }

            if (menuButton.targetGraphic != null)
            {
                menuButton.targetGraphic.CrossFadeAlpha(alpha, duration, true);
            }

            TextMeshProUGUI label = menuButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.CrossFadeAlpha(alpha, duration, true);
            }
        }

        /// <summary>
        /// 从 Assets/Resources/Ending 加载对应结局背景图：
        /// Ending.A -> Ending/Ending_A，以此类推；未知 id 回退到 Ending_A。
        /// </summary>
        private static Sprite LoadEndingSprite(string endingId)
        {
            string letter = string.IsNullOrEmpty(endingId) ? null : endingId.Substring(endingId.LastIndexOf('.') + 1);
            if (!string.IsNullOrEmpty(letter) && letter.Length == 1 && letter[0] >= 'A' && letter[0] <= 'Z')
            {
                Sprite sprite = Resources.Load<Sprite>("Ending/Ending_" + letter);
                if (sprite != null)
                {
                    return sprite;
                }

                Debug.LogWarning("[EndingView] 未找到背景图 Resources/Ending/Ending_" + letter + ".png，使用 Ending_A 兜底。");
            }

            return Resources.Load<Sprite>("Ending/Ending_A");
        }
    }
}
