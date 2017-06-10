using SharpMath2;
using System.Collections.Immutable;

namespace IORPG.Game
{
    public class Team
    {
        public readonly int ID;
        public readonly ImmutableHashSet<int> Members;
        public readonly Rect2 SpawnRect;
        public readonly int LastSpawnTime;

        public Team(int id, ImmutableHashSet<int> members, Rect2 spawnRect, int lastSpawnTime)
        {
            ID = id;
            Members = members;
            SpawnRect = spawnRect;
            LastSpawnTime = lastSpawnTime;
        }
    }
}