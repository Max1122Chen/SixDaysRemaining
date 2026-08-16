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
        public void AllocateFood_ReducesStockAndIncreasesHunger()
        {
            Survivor survivor = new Survivor { name = "Test", hunger = 1, status = SurvivorStatus.Hungry };
            shelter.RegisterSurvivor(survivor);
            state.foodStock = 5;

            bool ok = shelter.AllocateFood(survivor, 2);

            Assert.IsTrue(ok);
            Assert.AreEqual(3, state.foodStock);
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
        public void DyingSurvivor_CanBeSavedByAllocationBeforeNextEndOfDay()
        {
            Survivor survivor = new Survivor
            {
                name = "Test",
                hunger = 0,
                status = SurvivorStatus.Dying,
                hungryToDyingDays = 2
            };
            shelter.RegisterSurvivor(survivor);
            state.foodStock = 3;

            Assert.IsTrue(shelter.AllocateFood(survivor, 2));
            Assert.AreEqual(SurvivorStatus.Healthy, survivor.status);

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
    }
}
