using IORPG.Util;
using Microsoft.Xna.Framework;
using SharpMath2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
            var watch = new Stopwatch();
            watch.Start();
            var spawnBounds = SpawnBounds;
            var team = Team;
            if (team == -1)
            {
                MutatingTeam teamObj = null;
                foreach (var teamkvp in world.Teams)
                {
                    if (teamObj == null || teamkvp.Value.Members.Count < teamObj.Members.Count)
                    {
                        team = teamkvp.Key;
                        teamObj = teamkvp.Value;
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
                    var inter = world.Entities.Select((e) => Tuple.Create(e, Polygon2.IntersectMTV(e.Value.Attributes.Bounds, Attributes.Bounds, e.Value.Location, spawnLoc))).FirstOrDefault((tup) => tup.Item2 != null);
                    if (inter != null)
                    {
                        if(alreadyCollidedIds.Contains(inter.Item1.Key))
                        {
                            break;
                        }

                        alreadyCollidedIds.Add(inter.Item1.Key);
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
            var nearby = new HashSet<int>(); 
            foreach(var kvp in world.Entities)
            {
                if (Rect2.Intersects(kvp.Value.Attributes.Bounds.AABB, nearbyBounds, kvp.Value.Location, spawnLoc, true))
                {
                    nearby.Add(kvp.Key);
                }
            }

            foreach(var id in nearby)
            {
                var e = world.Entities[id];
                world.Entities[id] = new Entity(e, nearby: (Maybe<ImmutableHashSet<int>>)e.NearbyEntityIds.Add(entId));
            }

            var ent = new Entity(entId, team, Name, Attributes, spawnLoc, Vector2.Zero, Attributes.MaxHealth, Attributes.MaxMana, null,
                ImmutableDictionary.Create<int, int>(), nearby.ToImmutableHashSet(), ImmutableList<IModifier>.Empty);
            world.Add(ent);
            world.FinishedCallbacks.Add(() => Callback?.Invoke(ent));

            watch.Stop();
            var elapsedMS = watch.ElapsedMilliseconds;
            if(elapsedMS > 2)
            {
                Console.WriteLine($"Took a long time to spawn new entity! ({elapsedMS} ms)");
            }
        }
    }
}
