using IORPG.Game.Mutations;
using IORPG.Game.Spells;
using IORPG.Util;
using Microsoft.Xna.Framework;
using SharpMath2;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IORPG.Game
{
    public class Logic
    {
        public static readonly Rect2 NEARBY_BOUNDS = new Rect2(-1920 / 2, -1080 / 2, 1920 / 2, 1080 / 2);

        /// <summary>
        /// Moves the entity to the specified location with collision.
        /// </summary>
        /// <remarks>
        /// This always assumes that after moving entities by their MTVs they won't escape the NEARBY_BOUNDS, but 
        /// if isLargeMovement is true it recalculates EntityNearbyIds from loc rather than reusing the existing
        /// one.
        /// </remarks>
        /// <param name="world">The world</param>
        /// <param name="entity">The entity to move</param>
        /// <param name="loc">The desired location of the entity</param>
        /// <param name="isLargeMovement">If the entity might intersect entities that aren't in its EntityNearbyIds</param>
        /// <returns>The entity after attempting to move it to loc</returns>
        public static Entity DoMoveEntity(MutatingWorld world, Entity entity, Vector2 loc, Stopwatch watch, bool isLargeMovement = false)
        {
            loc.X = Math.Min(Math.Max(loc.X, 0), world.Width - entity.Attributes.Bounds.AABB.Width);
            loc.Y = Math.Min(Math.Max(loc.Y, 0), world.Height - entity.Attributes.Bounds.AABB.Height);
            int entId = entity.ID;
            HashSet<int> ignoreIds = null;
            
            IEnumerable<int> potentialCollisionIds;
            if(isLargeMovement)
            {
                var potColIds = new ConcurrentQueue<int>();
                Parallel.ForEach(world.Entities, (value, pls, index) =>
                {
                    if (value.Key != entId && Rect2.Intersects(value.Value.Attributes.Bounds.AABB, NEARBY_BOUNDS, value.Value.Location, loc, true))
                    {
                        potColIds.Enqueue(value.Key);
                    }
                });
                potentialCollisionIds = potColIds;
            }else
            {
                potentialCollisionIds = entity.NearbyEntityIds;
            }

            watch.Start();
            while (true)
            {
                int collidingId = -1;
                Tuple<Vector2, float> collidingMTV = null;

                foreach(var potentialCollidingEntityID in potentialCollisionIds)
                {
                    var potentialCollidingEntity = world.Entities[potentialCollidingEntityID];
                    if (potentialCollidingEntity.ID != entId && (ignoreIds == null || !ignoreIds.Contains(potentialCollidingEntity.ID)))
                    {
                        if (!Rect2.Intersects(entity.Attributes.Bounds.AABB, potentialCollidingEntity.Attributes.Bounds.AABB, entity.Location, potentialCollidingEntity.Location, true))
                            continue;

                        var mtv = Polygon2.IntersectMTV(entity.Attributes.Bounds, potentialCollidingEntity.Attributes.Bounds, loc, potentialCollidingEntity.Location);

                        if (mtv != null)
                        {
                            collidingId = potentialCollidingEntity.ID;
                            collidingMTV = mtv;
                            break;
                        }
                    }
                };


                if (collidingMTV != null)
                {
                    if (ignoreIds == null)
                        ignoreIds = new HashSet<int>(new[] { collidingId });
                    else
                        ignoreIds.Add(collidingId);

                    loc += collidingMTV.Item1 * collidingMTV.Item2;
                }
                else
                {
                    break;
                }
            }
            watch.Stop();
            var elapsed = watch.ElapsedMilliseconds;
            if(elapsed > 2)
            {
                Console.WriteLine($"Took a long time to resolve collisions: {elapsed} ms");
            }

            watch.Start();
            var nearbyIds = new HashSet<int>();
            var keys = new List<int>(world.Entities.Keys);
            foreach(var id in keys)
            {
                var e = world.Entities[id];
                if(e.ID != entId && Rect2.Intersects(e.Attributes.Bounds.AABB, NEARBY_BOUNDS, e.Location, loc, true))
                {
                    nearbyIds.Add(e.ID);
                }
            }

            foreach(var oldNearby in entity.NearbyEntityIds)
            {
                if(!nearbyIds.Contains(oldNearby))
                {
                    Entity oldEnt;
                    if(world.Entities.TryGetValue(oldNearby, out oldEnt))
                    {
                        world.Entities[oldNearby] = new Entity(oldEnt, nearby: (Maybe<ImmutableHashSet<int>>)oldEnt.NearbyEntityIds.Remove(entId));
                    }
                }
            }
            
            foreach(var nearby in nearbyIds)
            {
                var ent = world.Entities[nearby];

                if(!ent.NearbyEntityIds.Contains(entId))
                {
                    world.Entities[nearby] = new Entity(ent, nearby: (Maybe<ImmutableHashSet<int>>)ent.NearbyEntityIds.Add(entId));
                }
            }
            watch.Stop();
            elapsed = watch.ElapsedMilliseconds;
            if(elapsed > 3)
            {
                Console.WriteLine($"Took a long time to update nearby: {elapsed} ms");
            }
            return new Entity(entity, location: loc, nearby: (Maybe<ImmutableHashSet<int>>)nearbyIds.ToImmutableHashSet());
        }

        public static Entity InformModifiers(MutatingWorld world, Entity entity, Func<IModifier, IModifier> modFunc)
        {
            if (entity.Modifiers.Count > 0)
            {
                List<IModifier> newModifiers = new List<IModifier>(entity.Modifiers.Count);
                for (int i = 0; i < entity.Modifiers.Count; i++)
                {
                    var mod = entity.Modifiers[i];
                    mod = modFunc(mod);
                    if (mod != null)
                    {
                        newModifiers.Add(mod);
                    }
                }

                world.Entities[entity.ID] = new Entity(world.Entities[entity.ID], modifiers: (Maybe<ImmutableList<IModifier>>)ImmutableList.CreateRange(newModifiers));
            }

            return world.Entities[entity.ID];
        }

        public static Entity SimulateTimePassing(MutatingWorld world, Entity entity, int timeMS, Stopwatch watch)
        {
            entity = InformModifiers(world, entity, (mod) => mod.OnTicking(entity.ID, timeMS));

            if(entity.Mana != entity.Attributes.MaxMana && entity.Attributes.ManaRegen > 0)
            {
                var newMana = (float)(entity.Mana + (double)entity.Attributes.ManaRegen * timeMS); // up precision before addition because numbers are way different
                entity = new Entity(entity, mana: Math.Min(entity.Attributes.MaxMana, newMana));
                world.Entities[entity.ID] = entity;
            }
            if (entity.Spell != null)
            {
                var timeRemaining = entity.Spell.CastTimeRemainingMS - timeMS;

                if(timeRemaining <= 0)
                {
                    entity.Spell.SpellEffect.OnCast(world, entity, entity.Spell.SpellTargeter.GetTargets(world));
                    entity = world.Entities[entity.ID]; // in case the entity was mutated
                    entity = new Entity(entity, spell: new Maybe<SpellInfo>(null)); 
                }else
                {
                    var spell = new SpellInfo(entity.Spell.SpellCooldownID, entity.Spell.TotalCastTimeMS, timeRemaining, entity.Spell.SpellTargeter, entity.Spell.SpellEffect);
                    entity = new Entity(entity, spell: (Maybe<SpellInfo>)spell);
                }
            }

            if (entity.Velocity == Vector2.Zero)
                return entity;

            var loc = entity.Location + entity.Velocity * timeMS;

            return DoMoveEntity(world, entity, loc, watch);
        }

        public static MutatingWorld SimulateTimePassing(World world, Random random, ConcurrentDictionary<WorldMutationTime, ConcurrentQueue<IWorldMutation>> mutations, int timeMS)
        {
            var mutating = new MutatingWorld(world.Width, world.Height, world.Entities.Count, world.Timestamp + timeMS, world.IDCounter, random);
            foreach (var team in world.Teams)
            {
                mutating.Teams[team.Key] = new MutatingTeam(team.Key, new HashSet<int>(), team.Value.SpawnRect, team.Value.LastSpawnTime);
            }
            mutating.AddRange(world.Entities);

            ConcurrentQueue<IWorldMutation> beforeTick;
            if (mutations.TryGetValue(WorldMutationTime.BeforeEntitiesTick, out beforeTick))
            {
                while(beforeTick.Count > 0)
                {
                    IWorldMutation mut;
                    if (beforeTick.TryDequeue(out mut))
                    {
                        mut.Apply(mutating);
                    }else
                    {
                        break;
                    }
                }
            }
            
            var watch = new Stopwatch();
            var keys = new List<int>(mutating.Entities.Keys);
            foreach (var id in keys)
            {
                mutating.Entities[id] = SimulateTimePassing(mutating, mutating.Entities[id], timeMS, watch);
                watch.Reset();
            }

            foreach(int id in keys)
            {
                if(mutating.Entities[id].Health <= 0)
                {
                    mutating.RemoveByID(id);
                }
            }

            ConcurrentQueue<IWorldMutation> afterTick;
            if (mutations.TryGetValue(WorldMutationTime.AfterEntitiesTick, out afterTick))
            {
                while(afterTick.Count > 0)
                {
                    IWorldMutation mut;
                    if (afterTick.TryDequeue(out mut))
                    {
                        mut.Apply(mutating);
                    }else
                    {
                        break;
                    }
                }
            }

            return mutating;
        }
    }
}
