using System.Collections.Generic;
using SixDaysRemaining.App.Ending;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;

namespace SixDaysRemaining.App
{
    /// <summary>
    /// 结局规则评估（END-F01 / END-F02）。条件与文案来自 EndingContent JSON。
    /// </summary>
    public static class EndingEvaluator
    {
        public static bool TryResolveCombatEnd(
            CombatResult result,
            ShelterManager shelter,
            out string endingId)
        {
            endingId = null;
            if (result.RunEndedByCorruption || result.Outcome != CombatOutcome.Lose)
            {
                return false;
            }

            EndingDef def = Select(EndingTrigger.CombatLose, shelter, ReadCorruption(shelter));
            if (def == null)
            {
                return false;
            }

            endingId = def.Id;
            return true;
        }

        public static bool TryResolvePopulationZero(ShelterManager shelter, out string endingId)
        {
            endingId = null;
            if (shelter == null || shelter.Population > 0)
            {
                return false;
            }

            EndingDef def = Select(EndingTrigger.PopulationZero, shelter, ReadCorruption(shelter));
            if (def == null)
            {
                endingId = EndingIds.F;
                return true;
            }

            endingId = def.Id;
            return true;
        }

        public static bool TryResolveRunComplete(ShelterManager shelter, GameplaySubsystem gameplay, out string endingId)
        {
            endingId = null;
            int corruption = gameplay != null && gameplay.State != null ? gameplay.State.corruption : 0;
            EndingDef def = Select(EndingTrigger.RunComplete, shelter, corruption);
            if (def == null)
            {
                endingId = EndingIds.MaxDay;
                return true;
            }

            endingId = def.Id;
            return true;
        }

        public static EndingDef GetDef(string endingId)
        {
            if (string.IsNullOrEmpty(endingId))
            {
                return null;
            }

            return EndingContent.Ensure().GetOrNull(endingId);
        }

        public static string ResolveDisplayText(string endingId)
        {
            EndingDef def = GetDef(endingId);
            if (def == null)
            {
                if (string.IsNullOrEmpty(endingId))
                {
                    return "六日已过，避难所的故事暂时告一段落。\n结局内容待策划标准化后接入。";
                }

                if (string.Equals(endingId, EndingIds.Debug, System.StringComparison.Ordinal))
                {
                    return "（Debug 强制终局）";
                }

                return "终局：" + endingId + "\n（文案待补）";
            }

            string title = string.IsNullOrEmpty(def.Title) ? def.Id : def.Title;
            string body = def.Body ?? string.Empty;
            if (string.IsNullOrEmpty(body))
            {
                return title;
            }

            return "《" + title + "》\n" + body;
        }

        public static string ResolveCriteriaText(string endingId)
        {
            EndingDef def = GetDef(endingId);
            if (def == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(def.CriteriaHint))
            {
                return def.CriteriaHint.Trim();
            }

            return BuildCriteriaFromDef(def);
        }

        private static string BuildCriteriaFromDef(EndingDef def)
        {
            if (def == null)
            {
                return string.Empty;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            switch (def.Trigger)
            {
                case EndingTrigger.CorruptionFuse:
                    sb.Append("腐蚀度 ≥ 100");
                    break;
                case EndingTrigger.CombatLose:
                    sb.Append("战斗失败");
                    if (def.RequiredSurvivorIds != null && def.RequiredSurvivorIds.Length > 0)
                    {
                        sb.Append("，且庇护所有「").Append(string.Join("、", def.RequiredSurvivorIds)).Append("」");
                    }

                    break;
                case EndingTrigger.PopulationZero:
                    sb.Append("庇护所人数 = 0");
                    break;
                case EndingTrigger.RunComplete:
                    AppendRange(sb, "腐蚀度", def.CorruptionMin, def.CorruptionMax);
                    AppendRange(sb, "人数", def.PopulationMin, def.PopulationMax);
                    if (sb.Length == 0)
                    {
                        sb.Append("六日已到，未匹配其他分支");
                    }

                    break;
            }

            return sb.ToString();
        }

        private static void AppendRange(
            System.Text.StringBuilder sb,
            string label,
            int? min,
            int? max)
        {
            if (!min.HasValue && !max.HasValue)
            {
                return;
            }

            if (sb.Length > 0)
            {
                sb.Append("；");
            }

            if (min.HasValue && max.HasValue && min.Value == max.Value)
            {
                sb.Append(label).Append(" = ").Append(min.Value);
                return;
            }

            if (min.HasValue && max.HasValue)
            {
                sb.Append(min.Value).Append(" ≤ ").Append(label).Append(" ≤ ").Append(max.Value);
                return;
            }

            if (min.HasValue)
            {
                sb.Append(label).Append(" ≥ ").Append(min.Value);
            }
            else
            {
                sb.Append(label).Append(" ≤ ").Append(max.Value);
            }
        }

        private static EndingDef Select(EndingTrigger trigger, ShelterManager shelter, int corruption)
        {
            EndingQuery query = new EndingQuery
            {
                Trigger = trigger,
                Corruption = corruption,
                Population = shelter != null ? shelter.Population : 0,
                OwnedSurvivorDefIds = BuildOwnedIds(shelter)
            };

            return EndingRequirements.SelectBest(EndingContent.Ensure().All, query);
        }

        private static int ReadCorruption(ShelterManager shelter)
        {
            return shelter != null && shelter.State != null ? shelter.State.corruption : 0;
        }

        private static string[] BuildOwnedIds(ShelterManager shelter)
        {
            if (shelter?.Survivors == null || shelter.Survivors.Count == 0)
            {
                return System.Array.Empty<string>();
            }

            List<string> owned = new List<string>();
            for (int i = 0; i < shelter.Survivors.Count; i++)
            {
                Survivor s = shelter.Survivors[i];
                if (s != null && !string.IsNullOrEmpty(s.defId)
                    && s.status != SurvivorStatus.Dead && s.status != SurvivorStatus.Left)
                {
                    owned.Add(s.defId);
                }
            }

            return owned.ToArray();
        }
    }
}
