using IORPG.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game.Mutations
{
    public class EntityRemovedMutation : IWorldMutation
    {
        public WorldMutationTime Time => WorldMutationTime.BeforeEntitiesTick;
        private int ID;

        public EntityRemovedMutation(int id)
        {
            ID = id;
        }

        public void Apply(MutatingWorld world)
        {
            var ind = world.Entities.FindIndex((e) => e.ID == ID);
            if (ind >= 0)
                world.RemoveByIndex(ind);
        }
    }
}
