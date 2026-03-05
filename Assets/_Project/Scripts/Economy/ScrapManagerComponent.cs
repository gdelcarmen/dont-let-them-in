using UnityEngine;

namespace DontLetThemIn.Economy
{
    public sealed class ScrapManagerComponent : MonoBehaviour
    {
        [SerializeField] private int startingScrap = 60;

        public ScrapManager Runtime { get; private set; }

        public ScrapManager Initialize(int initialScrap)
        {
            startingScrap = initialScrap;
            Runtime = new ScrapManager(startingScrap);
            return Runtime;
        }

        private void Awake()
        {
            Runtime ??= new ScrapManager(startingScrap);
        }
    }
}
