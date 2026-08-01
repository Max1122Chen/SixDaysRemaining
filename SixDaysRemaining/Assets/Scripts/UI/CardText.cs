using System.Collections.Generic;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 临时卡牌文案工具：在标准化数据模板接入前，把现有 EffectSpec 转成可读文本。
    /// </summary>
    public static class CardText
    {
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
                    case EffectOp.GainBlock:
                        parts.Add("格挡 " + FormatNumber(e.Amount));
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
