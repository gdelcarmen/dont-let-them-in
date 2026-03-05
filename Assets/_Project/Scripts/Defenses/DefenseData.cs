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
        public float AttackInterval = 1f;
        public float MoveSpeed = 3f;
        public float ContactRadius = 0.35f;
        public float EffectDuration = 0f;
        public int KnockbackNodes = 0;
        public int MaxActivePerFloor = 0;
        public bool RequiresHallwayPlacement;
        public Color DisplayColor = Color.white;
        [TextArea] public string Description = "Cheap improvised trap with attitude.";
        public bool BlocksPath = true;
    }
}
