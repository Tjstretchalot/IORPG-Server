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
        public List<Entity> Entities;
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
            Entities = new List<Entity>(predictedNumEntites);
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
            Entities.Add(e);

            Teams[e.Team].Members.Add(e.ID);
        }

        public void RemoveByIndex(int index)
        {
            var ent = Entities[index];
            foreach (var nearbyID in ent.NearbyEntityIds)
            {
                var nearbyEntIndex = Entities.FindIndex((e) => e.ID == nearbyID);
                var nearbyEnt = Entities[nearbyEntIndex];

                if (nearbyEnt.NearbyEntityIds.Contains(ent.ID))
                {
                    Entities[nearbyEntIndex] = new Entity(nearbyEnt, nearby: (Maybe<ImmutableHashSet<int>>)nearbyEnt.NearbyEntityIds.Remove(ent.ID));
                }
            }
            Teams[ent.Team].Members.Remove(ent.ID);
            Entities.RemoveAt(index);
        }

        public void RemoveByID(int id)
        {
            RemoveByIndex(Entities.FindIndex((e) => e.ID == id));
        }

        public Entity GetByID(int id)
        {
            return Entities.Find((e) => e.ID == id);
        }

        public World AsReadOnly()
        {
            var idsToIndexes = new Dictionary<int, int>();
            for(int i = 0; i < Entities.Count; i++)
            {
                idsToIndexes.Add(Entities[i].ID, i);
            }

            return new World(Width, Height, Timestamp, IDCounter, Entities.AsReadOnly(), new ReadOnlyDictionary<int, int>(idsToIndexes), DictUtils.FromEnumerable(Teams.Select((kvp) => new KeyValuePair<int, Team>(kvp.Key, kvp.Value.AsReadOnly()))));
        }
    }
}
