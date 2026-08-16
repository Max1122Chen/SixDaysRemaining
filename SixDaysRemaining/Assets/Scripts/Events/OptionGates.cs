using System;

namespace SixDaysRemaining.Events
{
    public enum OptionGateOp
    {
        CorruptionAtLeast = 0,
        CorruptionAtMost = 1,
        HasSurvivor = 2,
        LacksSurvivor = 3,
        HasTag = 4,
        LacksTag = 5,
        FoodAtLeast = 6
    }

    public sealed class OptionGateDef
    {
        public OptionGateOp Op;
        public int Amount;
        public string SurvivorDefId;
        public string TagId;
    }

    /// <summary>
    /// 选项门禁：全部通过才允许点击。
    /// </summary>
    public static class OptionGates
    {
        public static bool Passes(
            GameEventOptionDef option,
            GameEventQuery query,
            out string failHint)
        {
            failHint = null;
            if (option?.Gates == null || option.Gates.Length == 0)
            {
                return true;
            }

            if (query == null)
            {
                failHint = "状态不可用";
                return false;
            }

            for (int i = 0; i < option.Gates.Length; i++)
            {
                OptionGateDef gate = option.Gates[i];
                if (gate == null)
                {
                    continue;
                }

                if (!PassesOne(gate, query, out failHint))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool PassesOne(OptionGateDef gate, GameEventQuery query, out string failHint)
        {
            failHint = null;
            switch (gate.Op)
            {
                case OptionGateOp.CorruptionAtLeast:
                    if (query.Corruption < gate.Amount)
                    {
                        failHint = "需要腐蚀度 ≥ " + gate.Amount;
                        return false;
                    }

                    return true;
                case OptionGateOp.CorruptionAtMost:
                    if (query.Corruption > gate.Amount)
                    {
                        failHint = "需要腐蚀度 ≤ " + gate.Amount;
                        return false;
                    }

                    return true;
                case OptionGateOp.HasSurvivor:
                    if (!Contains(query.OwnedSurvivorDefIds, gate.SurvivorDefId))
                    {
                        failHint = "需要幸存者：" + gate.SurvivorDefId;
                        return false;
                    }

                    return true;
                case OptionGateOp.LacksSurvivor:
                    if (Contains(query.OwnedSurvivorDefIds, gate.SurvivorDefId))
                    {
                        failHint = "不能有：" + gate.SurvivorDefId;
                        return false;
                    }

                    return true;
                case OptionGateOp.HasTag:
                    if (!Contains(query.ActiveTags, gate.TagId))
                    {
                        failHint = "条件未满足";
                        return false;
                    }

                    return true;
                case OptionGateOp.LacksTag:
                    if (Contains(query.ActiveTags, gate.TagId))
                    {
                        failHint = "条件未满足";
                        return false;
                    }

                    return true;
                case OptionGateOp.FoodAtLeast:
                    if (query.FoodStock < gate.Amount)
                    {
                        failHint = "需要食物 ≥ " + gate.Amount;
                        return false;
                    }

                    return true;
                default:
                    failHint = "未知门禁";
                    return false;
            }
        }

        private static bool Contains(string[] ids, string need)
        {
            if (ids == null || string.IsNullOrEmpty(need))
            {
                return false;
            }

            for (int i = 0; i < ids.Length; i++)
            {
                if (string.Equals(ids[i], need, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
