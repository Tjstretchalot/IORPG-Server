using IORPG.Game.Spells;
using IORPG.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game.Mutations
{
    public class EntityCastSpellMutation : IWorldMutation
    {
        public WorldMutationTime Time => WorldMutationTime.BeforeEntitiesTick;

        public readonly int EntityID;
        public readonly SpellInfo Spell;

        public EntityCastSpellMutation(int id, SpellInfo spell)
        {
            EntityID = id;
            Spell = spell;
        }

        public void Apply(MutatingWorld world)
        {
            var entityIndex = world.Entities.FindIndex((e) => e.ID == EntityID);
            if (entityIndex < 0)
                return;

            var entity = world.Entities[entityIndex];
            world.Entities[entityIndex] = new Entity(entity, spell: (Maybe<SpellInfo>) Spell);
        }
    }
}
