using UnityEngine;

namespace DontLetThemIn.Defenses
{
    [CreateAssetMenu(menuName = "Don't Let Them In/Defenses/Defense Data", fileName = "DefenseData")]
    public sealed class DefenseData : ScriptableObject
    {
        public string DefenseName = "Tripwire Trap";
        public DefenseCategory Category = DefenseCategory.A;
        public int ScrapCost = 20;
        public float Damage = 8f;
        public int Range = 0;
        public int Uses = 999;
        [TextArea] public string Description = "Cheap improvised trap with attitude.";
        public bool BlocksPath = true;
    }
}
