using System.Collections.Generic;

namespace IORPG.Game.Spells
{
    public interface ISpellEffect
    {
        void OnCast(MutatingWorld world, Entity caster, IEnumerable<int> targets);
    }
}