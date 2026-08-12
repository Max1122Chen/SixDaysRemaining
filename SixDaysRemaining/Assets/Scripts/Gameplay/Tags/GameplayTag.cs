using System;
using System.Collections.Generic;

namespace SixDaysRemaining.Gameplay
{
    /// <summary>
    /// 层级 gameplay tag 值对象；命名以 '.' 分段，例如 State.ForbiddenExpedition.Once。
    /// </summary>
    public readonly struct GameplayTag : IEquatable<GameplayTag>
    {
        public string Name { get; }

        public IReadOnlyList<string> Segments { get; }

        private GameplayTag(string name, string[] segments)
        {
            Name = name;
            Segments = segments;
        }

        public static GameplayTag Parse(string raw)
        {
            if (!TryParse(raw, out GameplayTag tag))
            {
                throw new ArgumentException($"Invalid gameplay tag: '{raw}'.", nameof(raw));
            }

            return tag;
        }

        public static bool TryParse(string raw, out GameplayTag tag)
        {
            tag = default;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string normalized = raw.Trim();
            if (normalized.StartsWith(".", StringComparison.Ordinal)
                || normalized.EndsWith(".", StringComparison.Ordinal)
                || normalized.Contains("..", StringComparison.Ordinal))
            {
                return false;
            }

            string[] parts = normalized.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                {
                    return false;
                }
            }

            tag = new GameplayTag(normalized, parts);
            return true;
        }

        public bool MatchesExact(GameplayTag other)
        {
            return string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// 层级匹配：本 tag 等于 other，或本 tag 是 other 的子 tag。
        /// </summary>
        public bool MatchesTag(GameplayTag other)
        {
            if (MatchesExact(other))
            {
                return true;
            }

            if (Name.Length <= other.Name.Length)
            {
                return false;
            }

            return Name.StartsWith(other.Name + ".", StringComparison.Ordinal);
        }

        public bool Equals(GameplayTag other)
        {
            return MatchesExact(other);
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayTag other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return Name ?? string.Empty;
        }

        public static bool operator ==(GameplayTag left, GameplayTag right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GameplayTag left, GameplayTag right)
        {
            return !left.Equals(right);
        }
    }
}
