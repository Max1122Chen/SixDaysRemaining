using System.Reflection;
using NUnit.Framework;
using SixDaysRemaining.App;
using SixDaysRemaining.Gameplay;
using UnityEngine;

namespace SixDaysRemaining.Tests.EditMode
{
    public class AppFlowControllerTests
    {
        [Test]
        public void OnDayEndContinue_BlockedExpeditionDay_AdvancesDayAndClearsTag()
        {
            GameObject giGo = new GameObject("GameInstanceTest");
            GameObject flowGo = new GameObject("AppFlowControllerTest");
            try
            {
                GameInstance gi = giGo.AddComponent<GameInstance>();
                gi.StartNewGame(1);
                gi.Gameplay.SetPhase(GameplayPhase.ExpeditionPrep);
                gi.Gameplay.State.day = 3;
                gi.Gameplay.AddTag(GameplayTags.ForbiddenExpeditionOnce);

                AppFlowController flow = flowGo.AddComponent<AppFlowController>();
                flow.BindGame(gi);
                flow.CloseOverlayCallback = () => { };

                flow.OnDayEndContinue();

                Assert.IsFalse(gi.Gameplay.HasTag(GameplayTags.ForbiddenExpedition));
                Assert.AreEqual(4, gi.Gameplay.State.day);
                Assert.AreEqual(GameplayPhase.ExpeditionPrep, gi.Gameplay.CurrentPhase);
            }
            finally
            {
                Object.DestroyImmediate(flowGo);
                Object.DestroyImmediate(giGo);
            }
        }

        [Test]
        public void HandleEventSequenceFinished_BeforeDepart_ClosesOverlay()
        {
            GameObject go = new GameObject("AppFlowControllerTests");
            try
            {
                AppFlowController flow = go.AddComponent<AppFlowController>();
                int closeCalls = 0;
                int refreshCalls = 0;
                flow.CloseOverlayCallback = () => closeCalls++;
                flow.RefreshHud = () => refreshCalls++;

                System.Type flowType = typeof(AppFlowController);
                FieldInfo phaseField = flowType.GetField("eventChainPhase", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(phaseField);
                phaseField.SetValue(flow, System.Enum.ToObject(phaseField.FieldType, 3));

                MethodInfo finishMethod = flowType.GetMethod("HandleEventSequenceFinished", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(finishMethod);
                finishMethod.Invoke(flow, null);

                Assert.AreEqual(1, closeCalls);
                Assert.AreEqual(1, refreshCalls);
                Assert.AreEqual(System.Enum.ToObject(phaseField.FieldType, 0), phaseField.GetValue(flow));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
