using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game
{
    public class ModifierFactory
    {
        public const int BLOCK_DURATION = 2000;
        public const float BLOCK_MULTIPLIER = 0.20f;

        public static IModifier CreateBlockModifier(int durRemaining = BLOCK_DURATION)
        {
            if (durRemaining <= 0)
                return null;

            return new LinqModifier(false, BLOCK_DURATION, durRemaining,
                OnTicking: (me, parInd, deltaMS) => CreateBlockModifier(me.DurationRemainingMS - deltaMS),
                OnTakingDamage: (IModifier me, MutatingWorld world, int attackerInd, int parInd, ref float dmg) => { dmg *= BLOCK_MULTIPLIER; return null; },
                Serialize: () => new Dictionary<string, object> { { "blocked", true }, { "duration", (int)(((float) (BLOCK_DURATION - durRemaining) / BLOCK_DURATION) * 100) } });
        }
    }
}
