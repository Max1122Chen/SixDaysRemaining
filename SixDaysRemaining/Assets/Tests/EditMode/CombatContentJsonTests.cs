using System;
using System.IO;
using NUnit.Framework;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;
using SixDaysRemaining.Combat.Content;
using UnityEngine;

namespace SixDaysRemaining.Tests.EditMode
{
    public class CombatContentJsonTests
    {
        [TearDown]
        public void TearDown()
        {
            CombatContent.ClearForTests();
        }

        [Test]
        public void LoadFromStreamingAssets_LoadsJianYiAndDayOneEncounter()
        {
            CombatContent.ClearForTests();
            CombatContent.Ensure();

            CardDef jianYi = CombatContent.Cards.Get(CardIds.JianYi);
            Assert.AreEqual("剑意", jianYi.DisplayName);
            Assert.AreEqual(1, jianYi.Effects.Length);
            Assert.AreEqual(EffectOp.DealDamage, jianYi.Effects[0].Op);
            Assert.AreEqual(5f, jianYi.Effects[0].Amount);

            EnemyEncounterDef day1 = CombatContent.Encounters.GetForDay(1);
            Assert.AreEqual(EncounterIds.Mob01, day1.Id);
            Assert.AreEqual(35f, day1.MaxHp);
            Assert.AreEqual(5, day1.RoundPlans[0].Length);
            Assert.AreEqual(CardIds.Attack(3), day1.RoundPlans[0][0]);
            Assert.AreEqual(CardIds.SleepFive, day1.RoundPlans[3][2]);

            CardDef defend3 = CombatContent.Cards.Get(CardIds.Defend(3));
            Assert.AreEqual(3f, defend3.Effects[0].Amount);
            CardDef sleepFive = CombatContent.Cards.Get(CardIds.SleepFive);
            Assert.AreEqual(EffectOp.Heal, sleepFive.Effects[0].Op);
            Assert.AreEqual(5f, sleepFive.Effects[0].Amount);

            System.Collections.Generic.List<CardDef> starter = CombatContent.CreateDefaultStarterDefs();
            Assert.AreEqual(16, starter.Count);
        }

        [Test]
        public void LoadFromFolder_MissingFile_Throws()
        {
            string dir = Path.Combine(Application.temporaryCachePath, "combat-json-missing-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                Assert.Throws<FileNotFoundException>(() => CombatContentJsonLoader.LoadFromFolder(dir));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void LoadFromFolder_DuplicateCardId_Throws()
        {
            string dir = CopyBaselineToTemp();
            try
            {
                string cardsPath = Path.Combine(dir, CombatContentJsonLoader.CardsFileName);
                string text = File.ReadAllText(cardsPath);
                // Duplicate first card entry crudely by appending a second cards array is hard;
                // rewrite a minimal invalid file.
                File.WriteAllText(cardsPath, @"{
  ""cards"": [
    { ""id"": 1000, ""displayName"": ""A"", ""tags"": [""Attack""], ""canBlacken"": true,
      ""effects"": [ { ""op"": ""DealDamage"", ""amount"": 1, ""target"": ""Enemy"" } ] },
    { ""id"": 1000, ""displayName"": ""B"", ""tags"": [""Attack""], ""canBlacken"": true,
      ""effects"": [ { ""op"": ""DealDamage"", ""amount"": 1, ""target"": ""Enemy"" } ] }
  ]
}");
                // encounters/starter still valid from copy — cards duplicate should throw during BuildCards
                Assert.Throws<InvalidOperationException>(() => CombatContentJsonLoader.LoadFromFolder(dir));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void LoadFromFolder_RoundPlanWrongLength_Throws()
        {
            string dir = CopyBaselineToTemp();
            try
            {
                File.WriteAllText(Path.Combine(dir, CombatContentJsonLoader.EncountersFileName), @"{
  ""encounters"": [
    {
      ""id"": 1,
      ""displayName"": ""bad"",
      ""maxHp"": 10,
      ""damageBonus"": 0,
      ""roundPlans"": [ { ""slots"": [2204, 0, 2090] } ]
    }
  ],
  ""dayMap"": [
    { ""day"": 1, ""encounterId"": 1 },
    { ""day"": 2, ""encounterId"": 1 },
    { ""day"": 3, ""encounterId"": 1 },
    { ""day"": 4, ""encounterId"": 1 },
    { ""day"": 5, ""encounterId"": 1 },
    { ""day"": 6, ""encounterId"": 1 }
  ]
}");
                Assert.Throws<InvalidOperationException>(() => CombatContentJsonLoader.LoadFromFolder(dir));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        private static string CopyBaselineToTemp()
        {
            string src = CombatContentJsonLoader.CombatFolderPath;
            string dir = Path.Combine(Application.temporaryCachePath, "combat-json-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            File.Copy(
                Path.Combine(src, CombatContentJsonLoader.CardsFileName),
                Path.Combine(dir, CombatContentJsonLoader.CardsFileName));
            File.Copy(
                Path.Combine(src, CombatContentJsonLoader.EncountersFileName),
                Path.Combine(dir, CombatContentJsonLoader.EncountersFileName));
            File.Copy(
                Path.Combine(src, CombatContentJsonLoader.StarterFileName),
                Path.Combine(dir, CombatContentJsonLoader.StarterFileName));
            return dir;
        }
    }
}
