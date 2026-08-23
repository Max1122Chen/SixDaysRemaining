using System.Collections.Generic;
using NUnit.Framework;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;

namespace SixDaysRemaining.Tests.EditMode
{
    public class ShelterManagerTests
    {
        private GameState state;
        private ShelterManager shelter;

        [SetUp]
        public void SetUp()
        {
            ShelterContent.ClearForTests();
            state = new GameState();
            shelter = new ShelterManager(state);
        }

        [TearDown]
        public void TearDown()
        {
            ShelterContent.ClearForTests();
        }

        [Test]
        public void DepositFood_IncreasesFoodStock()
        {
            state.foodStock = 5;
            shelter.DepositFood(3);

            Assert.AreEqual(8, state.foodStock);
        }

        [Test]
        public void DepositFood_IgnoresNonPositiveAmount()
        {
            state.foodStock = 5;
            shelter.DepositFood(0);
            shelter.DepositFood(-1);

            Assert.AreEqual(5, state.foodStock);
        }

        [Test]
        public void AllocateFood_ReducesStock_DeferredHungerUntilNextDay()
        {
            Survivor survivor = new Survivor { name = "Test", hunger = 1, status = SurvivorStatus.Hungry };
            shelter.RegisterSurvivor(survivor);
            state.foodStock = 5;

            bool ok = shelter.AllocateFood(survivor, 2);

            Assert.IsTrue(ok);
            Assert.AreEqual(3, state.foodStock);
            Assert.AreEqual(1, survivor.hunger);
            Assert.AreEqual(SurvivorStatus.Hungry, survivor.status);

            shelter.ApplyFedYesterdayRecovery(new Dictionary<string, int> { { "__name:Test", 2 } });

            Assert.AreEqual(3, survivor.hunger);
            Assert.AreEqual(SurvivorStatus.Healthy, survivor.status);
            Assert.AreEqual(0, survivor.hungryDayCount);
        }

        [Test]
        public void AllocateFood_FailsWhenInsufficientStock()
        {
            Survivor survivor = new Survivor { name = "Test", hunger = 0, status = SurvivorStatus.Dying };
            shelter.RegisterSurvivor(survivor);
            state.foodStock = 1;

            bool ok = shelter.AllocateFood(survivor, 2);

            Assert.IsFalse(ok);
            Assert.AreEqual(1, state.foodStock);
            Assert.AreEqual(0, survivor.hunger);
        }

        [Test]
        public void AllocateFood_FailsForDeadSurvivor()
        {
            Survivor survivor = new Survivor { name = "Test", hunger = 0, status = SurvivorStatus.Dead };
            shelter.RegisterSurvivor(survivor);
            state.foodStock = 5;

            Assert.IsFalse(shelter.AllocateFood(survivor, 1));
        }

        [Test]
        public void ProcessEndOfDay_ReducesHungerAndSetsHungry()
        {
            Survivor survivor = new Survivor
            {
                name = "Test",
                hunger = 2,
                status = SurvivorStatus.Healthy,
                hungryToDyingDays = 2
            };
            shelter.RegisterSurvivor(survivor);

            shelter.ProcessEndOfDay();

            Assert.AreEqual(1, survivor.hunger);
            Assert.AreEqual(SurvivorStatus.Hungry, survivor.status);
            Assert.AreEqual(1, survivor.hungryDayCount);
        }

        [Test]
        public void ProcessEndOfDay_SetsDyingWhenHungerReachesZero()
        {
            Survivor survivor = new Survivor { name = "Test", hunger = 1, status = SurvivorStatus.Hungry };
            shelter.RegisterSurvivor(survivor);

            shelter.ProcessEndOfDay();

            Assert.AreEqual(0, survivor.hunger);
            Assert.AreEqual(SurvivorStatus.Dying, survivor.status);
        }

        [Test]
        public void ProcessEndOfDay_DyingBecomesDeadOnSecondEndOfDay()
        {
            Survivor survivor = new Survivor { name = "Test", hunger = 0, status = SurvivorStatus.Dying };
            shelter.RegisterSurvivor(survivor);

            shelter.ProcessEndOfDay();
            Assert.AreEqual(SurvivorStatus.Dying, survivor.status);
            Assert.IsTrue(survivor.dyingGraceConsumed);

            shelter.ProcessEndOfDay();

            Assert.AreEqual(SurvivorStatus.Dead, survivor.status);
            Assert.AreEqual(0, shelter.Population);
            Assert.AreEqual(0, state.population);
        }

