using System;
using System.Collections.Generic;

namespace DontLetThemIn.Core
{
    public sealed class GameStateMachine
    {
        private static readonly Dictionary<GameState, HashSet<GameState>> AllowedTransitions = new()
        {
            [GameState.PrepPhase] = new HashSet<GameState> { GameState.WaveActive, GameState.RunEnd },
            [GameState.WaveActive] = new HashSet<GameState>
            {
                GameState.WaveClear,
                GameState.FloorClear,
                GameState.FloorLost,
                GameState.RunEnd
            },
            [GameState.WaveClear] = new HashSet<GameState>
            {
                GameState.PrepPhase,
                GameState.WaveActive,
                GameState.FloorClear,
                GameState.RunEnd
            },
            [GameState.FloorClear] = new HashSet<GameState> { GameState.DraftPick, GameState.RunEnd },
            [GameState.DraftPick] = new HashSet<GameState> { GameState.Transitioning, GameState.RunEnd },
            [GameState.FloorLost] = new HashSet<GameState> { GameState.Transitioning, GameState.RunEnd },
            [GameState.Transitioning] = new HashSet<GameState> { GameState.PrepPhase, GameState.RunEnd },
            [GameState.RunEnd] = new HashSet<GameState>()
        };

        public event Action<GameState> StateChanged;

        public GameState CurrentState { get; private set; } = GameState.PrepPhase;

        public bool CanTransitionTo(GameState next)
        {
            return AllowedTransitions.TryGetValue(CurrentState, out HashSet<GameState> targets) && targets.Contains(next);
        }

        public bool TrySetState(GameState next)
        {
            if (CurrentState == next)
            {
                return false;
            }

            if (!CanTransitionTo(next))
            {
                return false;
            }

            CurrentState = next;
            StateChanged?.Invoke(next);
            return true;
        }

        public void ForceState(GameState next)
        {
            if (CurrentState == next)
            {
                return;
            }

            CurrentState = next;
            StateChanged?.Invoke(next);
        }

        public void SetState(GameState next)
        {
            TrySetState(next);
        }
    }
}
