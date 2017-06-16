using IORPG.Util;
using Microsoft.Xna.Framework;
using SharpMath2;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game.Spells
{
    public class SpellFactory
    {
        public const int LESSER_HEAL_INDEX = 0; // note that this isn't actually unique - it's unique *per unit* (i.e. no unit has two spells of the same index)
        public const int LESSER_HEAL_MANA = 20;
        public const int LESSER_HEAL_RANGE = 300;
        public const int LESSER_HEAL_CAST_TIME = 1000;
        public const int LESSER_HEAL_HEAL = 10;

        public const int GREATER_HEAL_INDEX = 1;
        public const int GREATER_HEAL_MANA = 5;
        public const int GREATER_HEAL_RANGE = 400;
        public const int GREATER_HEAL_CAST_TIME = 3000;
        public const int GREATER_HEAL_HEAL = 30;

        public const int HEALING_STRIKE_INDEX = 2;
        public const int HEALING_STRIKE_MANA = 50;
        public const int HEALING_STRIKE_RANGE = 300;
        public const int HEALING_STRIKE_RADIUS = 150;
        public const int HEALING_STRIKE_CAST_TIME = 2000;
        public const int HEALING_STRIKE_HEAL = 20;

        public const int SHOOT_INDEX = 0;
        public const int SHOOT_MANA = 10;
        public const int SHOOT_RANGE = 300;
        public const int SHOOT_CAST_TIME = 800;
        public const int SHOOT_DAMAGE = 8;

        public const int DELIBERATE_SHOT_INDEX = 1;
        public const int DELIBERATE_SHOT_MANA = 3;
        public const int DELIBERATE_SHOT_RANGE = 400;
        public const int DELIBERATE_SHOT_CAST_TIME = 3000;
        public const int DELIBERATE_SHOT_DAMAGE = 40;

        public const int PUSH_INDEX = 0;
        public const int PUSH_MANA = 10;
        public const int PUSH_RANGE = 200;
        public const int PUSH_END_RANGE = 400;
        public const int PUSH_CAST_TIME = 800;

        public const int BLOCK_INDEX = 1;
        public const int BLOCK_MANA = 20;
        public const int BLOCK_CAST_TIME = 500;

        public static bool CheckRange(Entity caster, Entity target, int range)
        {
            /*
            This is more accurate but harder for the client to render
            var minD = Polygon2.MinDistance(caster.Attributes.Bounds, target.Attributes.Bounds, caster.Location, target.Location);
            return minD == null || minD.Item2 <= range;
            */

            var minD = Polygon2.MinDistance(target.Attributes.Bounds, target.Location, caster.Attributes.Bounds.Center + caster.Location);
            return minD == null || minD.Item2 <= range;
        }

        public static SpellInfo CreateLesserHeal(int targetID)
        {
            return new SpellInfo(LESSER_HEAL_INDEX, LESSER_HEAL_CAST_TIME, LESSER_HEAL_CAST_TIME, new LinqSpellTargeter((world) => new[] { targetID }), new LinqSpellEffect((world, caster, targets) =>
            {
                if (caster.Mana < LESSER_HEAL_MANA)
                    return; // whiffs
                
                if (!world.Entities.ContainsKey(caster.ID))
                    return; // weird state

                world.Entities[caster.ID] = new Entity(caster, mana: caster.Mana - LESSER_HEAL_MANA);

                foreach(var target in targets)
                {
                    Entity tarEnt;
                    if(!world.Entities.TryGetValue(target, out tarEnt))
                        continue;
                    
                    if (!CheckRange(caster, tarEnt, LESSER_HEAL_RANGE))
                        continue;

                    float healing = LESSER_HEAL_HEAL;
                    caster = Logic.InformModifiers(world, caster, (mod) => mod.OnHealing(world, target, caster.ID, ref healing));
                    tarEnt = Logic.InformModifiers(world, tarEnt, (mod) => mod.OnBeingHealed(world, caster.ID, target, ref healing));
                    healing = Math.Max(healing, 0);

                    var newHP = Math.Min(tarEnt.Health + healing, tarEnt.Attributes.MaxHealth);
                    world.Entities[target] = new Entity(tarEnt, health: newHP);
                }
            }));
        }

        public static SpellInfo CreateGreaterHeal(int targetID)
        {
            return new SpellInfo(GREATER_HEAL_INDEX, GREATER_HEAL_CAST_TIME, GREATER_HEAL_CAST_TIME, new LinqSpellTargeter((world) => new[] { targetID }), new LinqSpellEffect((world, caster, targets) =>
           {
               if (caster.Mana < GREATER_HEAL_MANA)
                   return;

               if (!world.Entities.ContainsKey(caster.ID))
                   return; // weird state

               world.Entities[caster.ID] = new Entity(caster, mana: caster.Mana - GREATER_HEAL_MANA);

               foreach(var target in targets)
               {
                   Entity tarEnt;
                   if (!world.Entities.TryGetValue(target, out tarEnt))
                       continue;

                   if (!CheckRange(caster, tarEnt, GREATER_HEAL_RANGE))
                       continue;

                   float healing = GREATER_HEAL_HEAL;
                   caster = Logic.InformModifiers(world, caster, (mod) => mod.OnHealing(world, target, caster.ID, ref healing));
                   tarEnt = Logic.InformModifiers(world, tarEnt, (mod) => mod.OnBeingHealed(world, caster.ID, target, ref healing));
                   healing = Math.Max(healing, 0);

                   var newHP = Math.Min(tarEnt.Health + healing, tarEnt.Attributes.MaxHealth);
                   world.Entities[target] = new Entity(tarEnt, health: newHP);
               }
           }));
        }

        public static SpellInfo CreateHealingStrike(Vector2 targetWorld)
        {
            return new SpellInfo(HEALING_STRIKE_INDEX, HEALING_STRIKE_CAST_TIME, HEALING_STRIKE_CAST_TIME, new LinqSpellTargeter((world) =>
            {
                return world.Entities.Where((e) =>
                {
                    var mind = Polygon2.MinDistance(e.Value.Attributes.Bounds, e.Value.Location, targetWorld);
                    return mind == null || mind.Item2 < HEALING_STRIKE_RADIUS;
                }).Select((kvp) => kvp.Key).ToArray();
            }), new LinqSpellEffect((world, caster, targets) =>
            {
                if (!world.Entities.ContainsKey(caster.ID))
                    return; // weird state

                world.Entities[caster.ID] = new Entity(caster, mana: caster.Mana - HEALING_STRIKE_MANA);

                foreach (var target in targets)
                {
                    Entity tarEnt;
                    if (!world.Entities.TryGetValue(target, out tarEnt))
                        continue;
                    if (tarEnt.ID == caster.ID)
                        continue;
                    if (tarEnt.Team != caster.Team)
                        continue;

                    float healing = HEALING_STRIKE_HEAL;
                    caster = Logic.InformModifiers(world, caster, (mod) => mod.OnHealing(world, target, caster.ID, ref healing));
                    tarEnt = Logic.InformModifiers(world, tarEnt, (mod) => mod.OnBeingHealed(world, caster.ID, target, ref healing));
                    healing = Math.Max(healing, 0);

                    var newHP = Math.Min(tarEnt.Health + healing, tarEnt.Attributes.MaxHealth);
                    world.Entities[target] = new Entity(tarEnt, health: newHP);
                }

            }));
        }

        public static SpellInfo CreateShoot(int targetID)
        {
            return new SpellInfo(SHOOT_INDEX, SHOOT_CAST_TIME, SHOOT_CAST_TIME, new LinqSpellTargeter((world) => new[] { targetID }), new LinqSpellEffect((world, caster, targets) =>
            {
                if (caster.Mana < SHOOT_MANA)
                    return;

                if (!world.Entities.ContainsKey(caster.ID))
                    return; // weird state

                world.Entities[caster.ID] = new Entity(caster, mana: caster.Mana - SHOOT_MANA);

                foreach(var target in targets)
                {
                    Entity tarEnt;
                    if (!world.Entities.TryGetValue(target, out tarEnt))
                        continue;
                    if (!CheckRange(caster, tarEnt, SHOOT_RANGE))
                        continue;

                    float damage = SHOOT_DAMAGE;
                    caster = Logic.InformModifiers(world, caster, (mod) => mod.OnDealingDamage(world, target, caster.ID, ref damage));
                    tarEnt = Logic.InformModifiers(world, tarEnt, (mod) => mod.OnTakingDamage(world, caster.ID, target, ref damage));
                    damage = Math.Max(damage, 0);
                    

                    world.Entities[target] = new Entity(tarEnt, health: tarEnt.Health - damage);
                }
            }));
        }

        public static SpellInfo CreateDeliberateShot(int targetID)
        {
            return new SpellInfo(DELIBERATE_SHOT_INDEX, DELIBERATE_SHOT_CAST_TIME, DELIBERATE_SHOT_CAST_TIME, new LinqSpellTargeter((world) => new[] { targetID }), new LinqSpellEffect((world, caster, targets) =>
            {
                 if (caster.Mana < DELIBERATE_SHOT_MANA)
                     return;

                if (!world.Entities.ContainsKey(caster.ID))
                    return; // weird state

                world.Entities[caster.ID] = new Entity(caster, mana: caster.Mana - DELIBERATE_SHOT_MANA);

                 foreach (var target in targets)
                {
                    Entity tarEnt;
                    if (!world.Entities.TryGetValue(target, out tarEnt))
                        continue;
                    if (!CheckRange(caster, tarEnt, DELIBERATE_SHOT_RANGE))
                         continue;

                     float damage = DELIBERATE_SHOT_DAMAGE;
                     caster = Logic.InformModifiers(world, caster, (mod) => mod.OnDealingDamage(world, target, caster.ID, ref damage));
                     tarEnt = Logic.InformModifiers(world, tarEnt, (mod) => mod.OnTakingDamage(world, caster.ID, target, ref damage));
                     damage = Math.Max(damage, 0);


                     world.Entities[target] = new Entity(tarEnt, health: tarEnt.Health - damage);
                 }
             }));
        }

        public static SpellInfo CreatePush(int targetID)
        {
            return new SpellInfo(PUSH_INDEX, PUSH_CAST_TIME, PUSH_CAST_TIME, new LinqSpellTargeter((world) => new[] { targetID }), new LinqSpellEffect((world, caster, targets) =>
            {
                if (caster.Mana < PUSH_MANA)
                    return;

                if (!world.Entities.ContainsKey(caster.ID))
                    return; // weird state

                world.Entities[caster.ID] = new Entity(caster, mana: caster.Mana - PUSH_MANA);

                foreach(var target in targets)
                {
                    Entity tarEnt;
                    if (!world.Entities.TryGetValue(target, out tarEnt))
                        continue;

                    // do range check here so we can reuse the vector
                    var minD = Polygon2.MinDistance(tarEnt.Attributes.Bounds, tarEnt.Location, caster.Attributes.Bounds.Center + caster.Location);
                    if (minD.Item2 > PUSH_RANGE)
                        continue;

                    var pushVec = minD.Item1 * (-(PUSH_END_RANGE - minD.Item2));
                    
                    var newLoc = tarEnt.Location + pushVec;
                    world.Entities[target] = Logic.DoMoveEntity(world, tarEnt, newLoc, new System.Diagnostics.Stopwatch());
                }
            }));
        }

        public static SpellInfo CreateBlock()
        {
            return new SpellInfo(BLOCK_INDEX, BLOCK_CAST_TIME, BLOCK_CAST_TIME, new LinqSpellTargeter((world) => Enumerable.Empty<int>()), new LinqSpellEffect((world, caster, targets) =>
            {
                if (caster.Mana < BLOCK_MANA)
                    return;

                if (!world.Entities.ContainsKey(caster.ID))
                    return; // weird state

                world.Entities[caster.ID] = new Entity(caster, mana: caster.Mana - BLOCK_MANA, modifiers: (Maybe<ImmutableList<IModifier>>)caster.Modifiers.Add(ModifierFactory.CreateBlockModifier()));
            }));
        }
    }
}
