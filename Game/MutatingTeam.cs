using SharpMath2;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game
{
    public class MutatingTeam
    {
        public int ID;
        public HashSet<int> Members;
        public Rect2 SpawnRect;
        public int LastSpawnTimeMS;
        
        public MutatingTeam(int id, HashSet<int> members, Rect2 spawnRect, int lastSpawnTimeMS)
        {
            ID = id;
            Members = members;
            SpawnRect = spawnRect;
            LastSpawnTimeMS = lastSpawnTimeMS;
        }

        public Team AsReadOnly()
        {
            return new Team(ID, ImmutableHashSet.CreateRange(Members), SpawnRect, LastSpawnTimeMS);
        }
    }
}
