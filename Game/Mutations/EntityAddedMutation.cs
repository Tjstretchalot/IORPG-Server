using IORPG.Util;
using Microsoft.Xna.Framework;
using SharpMath2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game.Mutations
{
    public class EntityAddedMutation : IWorldMutation
    {
        public WorldMutationTime Time => WorldMutationTime.BeforeEntitiesTick;
        public readonly string Name;
        public readonly int Team;
        public readonly EntityAttributes Attributes;
        public readonly Rect2 SpawnBounds;
        public readonly Action<Entity> Callback;

        /// <summary>
        /// If team is -1 then the team will be autoassigned and the spawnBounds will be the spawn bounds of the 
        /// assigned team.
        /// </summary>
        /// <param name="name">Name of the new entity</param>
        /// <param name="team">Team of the new entity or -1</param>
        /// <param name="attributes">Attributes of the entity</param>
        /// <param name="spawnBounds">Spawn bounds - ignored if team is -1</param>
        /// <param name="callback">Optional callback for after the first world created with entity in it</param>
        public EntityAddedMutation(string name, int team, EntityAttributes attributes, Rect2 spawnBounds, Action<Entity> callback)
        {
            Name = name;
            Team = team;
            Attributes = attributes;
            SpawnBounds = spawnBounds;
            Callback = callback;
        }

        public void Apply(MutatingWorld world)
        {
            var spawnBounds = SpawnBounds;
            var team = Team;
            if (team == -1)
            {
                MutatingTeam teamObj = null;
                foreach (var teamkvp in world.Teams)
                {
                    if (teamkvp.Value.LastSpawnTimeMS == 0)
                    {
                        team = teamkvp.Key;
                        break;
                    }
                    else if (teamObj == null || teamkvp.Value.LastSpawnTimeMS < teamObj.LastSpawnTimeMS)
                    {
                        teamObj = teamkvp.Value;
                        team = teamkvp.Key;
                    }
                }
                world.Teams[team].LastSpawnTimeMS = world.Timestamp;
                spawnBounds = world.Teams[team].SpawnRect;
            }

            Vector2 spawnLoc;
            while(true)
            {
                spawnLoc = new Vector2(
                    (float)(spawnBounds.Min.X + world.RandomGen.NextDouble() * spawnBounds.Width),
                    (float)(spawnBounds.Min.Y + world.RandomGen.NextDouble() * spawnBounds.Height));

                var alreadyCollidedIds = new HashSet<int>();
                var done = true;
                while (true)
                {
                    var inter = world.Entities.Select((e) => Tuple.Create(e, Polygon2.IntersectMTV(e.Attributes.Bounds, Attributes.Bounds, e.Location, spawnLoc))).FirstOrDefault((tup) => tup.Item2 != null);
                    if (inter != null)
                    {
                        if(alreadyCollidedIds.Contains(inter.Item1.ID))
                        {
                            break;
                        }

                        alreadyCollidedIds.Add(inter.Item1.ID);
                        spawnLoc += inter.Item2.Item1 * (inter.Item2.Item2 * 1.1f);
                        done = false;
                    }else
                    {
                        break;
                    }
                }

                if (done)
                    break;
            }

            var entId = world.IDCounter++;
            var nearbyBounds = Logic.NEARBY_BOUNDS;
            var nearby = new ConcurrentQueue<Tuple<int, int>>(); 
            Parallel.ForEach(world.Entities, (value, pls, index) =>
            {
                if (Rect2.Intersects(value.Attributes.Bounds.AABB, nearbyBounds, value.Location, spawnLoc, true))
                {
                    nearby.Enqueue(Tuple.Create(value.ID, (int)index));

                }
            });

            foreach(var ele in nearby)
            {
                var id = ele.Item1;
                var ind = ele.Item2;
                var e = world.Entities[ind];
                world.Entities[ind] = new Entity(e, nearby: (Maybe<ImmutableHashSet<int>>)e.NearbyEntityIds.Add(entId));
            }

            var ent = new Entity(entId, team, Name, Attributes, spawnLoc, Vector2.Zero, Attributes.MaxHealth, Attributes.MaxMana, null,
                ImmutableDictionary.Create<int, int>(), ImmutableHashSet.CreateRange(nearby.Select((tup) => tup.Item1)),
                ImmutableList<IModifier>.Empty);
            world.Add(ent);
            world.FinishedCallbacks.Add(() => Callback?.Invoke(ent));
        }
    }
}
