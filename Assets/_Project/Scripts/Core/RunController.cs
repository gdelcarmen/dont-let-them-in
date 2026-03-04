using UnityEngine;

namespace DontLetThemIn.Core
{
    public sealed class RunController : MonoBehaviour
    {
        public int RunNumber { get; private set; } = 1;

        public void BeginNewRun()
        {
            RunNumber++;
        }
    }
}