        [Test]
        public void TakeInDying_SurvivesAdmissionDayEnd()
        {
            ShelterContent.ClearForTests();
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager bound = new ShelterManager(gameplay.State);
            bound.BindGameplay(gameplay);
            bound.InitializeDefaultRoster(10);
            bound.TakeIn(SurvivorIds.Doctor);

            Survivor doctor = null;
            for (int i = 0; i < bound.Survivors.Count; i++)
            {
                if (bound.Survivors[i] != null
                    && bound.Survivors[i].defId == SurvivorIds.Doctor)
                {
                    doctor = bound.Survivors[i];
                    break;
                }
            }

            Assert.IsNotNull(doctor);
            Assert.AreEqual(SurvivorStatus.Dying, doctor.status);
            Assert.AreEqual(0, doctor.hunger);

            bound.ProcessEndOfDay();
            Assert.AreEqual(SurvivorStatus.Dying, doctor.status);
            Assert.IsFalse(doctor.status == SurvivorStatus.Dead);

            bound.ProcessEndOfDay();
            Assert.AreEqual(SurvivorStatus.Dead, doctor.status);
            ShelterContent.ClearForTests();
        }

        [Test]
        public void DyingSurvivor_FedYesterday_RecoversAfterDayAdvance()
        {
            Survivor survivor = new Survivor
            {
                defId = SurvivorIds.Farmer,
                name = "Test",
                hunger = 0,
                status = SurvivorStatus.Dying,
                hungryToDyingDays = 2
            };
            shelter.RegisterSurvivor(survivor);
            state.foodStock = 3;

            Assert.IsTrue(shelter.AllocateFood(survivor, 2));
            Assert.AreEqual(SurvivorStatus.Dying, survivor.status);
            Assert.AreEqual(0, survivor.hunger);

            shelter.ApplyFedYesterdayRecovery(new Dictionary<string, int> { { SurvivorIds.Farmer, 2 } });
            Assert.AreEqual(SurvivorStatus.Hungry, survivor.status);
            Assert.AreEqual(2, survivor.hunger);

            shelter.ProcessEndOfDay();

            Assert.AreEqual(SurvivorStatus.Hungry, survivor.status);
            Assert.AreEqual(1, survivor.hunger);
            Assert.AreNotEqual(SurvivorStatus.Dead, survivor.status);
        }

        [Test]
        public void InitializeDefaultRoster_SetsTwoSurvivorsAndStartingFood()
        {
            shelter.InitializeDefaultRoster();

            Assert.AreEqual(2, shelter.Survivors.Count);
            Assert.AreEqual(SurvivorIds.Child, shelter.Survivors[0].defId);
            Assert.AreEqual(SurvivorIds.Farmer, shelter.Survivors[1].defId);
            Assert.AreEqual("幼童", shelter.Survivors[0].name);
            Assert.AreEqual("农民", shelter.Survivors[1].name);
            Assert.AreEqual(ShelterManager.DefaultStartingFoodStock, state.foodStock);
            Assert.AreEqual(2, shelter.Population);
            Assert.AreEqual(2, state.population);
        }

        [Test]
        public void HungryThreshold_CanBeConfigured()
        {
            shelter.HungryThreshold = 2;
            Survivor survivor = new Survivor { name = "Test", hunger = 2, status = SurvivorStatus.Healthy };
            shelter.RegisterSurvivor(survivor);

            shelter.UpdateSurvivorStatus(survivor);

            Assert.AreEqual(SurvivorStatus.Hungry, survivor.status);
        }

        [Test]
        public void AllocateFood_DyingSurvivor_KeepsDyingPortraitSameDay()
        {
            Survivor survivor = new Survivor
            {
                defId = SurvivorIds.Child,
                name = "幼童",
                hunger = 0,
                status = SurvivorStatus.Dying
            };
            shelter.RegisterSurvivor(survivor);
            state.foodStock = 2;

            Assert.IsTrue(shelter.AllocateFood(survivor, 1));
            Assert.AreEqual(SurvivorStatus.Dying, survivor.status);
            Assert.AreEqual(0, survivor.hunger);
            Assert.IsTrue(shelter.IsFedToday(survivor));
        }

        [Test]
        public void ApplyFedYesterdayRecovery_DyingBecomesHungryAndGainsHunger()
        {
            Survivor survivor = new Survivor
            {
                defId = SurvivorIds.Child,
                name = "幼童",
                hunger = 0,
                status = SurvivorStatus.Dying
            };
            shelter.RegisterSurvivor(survivor);
            state.foodStock = 2;
            Assert.IsTrue(shelter.AllocateFood(survivor, 1));

            shelter.ApplyFedYesterdayRecovery(new Dictionary<string, int> { { SurvivorIds.Child, 1 } });

            Assert.AreEqual(SurvivorStatus.Hungry, survivor.status);
            Assert.AreEqual(1, survivor.hunger);
        }

