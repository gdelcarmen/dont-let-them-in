using System;
using System.Collections.Generic;
using DontLetThemIn.Aliens;
using UnityEngine;

namespace DontLetThemIn.Waves
{
    [CreateAssetMenu(menuName = "Don't Let Them In/Waves/Wave Config", fileName = "WaveConfig")]
    public sealed class WaveConfig : ScriptableObject
    {
        public string WaveName = "Wave 1";
        public List<WaveSpawnDirective> Spawns = new();
    }

    [Serializable]
    public sealed class WaveSpawnDirective
    {
        public AlienData Alien;
        public int Count = 3;
        public float SpawnDelay = 1f;
        public int EntryPointIndex;
    }
}
