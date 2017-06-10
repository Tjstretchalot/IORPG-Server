using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game.Spells
{
    public class LinqSpellEffect : ISpellEffect
    {
        private readonly Action<MutatingWorld, Entity, IEnumerable<int>> Delegate;

        public LinqSpellEffect(Action<MutatingWorld, Entity, IEnumerable<int>> func)
        {
            Delegate = func;
        }

        public void OnCast(MutatingWorld world, Entity caster, IEnumerable<int> targets)
        {
            Delegate(world, caster, targets);
        }
    }
}