        [Test]
        public void ApplyFedYesterdayRecovery_HungryBecomesHealthyAndGainsHunger()
        {
            Survivor survivor = new Survivor
            {
                defId = SurvivorIds.Farmer,
                name = "农民",
                hunger = 1,
                status = SurvivorStatus.Hungry
            };
            shelter.RegisterSurvivor(survivor);
            state.foodStock = 2;
            Assert.IsTrue(shelter.AllocateFood(survivor, 1));

            shelter.ApplyFedYesterdayRecovery(new Dictionary<string, int> { { SurvivorIds.Farmer, 1 } });

            Assert.AreEqual(SurvivorStatus.Healthy, survivor.status);
            Assert.AreEqual(2, survivor.hunger);
        }

        [Test]
        public void FullDayFlow_WithGameplaySubsystem()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager dayShelter = new ShelterManager(gameplay.State);
            dayShelter.InitializeDefaultRoster(10);

            Assert.AreEqual(GameplayPhase.ExpeditionPrep, gameplay.CurrentPhase);

            Survivor first = dayShelter.Survivors[0];
            Assert.IsTrue(dayShelter.AllocateFood(first, 2));

            gameplay.AdvancePhase();
            Assert.AreEqual(GameplayPhase.Combat, gameplay.CurrentPhase);

            gameplay.AdvancePhase();
            Assert.AreEqual(GameplayPhase.TriumphReturn, gameplay.CurrentPhase);

            dayShelter.DepositFood(5);
            dayShelter.ProcessEndOfDay();
            gameplay.AdvancePhase();

            Assert.AreEqual(2, gameplay.State.day);
            Assert.AreEqual(GameplayPhase.ExpeditionPrep, gameplay.CurrentPhase);
            Assert.AreEqual(13, gameplay.State.foodStock);
        }

        [Test]
        public void DoctorBigu_ActivatesOnEndOfDay_ThenSkipsAllocAndHunger()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager dayShelter = new ShelterManager(gameplay.State);
            dayShelter.BindGameplay(gameplay);
            dayShelter.InitializeDefaultRoster(10);
            dayShelter.TakeIn(SurvivorIds.Doctor);

            Survivor doctor = null;
            for (int i = 0; i < dayShelter.Survivors.Count; i++)
            {
                if (dayShelter.Survivors[i].defId == SurvivorIds.Doctor)
                {
                    doctor = dayShelter.Survivors[i];
                    break;
                }
            }

            Assert.IsNotNull(doctor);
            doctor.hunger = 1;
            doctor.status = SurvivorStatus.Hungry;
            gameplay.AddTag(GameplayTags.DoctorBiguFunded);

            dayShelter.ProcessEndOfDay();

            Assert.IsTrue(gameplay.HasTagExact(GameplayTags.DoctorBiguActive));
            Assert.IsTrue(dayShelter.IsBiguExempt(doctor));
            Assert.AreEqual(SurvivorStatus.Healthy, doctor.status);
            Assert.Greater(doctor.hunger, dayShelter.HungryThreshold);

            int stockBefore = gameplay.State.foodStock;
            Assert.IsFalse(dayShelter.AllocateFood(doctor, 1));
            Assert.AreEqual(stockBefore, gameplay.State.foodStock);

            int hungerAfterActivate = doctor.hunger;
            dayShelter.ProcessEndOfDay();
            Assert.AreEqual(hungerAfterActivate, doctor.hunger);
            Assert.AreEqual(SurvivorStatus.Healthy, doctor.status);
        }

        [Test]
        public void SetSurvivorHealthy_AndTryPickRandomAlive()
        {
            Survivor a = new Survivor
            {
                defId = "a",
                name = "A",
                hunger = 0,
                status = SurvivorStatus.Dying
            };
            Survivor b = new Survivor
            {
                defId = "b",
                name = "B",
                hunger = 3,
                status = SurvivorStatus.Healthy
            };
            shelter.RegisterSurvivor(a);
            shelter.RegisterSurvivor(b);

            Assert.IsTrue(shelter.SetSurvivorHealthy(a));
            Assert.AreEqual(SurvivorStatus.Healthy, a.status);
            Assert.GreaterOrEqual(a.hunger, 2);

            Survivor picked;
            Assert.IsTrue(shelter.TryPickRandomAlive(out picked));
            Assert.IsNotNull(picked);
            Assert.AreNotEqual(SurvivorStatus.Dead, picked.status);
        }
    }
}
