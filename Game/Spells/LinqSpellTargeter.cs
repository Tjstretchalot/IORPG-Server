using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game.Spells
{
    public class LinqSpellTargeter : ISpellTargeter
    {
        private readonly Func<MutatingWorld, IEnumerable<int>> Delegate;

        public LinqSpellTargeter(Func<MutatingWorld, IEnumerable<int>> func)
        {
            Delegate = func;
        }

        public IEnumerable<int> GetTargets(MutatingWorld world)
        {
            return Delegate(world);
        }
    }
}
