using System.Collections.Generic;
using UnityEngine;

namespace SixDaysRemaining.Shelter
{
    /// <summary>
    /// 庇护所房间场景定义：厕所（左）/ 大厅（中）/ 厨房（右）。
    /// 每个场景固定 5 个 NPC 落座点位；场景之间共享同一份幸存者数据（全局同步）。
    /// 背景图可从 Resources/Shelter/&lt;BackgroundResource&gt;.png 自动加载，未提供时使用纯色占位。
    /// </summary>
    public sealed class ShelterRoomDef
    {
        public string Id;
        public string DisplayName;
        public string BackgroundResource;
        public Color BackgroundColor;
        public Vector2[] Seats;
    }

    public static class ShelterRooms
    {
        public const int SeatCount = 5;

        private static readonly List<ShelterRoomDef> rooms = new List<ShelterRoomDef>
        {
            new ShelterRoomDef
            {
                Id = "toilet",
                DisplayName = "厕所",
                BackgroundResource = "Shelter/厕所",
                BackgroundColor = new Color(0.07f, 0.10f, 0.13f, 1f),
                Seats = BuildSeatRow(new float[] { -1f, -0.5f, 0f, 0.5f, 1f }, -300f, 330f)
            },
            new ShelterRoomDef
            {
                Id = "hall",
                DisplayName = "大厅",
                BackgroundResource = "Shelter/大厅",
                BackgroundColor = new Color(0.10f, 0.11f, 0.13f, 1f),
                Seats = BuildSeatRow(new float[] { -1f, -0.5f, 0f, 0.5f, 1f }, -280f, 330f)
            },
            new ShelterRoomDef
            {
                Id = "kitchen",
                DisplayName = "厨房",
                BackgroundResource = "Shelter/餐厅",
                BackgroundColor = new Color(0.12f, 0.10f, 0.08f, 1f),
                Seats = BuildSeatRow(new float[] { -1f, -0.5f, 0f, 0.5f, 1f }, -300f, 330f)
            }
        };

        public static IReadOnlyList<ShelterRoomDef> All
        {
            get { return rooms; }
        }

        public static int Count
        {
            get { return rooms.Count; }
        }

        public static ShelterRoomDef Get(int index)
        {
            if (index < 0)
            {
                index = 0;
            }

            if (index >= rooms.Count)
            {
                index = rooms.Count - 1;
            }

            return rooms[index];
        }

        /// <summary>加载房间背景 Sprite；未放置资源时返回 null。</summary>
        public static Sprite LoadBackground(ShelterRoomDef room)
        {
            if (room == null || string.IsNullOrEmpty(room.BackgroundResource))
            {
                return null;
            }

            return Resources.Load<Sprite>(room.BackgroundResource);
        }

        private static Vector2[] BuildSeatRow(float[] offsets, float y, float spread)
        {
            Vector2[] seats = new Vector2[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                seats[i] = new Vector2(offsets[i] * spread, y);
            }

            return seats;
        }
    }
}
