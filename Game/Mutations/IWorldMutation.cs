using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game.Mutations
{
    public interface IWorldMutation
    {
        WorldMutationTime Time { get; }

        void Apply(MutatingWorld world);
    }
}
