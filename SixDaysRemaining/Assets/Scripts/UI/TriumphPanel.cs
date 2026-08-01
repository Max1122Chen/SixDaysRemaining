using SixDaysRemaining.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    public class TriumphPanel : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text txtResult;

        [SerializeField]
        private Button btnContinue;

        private AppFlowController flow;

        public void Bind(AppFlowController appFlow)
        {
            flow = appFlow;
            if (btnContinue != null)
            {
                btnContinue.onClick.RemoveAllListeners();
                btnContinue.onClick.AddListener(OnContinue);
            }
        }

        public void BindRefs(TMP_Text result, Button cont)
        {
            txtResult = result;
            btnContinue = cont;
        }

        public void ShowResult(CombatResult result)
        {
            string text = "Outcome=" + result.Outcome
                + "\nFoodGained=" + result.FoodGained
                + "\nCorruptionDelta=" + result.CorruptionDelta
                + "\nTurnsElapsed=" + result.TurnsElapsed;
            if (txtResult != null)
            {
                txtResult.text = text;
            }

            Debug.Log("[Flow] " + text.Replace('\n', ' '));
        }

        private void OnContinue()
        {
            if (flow != null)
            {
                flow.OnTriumphContinue();
            }
        }
    }
}
