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
        public static Entity DoMoveEntity(MutatingWorld world, Entity entity, Vector2 loc, bool isLargeMovement = false)
        {
            loc.X = Math.Min(Math.Max(loc.X, 0), world.Width - entity.Attributes.Bounds.AABB.Width);
            loc.Y = Math.Min(Math.Max(loc.Y, 0), world.Height - entity.Attributes.Bounds.AABB.Height);
            int entId = entity.ID;
            HashSet<int> ignoreIds = null;
            
            IEnumerable<int> potentialCollisionIndexes;
            if(isLargeMovement)
            {
                var potColIds = new ConcurrentQueue<int>();
                Parallel.ForEach(world.Entities, (value, pls, index) =>
                {
                    if (value.ID != entId && Rect2.Intersects(value.Attributes.Bounds.AABB, NEARBY_BOUNDS, value.Location, loc, true))
                    {
                        potColIds.Enqueue((int)index);
                    }
                });
                potentialCollisionIndexes = potColIds;
            }else
            {
                potentialCollisionIndexes = entity.NearbyEntityIds.Select((id) => world.Entities.FindIndex((e) => e.ID == id));
            }

            while (true)
            {
                int collidingId = -1;
                Tuple<Vector2, float> collidingMTV = null;

                foreach(var potentialCollidingEntityIndexInEntities in potentialCollisionIndexes)
                {
                    var potentialCollidingEntity = world.Entities[potentialCollidingEntityIndexInEntities];
                    if (potentialCollidingEntity.ID != entId && (ignoreIds == null || !ignoreIds.Contains(potentialCollidingEntity.ID)))
                    {
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

            var nearbyIds = new ConcurrentQueue<Tuple<int, int>>();
            Parallel.ForEach(world.Entities, (value, pls, index) =>
            {
                if (value.ID != entId && Rect2.Intersects(value.Attributes.Bounds.AABB, NEARBY_BOUNDS, value.Location, loc, true))
                {
                    nearbyIds.Enqueue(Tuple.Create(value.ID, (int)index));
                }
            });
            
            foreach(var nearby in nearbyIds)
            {
                var ent = world.Entities[nearby.Item2];

                if(!ent.NearbyEntityIds.Contains(entId))
                {
                    world.Entities[nearby.Item2] = new Entity(ent, nearby: (Maybe<ImmutableHashSet<int>>)ent.NearbyEntityIds.Add(entId));
                }
            }

            return new Entity(entity, location: loc, nearby: (Maybe<ImmutableHashSet<int>>)ImmutableHashSet.CreateRange(nearbyIds.Select((tup) => tup.Item1)));
        }

        public static Entity InformModifiers(MutatingWorld world, Entity entity, int entIndex, Func<IModifier, IModifier> modFunc)
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

                world.Entities[entIndex] = new Entity(world.Entities[entIndex], modifiers: (Maybe<ImmutableList<IModifier>>)ImmutableList.CreateRange(newModifiers));
            }

            return world.Entities[entIndex];
        }

        public static Entity SimulateTimePassing(MutatingWorld world, Entity entity, int entIndex, int timeMS)
        {
            entity = InformModifiers(world, entity, entIndex, (mod) => mod.OnTicking(entIndex, timeMS));

            if(entity.Mana != entity.Attributes.MaxMana && entity.Attributes.ManaRegen > 0)
            {
                var newMana = (float)(entity.Mana + (double)entity.Attributes.ManaRegen * timeMS); // up precision before addition because numbers are way different
                entity = new Entity(entity, mana: Math.Min(entity.Attributes.MaxMana, newMana));
                world.Entities[entIndex] = entity;
            }
            if (entity.Spell != null)
            {
                var timeRemaining = entity.Spell.CastTimeRemainingMS - timeMS;

                if(timeRemaining <= 0)
                {
                    entity.Spell.SpellEffect.OnCast(world, entity, entity.Spell.SpellTargeter.GetTargets(world));
                    entity = world.Entities[entIndex]; // in case the entity was mutated
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

            return DoMoveEntity(world, entity, loc);
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

            for(int i = 0; i < mutating.Entities.Count; i++)
            {
                mutating.Entities[i] = SimulateTimePassing(mutating, mutating.Entities[i], i, timeMS);
            }

            for(int i = mutating.Entities.Count - 1; i >= 0; i--)
            {
                if(mutating.Entities[i].Health <= 0)
                {
                    mutating.RemoveByIndex(i);
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
