using UnityEngine;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 手动搭建庇护所场景时，把本组件挂到每个座椅 GameObject 上，并在 Inspector 指定 seatIndex。
    /// 运行时 ShelterView 会按 seatIndex 顺序把存活 NPC 生成为该座椅的子物体；
    /// NPC 立绘、名字、状态、身份的生成位置由下方四个偏移量控制，可逐椅调整。
    /// </summary>
    public class ShelterSeatSlot : MonoBehaviour
    {
        [SerializeField]
        private int seatIndex;

        [SerializeField]
        private Vector2 npcOffset = new Vector2(0f, 90f);

        [SerializeField]
        private Vector2 nameOffset = new Vector2(0f, -10f);

        [SerializeField]
        private Vector2 statusOffset = new Vector2(0f, -38f);

        [SerializeField]
        private Vector2 identityOffset = new Vector2(0f, -58f);

        public int SeatIndex
        {
            get { return seatIndex; }
        }

        public Vector2 NpcOffset
        {
            get { return npcOffset; }
        }

        public Vector2 NameOffset
        {
            get { return nameOffset; }
        }

        public Vector2 StatusOffset
        {
            get { return statusOffset; }
        }

        public Vector2 IdentityOffset
        {
            get { return identityOffset; }
        }
    }
}
