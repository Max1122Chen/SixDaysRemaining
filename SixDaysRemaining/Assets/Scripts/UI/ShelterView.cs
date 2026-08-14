using System.Collections.Generic;
using SixDaysRemaining.App;
using SixDaysRemaining.Combat.Traits;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 庇护所界面：每日分配、住户卡片、住户详情、道具占位、腐蚀蒙版与弹幕公告。
    /// 场景里的 ShelterPanel 会在运行时按此布局重建，后续可整体替换为 Prefab。
    /// </summary>
    public class ShelterView : MonoBehaviour
    {
        private AppFlowController flow;
        private bool layoutBuilt;
        private string selectedDefId;

        // 保留场景旧序列化字段，避免 MainScene 反序列化告警；运行时会被重建布局覆盖。
        [SerializeField]
        private TextMeshProUGUI statusText;

        [SerializeField]
        private TextMeshProUGUI survivorText;

        [SerializeField]
        private Button departButton;

        [SerializeField]
        private Button settingsButton;

        [SerializeField]
        private Button menuButton;

        [SerializeField]
        private Button dayEndButton;

        [SerializeField]
        private TextMeshProUGUI dayText;

        [SerializeField]
        private TextMeshProUGUI phaseText;

        [SerializeField]
        private TextMeshProUGUI foodValueText;

        [SerializeField]
        private TextMeshProUGUI corruptionValueText;

        [SerializeField]
        private TextMeshProUGUI populationValueText;

        [SerializeField]
        private RectTransform residentRow;

        [SerializeField]
        private TextMeshProUGUI propsStateText;

        [SerializeField]
        private GameObject detailGroup;

        [SerializeField]
        private TextMeshProUGUI detailNameText;

        [SerializeField]
        private TextMeshProUGUI detailStatusText;

        [SerializeField]
        private TextMeshProUGUI detailTraitsText;

        [SerializeField]
        private TextMeshProUGUI detailMessageText;

        [SerializeField]
        private Button closeDetailButton;

        [SerializeField]
        private Image fogOverlay;

        [SerializeField]
        private RectTransform bannerRoot;

        private readonly List<GameObject> residentCards = new List<GameObject>();

        public static ShelterView Build(Transform parent, AppFlowController flow)
        {
            GameObject panel = UiFactory.CreatePanel(parent, "ShelterScreen", new Color(0.08f, 0.10f, 0.12f, 1f));
            ShelterView view = panel.AddComponent<ShelterView>();
            view.flow = flow;
            view.EnsureLayout();
            view.Wire(flow);
            return view;
        }

        private void Awake()
        {
            EnsureLayout();
        }

        public void Wire(AppFlowController appFlow)
        {
            flow = appFlow;
            EnsureLayout();

            WireButton(departButton, () => flow.OnDepart());
            WireButton(dayEndButton, () => flow.BeginDayEnd());
            WireButton(settingsButton, () => flow.ShowSettings());
            WireButton(menuButton, () => flow.OnBackToMenu());
            WireButton(closeDetailButton, CloseDetail);
        }

        public void Refresh()
        {
            EnsureLayout();

            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            if (gi == null || gi.Gameplay == null || gi.Gameplay.State == null || gi.Shelter == null)
            {
                SetText(dayText, "庇护所");
                SetText(phaseText, "尚未开始新局");
                SetText(foodValueText, "存粮  -");
                SetText(corruptionValueText, "腐蚀度  -");
                SetText(populationValueText, "人口  -");
                UpdateFog(0f);
                ClearResidentCards();
                HideDetail();
                return;
            }

            GameState state = gi.Gameplay.State;
            SetText(dayText, "庇护所 · 第 " + state.day + " 天");
            SetText(phaseText, PhaseLabel(state.currentPhase));
            SetText(foodValueText, "存粮  " + state.foodStock);
            SetText(corruptionValueText, "腐蚀度  " + state.corruption + " / 100");
            SetText(populationValueText, "人口  " + gi.Shelter.Population + " / 5");
            SetText(propsStateText, "暂无道具数据");

            UpdateFog(state.corruption);
            RebuildResidentCards(gi.Shelter, state.foodStock, state.corruption >= 40);
            RefreshDetail(gi.Shelter);
            ShowBulletins(gi.Shelter);
            RefreshExpeditionControls(gi, state);
        }

        private void RefreshExpeditionControls(GameInstance gi, GameState state)
        {
            bool expeditionBlocked = gi.Gameplay.HasTag(GameplayTags.ForbiddenExpedition);
            if (departButton != null)
            {
                departButton.interactable = !expeditionBlocked
                    && state.currentPhase == GameplayPhase.ExpeditionPrep
                    && (gi.Events == null || !gi.Events.IsSequenceActive);
                TextMeshProUGUI label = departButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = expeditionBlocked
                        ? "你答应了幼童，今天要陪他一起玩抛石头"
                        : "出发";
                }
            }

            if (dayEndButton != null)
            {
                bool showDayEnd = expeditionBlocked
                    && state.currentPhase == GameplayPhase.ExpeditionPrep
                    && (gi.Events == null || !gi.Events.IsSequenceActive);
                dayEndButton.gameObject.SetActive(showDayEnd);
                dayEndButton.interactable = showDayEnd;
            }
        }

        private void OnFeed(Survivor survivor)
        {
            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            if (gi == null || gi.Shelter == null || survivor == null)
            {
                return;
            }

            if (gi.Shelter.AllocateFood(survivor, 1))
            {
                Refresh();
                if (flow != null)
                {
                    flow.RefreshGlobalHud();
                }
            }
        }

        private void OpenDetail(string defId)
        {
            selectedDefId = defId;
            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            RefreshDetail(gi != null ? gi.Shelter : null);
        }

        private void CloseDetail()
        {
            selectedDefId = null;
            HideDetail();
        }

        private void HideDetail()
        {
            if (detailGroup != null)
            {
                detailGroup.SetActive(false);
            }
        }

        private void RefreshDetail(ShelterManager shelter)
        {
            if (detailGroup == null || shelter == null)
            {
                HideDetail();
                return;
            }

            Survivor selected = FindAliveByDefId(shelter, selectedDefId);
            if (selected == null)
            {
                HideDetail();
                return;
            }

            detailGroup.SetActive(true);
            SetText(detailNameText, selected.name);
            SetText(detailStatusText, "身份：" + selected.name + "\n状态：" + StatusName(selected.status));
            SetText(detailTraitsText, "特质：\n" + BuildTraitText(selected));
            SetText(detailMessageText, "每日留言：\n暂无每日留言数据");
        }

        private void RebuildResidentCards(ShelterManager shelter, int foodStock, bool corrupted)
        {
            ClearResidentCards();

            List<Survivor> alive = new List<Survivor>();
            for (int i = 0; i < shelter.Survivors.Count; i++)
            {
                Survivor s = shelter.Survivors[i];
                if (s.status != SurvivorStatus.Dead && s.status != SurvivorStatus.Left)
                {
                    alive.Add(s);
                }
            }

            for (int i = 0; i < alive.Count; i++)
            {
                float x = (i - (alive.Count - 1) * 0.5f) * 230f;
                CreateResidentCard(alive[i], new Vector2(x, 30f), foodStock, corrupted);
            }
        }

        private void CreateResidentCard(Survivor survivor, Vector2 pos, int foodStock, bool corrupted)
        {
            Color cardColor = new Color(0.14f, 0.16f, 0.20f, 0.98f);
            Image card = UiFactory.CreateImage(residentRow, "Card_" + survivor.defId, pos, new Vector2(200f, 270f), cardColor);
            Button open = card.gameObject.AddComponent<Button>();
            open.targetGraphic = card;
            open.onClick.AddListener(() => OpenDetail(survivor.defId));
            residentCards.Add(card.gameObject);

            Color avatarColor = StatusColor(survivor.status);
            if (corrupted)
            {
                avatarColor = Color.Lerp(avatarColor, new Color(0.55f, 0.20f, 0.66f, 1f), 0.55f);
            }

            Image avatar = UiFactory.CreateCircleImage(card.transform, "Avatar", new Vector2(0f, 74f), new Vector2(88f, 88f), avatarColor);
            avatar.raycastTarget = false;

            TextMeshProUGUI name = UiFactory.CreateText(card.transform, "Txt_Name", survivor.name, 26, new Vector2(0f, -6f), new Vector2(184f, 38f), TextAlignmentOptions.Center, Color.white);
            name.raycastTarget = false;
            TextMeshProUGUI identity = UiFactory.CreateText(card.transform, "Txt_Identity", "耐饿 " + survivor.hungryToDyingDays + " 天", 16, new Vector2(0f, -44f), new Vector2(184f, 24f), TextAlignmentOptions.Center, new Color(0.68f, 0.72f, 0.78f, 1f));
            identity.raycastTarget = false;
            TextMeshProUGUI status = UiFactory.CreateText(card.transform, "Txt_Status", StatusName(survivor.status), 22, new Vector2(0f, -78f), new Vector2(184f, 30f), TextAlignmentOptions.Center, StatusColor(survivor.status));
            status.raycastTarget = false;
            TextMeshProUGUI hunger = UiFactory.CreateText(card.transform, "Txt_Hunger", "饱食度 " + survivor.hunger, 16, new Vector2(0f, -112f), new Vector2(184f, 24f), TextAlignmentOptions.Center, new Color(0.88f, 0.88f, 0.88f, 1f));
            hunger.raycastTarget = false;

            Button feed = UiFactory.CreateButton(
                card.transform,
                "Btn_Feed_" + survivor.defId,
                "喂食 +1",
                () => OnFeed(survivor),
                new Vector2(0f, -176f),
                new Vector2(132f, 40f),
                new Color(0.28f, 0.50f, 0.40f, 1f),
                18);
            feed.interactable = foodStock >= 1;
        }

        private void ClearResidentCards()
        {
            for (int i = 0; i < residentCards.Count; i++)
            {
                if (residentCards[i] != null)
                {
                    residentCards[i].SetActive(false);
                    Destroy(residentCards[i]);
                }
            }

            residentCards.Clear();
        }

        private void ShowBulletins(ShelterManager shelter)
        {
            if (shelter == null || bannerRoot == null)
            {
                return;
            }

            List<string> bulletins = shelter.ConsumeBulletins();
            for (int i = 0; i < bulletins.Count; i++)
            {
                Color color = bulletins[i].IndexOf("离世", System.StringComparison.Ordinal) >= 0
                    ? new Color(1f, 0.36f, 0.32f, 1f)
                    : new Color(1f, 0.76f, 0.36f, 1f);
                ShelterBulletBanner.Spawn(bannerRoot, bulletins[i], color, i);
            }
        }

        private void UpdateFog(float corruption)
        {
            if (fogOverlay == null)
            {
                return;
            }

            float alpha;
            if (corruption >= 40f)
            {
                alpha = 0.34f + Mathf.Clamp01((corruption - 40f) / 60f) * 0.28f;
            }
            else
            {
                alpha = corruption / 100f * 0.10f;
            }

            fogOverlay.color = new Color(0.01f, 0.005f, 0.03f, alpha);
        }

        private Survivor FindAliveByDefId(ShelterManager shelter, string defId)
        {
            if (shelter == null || string.IsNullOrEmpty(defId))
            {
                return null;
            }

            for (int i = 0; i < shelter.Survivors.Count; i++)
            {
                Survivor s = shelter.Survivors[i];
                if (s.defId == defId && s.status != SurvivorStatus.Dead && s.status != SurvivorStatus.Left)
                {
                    return s;
                }
            }

            return null;
        }

        private void EnsureLayout()
        {
            if (layoutBuilt)
            {
                return;
            }

            if (UiFactory.Font == null)
            {
                UiFactory.Font = UiCjkFont.Load();
            }

            DestroyAllChildren();
            BuildLayout();
            layoutBuilt = true;
        }

        private void DestroyAllChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private void BuildLayout()
        {
            dayText = UiFactory.CreateText(transform, "Txt_Day", "庇护所 · 第 1 天", 38, new Vector2(0f, 420f), new Vector2(620f, 56f), TextAlignmentOptions.Center, Color.white);
            dayText.raycastTarget = false;

            Image statsPanel = UiFactory.CreateImage(transform, "Panel_Stats", new Vector2(-700f, 40f), new Vector2(340f, 400f), new Color(0.11f, 0.13f, 0.17f, 0.96f));
            statsPanel.raycastTarget = false;
            TextMeshProUGUI statsTitle = UiFactory.CreateText(statsPanel.transform, "Txt_StatsTitle", "庇护所状态", 24, new Vector2(0f, 160f), new Vector2(300f, 40f), TextAlignmentOptions.Center, new Color(0.92f, 0.93f, 0.95f, 1f));
            statsTitle.raycastTarget = false;
            foodValueText = UiFactory.CreateText(statsPanel.transform, "Txt_Food", "", 22, new Vector2(0f, 96f), new Vector2(300f, 36f), TextAlignmentOptions.Center, new Color(0.85f, 0.93f, 0.80f, 1f));
            foodValueText.raycastTarget = false;
            corruptionValueText = UiFactory.CreateText(statsPanel.transform, "Txt_Corruption", "", 22, new Vector2(0f, 48f), new Vector2(300f, 36f), TextAlignmentOptions.Center, new Color(0.94f, 0.70f, 0.62f, 1f));
            corruptionValueText.raycastTarget = false;
            populationValueText = UiFactory.CreateText(statsPanel.transform, "Txt_Population", "", 22, new Vector2(0f, 0f), new Vector2(300f, 36f), TextAlignmentOptions.Center, new Color(0.78f, 0.84f, 0.96f, 1f));
            populationValueText.raycastTarget = false;
            phaseText = UiFactory.CreateText(statsPanel.transform, "Txt_Phase", "", 20, new Vector2(0f, -60f), new Vector2(300f, 32f), TextAlignmentOptions.Center, new Color(0.92f, 0.93f, 0.95f, 1f));
            phaseText.raycastTarget = false;
            TextMeshProUGUI allocationTitle = UiFactory.CreateText(statsPanel.transform, "Txt_Allocation", "每日分配", 20, new Vector2(0f, -124f), new Vector2(300f, 32f), TextAlignmentOptions.Center, new Color(0.82f, 0.86f, 0.92f, 1f));
            allocationTitle.raycastTarget = false;

            Image propsPanel = UiFactory.CreateImage(transform, "Panel_Props", new Vector2(-700f, -240f), new Vector2(340f, 170f), new Color(0.11f, 0.13f, 0.17f, 0.96f));
            propsPanel.raycastTarget = false;
            TextMeshProUGUI propsTitle = UiFactory.CreateText(propsPanel.transform, "Txt_PropsTitle", "道具", 24, new Vector2(0f, 52f), new Vector2(300f, 40f), TextAlignmentOptions.Center, new Color(0.92f, 0.93f, 0.95f, 1f));
            propsTitle.raycastTarget = false;
            propsStateText = UiFactory.CreateText(propsPanel.transform, "Txt_PropsState", "", 18, new Vector2(0f, -12f), new Vector2(300f, 80f), TextAlignmentOptions.Center, new Color(0.62f, 0.66f, 0.72f, 1f));
            propsStateText.raycastTarget = false;

            residentRow = CreateEmptyRect(transform, "Row_Residents");

            departButton = UiFactory.CreateButton(transform, "Btn_Depart", "出发", null, new Vector2(0f, -420f), new Vector2(220f, 60f), null, 24);
            dayEndButton = UiFactory.CreateButton(transform, "Btn_DayEnd", "结束今天", null, new Vector2(0f, -350f), new Vector2(220f, 48f), new Color(0.22f, 0.32f, 0.28f, 1f), 20);
            settingsButton = UiFactory.CreateButton(transform, "Btn_Settings", "设置", null, new Vector2(-330f, -420f), new Vector2(150f, 48f), new Color(0.22f, 0.26f, 0.32f, 1f), 20);
            menuButton = UiFactory.CreateButton(transform, "Btn_Menu", "返回主菜单", null, new Vector2(330f, -420f), new Vector2(190f, 48f), new Color(0.22f, 0.26f, 0.32f, 1f), 20);

            detailGroup = UiFactory.CreatePanel(transform, "Panel_Detail", new Color(0.11f, 0.13f, 0.17f, 1f), false);
            RectTransform detailRt = detailGroup.GetComponent<RectTransform>();
            detailRt.anchorMin = new Vector2(0.5f, 0.5f);
            detailRt.anchorMax = new Vector2(0.5f, 0.5f);
            detailRt.anchoredPosition = new Vector2(680f, 30f);
            detailRt.sizeDelta = new Vector2(460f, 470f);

            TextMeshProUGUI detailTitle = UiFactory.CreateText(detailGroup.transform, "Txt_DetailTitle", "住户详情", 28, new Vector2(0f, 195f), new Vector2(420f, 44f), TextAlignmentOptions.Center, Color.white);
            detailTitle.raycastTarget = false;
            detailNameText = UiFactory.CreateText(detailGroup.transform, "Txt_DetailName", "", 30, new Vector2(0f, 135f), new Vector2(420f, 44f), TextAlignmentOptions.Center, Color.white);
            detailNameText.raycastTarget = false;
            detailStatusText = UiFactory.CreateText(detailGroup.transform, "Txt_DetailStatus", "", 22, new Vector2(0f, 78f), new Vector2(420f, 36f), TextAlignmentOptions.Center, new Color(0.90f, 0.92f, 0.95f, 1f));
            detailStatusText.raycastTarget = false;
            detailTraitsText = UiFactory.CreateText(detailGroup.transform, "Txt_DetailTraits", "", 18, new Vector2(0f, -6f), new Vector2(420f, 150f), TextAlignmentOptions.TopLeft, new Color(0.86f, 0.88f, 0.92f, 1f));
            detailTraitsText.raycastTarget = false;
            detailMessageText = UiFactory.CreateText(detailGroup.transform, "Txt_DetailMessage", "", 18, new Vector2(0f, -160f), new Vector2(420f, 110f), TextAlignmentOptions.TopLeft, new Color(0.86f, 0.88f, 0.92f, 1f));
            detailMessageText.raycastTarget = false;
            closeDetailButton = UiFactory.CreateButton(detailGroup.transform, "Btn_CloseDetail", "关闭", null, new Vector2(0f, -215f), new Vector2(180f, 46f), new Color(0.30f, 0.34f, 0.40f, 1f), 20);

            GameObject fogGo = UiFactory.CreatePanel(transform, "FogOverlay", new Color(0.01f, 0.005f, 0.03f, 0f));
            fogOverlay = fogGo.GetComponent<Image>();
            fogOverlay.raycastTarget = false;

            bannerRoot = CreateEmptyRect(transform, "BulletLayer");
            detailGroup.SetActive(false);
        }

        private static RectTransform CreateEmptyRect(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            UiFactory.Stretch(rt);
            return rt;
        }

        private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static string StatusName(SurvivorStatus status)
        {
            switch (status)
            {
                case SurvivorStatus.Hungry: return "饥饿";
                case SurvivorStatus.Dying: return "濒死";
                case SurvivorStatus.Dead: return "已死亡";
                case SurvivorStatus.Left: return "已离开";
                default: return "健康";
            }
        }

        private static Color StatusColor(SurvivorStatus status)
        {
            switch (status)
            {
                case SurvivorStatus.Hungry: return new Color(0.90f, 0.64f, 0.24f, 1f);
                case SurvivorStatus.Dying: return new Color(0.82f, 0.30f, 0.28f, 1f);
                case SurvivorStatus.Dead: return new Color(0.55f, 0.55f, 0.55f, 1f);
                case SurvivorStatus.Left: return new Color(0.55f, 0.55f, 0.55f, 1f);
                default: return new Color(0.34f, 0.72f, 0.44f, 1f);
            }
        }

        private static string BuildTraitText(Survivor survivor)
        {
            List<string> lines = new List<string>();
            for (int i = 0; i < TraitCatalog.SlotDefs.Length; i++)
            {
                SurvivorTrait trait = TraitCatalog.SlotDefs[i];
                if (trait == null || trait.Id == TraitIds.Hero)
                {
                    continue;
                }

                if (TraitCatalog.IsOwnedByNames(trait, new[] { survivor.name }))
                {
                    lines.Add(trait.Title + "：" + trait.Description);
                }
            }

            return lines.Count > 0 ? string.Join("\n", lines) : "无特质数据";
        }

        private static string PhaseLabel(GameplayPhase phase)
        {
            switch (phase)
            {
                case GameplayPhase.Combat: return "阶段：战斗中";
                case GameplayPhase.TriumphReturn: return "阶段：凯旋结算";
                case GameplayPhase.Ending: return "阶段：结局";
                default: return "阶段：出征准备 · 每日分配";
            }
        }
    }
}
