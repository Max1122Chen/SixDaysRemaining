using System.Collections.Generic;
using SixDaysRemaining.App;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 庇护所界面，支持两种模式：
    /// 1) 手动搭建模式（推荐）：在 Inspector 把 manualRoot 拖入场景中的内容根节点后启用。
    ///    所有固定元素（任务栏、房间背景、座椅、信息面板、按钮等）都由你在场景里摆放，
    ///    代码只负责刷新文字、切换房间、按序生成 NPC 角色、打开图鉴。
    /// 2) 代码布局模式（兜底）：manualRoot 为空时，运行时自动重建整套 UI。
    /// </summary>
    public class ShelterView : MonoBehaviour
    {
        private static readonly Color DoneColor = new Color(0.50f, 0.82f, 0.55f, 1f);
        private static readonly Color LockedColor = new Color(0.42f, 0.44f, 0.48f, 1f);
        private static readonly Color ActiveColor = new Color(1f, 0.92f, 0.72f, 1f);
        private static readonly Color FeedGreen = new Color(0.28f, 0.50f, 0.40f, 1f);
        private static readonly Color PressedGray = new Color(0.22f, 0.24f, 0.27f, 1f);
        private static readonly Color DisabledGray = new Color(0.30f, 0.32f, 0.36f, 1f);

        private AppFlowController flow;
        private bool layoutBuilt;
        private string selectedDefId;
        private int roomIndex = 1; // 默认居中：厕所(0) / 大厅(1) / 厨房(2)
        private bool taskBarCollapsed;

        // ================= 手动搭建模式 =================
        // 把 ShelterPanel（或内容根节点）拖到这里即启用手动模式。
        [SerializeField]
        private RectTransform manualRoot;

        // 顶部
        [SerializeField]
        private TextMeshProUGUI dayText;

        [SerializeField]
        private TextMeshProUGUI phaseText;

        [SerializeField]
        private TextMeshProUGUI roomLabelText;

        // 今日进度栏（数组按顺序：0 分配食物 / 1 外出战斗 / 2 处理每日事件）
        [SerializeField]
        private RectTransform taskBarRoot;

        [SerializeField]
        private TextMeshProUGUI[] taskNodeTexts = new TextMeshProUGUI[0];

        [SerializeField]
        private GameObject[] taskArrows = new GameObject[0];

        /// <summary>收起任务栏时一起隐藏的额外元素（如“今日进度”标题）。</summary>
        [SerializeField]
        private GameObject[] taskExtraObjects = new GameObject[0];

        [SerializeField]
        private Button taskToggleButton;

        // 房间容器（顺序：0 厕所 / 1 大厅 / 2 厨房；运行时只激活当前房间）
        [SerializeField]
        private RectTransform[] roomRoots = new RectTransform[0];

        [SerializeField]
        private Button leftArrowButton;

        [SerializeField]
        private Button rightArrowButton;

        // 图鉴
        [SerializeField]
        private Button codexButton;

        [SerializeField]
        private ShelterCodexView codexView;

        // 信息面板
        [SerializeField]
        private GameObject detailGroup;

        [SerializeField]
        private Image detailAvatarImage;

        [SerializeField]
        private TextMeshProUGUI detailIdentityText;

        [SerializeField]
        private TextMeshProUGUI detailNameText;

        [SerializeField]
        private TextMeshProUGUI detailAgeText;

        [SerializeField]
        private TextMeshProUGUI detailStatusText;

        [SerializeField]
        private TextMeshProUGUI detailFitnessText;

        [SerializeField]
        private TextMeshProUGUI detailQuoteText;

        [SerializeField]
        private Button feedButton;

        // 操作按钮
        [SerializeField]
        private Button departButton;

        [SerializeField]
        private Button dayEndButton;

        [SerializeField]
        private Button settingsButton;

        [SerializeField]
        private Button menuButton;

        [SerializeField]
        private Button closeDetailButton;

        // 特效与公告
        [SerializeField]
        private Image fogOverlay;

        [SerializeField]
        private RectTransform bannerRoot;

        // ============ 旧场景序列化字段（保留兼容，手动模式可留空）============
        [SerializeField]
        private TextMeshProUGUI statusText;

        [SerializeField]
        private TextMeshProUGUI survivorText;

        [SerializeField]
        private RectTransform residentRow;

        [SerializeField]
        private TextMeshProUGUI propsStateText;

        [SerializeField]
        private TextMeshProUGUI detailTraitsText;

        [SerializeField]
        private TextMeshProUGUI detailMessageText;

        // ============ 代码布局模式专用（manualRoot 为空时使用）============
        private RectTransform roomSceneRoot;
        private Image roomBgImage;
        private TextMeshProUGUI roomTitleText;
        private readonly List<TextMeshProUGUI> taskNodeLabels = new List<TextMeshProUGUI>();
        private readonly List<GameObject> taskNodeObjects = new List<GameObject>();
        private readonly List<GameObject> taskArrowObjects = new List<GameObject>();
        private readonly List<GameObject> taskCollapsibleObjects = new List<GameObject>();

        // 手动模式：记录用户在场景里摆放的任务栏尺寸与 R 按钮位置，展开时恢复。
        private Vector2 manualTaskBarSize;
        private Vector2 manualTogglePos;
        private bool manualTaskBarCaptured;

        // 动态生成物（座位上生成的 NPC 节点等）
        private readonly List<GameObject> roomObjects = new List<GameObject>();

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

            WireButton(departButton, () =>
            {
                CloseCodex();
                flow.OnDepart();
            });
            WireButton(dayEndButton, () =>
            {
                CloseCodex();
                flow.BeginDayEnd();
            });
            WireButton(settingsButton, () => flow.ShowSettings());
            WireButton(menuButton, () =>
            {
                CloseCodex();
                flow.OnBackToMenu();
            });
            WireButton(closeDetailButton, CloseDetail);
            WireButton(leftArrowButton, () => SwitchRoom(-1));
            WireButton(rightArrowButton, () => SwitchRoom(1));
            WireButton(codexButton, OpenCodex);
            WireButton(taskToggleButton, ToggleTaskBar);
            WireButton(feedButton, OnFeedSelected);
            if (codexView != null)
            {
                codexView.Wire(flow);
            }
        }

        public void Refresh()
        {
            EnsureLayout();

            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            if (gi == null || gi.Gameplay == null || gi.Gameplay.State == null || gi.Shelter == null)
            {
                SetText(dayText, "庇护所");
                SetText(phaseText, "尚未开始新局");
                UpdateFog(0f);
                HideDetail();
                return;
            }

            GameState state = gi.Gameplay.State;
            SetText(dayText, "第 " + state.day + " 天");
            SetText(phaseText, PhaseLabel(state.currentPhase));
            UpdateTaskBar(state.currentPhase);
            RebuildRoomScene(state);
            RefreshDetail(gi.Shelter, state);
            UpdateFog(state.corruption);
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

        // ---------- 顶部任务栏 ----------

        private void UpdateTaskBar(GameplayPhase phase)
        {
            bool node0Done;
            bool node1Done;
            bool node2Done;
            switch (phase)
            {
                case GameplayPhase.Combat:
                    node0Done = true;
                    node1Done = false;
                    node2Done = false;
                    break;
                case GameplayPhase.TriumphReturn:
                    node0Done = true;
                    node1Done = true;
                    node2Done = false;
                    break;
                case GameplayPhase.Ending:
                    node0Done = true;
                    node1Done = true;
                    node2Done = true;
                    break;
                default:
                    node0Done = false;
                    node1Done = false;
                    node2Done = false;
                    break;
            }

            bool[] done = { node0Done, node1Done, node2Done };
            string[] names = { "分配食物", "外出战斗", "处理每日事件" };
            for (int i = 0; i < taskNodeLabels.Count && i < 3; i++)
            {
                bool locked = i > 0 && !done[i - 1];
                TextMeshProUGUI label = taskNodeLabels[i];
                label.text = done[i] ? names[i] + " √" : names[i];
                if (done[i])
                {
                    label.color = DoneColor;
                }
                else if (locked)
                {
                    label.color = LockedColor;
                }
                else
                {
                    label.color = ActiveColor;
                }
            }

            // 收起/展开逻辑依赖 TaskBar 根节点引用；节点文字/√ 的刷新不依赖它。
            if (taskBarRoot == null)
            {
                return;
            }

            bool collapsed = taskBarCollapsed;
            for (int i = 0; i < taskNodeObjects.Count; i++)
            {
                if (taskNodeObjects[i] != null)
                {
                    taskNodeObjects[i].SetActive(!collapsed);
                }
            }

            for (int i = 0; i < taskArrowObjects.Count; i++)
            {
                if (taskArrowObjects[i] != null)
                {
                    taskArrowObjects[i].SetActive(!collapsed);
                }
            }

            for (int i = 0; i < taskCollapsibleObjects.Count; i++)
            {
                if (taskCollapsibleObjects[i] != null)
                {
                    taskCollapsibleObjects[i].SetActive(!collapsed);
                }
            }

            RectTransform barRt = taskBarRoot;
            RectTransform toggleRt = taskToggleButton != null ? taskToggleButton.GetComponent<RectTransform>() : null;
            if (collapsed)
            {
                barRt.sizeDelta = new Vector2(76f, 42f);
                if (toggleRt != null)
                {
                    if (manualRoot != null)
                    {
                        // 把 R 按钮的锚点参考点移到小方块中心（与父节点 pivot 无关）。
                        Vector2 anchor = (toggleRt.anchorMin + toggleRt.anchorMax) * 0.5f;
                        toggleRt.anchoredPosition = new Vector2(
                            (0.5f - anchor.x) * 76f,
                            (0.5f - anchor.y) * 42f);
                    }
                    else
                    {
                        toggleRt.anchoredPosition = Vector2.zero;
                    }
                }
            }
            else
            {
                if (manualRoot != null && manualTaskBarCaptured)
                {
                    barRt.sizeDelta = manualTaskBarSize;
                    if (toggleRt != null)
                    {
                        toggleRt.anchoredPosition = manualTogglePos;
                    }
                }
                else
                {
                    barRt.sizeDelta = new Vector2(940f, 54f);
                    if (toggleRt != null)
                    {
                        toggleRt.anchoredPosition = new Vector2(412f, 0f);
                    }
                }
            }
        }

        private void ToggleTaskBar()
        {
            taskBarCollapsed = !taskBarCollapsed;
            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            if (gi != null && gi.Gameplay != null)
            {
                UpdateTaskBar(gi.Gameplay.CurrentPhase);
            }
        }

        // ---------- 房间场景 ----------

        private void SwitchRoom(int delta)
        {
            int next = roomIndex + delta;
            if (next < 0 || next >= ShelterRooms.Count)
            {
                return;
            }

            roomIndex = next;
            Refresh();
        }

        private void RebuildRoomScene(GameState state)
        {
            ClearRoomObjects();
            UpdateRoomChrome();

            List<Survivor> alive = CollectAliveSurvivors();
            bool manual = manualRoot != null && roomRoots != null && roomRoots.Length > 0;
            if (manual)
            {
                if (roomIndex >= roomRoots.Length)
                {
                    roomIndex = roomRoots.Length - 1;
                }

                RebuildManualRoom(state, alive);
            }
            else if (roomSceneRoot != null)
            {
                RebuildCodeRoom(state, alive);
            }
        }

        private void UpdateRoomChrome()
        {
            ShelterRoomDef room = ShelterRooms.Get(roomIndex);
            SetText(roomLabelText, room.DisplayName);
            if (leftArrowButton != null)
            {
                leftArrowButton.gameObject.SetActive(roomIndex > 0);
            }

            if (rightArrowButton != null)
            {
                rightArrowButton.gameObject.SetActive(roomIndex < ShelterRooms.Count - 1);
            }
        }

        private void RebuildManualRoom(GameState state, List<Survivor> alive)
        {
            ApplyRoomActive(roomIndex);
            RectTransform current = roomRoots[roomIndex];
            ShelterSeatSlot[] slots = CollectSeatSlots(current);
            int count = Mathf.Min(alive.Count, slots.Length);
            for (int i = 0; i < count; i++)
            {
                CreateNpcNodeOnSlot(alive[i], slots[i], state.day);
            }
        }

        private void RebuildCodeRoom(GameState state, List<Survivor> alive)
        {
            ShelterRoomDef room = ShelterRooms.Get(roomIndex);
            Sprite bg = ShelterRooms.LoadBackground(room);
            if (roomBgImage != null)
            {
                roomBgImage.sprite = bg;
                roomBgImage.color = bg != null ? Color.white : room.BackgroundColor;
            }

            if (roomTitleText != null)
            {
                roomTitleText.text = room.DisplayName;
            }

            for (int i = 0; i < ShelterRooms.SeatCount; i++)
            {
                CreateSeat(room.Seats[i], i);
            }

            for (int i = 0; i < alive.Count && i < ShelterRooms.SeatCount; i++)
            {
                CreateNpcNode(alive[i], room.Seats[i], state.day);
            }
        }

        private void ApplyRoomActive(int index)
        {
            for (int i = 0; i < roomRoots.Length; i++)
            {
                if (roomRoots[i] != null)
                {
                    roomRoots[i].gameObject.SetActive(i == index);
                }
            }
        }

        private static ShelterSeatSlot[] CollectSeatSlots(RectTransform roomRoot)
        {
            if (roomRoot == null)
            {
                return new ShelterSeatSlot[0];
            }

            ShelterSeatSlot[] slots = roomRoot.GetComponentsInChildren<ShelterSeatSlot>(true);
            System.Array.Sort(slots, delegate (ShelterSeatSlot a, ShelterSeatSlot b)
            {
                return a.SeatIndex.CompareTo(b.SeatIndex);
            });
            return slots;
        }

        private void CreateNpcNodeOnSlot(Survivor survivor, ShelterSeatSlot slot, int day)
        {
            CreateNpcNode(
                survivor,
                slot.transform,
                Vector2.zero,
                slot.NpcOffset,
                slot.NameOffset,
                slot.StatusOffset,
                slot.IdentityOffset,
                day);
        }

        private void CreateNpcNode(Survivor survivor, Vector2 seatPos, int day)
        {
            CreateNpcNode(
                survivor,
                roomSceneRoot,
                seatPos,
                new Vector2(0f, 78f),
                new Vector2(0f, -56f),
                new Vector2(0f, -84f),
                new Vector2(0f, -106f),
                day);
        }

        private void CreateNpcNode(
            Survivor survivor,
            Transform parent,
            Vector2 anchorPos,
            Vector2 portraitPos,
            Vector2 namePos,
            Vector2 statusPos,
            Vector2 identityPos,
            int day)
        {
            SurvivorDef def;
            bool hasDef = ShelterContent.Survivors.TryGet(survivor.defId, out def);
            Sprite portrait = ShelterPortraits.Load(def, survivor.status, day);

            // 点击区域覆盖整个 NPC 块（立绘 + 名字 + 状态 + 身份），避免只能点中立绘。
            float top = portraitPos.y + 66f;
            float bottom = Mathf.Min(
                portraitPos.y - 66f,
                namePos.y - 13f,
                statusPos.y - 11f,
                identityPos.y - 10f);
            float centerY = (top + bottom) * 0.5f;
            float height = top - bottom;

            GameObject nodeGo = new GameObject("NpcNode_" + survivor.defId);
            nodeGo.transform.SetParent(parent, false);
            RectTransform nodeRt = nodeGo.AddComponent<RectTransform>();
            nodeRt.anchoredPosition = anchorPos + new Vector2(0f, centerY);
            nodeRt.sizeDelta = new Vector2(170f, height);
            Image nodeBg = nodeGo.AddComponent<Image>();
            nodeBg.color = new Color(1f, 1f, 1f, 0.01f);
            Button click = nodeGo.AddComponent<Button>();
            click.targetGraphic = nodeBg;
            click.onClick.AddListener(() => OnNpcClicked(survivor.defId));
            roomObjects.Add(nodeGo);

            Vector2 childPortrait = new Vector2(portraitPos.x, portraitPos.y - centerY);
            Vector2 childName = new Vector2(namePos.x, namePos.y - centerY);
            Vector2 childStatus = new Vector2(statusPos.x, statusPos.y - centerY);
            Vector2 childIdentity = new Vector2(identityPos.x, identityPos.y - centerY);

            Image portraitImage;
            if (portrait != null)
            {
                portraitImage = UiFactory.CreateImage(nodeGo.transform, "Portrait_" + survivor.defId, childPortrait, new Vector2(104f, 132f), Color.white);
                portraitImage.sprite = portrait;
                portraitImage.preserveAspect = true;
            }
            else
            {
                portraitImage = UiFactory.CreateCircleImage(nodeGo.transform, "Portrait_" + survivor.defId, childPortrait, new Vector2(104f, 104f), StatusColor(survivor.status));
            }

            portraitImage.raycastTarget = false;

            TextMeshProUGUI name = UiFactory.CreateText(nodeGo.transform, "Txt_NpcName_" + survivor.defId, survivor.name, 18, childName, new Vector2(170f, 26f), TextAlignmentOptions.Center, Color.white);
            name.raycastTarget = false;

            TextMeshProUGUI status = UiFactory.CreateText(nodeGo.transform, "Txt_NpcStatus_" + survivor.defId, StatusName(survivor.status), 15, childStatus, new Vector2(140f, 22f), TextAlignmentOptions.Center, StatusColor(survivor.status));
            status.raycastTarget = false;

            if (hasDef)
            {
                TextMeshProUGUI identity = UiFactory.CreateText(nodeGo.transform, "Txt_NpcIdentity_" + survivor.defId, def.DisplayName, 14, childIdentity, new Vector2(140f, 20f), TextAlignmentOptions.Center, new Color(0.62f, 0.66f, 0.72f, 1f));
                identity.raycastTarget = false;
            }
        }

        private List<Survivor> CollectAliveSurvivors()
        {
            List<Survivor> alive = new List<Survivor>();
            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            if (gi != null && gi.Shelter != null)
            {
                IReadOnlyList<Survivor> roster = gi.Shelter.Survivors;
                for (int i = 0; i < roster.Count; i++)
                {
                    Survivor s = roster[i];
                    if (s.status != SurvivorStatus.Dead && s.status != SurvivorStatus.Left)
                    {
                        alive.Add(s);
                    }
                }
            }

            return alive;
        }

        private void CreateSeat(Vector2 pos, int index)
        {
            Image seat = UiFactory.CreateImage(roomSceneRoot, "Seat_" + index, pos, new Vector2(136f, 44f), new Color(0.33f, 0.27f, 0.21f, 1f));
            seat.raycastTarget = false;
            roomObjects.Add(seat.gameObject);

            Image cushion = UiFactory.CreateImage(seat.transform, "Cushion", new Vector2(0f, 14f), new Vector2(112f, 18f), new Color(0.42f, 0.35f, 0.28f, 1f));
            cushion.raycastTarget = false;
        }

        private void ClearRoomObjects()
        {
            for (int i = 0; i < roomObjects.Count; i++)
            {
                if (roomObjects[i] != null)
                {
                    roomObjects[i].SetActive(false);
                    Destroy(roomObjects[i]);
                }
            }

            roomObjects.Clear();
        }

        // ---------- NPC 信息面板 ----------

        private void OnNpcClicked(string defId)
        {
            if (string.Equals(selectedDefId, defId, System.StringComparison.Ordinal))
            {
                selectedDefId = null;
            }
            else
            {
                selectedDefId = defId;
            }

            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            if (gi != null && gi.Shelter != null && gi.Gameplay != null)
            {
                RefreshDetail(gi.Shelter, gi.Gameplay.State);
            }
        }

        private void RefreshDetail(ShelterManager shelter, GameState state)
        {
            if (detailGroup == null || shelter == null || state == null)
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

            SurvivorDef def;
            ShelterContent.Survivors.TryGet(selected.defId, out def);
            SurvivorProfile profile = ShelterProfiles.Resolve(def);

            detailGroup.SetActive(true);
            SetText(detailIdentityText, "身份：" + (def != null ? def.DisplayName : selected.name));
            SetText(detailNameText, def != null ? def.DisplayName : selected.name);
            SetText(detailAgeText, "年龄：" + (profile.Age > 0 ? profile.Age + " 岁" : "未知"));
            SetText(detailStatusText, "生存状态：" + StatusName(selected.status) + "　饱食度 " + selected.hunger);
            SetText(detailFitnessText, "身体素质：" + (string.IsNullOrEmpty(profile.Fitness) ? "未知" : profile.Fitness));
            SetText(detailQuoteText, "语录：\n“" + (string.IsNullOrEmpty(profile.Quote) ? "（暂无语录）" : profile.Quote) + "”");

            Sprite portrait = ShelterPortraits.Load(def, selected.status, state.day);
            if (detailAvatarImage != null)
            {
                if (portrait != null)
                {
                    detailAvatarImage.sprite = portrait;
                    detailAvatarImage.color = Color.white;
                    detailAvatarImage.rectTransform.sizeDelta = new Vector2(104f, 132f);
                }
                else
                {
                    detailAvatarImage.sprite = UiFactory.CircleSprite;
                    detailAvatarImage.color = StatusColor(selected.status);
                    detailAvatarImage.rectTransform.sizeDelta = new Vector2(104f, 104f);
                }
            }

            RefreshFeedButton(shelter, selected, state);
        }

        private void RefreshFeedButton(ShelterManager shelter, Survivor survivor, GameState state)
        {
            if (feedButton == null)
            {
                return;
            }

            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            bool bigu = gi != null && gi.Shelter != null && gi.Shelter.IsBiguExempt(survivor);
            bool fedToday = shelter.IsFedToday(survivor);
            bool inFeedPhase = state.currentPhase == GameplayPhase.ExpeditionPrep;

            Image buttonBg = feedButton.GetComponent<Image>();
            if (bigu)
            {
                SetButtonState(feedButton, buttonBg, "辟谷中", false, DisabledGray);
            }
            else if (fedToday)
            {
                SetButtonState(feedButton, buttonBg, "已分配食物", false, PressedGray);
            }
            else if (!inFeedPhase)
            {
                // 不在“出征准备·每日分配”阶段：按钮置灰但保持“分配食物”文案，方便区分原因。
                SetButtonState(feedButton, buttonBg, "分配食物", false, DisabledGray);
            }
            else if (state.foodStock < 1)
            {
                SetButtonState(feedButton, buttonBg, "粮食不足", false, DisabledGray);
            }
            else
            {
                SetButtonState(feedButton, buttonBg, "分配食物", true, FeedGreen);
            }
        }

        private static void SetButtonState(Button button, Image bg, string label, bool interactable, Color color)
        {
            button.interactable = interactable;
            if (bg != null)
            {
                bg.color = color;
            }

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = label;
                text.color = interactable ? Color.white : new Color(0.55f, 0.58f, 0.63f, 1f);
            }
        }

        private void OnFeedSelected()
        {
            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            if (gi == null || gi.Shelter == null || gi.Gameplay == null)
            {
                return;
            }

            Survivor selected = FindAliveByDefId(gi.Shelter, selectedDefId);
            if (selected == null)
            {
                return;
            }

            if (gi.Shelter.AllocateFood(selected, 1))
            {
                Refresh();
                flow.RefreshGlobalHud();
            }
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

        // ---------- 图鉴 ----------

        private void OpenCodex()
        {
            EnsureCodexView();
            if (codexView != null)
            {
                codexView.Open();
            }
        }

        private void CloseCodex()
        {
            if (codexView != null)
            {
                codexView.Close();
            }
        }

        private void EnsureCodexView()
        {
            if (codexView != null)
            {
                return;
            }

            codexView = FindObjectOfType<ShelterCodexView>(true);
            if (codexView == null)
            {
                Transform parent = transform.parent != null ? transform.parent : transform;
                codexView = ShelterCodexView.Build(parent, flow);
            }

            if (codexView != null)
            {
                codexView.Wire(flow);
            }
        }

        // ---------- 杂项（公告 / 腐蚀 / 工具） ----------

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

        // ---------- 布局 ----------

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

            if (manualRoot != null)
            {
                BindManualRefs();
                layoutBuilt = true;
                return;
            }

            DestroyAllChildren();
            BuildLayout();
            layoutBuilt = true;
        }

        private void BindManualRefs()
        {
            taskNodeLabels.Clear();
            taskNodeObjects.Clear();
            taskArrowObjects.Clear();
            taskCollapsibleObjects.Clear();
            manualTaskBarCaptured = false;

            if (taskNodeTexts != null)
            {
                for (int i = 0; i < taskNodeTexts.Length; i++)
                {
                    if (taskNodeTexts[i] != null)
                    {
                        taskNodeLabels.Add(taskNodeTexts[i]);
                        taskNodeObjects.Add(taskNodeTexts[i].gameObject);
                    }
                }
            }

            if (taskArrows != null)
            {
                for (int i = 0; i < taskArrows.Length; i++)
                {
                    if (taskArrows[i] != null)
                    {
                        taskArrowObjects.Add(taskArrows[i]);
                    }
                }
            }

            if (taskExtraObjects != null)
            {
                for (int i = 0; i < taskExtraObjects.Length; i++)
                {
                    if (taskExtraObjects[i] != null)
                    {
                        taskCollapsibleObjects.Add(taskExtraObjects[i]);
                    }
                }
            }

            if (taskBarRoot != null && taskToggleButton != null)
            {
                manualTaskBarSize = taskBarRoot.sizeDelta;
                manualTogglePos = taskToggleButton.GetComponent<RectTransform>().anchoredPosition;
                manualTaskBarCaptured = true;
            }
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
            // 房间场景层最先创建，后续顶部 UI 会盖在其上。
            roomSceneRoot = CreateRect(transform, "RoomScene", new Vector2(0f, -140f), new Vector2(1920f, 760f));
            roomBgImage = UiFactory.CreateImage(roomSceneRoot, "Bg_Room", Vector2.zero, new Vector2(1920f, 760f), new Color(0.10f, 0.11f, 0.13f, 1f));
            roomBgImage.raycastTarget = false;
            roomTitleText = UiFactory.CreateText(roomSceneRoot, "Txt_RoomTitle", "大厅", 64, new Vector2(0f, 180f), new Vector2(500f, 90f), TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.08f));
            roomTitleText.raycastTarget = false;

            dayText = UiFactory.CreateText(transform, "Txt_Day", "第 1 天", 36, new Vector2(0f, -110f), new Vector2(420f, 48f), TextAlignmentOptions.Center, Color.white);
            dayText.raycastTarget = false;

            roomLabelText = UiFactory.CreateText(transform, "Txt_RoomLabel", "大厅", 26, new Vector2(650f, -110f), new Vector2(240f, 40f), TextAlignmentOptions.Right, new Color(0.90f, 0.92f, 0.95f, 1f));
            roomLabelText.raycastTarget = false;

            phaseText = UiFactory.CreateText(transform, "Txt_Phase", "", 16, new Vector2(0f, -70f), new Vector2(420f, 26f), TextAlignmentOptions.Center, new Color(0.55f, 0.60f, 0.66f, 1f));
            phaseText.raycastTarget = false;

            BuildTaskBar();

            codexButton = UiFactory.CreateButton(transform, "Btn_Codex", "图鉴", OpenCodex, new Vector2(810f, -120f), new Vector2(110f, 42f), new Color(0.24f, 0.27f, 0.33f, 1f), 20);

            leftArrowButton = UiFactory.CreateButton(transform, "Btn_RoomLeft", "<", () => SwitchRoom(-1), new Vector2(-900f, -300f), new Vector2(56f, 64f), new Color(0.20f, 0.23f, 0.28f, 1f), 34);
            rightArrowButton = UiFactory.CreateButton(transform, "Btn_RoomRight", ">", () => SwitchRoom(1), new Vector2(900f, -300f), new Vector2(56f, 64f), new Color(0.20f, 0.23f, 0.28f, 1f), 34);

            departButton = UiFactory.CreateButton(transform, "Btn_Depart", "出发", null, new Vector2(0f, -520f), new Vector2(220f, 60f), null, 24);
            dayEndButton = UiFactory.CreateButton(transform, "Btn_DayEnd", "结束今天", null, new Vector2(0f, -500f), new Vector2(220f, 40f), new Color(0.22f, 0.32f, 0.28f, 1f), 20);
            settingsButton = UiFactory.CreateButton(transform, "Btn_Settings", "设置", null, new Vector2(-360f, -520f), new Vector2(150f, 48f), new Color(0.22f, 0.26f, 0.32f, 1f), 20);
            menuButton = UiFactory.CreateButton(transform, "Btn_Menu", "返回主菜单", null, new Vector2(360f, -520f), new Vector2(190f, 48f), new Color(0.22f, 0.26f, 0.32f, 1f), 20);

            BuildDetailPanel();

            GameObject fogGo = UiFactory.CreatePanel(transform, "FogOverlay", new Color(0.01f, 0.005f, 0.03f, 0f));
            fogOverlay = fogGo.GetComponent<Image>();
            fogOverlay.raycastTarget = false;

            bannerRoot = CreateEmptyRect(transform, "BulletLayer");
            detailGroup.SetActive(false);
        }

        private void BuildTaskBar()
        {
            taskBarRoot = CreateRect(transform, "TaskBar", new Vector2(0f, -172f), new Vector2(940f, 54f));
            Image taskBarBg = taskBarRoot.gameObject.AddComponent<Image>();
            taskBarBg.color = new Color(0.16f, 0.18f, 0.22f, 0.98f);
            taskBarBg.raycastTarget = false;

            taskNodeLabels.Clear();
            taskNodeObjects.Clear();
            taskArrowObjects.Clear();
            taskCollapsibleObjects.Clear();

            string[] names = { "分配食物", "外出战斗", "处理每日事件" };
            float[] xs = { -320f, -75f, 170f };
            for (int i = 0; i < names.Length; i++)
            {
                TextMeshProUGUI label = UiFactory.CreateText(taskBarRoot, "Txt_Task_" + i, names[i], 22, new Vector2(xs[i], 0f), new Vector2(210f, 32f), TextAlignmentOptions.Center, ActiveColor);
                label.raycastTarget = false;
                taskNodeLabels.Add(label);
                taskNodeObjects.Add(label.gameObject);
            }

            float[] arrowXs = { -215f, 45f };
            for (int i = 0; i < arrowXs.Length; i++)
            {
                TextMeshProUGUI arrow = UiFactory.CreateText(taskBarRoot, "Txt_TaskArrow_" + i, "→", 20, new Vector2(arrowXs[i], 0f), new Vector2(40f, 28f), TextAlignmentOptions.Center, new Color(0.55f, 0.60f, 0.66f, 1f));
                arrow.raycastTarget = false;
                taskArrowObjects.Add(arrow.gameObject);
            }

            taskToggleButton = UiFactory.CreateButton(taskBarRoot, "Btn_TaskToggle", "R", ToggleTaskBar, new Vector2(412f, 0f), new Vector2(44f, 44f), new Color(0.30f, 0.34f, 0.40f, 1f), 22);
        }

        private void BuildDetailPanel()
        {
            detailGroup = UiFactory.CreatePanel(transform, "Panel_Detail", new Color(0.11f, 0.13f, 0.17f, 1f), false);
            RectTransform detailRt = detailGroup.GetComponent<RectTransform>();
            detailRt.anchorMin = new Vector2(0.5f, 0.5f);
            detailRt.anchorMax = new Vector2(0.5f, 0.5f);
            detailRt.anchoredPosition = new Vector2(670f, -340f);
            detailRt.sizeDelta = new Vector2(380f, 390f);

            UiFactory.CreateText(detailGroup.transform, "Txt_DetailTitle", "人物信息", 28, new Vector2(0f, 168f), new Vector2(360f, 40f), TextAlignmentOptions.Center, Color.white);

            Image frame = UiFactory.CreateImage(detailGroup.transform, "Frame_Avatar", new Vector2(0f, 108f), new Vector2(116f, 140f), new Color(0.28f, 0.32f, 0.38f, 1f));
            frame.raycastTarget = false;
            detailAvatarImage = UiFactory.CreateImage(frame.transform, "Avatar", Vector2.zero, new Vector2(104f, 128f), new Color(0.34f, 0.72f, 0.44f, 1f));
            detailAvatarImage.raycastTarget = false;

            detailIdentityText = UiFactory.CreateText(detailGroup.transform, "Txt_DetailIdentity", "", 24, new Vector2(0f, 42f), new Vector2(360f, 34f), TextAlignmentOptions.Center, Color.white);
            detailIdentityText.raycastTarget = false;
            detailNameText = detailIdentityText;

            detailAgeText = UiFactory.CreateText(detailGroup.transform, "Txt_DetailAge", "", 19, new Vector2(0f, 2f), new Vector2(360f, 28f), TextAlignmentOptions.Center, new Color(0.88f, 0.90f, 0.93f, 1f));
            detailAgeText.raycastTarget = false;
            detailStatusText = UiFactory.CreateText(detailGroup.transform, "Txt_DetailStatus", "", 19, new Vector2(0f, -30f), new Vector2(360f, 28f), TextAlignmentOptions.Center, new Color(0.88f, 0.90f, 0.93f, 1f));
            detailStatusText.raycastTarget = false;
            detailFitnessText = UiFactory.CreateText(detailGroup.transform, "Txt_DetailFitness", "", 19, new Vector2(0f, -62f), new Vector2(360f, 28f), TextAlignmentOptions.Center, new Color(0.88f, 0.90f, 0.93f, 1f));
            detailFitnessText.raycastTarget = false;
            detailQuoteText = UiFactory.CreateText(detailGroup.transform, "Txt_DetailQuote", "", 17, new Vector2(0f, -108f), new Vector2(380f, 78f), TextAlignmentOptions.Top, new Color(0.80f, 0.84f, 0.90f, 1f));
            detailQuoteText.raycastTarget = false;

            feedButton = UiFactory.CreateButton(detailGroup.transform, "Btn_Feed", "分配食物", null, new Vector2(0f, -172f), new Vector2(200f, 50f), FeedGreen, 20);
        }

        private static RectTransform CreateEmptyRect(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            UiFactory.Stretch(rt);
            return rt;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
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
