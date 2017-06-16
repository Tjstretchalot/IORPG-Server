using IORPG.Util;
using Microsoft.Xna.Framework;
using SharpMath2;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game
{
    public class MutatingWorld
    {
        public Dictionary<int, Entity> Entities;
        public Dictionary<int, MutatingTeam> Teams;
        public int Timestamp;
        public int IDCounter;
        public Random RandomGen;
        public List<Action> FinishedCallbacks;
        public int Width;
        public int Height;

        public MutatingWorld(int width, int height, int predictedNumEntites, int timestamp, int idCounter, Random random)
        {
            Width = width;
            Height = height;
            Timestamp = timestamp;
            IDCounter = idCounter;
            Entities = new Dictionary<int, Entity>(predictedNumEntites);
            Teams = new Dictionary<int, MutatingTeam>();
            RandomGen = random;
            FinishedCallbacks = new List<Action>();
        }

        public void AddRange(IEnumerable<Entity> ents)
        {
            foreach(var e in ents)
            {
                Add(e);
            }
        }

        public void Add(Entity e)
        {
            Entities.Add(e.ID, e);

            Teams[e.Team].Members.Add(e.ID);
        }

        public void RemoveByID(int id)
        {
            var ent = Entities[id];
            foreach (var nearbyID in ent.NearbyEntityIds)
            {
                var nearbyEnt = Entities[nearbyID];

                if (nearbyEnt.NearbyEntityIds.Contains(ent.ID))
                {
                    Entities[nearbyID] = new Entity(nearbyEnt, nearby: (Maybe<ImmutableHashSet<int>>)nearbyEnt.NearbyEntityIds.Remove(ent.ID));
                }
            }
            Teams[ent.Team].Members.Remove(ent.ID);
            Entities.Remove(id);
        }

        public Entity GetByID(int id)
        {
            return Entities[id];
        }

        public World AsReadOnly()
        {
            var entities = new List<Entity>();
            var idsToIndexes = new Dictionary<int, int>();
            
            foreach(var id in Entities.Keys)
            {
                entities.Add(Entities[id]);
                idsToIndexes.Add(id, entities.Count - 1);
            }

            return new World(Width, Height, Timestamp, IDCounter, entities.ToImmutableList(), new ReadOnlyDictionary<int, int>(idsToIndexes), DictUtils.FromEnumerable(Teams.Select((kvp) => new KeyValuePair<int, Team>(kvp.Key, kvp.Value.AsReadOnly()))));
        }
    }
}
