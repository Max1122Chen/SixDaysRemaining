using System.Collections.Generic;
using SixDaysRemaining.App;
using SixDaysRemaining.App.Meta;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 图鉴：已解锁结局 + 幸存者图鉴。结局解锁来自 Meta 档案；
    /// 幸存者在本局遇到（开局成员/入住）时自动解锁。
    /// </summary>
    public class ShelterCodexView : MonoBehaviour
    {
        private sealed class EndingInfo
        {
            public string Id;
            public string Title;
            public string Description;
        }

        private static readonly EndingInfo[] Endings =
        {
            new EndingInfo
            {
                Id = EndingIds.G,
                Title = "腐蚀融合",
                Description = "腐蚀值达到 100，庇护所被黑暗吞没。"
            },
            new EndingInfo
            {
                Id = EndingIds.E,
                Title = "交涉崩解",
                Description = "政治家率队出征却战败，承诺化为泡影。"
            },
            new EndingInfo
            {
                Id = EndingIds.MaxDay,
                Title = "第七日",
                Description = "撑过第六天，迎来未知的第七日。"
            },
            new EndingInfo
            {
                Id = EndingIds.Debug,
                Title = "调试结局",
                Description = "仅供测试使用的结局。"
            }
        };

        private AppFlowController flow;

        /// <summary>手动搭建模式：图鉴覆盖层根节点（不填则使用本组件所在物体）。</summary>
        [SerializeField]
        private GameObject codexGroup;

        [SerializeField]
        private TextMeshProUGUI endingsText;

        [SerializeField]
        private RectTransform survivorContent;

        /// <summary>手动搭建简化模式：用一个普通 TMP 文本框显示幸存者列表（替代 ScrollRect）。</summary>
        [SerializeField]
        private TextMeshProUGUI survivorText;

        [SerializeField]
        private Button closeButton;

        private readonly List<GameObject> survivorEntries = new List<GameObject>();

        public static ShelterCodexView Build(Transform parent, AppFlowController flow)
        {
            GameObject overlay = UiFactory.CreatePanel(parent, "ShelterCodexOverlay", new Color(0f, 0f, 0f, 0.74f));
            ShelterCodexView view = overlay.AddComponent<ShelterCodexView>();
            view.codexGroup = overlay;

            GameObject panel = UiFactory.CreatePanel(overlay.transform, "CodexPanel", UiFactory.PanelLight, false);
            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(1080f, 660f);

            UiFactory.CreateText(panel.transform, "Txt_CodexTitle", "图鉴", 40, new Vector2(0f, 285f), new Vector2(400f, 56f), TextAlignmentOptions.Center, Color.white);

            UiFactory.CreateText(panel.transform, "Txt_EndingSectionTitle", "结局图鉴", 26, new Vector2(-350f, 215f), new Vector2(320f, 40f), TextAlignmentOptions.Center, UiFactory.Accent);
            view.endingsText = UiFactory.CreateText(panel.transform, "Txt_EndingList", "", 19, new Vector2(-350f, 55f), new Vector2(380f, 300f), TextAlignmentOptions.TopLeft, UiFactory.TextColor);
            view.endingsText.raycastTarget = false;

            UiFactory.CreateText(panel.transform, "Txt_SurvivorSectionTitle", "幸存者图鉴", 26, new Vector2(290f, 215f), new Vector2(360f, 40f), TextAlignmentOptions.Center, UiFactory.Accent);
            UiFactory.CreateScrollArea(panel.transform, "Scroll_Survivor", new Vector2(290f, 25f), new Vector2(440f, 420f), out view.survivorContent);

            view.closeButton = UiFactory.CreateButton(panel.transform, "Btn_CloseCodex", "关闭", null, new Vector2(0f, -290f), new Vector2(180f, 48f), new Color(0.30f, 0.34f, 0.40f, 1f), 20);

            view.Wire(flow);
            overlay.SetActive(false);
            return view;
        }

        public void Wire(AppFlowController appFlow)
        {
            flow = appFlow;
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
        }

        public void Open()
        {
            Refresh();
            GameObject target = codexGroup != null ? codexGroup : gameObject;
            target.SetActive(true);
            target.transform.SetAsLastSibling();
        }

        public void Close()
        {
            GameObject target = codexGroup != null ? codexGroup : gameObject;
            target.SetActive(false);
        }

        public void Refresh()
        {
            ReconcileUnlocks();
            RefreshEndingList();
            RefreshSurvivorList();
        }

        private void ReconcileUnlocks()
        {
            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            if (gi == null || gi.Meta == null || gi.Shelter == null)
            {
                return;
            }

            IReadOnlyList<Survivor> roster = gi.Shelter.Survivors;
            for (int i = 0; i < roster.Count; i++)
            {
                Survivor survivor = roster[i];
                if (survivor != null && !string.IsNullOrEmpty(survivor.defId))
                {
                    gi.Meta.UnlockSurvivor(survivor.defId);
                }
            }

            string[] starters = ShelterContent.StarterIds;
            for (int i = 0; i < starters.Length; i++)
            {
                gi.Meta.UnlockSurvivor(starters[i]);
            }
        }

        private void RefreshEndingList()
        {
            if (endingsText == null)
            {
                return;
            }

            MetaProfileService meta = GetMeta();
            List<string> lines = new List<string>();
            for (int i = 0; i < Endings.Length; i++)
            {
                EndingInfo info = Endings[i];
                bool unlocked = meta != null && meta.HasEnding(info.Id);
                if (unlocked)
                {
                    lines.Add(info.Title + "（已解锁）");
                    lines.Add("　" + info.Description);
                }
                else
                {
                    lines.Add("？？？");
                    lines.Add("　尚未解锁");
                }

                if (i < Endings.Length - 1)
                {
                    lines.Add("");
                }
            }

            endingsText.text = string.Join("\n", lines.ToArray());
        }

        private void RefreshSurvivorList()
        {
            ClearSurvivorEntries();
            if (survivorText != null)
            {
                survivorText.text = BuildSurvivorText();
                return;
            }

            if (survivorContent == null)
            {
                return;
            }

            MetaProfileService meta = GetMeta();
            IReadOnlyList<SurvivorDef> defs = ShelterContent.Survivors.All;
            for (int i = 0; i < defs.Count; i++)
            {
                SurvivorDef def = defs[i];
                if (def == null)
                {
                    continue;
                }

                CreateSurvivorEntry(def, meta != null && meta.HasSurvivor(def.Id), i);
            }

            float height = defs.Count * 84f + 24f;
            survivorContent.sizeDelta = new Vector2(0f, height);
        }

        /// <summary>纯文本模式的幸存者列表内容。</summary>
        private string BuildSurvivorText()
        {
            MetaProfileService meta = GetMeta();
            IReadOnlyList<SurvivorDef> defs = ShelterContent.Survivors.All;
            List<string> lines = new List<string>();
            for (int i = 0; i < defs.Count; i++)
            {
                SurvivorDef def = defs[i];
                if (def == null)
                {
                    continue;
                }

                bool unlocked = meta != null && meta.HasSurvivor(def.Id);
                if (unlocked)
                {
                    SurvivorProfile profile = ShelterProfiles.Resolve(def);
                    lines.Add(def.DisplayName + "（已解锁）");
                    lines.Add("　年龄 " + (profile.Age > 0 ? profile.Age + " 岁" : "未知")
                        + " · 身体素质 " + (string.IsNullOrEmpty(profile.Fitness) ? "未知" : profile.Fitness));
                    lines.Add("　语录：" + (string.IsNullOrEmpty(profile.Quote) ? "（暂无语录）" : profile.Quote));
                }
                else
                {
                    lines.Add("？？？");
                    lines.Add("　尚未解锁");
                }

                if (i < defs.Count - 1)
                {
                    lines.Add("");
                }
            }

            return string.Join("\n", lines.ToArray());
        }

        private void CreateSurvivorEntry(SurvivorDef def, bool unlocked, int index)
        {
            float y = -42f - index * 84f;
            Image row = UiFactory.CreateImage(survivorContent, "Row_" + def.Id, new Vector2(0f, y), new Vector2(404f, 74f), new Color(0.12f, 0.14f, 0.18f, 1f));
            row.raycastTarget = false;

            Sprite portrait = ShelterPortraits.Load(def, SurvivorStatus.Healthy, 1);
            Image avatar = portrait != null
                ? UiFactory.CreateImage(row.transform, "Avatar", new Vector2(-156f, 0f), new Vector2(50f, 62f), Color.white)
                : UiFactory.CreateCircleImage(row.transform, "Avatar", new Vector2(-156f, 0f), new Vector2(50f, 50f), new Color(0.32f, 0.36f, 0.42f, 1f));
            avatar.raycastTarget = false;
            if (portrait != null)
            {
                avatar.sprite = portrait;
                avatar.preserveAspect = true;
            }

            string name = unlocked ? def.DisplayName : "？？？";
            TextMeshProUGUI nameText = UiFactory.CreateText(row.transform, "Txt_Name", name, 22, new Vector2(-42f, 16f), new Vector2(180f, 30f), TextAlignmentOptions.Left, unlocked ? Color.white : new Color(0.45f, 0.48f, 0.54f, 1f));
            nameText.raycastTarget = false;

            string detail;
            Color detailColor;
            if (unlocked)
            {
                SurvivorProfile profile = ShelterProfiles.Resolve(def);
                detail = "年龄 " + (profile.Age > 0 ? profile.Age + " 岁" : "未知") + " · 身体素质 " + (string.IsNullOrEmpty(profile.Fitness) ? "未知" : profile.Fitness);
                detailColor = new Color(0.72f, 0.76f, 0.82f, 1f);
            }
            else
            {
                detail = "尚未解锁";
                detailColor = new Color(0.45f, 0.48f, 0.54f, 1f);
            }

            TextMeshProUGUI detailText = UiFactory.CreateText(row.transform, "Txt_Detail", detail, 16, new Vector2(-42f, -16f), new Vector2(300f, 26f), TextAlignmentOptions.Left, detailColor);
            detailText.raycastTarget = false;
            survivorEntries.Add(row.gameObject);
        }

        private void ClearSurvivorEntries()
        {
            for (int i = 0; i < survivorEntries.Count; i++)
            {
                if (survivorEntries[i] != null)
                {
                    survivorEntries[i].SetActive(false);
                    Destroy(survivorEntries[i]);
                }
            }

            survivorEntries.Clear();
        }

        private MetaProfileService GetMeta()
        {
            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            return gi != null ? gi.Meta : null;
        }
    }
}
