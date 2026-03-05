using System;

namespace DontLetThemIn.Economy
{
    public sealed class ScrapManager
    {
        public ScrapManager(int startingScrap)
        {
            CurrentScrap = startingScrap;
        }

        public event Action<int> ScrapChanged;

        public int CurrentScrap { get; private set; }

        public bool CanAfford(int amount)
        {
            return amount <= CurrentScrap;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || !CanAfford(amount))
            {
                return false;
            }

            CurrentScrap -= amount;
            ScrapChanged?.Invoke(CurrentScrap);
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentScrap += amount;
            ScrapChanged?.Invoke(CurrentScrap);
        }

        public void SetCurrentScrap(int amount)
        {
            CurrentScrap = Math.Max(0, amount);
            ScrapChanged?.Invoke(CurrentScrap);
        }
    }
}
