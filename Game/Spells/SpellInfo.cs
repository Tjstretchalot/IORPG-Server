using System.Collections.Immutable;

namespace IORPG.Game.Spells
{
    public class SpellInfo
    {
        public readonly int SpellCooldownID;
        public readonly int TotalCastTimeMS;
        public readonly int CastTimeRemainingMS;
        public readonly ISpellTargeter SpellTargeter;
        public readonly ISpellEffect SpellEffect;

        private SpellInfo() { SpellCooldownID = -1; }

        public SpellInfo(int spellCooldownID, int totalCastTime, int castTimeRemaining, ISpellTargeter targeter, ISpellEffect effect)
        {
            SpellCooldownID = spellCooldownID;
            TotalCastTimeMS = totalCastTime;
            CastTimeRemainingMS = castTimeRemaining;
            SpellTargeter = targeter;
            SpellEffect = effect;
        }
    }
}