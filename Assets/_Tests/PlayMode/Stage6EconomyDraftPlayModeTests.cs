using System.Collections;
using System.Linq;
using DontLetThemIn.Core;
using DontLetThemIn.Defenses;
using DontLetThemIn.Economy;
using DontLetThemIn.Grid;
using DontLetThemIn.Hazards;
using DontLetThemIn.UI;
using DontLetThemIn.Waves;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DontLetThemIn.Tests.PlayMode
{
    public sealed class Stage6EconomyDraftPlayModeTests
    {
        [UnityTest]
        public IEnumerator CompleteFloor_ShowsDraftPickWithThreeCards()
        {
            GameManager manager = CreateConfiguredManager(autoSelectDraft: false);

            yield return WaitForState(manager, GameState.PrepPhase, 20f);

            manager.DebugForceFloorClear();
            yield return WaitForState(manager, GameState.DraftPick, 20f);

            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.IsDraftPickVisible, Is.True);
            Assert.That(manager.CurrentDraftOffers.Count, Is.EqualTo(3));

            yield return CleanupGeneratedSceneObjects();
        }

        [UnityTest]
        public IEnumerator SelectingDraftCard_AppliesEffectAndPersistsOnNextFloor()
        {
            GameManager manager = CreateConfiguredManager(autoSelectDraft: false);

            yield return WaitForState(manager, GameState.PrepPhase, 20f);

            int defenseCountBefore = manager.AvailableDefenseCount;
            manager.DebugForceFloorClear();
            yield return WaitForState(manager, GameState.DraftPick, 20f);

            Assert.That(manager.CurrentDraftOffers.Count, Is.EqualTo(3));
            DraftOffer selectedOffer = manager.CurrentDraftOffers[0];

            float damageBefore = 0f;
            int rangeBefore = 0;
            float intervalBefore = 0f;
            float moveSpeedBefore = 0f;

            if (selectedOffer.OfferType == DraftOfferType.DefenseUpgrade)
            {
                DefenseData target = manager.GetAvailableDefenseByName(selectedOffer.TargetDefenseName);
                Assert.That(target, Is.Not.Null);
                damageBefore = target.Damage;
                rangeBefore = target.Range;
                intervalBefore = target.AttackInterval;
                moveSpeedBefore = target.MoveSpeed;
            }

            Assert.That(manager.DebugSelectDraftOption(0), Is.True);

            yield return WaitForCondition(
                () => manager.CurrentFloorIndex == 1 && manager.CurrentState == GameState.PrepPhase,
                25f,
                "draft transition to Upper Floor prep");

            switch (selectedOffer.OfferType)
            {
                case DraftOfferType.NewDefense:
                    Assert.That(manager.AvailableDefenseCount, Is.EqualTo(defenseCountBefore + 1));
                    Assert.That(manager.GetAvailableDefenseByName(selectedOffer.DefenseTemplate.DefenseName), Is.Not.Null);
                    break;
                case DraftOfferType.DefenseUpgrade:
                {
                    DefenseData upgraded = manager.GetAvailableDefenseByName(selectedOffer.TargetDefenseName);
                    Assert.That(upgraded, Is.Not.Null);

                    if (selectedOffer.DamageBonus != 0f)
                    {
                        Assert.That(upgraded.Damage, Is.EqualTo(damageBefore + selectedOffer.DamageBonus).Within(0.001f));
                    }

                    if (selectedOffer.RangeBonus != 0)
                    {
                        Assert.That(upgraded.Range, Is.EqualTo(rangeBefore + selectedOffer.RangeBonus));
                    }

                    if (selectedOffer.AttackIntervalMultiplier != 1f)
                    {
                        Assert.That(upgraded.AttackInterval, Is.EqualTo(intervalBefore * selectedOffer.AttackIntervalMultiplier).Within(0.001f));
                    }

                    if (selectedOffer.MoveSpeedMultiplier != 1f)
                    {
                        Assert.That(upgraded.MoveSpeed, Is.EqualTo(moveSpeedBefore * selectedOffer.MoveSpeedMultiplier).Within(0.001f));
                    }

                    break;
                }
                case DraftOfferType.Perk:
                    if (selectedOffer.PerkType == DraftPerkType.BonusStartingScrap)
                    {
                        int expectedStartingScrap = 60 + Mathf.Max(10, selectedOffer.PerkAmount);
                        Assert.That(manager.CurrentFloorStartingScrap, Is.EqualTo(expectedStartingScrap));
                    }

                    if (selectedOffer.PerkType == DraftPerkType.TrapReset)
                    {
                        Assert.That(manager.HasDraftPerk(DraftPerkType.TrapReset), Is.True);
                    }

                    break;
            }

            yield return CleanupGeneratedSceneObjects();
        }

        [UnityTest]
        public IEnumerator FullRun_ShowsRunEndStats_AndReturnToMenuWorks()
        {
            GameManager manager = CreateConfiguredManager(autoSelectDraft: true);

            yield return WaitForState(manager, GameState.PrepPhase, 20f);

            manager.DebugForceFloorClear();
            yield return WaitForCondition(() => manager.CurrentFloorIndex == 1 && manager.CurrentState == GameState.PrepPhase, 20f, "to upper floor");

            manager.DebugForceFloorClear();
            yield return WaitForCondition(() => manager.CurrentFloorIndex == 2 && manager.CurrentState == GameState.PrepPhase, 20f, "to attic floor");

            manager.DebugForceFloorClear();
            yield return WaitForState(manager, GameState.RunEnd, 20f);

            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.IsRunEndVisible, Is.True);
            Assert.That(manager.IsRunWon, Is.True);
            Assert.That(manager.FloorsCleared, Is.EqualTo(3));

            Button returnButton = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(button => button != null && button.name == "ReturnButton");
            Assert.That(returnButton, Is.Not.Null);

            returnButton.onClick.Invoke();

            yield return WaitForCondition(
                () => SceneManager.GetActiveScene().name == "MainMenu",
                15f,
                "return to main menu scene");
        }

        private static GameManager CreateConfiguredManager(bool autoSelectDraft)
        {
            GameObject host = new("GameManager");
            host.SetActive(false);
            GameManager manager = host.AddComponent<GameManager>();
            manager.ConfigureDebugTimings(999f, 0f, autoSelectDraft);
            host.SetActive(true);
            return manager;
        }

        private static IEnumerator WaitForState(GameManager manager, GameState state, float timeoutSeconds)
        {
            yield return WaitForCondition(() => manager != null && manager.CurrentState == state, timeoutSeconds, $"state={state}");
        }

        private static IEnumerator WaitForCondition(
            System.Func<bool> condition,
            float timeoutSeconds,
            string context)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"Condition timed out after {timeoutSeconds} seconds ({context}).");
        }

        private static IEnumerator CleanupGeneratedSceneObjects()
        {
            DestroyComponents<WaveSpawner>();
            DestroyComponents<DefensePlacementController>();
            DestroyComponents<HazardSystem>();
            DestroyComponents<GridDebugDrawer>();
            DestroyComponents<FloorRenderer>();
            DestroyComponents<ScrapManagerComponent>();
            DestroyComponents<HUDController>();
            DestroyComponents<Canvas>();
            DestroyComponents<EventSystem>();
            DestroyComponents<Camera>();

            yield return null;
        }

        private static void DestroyComponents<T>() where T : Component
        {
            T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (T component in components)
            {
                if (component != null)
                {
                    Object.Destroy(component.gameObject);
                }
            }
        }
    }
}
