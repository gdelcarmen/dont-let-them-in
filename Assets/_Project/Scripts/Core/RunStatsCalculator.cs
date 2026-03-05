using System;
using System.Collections.Generic;
using System.Linq;

namespace DontLetThemIn.Core
{
    public readonly struct RunEndStats
    {
        public RunEndStats(
            bool survived,
            int floorsCleared,
            int totalKills,
            int totalScrapEarned,
            string bestDefenseSummary)
        {
            Survived = survived;
            FloorsCleared = Math.Max(0, floorsCleared);
            TotalKills = Math.Max(0, totalKills);
            TotalScrapEarned = Math.Max(0, totalScrapEarned);
            BestDefenseSummary = string.IsNullOrWhiteSpace(bestDefenseSummary) ? "N/A" : bestDefenseSummary;
        }

        public bool Survived { get; }

        public int FloorsCleared { get; }

        public int TotalKills { get; }

        public int TotalScrapEarned { get; }

        public string BestDefenseSummary { get; }
    }

    public static class RunStatsCalculator
    {
        public static RunEndStats Build(
            bool survived,
            int floorsCleared,
            int totalKills,
            int totalScrapEarned,
            IReadOnlyDictionary<string, int> defenseKillCounts)
        {
            return new RunEndStats(
                survived,
                floorsCleared,
                totalKills,
                totalScrapEarned,
                ResolveBestDefenseSummary(defenseKillCounts));
        }

        public static string ResolveBestDefenseSummary(IReadOnlyDictionary<string, int> defenseKillCounts)
        {
            if (defenseKillCounts == null || defenseKillCounts.Count == 0)
            {
                return "N/A";
            }

            KeyValuePair<string, int> best = defenseKillCounts
                .Where(pair => pair.Value > 0 && !string.IsNullOrWhiteSpace(pair.Key))
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            return best.Value <= 0 ? "N/A" : $"{best.Key} ({best.Value} KOs)";
        }
    }
}
