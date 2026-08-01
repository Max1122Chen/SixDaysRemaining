using SixDaysRemaining.Bootstrap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    public class EndingPanel : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text txtEnding;

        [SerializeField]
        private Button btnToMenu;

        private AppFlowController flow;

        public void Bind(AppFlowController appFlow)
        {
            flow = appFlow;
            if (btnToMenu != null)
            {
                btnToMenu.onClick.RemoveAllListeners();
                btnToMenu.onClick.AddListener(OnToMenu);
            }
        }

        public void BindRefs(TMP_Text ending, Button toMenu)
        {
            txtEnding = ending;
            btnToMenu = toMenu;
        }

        public void Refresh()
        {
            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            string text = gi != null && gi.Gameplay != null
                ? "Ending. day=" + gi.Gameplay.State.day
                : "Ending";
            if (txtEnding != null)
            {
                txtEnding.text = text;
            }
        }

        private void OnToMenu()
        {
            if (flow != null)
            {
                flow.OnBackToMenu();
            }
        }
    }
}
