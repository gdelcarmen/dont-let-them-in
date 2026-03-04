using UnityEngine;

namespace DontLetThemIn.Aliens
{
    [CreateAssetMenu(menuName = "Don't Let Them In/Aliens/Alien Data", fileName = "AlienData")]
    public sealed class AlienData : ScriptableObject
    {
        public string AlienName = "Grey";
        public AlienType AlienType = AlienType.Grey;
        public float MaxHealth = 20f;
        public float Speed = 2f;
        public int ScrapReward = 12;
        public bool HasSpecialAbility;
    }
}
