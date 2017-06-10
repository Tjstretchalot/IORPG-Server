using System.Collections.Generic;

namespace IORPG.Game.Spells
{
    public interface ISpellTargeter
    {
        IEnumerable<int> GetTargets(MutatingWorld world);
    }
}