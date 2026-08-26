using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Packspire.Tests
{
    public sealed class JourneySessionSaveTests
    {
        [Test]
        public void SaveExportImport_RetainsRewardBoundaryAndRunProgress()
        {
            MetaSave meta = NewMeta();
            RunState run = NewRun(meta);
            ExpeditionRouteNodePlan next = ExpeditionJourneySystem.Available(run).First();
            Assert.That(ExpeditionJourneySystem.Select(run, next.id), Is.True);
            Assert.That(
                ExpeditionJourneySystem.Commit(run, out ExpeditionRouteNodePlan committed, out _),
                Is.True);
            run.hp = 27;
            JourneyBattleRewardOffer offer = JourneyBattleRewardSystem.CreateOffer(
                run,
                JourneyBattleRewardTier.Danger,
                new System.Random(9021));

            Assert.That(
                JourneySessionSaveSystem.Capture(
                    meta,
                    run,
                    JourneyResumeStage.Reward,
                    offer),
                Is.True);

            MetaSave imported = SaveSystem.Import(SaveSystem.Export(meta));
            Assert.That(imported.version, Is.EqualTo(SaveSystem.CurrentVersion));
            Assert.That(
                JourneySessionSaveSystem.TryRestore(
                    imported,
                    out JourneySessionSnapshot restored),
                Is.True);
            Assert.That(restored.stage, Is.EqualTo(JourneyResumeStage.Reward));
            Assert.That(restored.run.hp, Is.EqualTo(27));
            Assert.That(restored.run.expeditionPlan.currentNodeId, Is.EqualTo(committed.id));
            Assert.That(restored.rewardOffer.candidates, Has.Length.EqualTo(3));
            Assert.That(
                restored.rewardOffer.candidates.Select(candidate => candidate.contentId),
                Is.EqualTo(offer.candidates.Select(candidate => candidate.contentId)));
        }

        [Test]
        public void Restore_ReturnsDetachedRunCopy()
        {
            MetaSave meta = NewMeta();
            RunState run = NewRun(meta);
            run.hp = 31;
            JourneySessionSaveSystem.Capture(
                meta,
                run,
                JourneyResumeStage.Choice);

            Assert.That(
                JourneySessionSaveSystem.TryRestore(
                    meta,
                    out JourneySessionSnapshot restored),
                Is.True);
            restored.run.hp = 1;
            restored.run.consumables.Add("heal");

            Assert.That(meta.activeJourney.run.hp, Is.EqualTo(31));
            Assert.That(meta.activeJourney.run.consumables, Is.Empty);
        }

        [Test]
        public void InvalidRewardBoundary_FallsBackToSafeChoice()
        {
            MetaSave meta = NewMeta();
            meta.activeJourney = new JourneySessionSnapshot
            {
                stage = JourneyResumeStage.Reward,
                run = NewRun(meta),
                rewardOffer = null
            };

            Assert.That(
                JourneySessionSaveSystem.TryRestore(
                    meta,
                    out JourneySessionSnapshot restored),
                Is.True);
            Assert.That(restored.stage, Is.EqualTo(JourneyResumeStage.Choice));
        }

        [Test]
        public void Version19Migration_DoesNotInventActiveJourney()
        {
            MetaSave imported = SaveSystem.Import(
                "{\"version\":19,\"selectedCharacterId\":\"ren\"}");

            Assert.That(imported.version, Is.EqualTo(20));
            Assert.That(imported.activeJourney, Is.Null);
            Assert.That(JourneySessionSaveSystem.CanResume(imported), Is.False);
        }

        [Test]
        public void Clear_RemovesResumeBoundary()
        {
            MetaSave meta = NewMeta();
            JourneySessionSaveSystem.Capture(
                meta,
                NewRun(meta),
                JourneyResumeStage.Battle);
            Assert.That(JourneySessionSaveSystem.CanResume(meta), Is.True);

            JourneySessionSaveSystem.Clear(meta);

            Assert.That(JourneySessionSaveSystem.CanResume(meta), Is.False);
        }

        [Test]
        public void ExpeditionView_ExposesResumeActionCopyContract()
        {
            VisualTreeAsset asset = PackspireResources.Load<VisualTreeAsset>(
                "UI/PackspireExpeditionView");
            Assert.That(asset, Is.Not.Null);
            VisualElement root = asset.CloneTree();

            Assert.That(
                root.Q<Label>("expedition-auth-action-sub"),
                Is.Not.Null);
            Assert.That(
                root.Q<Label>("expedition-auth-action-copy"),
                Is.Not.Null);
        }

        private static MetaSave NewMeta()
        {
            var meta = new MetaSave
            {
                currentRole = "warrior",
                selectedCharacterId = "ren"
            };
            RoleFrameworkSystem.Normalize(meta);
            return meta;
        }

        private static RunState NewRun(MetaSave meta)
        {
            var run = new RunState
            {
                dungeon = PackspireContent.Data.balance.defaultDungeonId,
                hp = 42,
                maxHp = 42,
                role = meta.currentRole,
                characterId = meta.selectedCharacterId
            };
            run.courierRoute = CourierRouteSystem.Create(run, meta);
            run.consumables.Clear();
            return run;
        }
    }
}
