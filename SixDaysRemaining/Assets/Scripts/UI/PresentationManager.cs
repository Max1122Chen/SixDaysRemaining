using System.Collections.Generic;
using SixDaysRemaining.App;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Events;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using UnityEngine;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// UI 呈现：切屏、Overlay、HUD。通过委托与 AppFlowController 链接。
    /// </summary>
    public class PresentationManager : MonoBehaviour
    {
        [SerializeField]
        private StartScreenView startView;

        [SerializeField]
        private StoryIntroView storyView;

        [SerializeField]
        private ShelterView shelterView;

        [SerializeField]
        private CombatView combatView;

        [SerializeField]
        private SettlementView settlementView;

        [SerializeField]
        private EndingView endingView;

        [SerializeField]
        private SettingsView settingsView;

        [SerializeField]
        private CreditsView creditsView;

        [SerializeField]
        private MetaReviewView metaReviewView;

        [SerializeField]
        private GlobalHudView hudView;

        [SerializeField]
        private GameEventView gameEventView;

        private AppFlowController flow;
        private GameObject activeScreen;
        private GameObject activeOverlay;

        public void Bind(
            AppFlowController appFlow,
            StartScreenView start,
            StoryIntroView story,
            ShelterView shelter,
            CombatView combat,
            SettlementView settlement,
            EndingView ending,
            SettingsView settings,
            CreditsView credits,
            GlobalHudView hud)
        {
            flow = appFlow;
            startView = start;
            storyView = story;
            shelterView = shelter;
            combatView = combat;
            settlementView = settlement;
            endingView = ending;
            settingsView = settings;
            creditsView = credits;
            hudView = hud;

            WirePresentationDelegates();
            WireViews();
        }

        public void BindMetaReview(MetaReviewView review)
        {
            metaReviewView = review;
            if (metaReviewView != null && flow != null)
            {
                metaReviewView.Wire(flow);
            }
        }

        private void WirePresentationDelegates()
        {
            if (flow == null)
            {
                return;
            }

            flow.ShowStartScreen = ShowStartScreen;
            flow.ShowStoryIntroScreen = ShowStoryIntroScreen;
            flow.ShowShelterScreen = ShowShelterScreen;
            flow.ShowCombatScreen = ShowCombatScreen;
            flow.ShowEndingScreen = ShowEndingScreen;
            flow.ShowSettingsOverlay = ShowSettings;
            flow.ShowCreditsOverlay = ShowCredits;
            flow.ShowMetaReviewOverlay = ShowMetaReview;
            flow.CloseOverlayCallback = CloseOverlay;
            flow.RefreshHud = RefreshHud;
            flow.RefreshDebugPresentation = RefreshDebugPresentation;
            flow.RefreshStartScreen = RefreshStartScreen;
            flow.ShowSettlementOverlay = ShowSettlement;
            flow.ShowGameEventOverlay = ShowGameEvent;
            flow.ShowGameEventResultOverlay = ShowGameEventResult;
            flow.ShowDayEndOverlay = ShowDayEnd;
            flow.ShowTakeInSwapOverlay = ShowTakeInSwap;
            flow.ShowDay4SavePromptOverlay = ShowDay4SavePrompt;
        }

        public void WireViews()
        {
            if (flow == null)
            {
                return;
            }

            if (hudView != null)
            {
                hudView.Wire(flow);
            }

            if (startView != null) startView.Wire(flow);
            if (storyView != null) storyView.Wire(flow);
            if (shelterView != null) shelterView.Wire(flow);
            if (combatView != null) combatView.Wire(flow);
            if (settlementView != null) settlementView.Wire(flow);
            if (endingView != null) endingView.Wire(flow);
            if (settingsView != null) settingsView.Wire(flow);
            if (creditsView != null) creditsView.Wire(flow);
            if (metaReviewView != null) metaReviewView.Wire(flow);

            GameEventView events = EnsureGameEventView();
            if (events != null)
            {
                events.Wire(flow);
            }
        }

        private void ShowStartScreen()
        {
            SwitchScreen(startView != null ? startView.gameObject : null);
            RefreshStartScreen();
            HideHud();
        }

        private void RefreshStartScreen()
        {
            if (startView != null)
            {
                startView.RefreshContinueState();
            }
        }

        private void ShowMetaReview()
        {
            MetaReviewView review = EnsureMetaReviewView();
            if (review != null)
            {
                review.Refresh();
                ShowOverlay(review.gameObject);
            }
        }

        private MetaReviewView EnsureMetaReviewView()
        {
            if (metaReviewView != null)
            {
                return metaReviewView;
            }

            Transform parent = startView != null ? startView.transform.parent : transform;
            metaReviewView = MetaReviewView.Build(parent, flow);
            return metaReviewView;
        }

        private void ShowStoryIntroScreen()
        {
            SwitchScreen(storyView != null ? storyView.gameObject : null);
            if (storyView != null)
            {
                storyView.Play();
            }

            HideHud();
        }

        private void ShowShelterScreen()
        {
            EnsureHud();
            SwitchScreen(shelterView != null ? shelterView.gameObject : null);
            if (hudView != null)
            {
                hudView.gameObject.SetActive(true);
                hudView.SetScreen("庇护所界面");
                hudView.Refresh();
            }

            if (shelterView != null)
            {
                shelterView.Refresh();
            }
        }

        private void ShowCombatScreen()
        {
            EnsureHud();
            SwitchScreen(combatView != null ? combatView.gameObject : null);
            if (hudView != null)
            {
                hudView.gameObject.SetActive(true);
                hudView.SetScreen("战斗界面");
                hudView.Refresh();
            }

            if (combatView != null)
            {
                combatView.OpenCombat();
            }
        }

        private void ShowEndingScreen()
        {
            SwitchScreen(endingView != null ? endingView.gameObject : null);
            if (endingView != null)
            {
                endingView.Refresh();
            }

            HideHud();
        }

        private void ShowSettings()
        {
            if (settingsView != null)
            {
                ShowOverlay(settingsView.gameObject);
                settingsView.Refresh();
            }
        }

        private void ShowCredits()
        {
            if (creditsView != null)
            {
                ShowOverlay(creditsView.gameObject);
            }
        }

        private void CloseOverlay()
        {
            if (activeOverlay != null)
            {
                activeOverlay.SetActive(false);
            }

            activeOverlay = null;
        }

        private void ShowSettlement(CombatResult result)
        {
            if (settlementView == null)
            {
                return;
            }

            ShowOverlay(settlementView.gameObject);
            settlementView.ShowResult(result, flow != null ? flow.Game : null);
        }

        private void ShowGameEvent(GameEventDef def)
        {
            GameEventView view = EnsureGameEventView();
            if (view == null)
            {
                return;
            }

            ShowOverlay(view.gameObject);
            view.ShowEvent(def);
        }

        private void ShowTakeInSwap(IReadOnlyList<Survivor> alive)
        {
            GameEventView view = EnsureGameEventView();
            if (view == null)
            {
                return;
            }

            ShowOverlay(view.gameObject);
            view.ShowTakeInSwap(alive);
        }

        private void ShowDay4SavePrompt()
        {
            GameEventView view = EnsureGameEventView();
            if (view == null)
            {
                return;
            }

            ShowOverlay(view.gameObject);
            view.ShowSavePrompt();
        }

        private void ShowGameEventResult(GameEventResult result)
        {
            GameEventView view = EnsureGameEventView();
            GameInstance gi = flow != null ? flow.Game : null;
            if (view == null || gi == null)
            {
                return;
            }

            view.ShowResult(result, gi);
        }

        private void ShowDayEnd(IReadOnlyList<string> personnelChanges)
        {
            GameInstance gi = flow != null ? flow.Game : null;
            GameEventView view = EnsureGameEventView();
            if (view == null || gi == null)
            {
                return;
            }

            ShowOverlay(view.gameObject);
            view.ShowDayEnd(gi, personnelChanges);
        }

        private void RefreshHud()
        {
            if (hudView != null && hudView.gameObject.activeSelf)
            {
                hudView.Refresh();
            }

            if (shelterView != null && activeScreen == shelterView.gameObject)
            {
                shelterView.Refresh();
            }
        }

        private void RefreshDebugPresentation()
        {
            RefreshHud();

            if (activeScreen == null)
            {
                return;
            }

            if (shelterView != null && activeScreen == shelterView.gameObject)
            {
                shelterView.Refresh();
            }
            else if (combatView != null && activeScreen == combatView.gameObject)
            {
                combatView.Refresh();
            }
            else if (endingView != null && activeScreen == endingView.gameObject)
            {
                endingView.Refresh();
            }
        }

        private void HideHud()
        {
            if (hudView != null)
            {
                hudView.gameObject.SetActive(false);
            }
        }

        private void SwitchScreen(GameObject go)
        {
            CloseOverlay();
            if (settingsView != null)
            {
                settingsView.gameObject.SetActive(false);
            }

            if (creditsView != null)
            {
                creditsView.gameObject.SetActive(false);
            }

            SetActive(startView, go != null && startView != null && go == startView.gameObject);
            SetActive(storyView, go != null && storyView != null && go == storyView.gameObject);
            SetActive(shelterView, go != null && shelterView != null && go == shelterView.gameObject);
            SetActive(combatView, go != null && combatView != null && go == combatView.gameObject);
            SetActive(settlementView, go != null && settlementView != null && go == settlementView.gameObject);
            SetActive(endingView, go != null && endingView != null && go == endingView.gameObject);
            activeScreen = go;
        }

        private void ShowOverlay(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (activeOverlay != null && activeOverlay != go)
            {
                activeOverlay.SetActive(false);
            }

            if (hudView != null)
            {
                hudView.transform.SetAsLastSibling();
            }

            go.SetActive(true);
            go.transform.SetAsLastSibling();
            activeOverlay = go;
        }

        private GameEventView EnsureGameEventView()
        {
            if (gameEventView != null)
            {
                return gameEventView;
            }

            Transform root = GetUiRoot();
            if (root == null || flow == null)
            {
                return null;
            }

            gameEventView = GameEventView.Build(root, flow);
            return gameEventView;
        }

        private Transform GetUiRoot()
        {
            if (shelterView != null)
            {
                return shelterView.transform.parent;
            }

            if (combatView != null)
            {
                return combatView.transform.parent;
            }

            if (startView != null)
            {
                return startView.transform.parent;
            }

            return transform;
        }

        private void EnsureHud()
        {
            if (hudView == null)
            {
                hudView = FindObjectOfType<GlobalHudView>(true);
                if (hudView != null && flow != null)
                {
                    hudView.Wire(flow);
                }
            }

            if (hudView != null || flow == null)
            {
                return;
            }

            Transform root = GetUiRoot();
            Canvas canvas = root != null ? root.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }

            hudView = GlobalHudView.Build(canvas != null ? canvas.transform : root, flow);
            if (hudView != null)
            {
                hudView.transform.SetAsLastSibling();
            }
        }

        private static void SetActive(MonoBehaviour view, bool on)
        {
            if (view != null && view.gameObject != null && view.gameObject.activeSelf != on)
            {
                view.gameObject.SetActive(on);
            }
        }
    }
}
