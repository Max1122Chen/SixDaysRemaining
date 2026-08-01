using System.Collections;
using System.Collections.Generic;
using SixDaysRemaining.Bootstrap;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 战斗主界面：手牌拖入卡槽、卡槽之间换位、拖出取消、回合切换横幅与怪物预告。
    /// 卡牌摆放只通过 PlayerCombatComponent 的选择 API 同步，不在这里重写战斗公式。
    /// </summary>
    public class CombatView : MonoBehaviour
    {
        private const int SlotCount = 5;
        private static readonly Vector2 HandSize = new Vector2(140f, 190f);
        private static readonly Vector2 SlotSize = new Vector2(150f, 200f);

        private AppFlowController flow;

        [SerializeField]
        private RectTransform cardLayer;

        [SerializeField]
        private TextMeshProUGUI playerStatusText;

        [SerializeField]
        private TextMeshProUGUI slotCountText;

        [SerializeField]
        private Button commitButton;

        [SerializeField]
        private Button clearButton;

        [SerializeField]
        private Button fleeButton;

        [SerializeField]
        private Button settingsButton;

        [SerializeField]
        private CanvasGroup bannerGroup;

        [SerializeField]
        private TextMeshProUGUI bannerText;

        [SerializeField]
        private EnemyPreviewView enemyPreview;

        private readonly List<CardView> handCards = new List<CardView>();
        private readonly CardView[] slotCards = new CardView[SlotCount];

        [SerializeField]
        private CardSlotView[] slots = new CardSlotView[SlotCount];

        private CardSlotView hoveredSlot;
        private bool inputEnabled = true;

        public static CombatView Build(Transform parent, AppFlowController flow)
        {
            GameObject panel = UiFactory.CreatePanel(parent, "CombatScreen", new Color(0.07f, 0.08f, 0.10f, 0.98f));
            CombatView view = panel.AddComponent<CombatView>();

            view.enemyPreview = EnemyPreviewView.Build(panel.transform, new Vector2(620f, 360f), new Vector2(420f, 220f));
            view.playerStatusText = UiFactory.CreateText(panel.transform, "Txt_PlayerStatus", "我方", 22, new Vector2(-620f, 360f), new Vector2(420f, 60f), TextAlignmentOptions.Left);

            GameObject layerGo = UiFactory.CreatePanel(panel.transform, "CardLayer", new Color(0f, 0f, 0f, 0f));
            layerGo.GetComponent<Image>().raycastTarget = false;
            view.cardLayer = layerGo.GetComponent<RectTransform>();

            for (int i = 0; i < SlotCount; i++)
            {
                view.slots[i] = CardSlotView.Create(view.cardLayer, i, SlotPos(i), SlotSize);
            }

            TextMeshProUGUI handLabel = UiFactory.CreateText(panel.transform, "Txt_HandLabel", "手牌（拖入下方卡槽）", 16, new Vector2(0f, -320f), new Vector2(600f, 30f));
            handLabel.color = new Color(0.75f, 0.78f, 0.82f, 1f);
            view.slotCountText = UiFactory.CreateText(panel.transform, "Txt_SlotCount", "0/5", 22, new Vector2(0f, 190f), new Vector2(120f, 30f));

            view.commitButton = UiFactory.CreateButton(panel.transform, "Btn_Commit", "结算", null, new Vector2(-160f, -450f), new Vector2(140f, 52f), null, 22);
            view.clearButton = UiFactory.CreateButton(panel.transform, "Btn_Clear", "清空", null, new Vector2(0f, -450f), new Vector2(120f, 48f), new Color(0.30f, 0.34f, 0.42f, 1f), 20);
            view.fleeButton = UiFactory.CreateButton(panel.transform, "Btn_Flee", "撤退", null, new Vector2(160f, -450f), new Vector2(120f, 48f), UiFactory.Danger, 20);
            view.settingsButton = UiFactory.CreateButton(panel.transform, "Btn_Settings", "设置", null, new Vector2(700f, -450f), new Vector2(110f, 44f), new Color(0.22f, 0.25f, 0.30f, 1f), 18);

            view.bannerGroup = BuildBanner(panel.transform, out view.bannerText);
            view.Wire(flow);
            return view;
        }

        /// <summary>
        /// 手动搭建场景时在 Inspector 拖好引用后调用；会按数组顺序修正卡槽 Index。
        /// </summary>
        public void Wire(AppFlowController flow)
        {
            this.flow = flow;
            WireButton(commitButton, OnCommit);
            WireButton(clearButton, OnClear);
            WireButton(fleeButton, OnFlee);
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(flow.ShowSettings);
            }

            ConfigureRaycastTargets();

            // 手动搭建场景时如果没有自己放 TurnBanner，自动补一个，避免 OpenCombat 崩溃。
            if (bannerGroup == null || bannerText == null)
            {
                bannerGroup = BuildBanner(transform, out bannerText);
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].Setup(i);
                }
            }
        }

        private void ConfigureRaycastTargets()
        {
            if (playerStatusText != null)
            {
                playerStatusText.raycastTarget = false;
            }

            if (slotCountText != null)
            {
                slotCountText.raycastTarget = false;
            }

            if (cardLayer != null)
            {
                Image layerImage = cardLayer.GetComponent<Image>();
                if (layerImage != null)
                {
                    layerImage.raycastTarget = false;
                }
            }

            if (enemyPreview != null)
            {
                Image background = enemyPreview.GetComponent<Image>();
                if (background != null)
                {
                    background.raycastTarget = false;
                }

                TextMeshProUGUI[] labels = enemyPreview.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i].raycastTarget = false;
                }
            }
        }

        private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static CanvasGroup BuildBanner(Transform parent, out TextMeshProUGUI text)
        {
            GameObject go = UiFactory.CreatePanel(parent, "TurnBanner", new Color(0.05f, 0.06f, 0.09f, 0.95f), false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 220f);
            rt.sizeDelta = new Vector2(640f, 72f);
            CanvasGroup group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            go.SetActive(false);
            text = UiFactory.CreateText(go.transform, "Txt_Banner", "", 28, Vector2.zero, new Vector2(620f, 60f));
            text.color = Color.white;
            return group;
        }

        public void OpenCombat()
        {
            // 上一场战斗结算时输入会被关掉，这里在每轮战斗开始前复位。
            inputEnabled = true;
            Refresh();
            ShowBanner("战斗开始");
        }

        public void Refresh()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null)
            {
                return;
            }

            PlayerCombatComponent player = gi.Combat.Session.Player;
            EnemyCombatComponent enemy = gi.Combat.Session.Enemies.Count > 0
                ? gi.Combat.Session.Enemies[0]
                : null;
            enemyPreview.Bind(enemy);

            playerStatusText.text = "我方  HP " + CardText.FormatNumber(player.Attributes.HP)
                + "/" + CardText.FormatNumber(player.Attributes.MaxHP)
                + "  格挡 " + CardText.FormatNumber(player.Attributes.Block);
            enemyPreview.Refresh(gi.Combat.IsPlayerTurn);

            RebuildCards(player);
            UpdateButtons();
        }

        private void RebuildCards(PlayerCombatComponent player)
        {
            DestroyCards();
            handCards.Clear();
            for (int i = 0; i < slotCards.Length; i++)
            {
                slotCards[i] = null;
            }

            IReadOnlyList<CardInstance> hand = player.Deck.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                CardView card = CardView.Create(cardLayer, hand[i], HandSize, CurrentSlotSize());
                card.Rect.anchoredPosition = HandPos(i, hand.Count);
                card.DragBegan += OnCardDragBegan;
                card.DragMoved += OnCardDragMoved;
                card.DragEnded += OnCardDragEnded;
                card.SetInteractable(inputEnabled);
                handCards.Add(card);
            }

            IReadOnlyList<CardInstance> selection = player.Deck.Selection;
            for (int i = 0; i < selection.Count && i < SlotCount; i++)
            {
                CardView card = FindCard(selection[i]);
                if (card == null)
                {
                    continue;
                }

                handCards.Remove(card);
                slotCards[i] = card;
                card.InSlot = true;
                card.SnapTo(CurrentSlotPos(i), CurrentSlotSize());
            }

            UpdateHandLayout(false);
            UpdateSlotCount();
        }

        private void DestroyCards()
        {
            for (int i = handCards.Count - 1; i >= 0; i--)
            {
                if (handCards[i] != null)
                {
                    Destroy(handCards[i].gameObject);
                }
            }

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] != null)
                {
                    Destroy(slotCards[i].gameObject);
                    slotCards[i] = null;
                }
            }
        }

        private void OnCardDragBegan(CardView card, Vector2 pointer)
        {
            ClearSlotHighlight();
        }

        private void OnCardDragMoved(CardView card, Vector2 pointer)
        {
            CardSlotView slot = RaycastSlot(pointer);
            if (hoveredSlot != slot)
            {
                ClearSlotHighlight();
                hoveredSlot = slot;
                if (hoveredSlot != null)
                {
                    hoveredSlot.SetHighlight(true);
                }
            }
        }

        private void OnCardDragEnded(CardView card, Vector2 pointer)
        {
            ClearSlotHighlight();
            // 拖拽时 Rect 的位置是屏幕坐标；先把当前落点换算回 CardLayer 本地坐标，
            // 后续动画才能从“松手位置”流畅滑向卡槽/手牌，而不是先跳回原位。
            if (card.Rect != null && cardLayer != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    cardLayer,
                    card.Rect.position,
                    null,
                    out Vector2 localPoint);
                card.Rect.anchoredPosition = localPoint;
            }

            CardSlotView target = RaycastSlot(pointer);
            if (target != null)
            {
                TryPlace(card, target);
            }
            else if (card.InSlot)
            {
                CancelFromSlot(card);
            }
            else
            {
                SnapOrAnimateToHand(card);
            }

            UpdateButtons();
        }

        private CardSlotView RaycastSlot(Vector2 screenPos)
        {
            if (EventSystem.current == null)
            {
                return null;
            }

            PointerEventData data = new PointerEventData(EventSystem.current);
            data.position = screenPos;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);
            for (int i = 0; i < results.Count; i++)
            {
                CardSlotView slot = results[i].gameObject.GetComponentInParent<CardSlotView>();
                if (slot != null)
                {
                    return slot;
                }
            }

            return null;
        }

        private void TryPlace(CardView card, CardSlotView target)
        {
            if (!card.InSlot)
            {
                int targetIndex = target.Index;
                if (slotCards[targetIndex] != null)
                {
                    targetIndex = FirstEmptySlot();
                }

                if (targetIndex < 0)
                {
                    Reject(card);
                    return;
                }

                handCards.Remove(card);
                slotCards[targetIndex] = card;
                card.AnimateToSlot(targetIndex, CurrentSlotPos(targetIndex));
            }
            else
            {
                int from = IndexOfSlot(card);
                int to = target.Index;
                if (from == to)
                {
                    card.AnimateToSlot(from, CurrentSlotPos(from));
                    return;
                }

                CardView other = slotCards[to];
                slotCards[to] = card;
                slotCards[from] = other;
                card.AnimateToSlot(to, CurrentSlotPos(to));
                if (other != null)
                {
                    other.AnimateToSlot(from, CurrentSlotPos(from));
                }
            }

            SyncSelection();
            UpdateHandLayout(true);
            UpdateSlotCount();
        }

        private void CancelFromSlot(CardView card)
        {
            int index = IndexOfSlot(card);
            if (index < 0)
            {
                return;
            }

            slotCards[index] = null;
            handCards.Add(card);
            SyncSelection();
            UpdateHandLayout(true);
            UpdateSlotCount();
        }

        private void SnapOrAnimateToHand(CardView card)
        {
            int index = handCards.IndexOf(card);
            if (index < 0)
            {
                return;
            }

            card.AnimateBackToHand(index, handCards.Count, HandPos(index, handCards.Count));
        }

        private void Reject(CardView card)
        {
            if (card.InSlot)
            {
                int index = IndexOfSlot(card);
                if (index >= 0)
                {
                    card.SnapTo(CurrentSlotPos(index), CurrentSlotSize());
                }
            }
            else
            {
                int index = handCards.IndexOf(card);
                if (index >= 0)
                {
                    card.SnapTo(HandPos(index, handCards.Count), HandSize);
                }
            }

            StartCoroutine(UiAnim.Shake(card.Rect, 0.25f, 12f));
        }

        private void SyncSelection()
        {
            GameInstance gi = flow.Game;
            PlayerCombatComponent player = gi.Combat.Session.Player;
            player.ClearSelection();
            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] == null)
                {
                    continue;
                }

                int handIndex = IndexOfCard(player.Deck.Hand, slotCards[i].Card);
                if (handIndex >= 0)
                {
                    player.SelectFromHand(handIndex);
                }
            }
        }

        private void UpdateHandLayout(bool animated)
        {
            int count = handCards.Count;
            for (int i = 0; i < handCards.Count; i++)
            {
                Vector2 pos = HandPos(i, count);
                if (animated)
                {
                    handCards[i].AnimateBackToHand(i, count, pos);
                }
                else
                {
                    handCards[i].SnapTo(pos, HandSize);
                }
            }
        }

        private void UpdateSlotCount()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null)
            {
                return;
            }

            int count = gi.Combat.Session.Player.Deck.Selection.Count;
            slotCountText.text = count + "/" + PlayerCombatComponent.CommitCount;
        }

        private void UpdateButtons()
        {
            GameInstance gi = flow.Game;
            bool playerTurn = gi != null && gi.Combat != null && gi.Combat.IsPlayerTurn && !gi.Combat.IsFinished;
            int count = gi != null && gi.Combat != null && gi.Combat.Session != null
                ? gi.Combat.Session.Player.Deck.Selection.Count
                : 0;
            commitButton.interactable = playerTurn && count == PlayerCombatComponent.CommitCount;
            clearButton.interactable = playerTurn && count > 0;
            fleeButton.interactable = playerTurn;
        }

        private void OnCommit()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null || !gi.Combat.IsPlayerTurn)
            {
                return;
            }

            if (gi.Combat.Session.Player.Deck.Selection.Count != PlayerCombatComponent.CommitCount)
            {
                ShowBanner("需要选满 5 张牌");
                return;
            }

            StartCoroutine(CommitRoutine());
        }

        private IEnumerator CommitRoutine()
        {
            SetInputEnabled(false);
            ShowBanner("结算中…");
            yield return new WaitForSecondsRealtime(0.45f);

            GameInstance gi = flow.Game;
            EnemyCombatComponent enemy = gi.Combat.Session.Enemies[0];
            gi.PlayerCombat.CommitPlay(enemy);
            gi.Combat.NotifyPlayerCommitted();
            if (gi.Combat.IsFinished)
            {
                flow.OnCombatFinished(gi.Combat.Result);
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.6f);
            Refresh();
            ShowBanner("你的回合");
            SetInputEnabled(true);
        }

        private void OnClear()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null)
            {
                return;
            }

            gi.PlayerCombat.ClearSelection();
            Refresh();
            ShowBanner("已清空");
        }

        private void OnFlee()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null)
            {
                return;
            }

            if (gi.Combat.Flee() && gi.Combat.IsFinished)
            {
                flow.OnCombatFinished(gi.Combat.Result);
            }
        }

        private void SetInputEnabled(bool on)
        {
            inputEnabled = on;
            for (int i = 0; i < handCards.Count; i++)
            {
                handCards[i].SetInteractable(on);
            }

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] != null)
                {
                    slotCards[i].SetInteractable(on);
                }
            }

            UpdateButtons();
        }

        private Coroutine bannerRoutine;

        private void ShowBanner(string text)
        {
            if (bannerText == null || bannerGroup == null)
            {
                return;
            }

            bannerText.text = text;
            if (bannerRoutine != null)
            {
                StopCoroutine(bannerRoutine);
            }

            bannerRoutine = StartCoroutine(BannerRoutine());
        }

        private IEnumerator BannerRoutine()
        {
            bannerGroup.gameObject.SetActive(true);
            bannerGroup.alpha = 0f;
            yield return UiAnim.Fade(bannerGroup, 1f, 0.15f);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return UiAnim.Fade(bannerGroup, 0f, 0.2f);
            bannerGroup.gameObject.SetActive(false);
        }

        private void ClearSlotHighlight()
        {
            if (hoveredSlot != null)
            {
                hoveredSlot.SetHighlight(false);
                hoveredSlot = null;
            }
        }

        private int FirstEmptySlot()
        {
            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        private int IndexOfSlot(CardView card)
        {
            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] == card)
                {
                    return i;
                }
            }

            return -1;
        }

        private CardView FindCard(CardInstance card)
        {
            for (int i = 0; i < handCards.Count; i++)
            {
                if (handCards[i].Card == card)
                {
                    return handCards[i];
                }
            }

            return null;
        }

        private static int IndexOfCard(IReadOnlyList<CardInstance> hand, CardInstance card)
        {
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i] == card)
                {
                    return i;
                }
            }

            return -1;
        }

        private Vector2 CurrentSlotPos(int index)
        {
            if (slots != null && index >= 0 && index < slots.Length && slots[index] != null && slots[index].Rect != null)
            {
                return slots[index].Rect.anchoredPosition;
            }

            return SlotPos(index);
        }

        private Vector2 CurrentSlotSize()
        {
            if (slots != null && slots.Length > 0 && slots[0] != null && slots[0].Rect != null)
            {
                return slots[0].Rect.sizeDelta;
            }

            return SlotSize;
        }

        private static Vector2 HandPos(int index, int count)
        {
            float spacing = 150f;
            float total = (count - 1) * spacing;
            return new Vector2(-total * 0.5f + index * spacing, -380f);
        }

        private static Vector2 SlotPos(int index)
        {
            return new Vector2(-300f + index * 150f, 40f);
        }
    }
}
