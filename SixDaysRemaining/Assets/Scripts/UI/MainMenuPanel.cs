using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    public class MainMenuPanel : MonoBehaviour
    {
        [SerializeField]
        private Button btnStart;

        [SerializeField]
        private Button btnQuit;

        private AppFlowController flow;

        public void Bind(AppFlowController appFlow)
        {
            flow = appFlow;
            Wire(btnStart, OnStart);
            Wire(btnQuit, OnQuit);
        }

        public void BindButtons(Button start, Button quit)
        {
            btnStart = start;
            btnQuit = quit;
        }

        private void OnStart()
        {
            if (flow != null)
            {
                flow.OnStartNewGame();
            }
        }

        private void OnQuit()
        {
            Debug.Log("[Flow] Quit requested (Editor 内不退出)。");
            Application.Quit();
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }
}
