using SixDaysRemaining.Bootstrap;
using SixDaysRemaining.Shelter;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    public class ShelterPanel : MonoBehaviour
    {
        [SerializeField]
        private Text txtStatus;

        [SerializeField]
        private Text txtSurvivors;

        [SerializeField]
        private Button btnAlloc0;

        [SerializeField]
        private Button btnAlloc1;

        [SerializeField]
        private Button btnDepositDebug;

        [SerializeField]
        private Button btnRefresh;

        [SerializeField]
        private Button btnDepart;

        private AppFlowController flow;

        public void Bind(AppFlowController appFlow)
        {
            flow = appFlow;
            Wire(btnAlloc0, () => Allocate(0));
            Wire(btnAlloc1, () => Allocate(1));
            Wire(btnDepositDebug, DepositDebug);
            Wire(btnRefresh, Refresh);
            Wire(btnDepart, Depart);
        }

        public void BindRefs(
            Text status,
            Text survivors,
            Button alloc0,
            Button alloc1,
            Button deposit,
            Button refresh,
            Button depart)
        {
            txtStatus = status;
            txtSurvivors = survivors;
            btnAlloc0 = alloc0;
            btnAlloc1 = alloc1;
            btnDepositDebug = deposit;
            btnRefresh = refresh;
            btnDepart = depart;
        }

        public void Refresh()
        {
            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            if (gi == null || gi.Gameplay == null)
            {
                return;
            }

            var state = gi.Gameplay.State;
            string status = "day=" + state.day
                + "\nphase=" + state.currentPhase
                + "\nfood=" + state.foodStock
                + "\ncorruption=" + state.corruption
                + "\npopulation=" + state.population;
            if (txtStatus != null)
            {
                txtStatus.text = status;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (gi.Shelter != null)
            {
                for (int i = 0; i < gi.Shelter.Survivors.Count; i++)
                {
                    Survivor s = gi.Shelter.Survivors[i];
                    sb.Append("[").Append(i).Append("] ")
                        .Append(s.name)
                        .Append(" hunger=").Append(s.hunger)
                        .Append(" status=").Append(s.status)
                        .Append('\n');
                }
            }

            if (txtSurvivors != null)
            {
                txtSurvivors.text = sb.ToString();
            }

            Debug.Log("[Shelter] " + status.Replace('\n', ' '));
        }

        private void Allocate(int index)
        {
            GameInstance gi = flow != null ? flow.Game : null;
            if (gi == null)
            {
                return;
            }

            gi.DebugAllocateFood(index, 1);
            Refresh();
        }

        private void DepositDebug()
        {
            GameInstance gi = flow != null ? flow.Game : null;
            if (gi == null)
            {
                return;
            }

            gi.DebugDepositFood(3);
            Refresh();
        }

        private void Depart()
        {
            if (flow != null)
            {
                flow.OnDepart();
            }
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
