using System.Text;
using SixDaysRemaining.Bootstrap;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    public class CombatPanel : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text txtHeader;

        [SerializeField]
        private TMP_Text txtHandHint;

        [SerializeField]
        private TMP_Text txtSelection;

        [SerializeField]
        private Button[] btnHands = new Button[8];

        [SerializeField]
        private Button btnCommit;

        [SerializeField]
        private Button btnClear;

        [SerializeField]
        private Button btnFlee;

        private AppFlowController flow;

        public void Bind(AppFlowController appFlow)
        {
            flow = appFlow;
            for (int i = 0; i < btnHands.Length; i++)
            {
                int index = i;
                Wire(btnHands[i], () => SelectHand(index));
            }

            Wire(btnCommit, Commit);
            Wire(btnClear, Clear);
            Wire(btnFlee, Flee);

            if (txtHandHint != null)
            {
                txtHandHint.text = "Hand 1-8 | Enter=Commit | C=Clear | F=Flee";
            }
        }

        public void BindRefs(
            TMP_Text header,
            TMP_Text handHint,
            TMP_Text selection,
            Button[] hands,
            Button commit,
            Button clear,
            Button flee)
        {
            txtHeader = header;
            txtHandHint = handHint;
            txtSelection = selection;
            btnHands = hands;
            btnCommit = commit;
            btnClear = clear;
            btnFlee = flee;
        }

        private void Update()
        {
            if (flow == null || flow.Game == null || flow.Game.Combat == null)
            {
                return;
            }

            if (!flow.Game.Combat.IsPlayerTurn)
            {
                return;
            }

            for (int i = 0; i < 8; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    SelectHand(i);
                }
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Commit();
            }

            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Backspace))
            {
                Clear();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                Flee();
            }
        }

        public void Refresh()
        {
            GameInstance gi = flow != null ? flow.Game : null;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null)
            {
                return;
            }

            PlayerCombatComponent player = gi.Combat.Session.Player;
            EnemyCombatComponent enemy = gi.Combat.Session.Enemies.Count > 0
                ? gi.Combat.Session.Enemies[0]
                : null;

            string header = "P HP=" + player.Attributes.HP + "/" + player.Attributes.MaxHP
                + " B=" + player.Attributes.Block;
            if (enemy != null)
            {
                header += " | E HP=" + enemy.Attributes.HP + "/" + enemy.Attributes.MaxHP
                    + " B=" + enemy.Attributes.Block;
            }

            header += " | turn=" + (gi.Combat.IsPlayerTurn ? "Player" : "Enemy/Done")
                + " finished=" + gi.Combat.IsFinished;
            if (txtHeader != null)
            {
                txtHeader.text = header;
            }

            StringBuilder sel = new StringBuilder("Sel: ");
            for (int i = 0; i < player.Deck.Selection.Count; i++)
            {
                if (i > 0)
                {
                    sel.Append(',');
                }

                int handIndex = IndexInHand(player, player.Deck.Selection[i]);
                sel.Append(handIndex);
            }

            sel.Append(" (").Append(player.Deck.Selection.Count).Append("/5)");
            if (txtSelection != null)
            {
                txtSelection.text = sel.ToString();
            }

            for (int i = 0; i < btnHands.Length; i++)
            {
                Button button = btnHands[i];
                if (button == null)
                {
                    continue;
                }

                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (i < player.Deck.Hand.Count)
                {
                    button.interactable = gi.Combat.IsPlayerTurn;
                    string id = player.Deck.Hand[i].Def != null ? player.Deck.Hand[i].Def.Id : "?";
                    if (label != null)
                    {
                        label.text = (i + 1) + ":" + id;
                    }
                }
                else
                {
                    button.interactable = false;
                    if (label != null)
                    {
                        label.text = "—";
                    }
                }
            }

            LogHand(player, header);
        }

        private void SelectHand(int index)
        {
            GameInstance gi = RequirePlayerTurn();
            if (gi == null)
            {
                return;
            }

            bool ok = gi.PlayerCombat.SelectFromHand(index);
            Debug.Log(ok
                ? "[Combat] Select hand=" + index
                : "[Combat] Select failed hand=" + index);
            Refresh();
        }

        private void Clear()
        {
            GameInstance gi = RequirePlayerTurn();
            if (gi == null)
            {
                return;
            }

            gi.PlayerCombat.ClearSelection();
            Debug.Log("[Combat] ClearSelection");
            Refresh();
        }

        private void Commit()
        {
            GameInstance gi = RequirePlayerTurn();
            if (gi == null || gi.Combat.Session == null)
            {
                return;
            }

            EnemyCombatComponent enemy = gi.Combat.Session.Enemies[0];
            bool ok = gi.PlayerCombat.CommitPlay(enemy);
            Debug.Log(ok ? "[Combat] CommitPlay OK" : "[Combat] CommitPlay failed (need 5)");
            if (!ok)
            {
                Refresh();
                return;
            }

            gi.Combat.NotifyPlayerCommitted();
            if (gi.Combat.IsFinished)
            {
                flow.OnCombatFinished(gi.Combat.Result);
                return;
            }

            Refresh();
        }

        private void Flee()
        {
            GameInstance gi = RequirePlayerTurn();
            if (gi == null)
            {
                return;
            }

            bool ok = gi.Combat.Flee();
            Debug.Log(ok ? "[Combat] Flee OK" : "[Combat] Flee failed");
            if (ok && gi.Combat.IsFinished)
            {
                flow.OnCombatFinished(gi.Combat.Result);
            }
        }

        private GameInstance RequirePlayerTurn()
        {
            GameInstance gi = flow != null ? flow.Game : null;
            if (gi == null || gi.Combat == null)
            {
                return null;
            }

            if (!gi.Combat.IsPlayerTurn)
            {
                Debug.Log("[Combat] 忽略输入：非玩家回合或已结束。");
                return null;
            }

            return gi;
        }

        private static int IndexInHand(PlayerCombatComponent player, CardInstance card)
        {
            for (int i = 0; i < player.Deck.Hand.Count; i++)
            {
                if (player.Deck.Hand[i] == card)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void LogHand(PlayerCombatComponent player, string header)
        {
            StringBuilder sb = new StringBuilder("[Combat] Hand:");
            for (int i = 0; i < player.Deck.Hand.Count; i++)
            {
                string id = player.Deck.Hand[i].Def != null ? player.Deck.Hand[i].Def.Id : "?";
                sb.Append(" [").Append(i).Append(']').Append(id);
            }

            sb.Append(" | ").Append(header);
            Debug.Log(sb.ToString());
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
