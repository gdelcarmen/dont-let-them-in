namespace DontLetThemIn.Core
{
    public enum FloorBreachOutcome
    {
        Retreat,
        RunFailed
    }

    public sealed class RunProgressionState
    {
        private readonly int _totalFloors;

        public RunProgressionState(int totalFloors)
        {
            _totalFloors = totalFloors <= 0 ? 1 : totalFloors;
        }

        public int CurrentFloorIndex { get; private set; }

        public int FloorsLost { get; private set; }

        public int FloorsCleared { get; private set; }

        public bool IsRunEnded { get; private set; }

        public bool IsRunWon { get; private set; }

        public int CalculateStartingScrap(int baseScrap)
        {
            int penalty = FloorsLost * 20;
            int result = baseScrap - penalty;
            return result > 0 ? result : 0;
        }

        public bool AdvanceAfterFloorClear()
        {
            if (IsRunEnded)
            {
                return false;
            }

            FloorsCleared++;
            if (CurrentFloorIndex >= _totalFloors - 1)
            {
                IsRunEnded = true;
                IsRunWon = true;
                return false;
            }

            CurrentFloorIndex++;
            return true;
        }

        public FloorBreachOutcome RegisterFloorBreach()
        {
            if (IsRunEnded)
            {
                return FloorBreachOutcome.RunFailed;
            }

            if (CurrentFloorIndex >= _totalFloors - 1)
            {
                IsRunEnded = true;
                IsRunWon = false;
                return FloorBreachOutcome.RunFailed;
            }

            FloorsLost++;
            CurrentFloorIndex++;
            return FloorBreachOutcome.Retreat;
        }
    }
}
