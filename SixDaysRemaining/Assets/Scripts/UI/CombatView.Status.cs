using System;
using System.Collections;
using System.Collections.Generic;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.App;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;
using SixDaysRemaining.Combat.Traits;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    public partial class CombatView
    {
        private void BuildRoundProgress(Transform parent)
        {
            Image bar = UiFactory.CreateImage(
                parent,
                "RoundProgressBar",
                new Vector2(0f, 300f),
                new Vector2(640f, 26f),
                new Color(0.10f, 0.12f, 0.15f, 1f));
            bar.raycastTarget = false;

            Image fill = UiFactory.CreateImage(bar.transform, "Fill", Vector2.zero, Vector2.zero, UiFactory.Accent);
            fill.raycastTarget = false;
            RectTransform fillRt = fill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            roundProgressFill = fill;

            RebuildRoundMarkers();

            roundProgressText = UiFactory.CreateText(parent, "Txt_RoundProgress", "", 18, new Vector2(0f, 262f), new Vector2(760f, 30f));
            roundProgressText.raycastTarget = false;
            roundProgressText.color = new Color(0.88f, 0.90f, 0.92f, 1f);
            if (totalRoundLabel == null)
            {
                totalRoundLabel = UiFactory.CreateText(parent, "Txt_TotalNum", "累计回合数", 18, new Vector2(0f, 330f), new Vector2(180f, 30f));
                totalRoundLabel.raycastTarget = false;
                totalRoundLabel.color = new Color(0.88f, 0.90f, 0.92f, 1f);
            }

            UpdateRoundProgress();
        }

        private void EnsureRoundProgress()
        {
            if (totalRoundLabel == null)
            {
                totalRoundLabel = FindChildText(transform, "Txt_TotalNum");
            }

            if (totalRoundLabel != null)
            {
                totalRoundLabel.text = "累计回合数";
            }

            if (roundProgressText == null)
            {
                roundProgressText = FindChildText(transform, "Txt_RoundProgress");
            }

            if (roundProgressFill == null || roundProgressText == null)
            {
                BuildRoundProgress(transform);
            }
            else
            {
                RebuildRoundMarkers();
                UpdateRoundProgress();
            }
        }

        private void RebuildRoundMarkers()
        {
            if (roundProgressFill == null)
            {
                return;
            }

            RectTransform bar = roundProgressFill.transform.parent as RectTransform;
            if (bar == null)
            {
                return;
            }

            List<Transform> oldMarkers = new List<Transform>();
            for (int i = 0; i < bar.childCount; i++)
            {
                Transform child = bar.GetChild(i);
                if (child != null && child.name.StartsWith("Marker", StringComparison.Ordinal))
                {
                    oldMarkers.Add(child);
                }
            }

            for (int i = 0; i < oldMarkers.Count; i++)
            {
                if (oldMarkers[i] != null)
                {
                    Destroy(oldMarkers[i].gameObject);
                }
            }

            Color markerColor = new Color(0.36f, 0.40f, 0.47f, 1f);
            float halfWidth = bar.rect.width * 0.5f;
            int[] rounds = CombatRewardTable.MarkerRounds;
            for (int i = 0; i < rounds.Length; i++)
            {
                float normalized = CombatRewardTable.Progress01(rounds[i]);
                float x = -halfWidth + bar.rect.width * normalized;
                Image marker = UiFactory.CreateImage(
                    bar,
                    "Marker" + rounds[i],
                    new Vector2(x, 0f),
                    new Vector2(3f, 26f),
                    markerColor);
                marker.raycastTarget = false;
            }
        }

        private void EnsureCombatStatusUi()
        {
            EnsureHpBars();
            EnsureEnemyActionSlots();
            EnsureTransitionOverlay();
            EnsureMonsterPortrait();
        }

        /// <summary>
        /// 怪物立绘位：场景手搭的 Img_Monster（战斗界面根节点下的 611x611 子物体）。
        /// 未在 Inspector 拖引用时按名字查找；找不到则立绘功能静默失效。
        /// </summary>
        private void EnsureMonsterPortrait()
        {
            if (monsterPortrait != null)
            {
                return;
            }

            monsterPortrait = FindChildImage(transform, "Img_Monster");
            if (monsterPortrait != null)
            {
                monsterPortrait.raycastTarget = false;
            }
        }

        /// <summary>
        /// 把当前遭遇的怪物立绘加载到 Img_Monster 上；未加载到资源时隐藏该元素。
        /// </summary>
        private void RefreshMonsterArt()
        {
            EnsureMonsterPortrait();
            if (monsterPortrait == null)
            {
                return;
            }

            GameInstance gi = flow != null ? flow.Game : null;
            EnemyCombatComponent enemy = gi != null && gi.Combat != null && gi.Combat.Session != null
                && gi.Combat.Session.Enemies.Count > 0
                ? gi.Combat.Session.Enemies[0]
                : null;
            Sprite art = LoadMonsterArt(enemy);
            monsterPortrait.gameObject.SetActive(art != null);
            if (art != null)
            {
                monsterPortrait.sprite = art;
                monsterPortrait.preserveAspect = true;
                monsterPortrait.color = Color.white;
            }
        }

        private void EnsureHpBars()
        {
            if (playerHpFill == null || playerHpText == null)
            {
                BuildPlayerHpBar(transform);
            }

            if (enemyHpFill == null || enemyHpText == null)
            {
                BuildEnemyHpBar(transform);
            }
        }

        private void BuildPlayerHpBar(Transform parent)
        {
            Image bar = UiFactory.CreateImage(
                parent,
                "PlayerHpBar",
                new Vector2(140f, 190f),
                new Vector2(320f, 24f),
                HpBarColor);
            RectTransform barRt = bar.rectTransform;
            barRt.anchorMin = Vector2.zero;
            barRt.anchorMax = Vector2.zero;
            barRt.anchoredPosition = new Vector2(140f, 190f);
            bar.raycastTarget = false;

            playerHpFill = UiFactory.CreateImage(bar.transform, "Fill", Vector2.zero, Vector2.zero, HpFullColor);
            playerHpFill.raycastTarget = false;
            RectTransform fillRt = playerHpFill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            playerHpText = UiFactory.CreateText(parent, "Txt_PlayerHp", "HP -/-", 14, new Vector2(140f, 162f), new Vector2(320f, 22f));
            RectTransform textRt = playerHpText.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.zero;
            textRt.anchoredPosition = new Vector2(140f, 162f);
            playerHpText.raycastTarget = false;
        }

        private void BuildEnemyHpBar(Transform parent)
        {
            Image bar = UiFactory.CreateImage(
                parent,
                "EnemyHpBar",
                new Vector2(-160f, -90f),
                new Vector2(600f, 24f),
                HpBarColor);
            RectTransform barRt = bar.rectTransform;
            barRt.anchorMin = Vector2.one;
            barRt.anchorMax = Vector2.one;
            barRt.anchoredPosition = new Vector2(-160f, -90f);
            bar.raycastTarget = false;

            enemyHpFill = UiFactory.CreateImage(bar.transform, "Fill", Vector2.zero, Vector2.zero, HpFullColor);
            enemyHpFill.raycastTarget = false;
            RectTransform fillRt = enemyHpFill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            enemyHpText = UiFactory.CreateText(parent, "Txt_EnemyHp", "HP -/-", 14, new Vector2(-160f, -120f), new Vector2(600f, 22f));
            RectTransform textRt = enemyHpText.rectTransform;
            textRt.anchorMin = Vector2.one;
            textRt.anchorMax = Vector2.one;
            textRt.anchoredPosition = new Vector2(-160f, -120f);
            enemyHpText.raycastTarget = false;
        }

        private void EnsureEnemyActionSlots()
        {
            if (enemyActionSlots == null || enemyActionSlots.Length != SlotCount)
            {
                enemyActionSlots = new EnemyActionSlotView[SlotCount];
            }

            Transform slotParent = cardLayer != null ? cardLayer : transform;
            for (int i = 0; i < enemyActionSlots.Length; i++)
            {
                if (enemyActionSlots[i] != null)
                {
                    enemyActionSlots[i].Setup(i);
                    continue;
                }

                enemyActionSlots[i] = EnemyActionSlotView.Create(
                    slotParent,
                    i,
                    EnemyActionSlotPos(i),
                    EnemyActionSlotSize);
            }
        }

        private Vector2 EnemyActionSlotPos(int index)
        {
            Vector2 pos = CurrentSlotPos(index);
            pos.y += 190f;
            return pos;
        }

        private void EnsureTransitionOverlay()
        {
            if (transitionGroup != null)
            {
                return;
            }

            GameObject overlay = UiFactory.CreatePanel(
                transform,
                "RoundTransition",
                new Color(0.02f, 0.02f, 0.04f, 0.97f));
            CanvasGroup group = overlay.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            overlay.SetActive(false);

            TextMeshProUGUI label = UiFactory.CreateText(
                overlay.transform,
                "Txt_NextRound",
                "下一回合",
                34,
                Vector2.zero,
                new Vector2(800f, 80f));
            label.color = Color.white;
            transitionGroup = group;
        }

        private void RefreshHpBars()
        {
            GameInstance gi = flow != null ? flow.Game : null;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null)
            {
                return;
            }

            PlayerCombatComponent player = gi.Combat.Session.Player;
            SetHpBar(
                playerHpFill,
                playerHpText,
                player != null ? player.Attributes.HP : 0f,
                player != null ? player.Attributes.MaxHP : 1f,
                player != null ? player.Attributes.Block : 0f);

            EnemyCombatComponent enemy = gi.Combat.Session.Enemies.Count > 0
                ? gi.Combat.Session.Enemies[0]
                : null;
            SetHpBar(
                enemyHpFill,
                enemyHpText,
                enemy != null ? enemy.Attributes.HP : 0f,
                enemy != null ? enemy.Attributes.MaxHP : 1f,
                enemy != null ? enemy.Attributes.Block : 0f);
        }

        private static void SetHpBar(
            Image fill,
            TextMeshProUGUI text,
            float hp,
            float maxHp,
            float block)
        {
            if (fill == null || text == null)
            {
                return;
            }

            float ratio = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
            RectTransform rt = fill.rectTransform;
            Vector2 anchorMax = rt.anchorMax;
            anchorMax.x = ratio;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            fill.color = ratio <= 0.25f ? HpLowColor : (ratio <= 0.5f ? HpMidColor : HpFullColor);
            text.text = "HP " + CardText.FormatNumber(hp)
                + "/" + CardText.FormatNumber(maxHp)
                + "  格挡 " + CardText.FormatNumber(block);
        }

        private void RefreshEnemyActions()
        {
            if (enemyActionSlots == null)
            {
                return;
            }

            GameInstance gi = flow != null ? flow.Game : null;
            EnemyCombatComponent enemy = gi != null && gi.Combat != null && gi.Combat.Session != null
                && gi.Combat.Session.Enemies.Count > 0
                ? gi.Combat.Session.Enemies[0]
                : null;
            CardInstance[] intents = enemy != null ? enemy.GetRoundCards() : null;
            for (int i = 0; i < enemyActionSlots.Length; i++)
            {
                if (enemyActionSlots[i] == null)
                {
                    continue;
                }

                CardDef def = intents != null && i < intents.Length && intents[i] != null
                    ? intents[i].Def
                    : null;
                enemyActionSlots[i].SetCard(def);
            }
        }

        /// <summary>
        /// 根据当前遭遇加载 Resources/Monster/ 下的怪物立绘；
        /// 强化版（Mob01Boost/Mob02Boost）沿用 01/02 的形象。未加载到资源时返回 null。
        /// </summary>
        private static Sprite LoadMonsterArt(EnemyCombatComponent enemy)
        {
            EnemyEncounterDef def = enemy != null ? enemy.Encounter : null;
            if (def == null)
            {
                return null;
            }

            string key;
            switch (def.Id)
            {
                case EncounterIds.Mob01:
                case EncounterIds.Mob01Boost:
                    key = "Monster_01";
                    break;
                case EncounterIds.Mob02:
                case EncounterIds.Mob02Boost:
                    key = "Monster_02";
                    break;
                case EncounterIds.Mob03:
                    key = "Monster_03";
                    break;
                default:
                    key = "Monster_" + def.Id.ToString("D2");
                    break;
            }

            return Resources.Load<Sprite>("Monster/" + key);
        }

        private void SetEnemyActionActive(int index, bool on)
        {
            if (enemyActionSlots == null)
            {
                return;
            }

            for (int i = 0; i < enemyActionSlots.Length; i++)
            {
                if (enemyActionSlots[i] != null)
                {
                    enemyActionSlots[i].SetActive(i == index && on);
                }
            }
        }

        private void EnsureChosenHighlights()
        {
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                Image highlight = FindChosenHighlight("CardChosen_" + i);
                if (highlight == null && slots[i] != null)
                {
                    highlight = FindChildImage(slots[i].transform, "CardChosen");
                }

                if (highlight == null)
                {
                    continue;
                }

                MoveChosenHighlightToLayer(highlight, "CardChosen_" + i);
                cardChosenHighlights[i] = highlight;
            }

            if (enemyActionSlots == null)
            {
                return;
            }

            for (int i = 0; i < enemyActionSlots.Length; i++)
            {
                Image highlight = FindChosenHighlight("EnemyChosen_" + i);
                if (highlight == null && enemyActionSlots[i] != null)
                {
                    highlight = FindChildImage(enemyActionSlots[i].transform, "EnemyChosen");
                }

                if (highlight == null)
                {
                    continue;
                }

                MoveChosenHighlightToLayer(highlight, "EnemyChosen_" + i);
                enemyChosenHighlights[i] = highlight;
            }
        }

        private void EnsureChosenHighlightLayer()
        {
            Transform existing = transform != null
                ? FindChildTransform(transform, "ChosenHighlightLayer")
                : null;
            if (existing != null)
            {
                chosenHighlightLayer = existing as RectTransform;
            }
            else
            {
                GameObject go = new GameObject("ChosenHighlightLayer");
                go.transform.SetParent(transform, false);
                chosenHighlightLayer = go.AddComponent<RectTransform>();
                chosenHighlightLayer.anchorMin = Vector2.zero;
                chosenHighlightLayer.anchorMax = Vector2.one;
                chosenHighlightLayer.pivot = new Vector2(0.5f, 0.5f);
                chosenHighlightLayer.offsetMin = Vector2.zero;
                chosenHighlightLayer.offsetMax = Vector2.zero;
                chosenHighlightLayer.localScale = Vector3.one;
            }

            if (chosenHighlightLayer != null)
            {
                chosenHighlightLayer.SetAsLastSibling();
            }
        }

        private Image FindChosenHighlight(string exactName)
        {
            if (chosenHighlightLayer == null)
            {
                return null;
            }

            for (int i = 0; i < chosenHighlightLayer.childCount; i++)
            {
                Transform child = chosenHighlightLayer.GetChild(i);
                if (child == null || child.name != exactName)
                {
                    continue;
                }

                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    return image;
                }
            }

            return null;
        }

        private void MoveChosenHighlightToLayer(Image highlight, string layerName)
        {
            if (highlight == null || chosenHighlightLayer == null)
            {
                return;
            }

            if (highlight.transform.parent != chosenHighlightLayer)
            {
                highlight.transform.SetParent(chosenHighlightLayer, true);
            }

            highlight.gameObject.name = layerName;
            highlight.raycastTarget = false;
            highlight.rectTransform.SetAsLastSibling();
        }

        private static Image FindChildImage(Transform parent, string namePrefix)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith(namePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    return image;
                }
            }

            return null;
        }

        private static TextMeshProUGUI FindChildText(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (child.name == name)
                {
                    TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
                    if (text != null)
                    {
                        return text;
                    }
                }

                TextMeshProUGUI nested = FindChildText(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static RectTransform FindChildTransform(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (child.name == name)
                {
                    return child as RectTransform;
                }

                RectTransform nested = FindChildTransform(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private void SetCardChosenHighlight(int index, bool on)
        {
            if (index < 0 || index >= cardChosenHighlights.Length)
            {
                return;
            }

            Image highlight = cardChosenHighlights[index];
            if (highlight == null)
            {
                return;
            }

            StopChosenPop(ref cardChosenPops[index]);
            highlight.gameObject.SetActive(on);
            if (on)
            {
                RectTransform rt = highlight.rectTransform;
                if (rt != null)
                {
                    rt.localScale = new Vector3(0.82f, 0.82f, 1f);
                    cardChosenPops[index] = StartCoroutine(UiAnim.Scale(rt, Vector3.one, 0.16f));
                }

                StartCardChosenPulse(index);
            }
            else
            {
                if (highlight.rectTransform != null)
                {
                    highlight.rectTransform.localScale = Vector3.one;
                }

                StopCardChosenPulse(index);
            }
        }

        private void SetEnemyChosenHighlight(int index, bool on)
        {
            if (index < 0 || index >= enemyChosenHighlights.Length)
            {
                return;
            }

            Image highlight = enemyChosenHighlights[index];
            if (highlight == null)
            {
                return;
            }

            StopChosenPop(ref enemyChosenPops[index]);
            highlight.gameObject.SetActive(on);
            if (on)
            {
                RectTransform rt = highlight.rectTransform;
                if (rt != null)
                {
                    rt.localScale = new Vector3(0.82f, 0.82f, 1f);
                    enemyChosenPops[index] = StartCoroutine(UiAnim.Scale(rt, Vector3.one, 0.16f));
                }
            }
            else if (highlight.rectTransform != null)
            {
                highlight.rectTransform.localScale = Vector3.one;
            }
        }

        private void ClearChosenHighlights()
        {
            for (int i = 0; i < cardChosenHighlights.Length; i++)
            {
                SetCardChosenHighlight(i, false);
            }

            for (int i = 0; i < enemyChosenHighlights.Length; i++)
            {
                SetEnemyChosenHighlight(i, false);
            }
        }

        private void StopChosenPop(ref Coroutine routine)
        {
            if (routine == null)
            {
                return;
            }

            StopCoroutine(routine);
            routine = null;
        }

        private void StartCardChosenPulse(int index)
        {
            StopCardChosenPulse(index);
            CardView card = slotCards != null && index >= 0 && index < slotCards.Length
                ? slotCards[index]
                : null;
            if (card == null || card.Rect == null)
            {
                return;
            }

            cardChosenPulses[index] = StartCoroutine(CardChosenPulseRoutine(card.Rect));
        }

        private void StopCardChosenPulse(int index)
        {
            if (index < 0 || index >= cardChosenPulses.Length)
            {
                return;
            }

            if (cardChosenPulses[index] != null)
            {
                StopCoroutine(cardChosenPulses[index]);
                cardChosenPulses[index] = null;
            }

            CardView card = slotCards != null && index < slotCards.Length
                ? slotCards[index]
                : null;
            if (card != null && card.Rect != null)
            {
                card.Rect.localScale = Vector3.one;
            }
        }

        private IEnumerator CardChosenPulseRoutine(RectTransform rt)
        {
            const float PulseScale = 1.06f;
            const float PulseDuration = 0.3f;
            Vector3 raisedScale = new Vector3(PulseScale, PulseScale, 1f);
            while (true)
            {
                if (rt == null)
                {
                    yield break;
                }

                yield return StartCoroutine(UiAnim.Scale(rt, raisedScale, PulseDuration));
                if (rt == null)
                {
                    yield break;
                }

                yield return StartCoroutine(UiAnim.Scale(rt, Vector3.one, PulseDuration));
            }
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        private void UpdateRoundProgress()
        {
            if (roundProgressFill == null || roundProgressText == null)
            {
                return;
            }

            GameInstance gi = flow != null ? flow.Game : null;
            if (gi == null || gi.Combat == null)
            {
                roundProgressText.text = "准备";
                SetRoundProgress(0f);
                if (totalRoundLabel != null)
                {
                    totalRoundLabel.text = "累计回合数0";
                }

                return;
            }

            if (gi.Combat.IsFinished)
            {
                roundProgressText.text = "准备";
                SetRoundProgress(0f);
                if (totalRoundLabel != null)
                {
                    totalRoundLabel.text = "累计回合数" + Mathf.Max(0, gi.Combat.Result.TurnsElapsed);
                }

                return;
            }

            int currentRound = Mathf.Max(0, gi.Combat.CurrentRound);
            int round = Mathf.Max(1, gi.Combat.IsRoundActive
                ? currentRound
                : gi.Combat.NextRound);
            roundProgressText.text = "第" + round + "回合";
            if (totalRoundLabel != null)
            {
                totalRoundLabel.text = "累计回合数" + currentRound;
            }

            SetRoundProgress(CombatRewardTable.Progress01(round));
        }

        private void SetRoundProgress(float value)
        {
            RectTransform rt = roundProgressFill.rectTransform;
            Vector2 anchorMax = rt.anchorMax;
            anchorMax.x = Mathf.Clamp01(value);
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void ConfigureRaycastTargets()
        {
            if (roundProgressFill != null)
            {
                roundProgressFill.raycastTarget = false;
            }

            if (roundProgressText != null)
            {
                roundProgressText.raycastTarget = false;
            }

            if (totalRoundLabel != null)
            {
                totalRoundLabel.raycastTarget = false;
            }

            if (playerHpFill != null)
            {
                playerHpFill.raycastTarget = false;
            }

            if (playerHpText != null)
            {
                playerHpText.raycastTarget = false;
            }

            if (enemyHpFill != null)
            {
                enemyHpFill.raycastTarget = false;
            }

            if (enemyHpText != null)
            {
                enemyHpText.raycastTarget = false;
            }

            if (cardLayer != null)
            {
                Image layerImage = cardLayer.GetComponent<Image>();
                if (layerImage != null)
                {
                    layerImage.raycastTarget = false;
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

        public void OpenCombat()
        {
            // 上一场战斗结算时输入会被关掉，这里在每轮战斗开始前复位。
            inputEnabled = true;
            Refresh();
            ClearChosenHighlights();
        }

        public void Refresh()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null)
            {
                return;
            }

            PlayerCombatComponent player = gi.Combat.Session.Player;
            RefreshTraitBar(player, gi);
            flow.RefreshGlobalHud();

            RefreshMonsterArt();
            RefreshHpBars();
            RefreshEnemyActions();
            UpdateRoundProgress();
            RebuildCards(player);
            UpdateButtons();
        }

        private void RefreshStatusOnly()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null)
            {
                return;
            }

            PlayerCombatComponent player = gi.Combat.Session.Player;
            RefreshTraitBar(player, gi);
            flow.RefreshGlobalHud();
            RefreshMonsterArt();
            RefreshHpBars();
            RefreshEnemyActions();
            UpdateRoundProgress();
        }

        private void RefreshTraitBar(PlayerCombatComponent player, GameInstance gi)
        {
            if (traitBar == null)
            {
                return;
            }

            bool playerTurn = gi != null && gi.Combat != null && gi.Combat.IsPlayerTurn;
            traitBar.Refresh(gi != null ? gi.Shelter : null, player, playerTurn);
        }

        private void OnTraitClicked(SurvivorTrait trait)
        {
            if (trait == null)
            {
                return;
            }

            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null)
            {
                return;
            }

            PlayerCombatComponent player = gi.Combat.Session.Player;
            if (trait.Trigger != TraitTrigger.ManualOnce)
            {
                return;
            }

            if (player.TryUseTrait(trait, gi.Combat.Session))
            {
                RefreshStatusOnly();
            }
        }

        private void UpdateButtons()
        {
            GameInstance gi = flow.Game;
            bool playerTurn = gi != null && gi.Combat != null && gi.Combat.IsPlayerTurn && !gi.Combat.IsFinished;
            if (commitButton != null)
            {
                // 允许空槽；随时可确认开战（含 0 张，将触发消极惩罚）。
                commitButton.interactable = playerTurn;
            }
        }
    }
}
