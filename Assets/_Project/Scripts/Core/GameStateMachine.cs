using System;

namespace DontLetThemIn.Core
{
    public sealed class GameStateMachine
    {
        public event Action<GameState> StateChanged;

        public GameState CurrentState { get; private set; } = GameState.Running;

        public void SetState(GameState next)
        {
            if (CurrentState == next)
            {
                return;
            }

            CurrentState = next;
            StateChanged?.Invoke(next);
        }
    }
}
