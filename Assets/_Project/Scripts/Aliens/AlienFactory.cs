using UnityEngine;

namespace DontLetThemIn.Aliens
{
    public static class AlienFactory
    {
        public static AlienBase CreateAlien(AlienData alienData, int sequenceNumber, Transform parent = null)
        {
            AlienType type = alienData != null ? alienData.AlienType : AlienType.Grey;
            GameObject alienObject = new($"Alien_{sequenceNumber}_{type}");
            if (parent != null)
            {
                alienObject.transform.SetParent(parent, false);
            }

            AlienBase alien = type switch
            {
                AlienType.Stalker => alienObject.AddComponent<StalkerAlien>(),
                AlienType.TechUnit => alienObject.AddComponent<TechUnitAlien>(),
                AlienType.Overlord => alienObject.AddComponent<OverlordAlien>(),
                _ => alienObject.AddComponent<GreyAlien>()
            };

            switch (alien)
            {
                case GreyAlien grey:
                    grey.BuildVisual();
                    break;
                case StalkerAlien stalker:
                    stalker.BuildVisual();
                    break;
                case TechUnitAlien techUnit:
                    techUnit.BuildVisual();
                    break;
                case OverlordAlien overlord:
                    overlord.BuildVisual();
                    break;
            }

            return alien;
        }
    }
}
