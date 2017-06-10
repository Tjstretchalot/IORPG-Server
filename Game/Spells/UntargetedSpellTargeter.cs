using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game.Spells
{
    public class UntargetedSpellTargeter : ISpellTargeter
    {
        public IEnumerable<int> GetTargets(MutatingWorld world)
        {
            return Enumerable.Empty<int>();
        }
    }
}
