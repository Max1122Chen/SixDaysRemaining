using System.Collections.Generic;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 卡牌/意图文案：把 EffectSpec 转成可读文本。
    /// </summary>
    public static class CardText
    {
        public static string DescribeCard(CardDef def)
        {
            if (def == null)
            {
                return "空";
            }

            if (!string.IsNullOrEmpty(def.DisplayName))
            {
                return def.DisplayName;
            }

            return DescribeDetail(def);
        }

        /// <summary>卡面/预告详情：优先 Description，否则效果列表。</summary>
        public static string DescribeDetail(CardDef def)
        {
            if (def == null)
            {
                return "空";
            }

            if (!string.IsNullOrEmpty(def.Description))
            {
                return def.Description;
            }

            if ((def.Tags & CardTag.Charge) != 0)
            {
                return "无行动。预示之后将有强力攻击。";
            }

            return DescribeEffects(def.Effects);
        }

        public static string DescribeEffects(EffectSpec[] effects)
        {
            if (effects == null || effects.Length == 0)
            {
                return "无效果";
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < effects.Length; i++)
            {
                EffectSpec e = effects[i];
                switch (e.Op)
                {
                    case EffectOp.DealDamage:
                        parts.Add("攻击 " + FormatNumber(e.Amount));
                        break;
                    case EffectOp.DealDamagePlusAttackCount:
                        parts.Add("攻击 " + FormatNumber(e.Amount) + "+攻牌数");
                        break;
                    case EffectOp.GainBlock:
                        parts.Add("格挡 " + FormatNumber(e.Amount));
                        break;
                    case EffectOp.GainBlockRandom:
                        parts.Add("格挡 " + FormatNumber(e.Amount) + "/" + FormatNumber(e.AmountSecondary));
                        break;
                    case EffectOp.Heal:
                        parts.Add("回复 " + FormatNumber(e.Amount));
                        break;
                    case EffectOp.AddCorruption:
                        parts.Add("腐蚀 +" + FormatNumber(e.Amount));
                        break;
                    case EffectOp.RemoveCorruption:
                        parts.Add("腐蚀 -" + FormatNumber(e.Amount));
                        break;
                    case EffectOp.Draw:
                        parts.Add("抽 " + ((int)e.Amount) + " 张");
                        break;
                    default:
                        parts.Add("未知");
                        break;
                }
            }

            return string.Join("  ", parts);
        }

        public static string FormatNumber(float value)
        {
            return value.ToString("0.##");
        }
    }
}
