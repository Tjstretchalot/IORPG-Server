using IORPG.Util;
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
        public const int LESSER_HEAL_CAST_TIME = 1500;
        public const int LESSER_HEAL_HEAL = 20;

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

                var indexOfCaster = world.Entities.FindIndex((e) => e.ID == caster.ID);
                if (indexOfCaster < 0)
                    return; // weird state

                world.Entities[indexOfCaster] = new Entity(caster, mana: caster.Mana - LESSER_HEAL_MANA);

                foreach(var target in targets)
                {
                    var index = world.Entities.FindIndex((e) => e.ID == target);

                    if (index < 0)
                        continue;

                    var tarEnt = world.Entities[index];
                    if (!CheckRange(caster, tarEnt, LESSER_HEAL_RANGE))
                        continue;

                    float healing = LESSER_HEAL_HEAL;
                    caster = Logic.InformModifiers(world, caster, indexOfCaster, (mod) => mod.OnHealing(world, index, indexOfCaster, ref healing));
                    tarEnt = Logic.InformModifiers(world, tarEnt, index, (mod) => mod.OnBeingHealed(world, indexOfCaster, index, ref healing));
                    healing = Math.Max(healing, 0);

                    var newHP = Math.Min(tarEnt.Health + healing, tarEnt.Attributes.MaxHealth);
                    world.Entities[index] = new Entity(tarEnt, health: newHP);
                }
            }));
        }

        public static SpellInfo CreateShoot(int targetID)
        {
            return new SpellInfo(SHOOT_INDEX, SHOOT_CAST_TIME, SHOOT_CAST_TIME, new LinqSpellTargeter((world) => new[] { targetID }), new LinqSpellEffect((world, caster, targets) =>
            {
                if (caster.Mana < SHOOT_MANA)
                    return;

                var indexOfCaster = world.Entities.FindIndex((e) => e.ID == caster.ID);
                if (indexOfCaster < 0)
                    return;

                world.Entities[indexOfCaster] = new Entity(caster, mana: caster.Mana - SHOOT_MANA);

                foreach(var target in targets)
                {
                    var index = world.Entities.FindIndex((e) => e.ID == target);
                    if (index < 0)
                        continue;

                    var tarEnt = world.Entities[index];
                    if (!CheckRange(caster, tarEnt, SHOOT_RANGE))
                        continue;

                    float damage = SHOOT_DAMAGE;
                    caster = Logic.InformModifiers(world, caster, indexOfCaster, (mod) => mod.OnDealingDamage(world, index, indexOfCaster, ref damage));
                    tarEnt = Logic.InformModifiers(world, tarEnt, index, (mod) => mod.OnTakingDamage(world, indexOfCaster, index, ref damage));
                    damage = Math.Max(damage, 0);

                    caster = world.Entities[indexOfCaster];
                    tarEnt = world.Entities[index];
                    

                    world.Entities[index] = new Entity(tarEnt, health: tarEnt.Health - damage);
                }
            }));
        }

        public static SpellInfo CreateDeliberateShot(int targetID)
        {
            return new SpellInfo(DELIBERATE_SHOT_INDEX, DELIBERATE_SHOT_CAST_TIME, DELIBERATE_SHOT_CAST_TIME, new LinqSpellTargeter((world) => new[] { targetID }), new LinqSpellEffect((world, caster, targets) =>
            {
                 if (caster.Mana < DELIBERATE_SHOT_MANA)
                     return;

                 var indexOfCaster = world.Entities.FindIndex((e) => e.ID == caster.ID);
                 if (indexOfCaster < 0)
                     return;

                 world.Entities[indexOfCaster] = new Entity(caster, mana: caster.Mana - DELIBERATE_SHOT_MANA);

                 foreach (var target in targets)
                 {
                     var index = world.Entities.FindIndex((e) => e.ID == target);
                     if (index < 0)
                         continue;

                     var tarEnt = world.Entities[index];
                     if (!CheckRange(caster, tarEnt, DELIBERATE_SHOT_RANGE))
                         continue;

                     float damage = DELIBERATE_SHOT_DAMAGE;
                     caster = Logic.InformModifiers(world, caster, indexOfCaster, (mod) => mod.OnDealingDamage(world, index, indexOfCaster, ref damage));
                     tarEnt = Logic.InformModifiers(world, tarEnt, index, (mod) => mod.OnTakingDamage(world, indexOfCaster, index, ref damage));
                     damage = Math.Max(damage, 0);

                     caster = world.Entities[indexOfCaster];
                     tarEnt = world.Entities[index];


                     world.Entities[index] = new Entity(tarEnt, health: tarEnt.Health - damage);
                 }
             }));
        }

        public static SpellInfo CreatePush(int targetID)
        {
            return new SpellInfo(PUSH_INDEX, PUSH_CAST_TIME, PUSH_CAST_TIME, new LinqSpellTargeter((world) => new[] { targetID }), new LinqSpellEffect((world, caster, targets) =>
            {
                if (caster.Mana < PUSH_MANA)
                    return;

                var indexOfCaster = world.Entities.FindIndex((e) => e.ID == caster.ID);
                if (indexOfCaster < 0)
                    return;

                world.Entities[indexOfCaster] = new Entity(caster, mana: caster.Mana - PUSH_MANA);

                foreach(var target in targets)
                {
                    var index = world.Entities.FindIndex((e) => e.ID == target);
                    if (index < 0)
                        continue;

                    var tarEnt = world.Entities[index];
                    
                    // do range check here so we can reuse the vector
                    var minD = Polygon2.MinDistance(tarEnt.Attributes.Bounds, tarEnt.Location, caster.Attributes.Bounds.Center + caster.Location);
                    if (minD.Item2 > PUSH_RANGE)
                        continue;

                    var pushVec = minD.Item1 * (-(PUSH_END_RANGE - minD.Item2));

                    var newLoc = tarEnt.Location + pushVec;
                    world.Entities[index] = Logic.DoMoveEntity(world, tarEnt, newLoc);
                }
            }));
        }

        public static SpellInfo CreateBlock()
        {
            return new SpellInfo(BLOCK_INDEX, BLOCK_CAST_TIME, BLOCK_CAST_TIME, new LinqSpellTargeter((world) => Enumerable.Empty<int>()), new LinqSpellEffect((world, caster, targets) =>
            {
                if (caster.Mana < BLOCK_MANA)
                    return;

                var indexOfCaster = world.Entities.FindIndex((e) => e.ID == caster.ID);
                if (indexOfCaster < 0)
                    return;

                world.Entities[indexOfCaster] = new Entity(caster, mana: caster.Mana - BLOCK_MANA, modifiers: (Maybe<ImmutableList<IModifier>>)caster.Modifiers.Add(ModifierFactory.CreateBlockModifier()));
            }));
        }
    }
}
