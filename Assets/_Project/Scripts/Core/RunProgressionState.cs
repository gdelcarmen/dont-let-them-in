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
        private readonly bool _endlessMode;

        public RunProgressionState(int totalFloors, bool endlessMode = false)
        {
            _totalFloors = totalFloors <= 0 ? 1 : totalFloors;
            _endlessMode = endlessMode;
        }

        public int CurrentFloorIndex { get; private set; }

        public int FloorsLost { get; private set; }

        public int FloorsCleared { get; private set; }

        public bool IsRunEnded { get; private set; }

        public bool IsRunWon { get; private set; }

        public bool IsEndlessMode => _endlessMode;

        public int CalculateStartingScrap(int baseScrap)
        {
            int penalty = FloorsLost * 10;
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
                if (_endlessMode)
                {
                    CurrentFloorIndex = 0;
                    return true;
                }

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
