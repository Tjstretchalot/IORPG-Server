using Microsoft.Xna.Framework;
using SharpMath2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game
{
    public class World
    {
        public readonly IReadOnlyList<Entity> Entities;
        public readonly IReadOnlyDictionary<int, int> EntityIDToIndex;
        public readonly IReadOnlyDictionary<int, Team> Teams;
        public readonly int Timestamp;
        public readonly int IDCounter;
        public readonly int Width;
        public readonly int Height;
        
        public World(int width, int height, int timestamp, int idCounter, IReadOnlyList<Entity> entities, IReadOnlyDictionary<int, int> idToIndex, IReadOnlyDictionary<int, Team> teams)
        {
            Width = width;
            Height = height;
            Timestamp = timestamp;
            IDCounter = idCounter;
            Entities = entities;
            EntityIDToIndex = idToIndex;
            Teams = teams;
        }

        public Entity GetByID(int id)
        {
            return Entities[EntityIDToIndex[id]];
        }

        public IEnumerable<Entity> GetEntitiesIn(Rect2 bounds, Vector2 loc)
        {
            return Entities.Where((e) => Shape2.Intersects(e.Attributes.Bounds, bounds, e.Location, loc, false));
        }
    }
}
