using DontLetThemIn.Core;
using NUnit.Framework;

namespace DontLetThemIn.Tests.EditMode
{
    public sealed class Stage5FloorProgressionEditModeTests
    {
        [Test]
        public void FloorTransition_AdvancesToCorrectNextFloor()
        {
            RunProgressionState progression = new(3);

            Assert.That(progression.CurrentFloorIndex, Is.EqualTo(0));
            Assert.That(progression.AdvanceAfterFloorClear(), Is.True);
            Assert.That(progression.CurrentFloorIndex, Is.EqualTo(1));
            Assert.That(progression.FloorsCleared, Is.EqualTo(1));
        }

        [Test]
        public void ScrapPenalty_AppliesCorrectly_WhenFloorsAreLost()
        {
            RunProgressionState progression = new(3);

            Assert.That(progression.CalculateStartingScrap(60), Is.EqualTo(60));

            progression.RegisterFloorBreach();
            Assert.That(progression.CalculateStartingScrap(60), Is.EqualTo(50));

            progression.RegisterFloorBreach();
            Assert.That(progression.CalculateStartingScrap(60), Is.EqualTo(40));
        }

        [Test]
        public void RunEnds_WhenAtticIsBreached()
        {
            RunProgressionState progression = new(3);
            progression.RegisterFloorBreach();
            progression.RegisterFloorBreach();

            FloorBreachOutcome outcome = progression.RegisterFloorBreach();

            Assert.That(outcome, Is.EqualTo(FloorBreachOutcome.RunFailed));
            Assert.That(progression.IsRunEnded, Is.True);
            Assert.That(progression.IsRunWon, Is.False);
        }

        [Test]
        public void RunVictory_Triggers_WhenAtticIsCleared()
        {
            RunProgressionState progression = new(3);
            progression.AdvanceAfterFloorClear();
            progression.AdvanceAfterFloorClear();

            bool hasNext = progression.AdvanceAfterFloorClear();

            Assert.That(hasNext, Is.False);
            Assert.That(progression.IsRunEnded, Is.True);
            Assert.That(progression.IsRunWon, Is.True);
        }
    }
}
