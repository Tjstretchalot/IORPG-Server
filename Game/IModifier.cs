using System.Collections.Generic;

namespace IORPG.Game
{
    public interface IModifier
    {
        // The only things these functions cannot do is remove or add entities directly, as that makes
        // it very difficult to iterate over the modifiers of an entity efficiently. Similiarly, they
        // cannot add/remove modifiers directly.

        // Modifier implementations must be immutable.
        
        // Adding/removing entities/modifiers should be done by adding to the FinishedCallbacks of the mutating world

        bool IsPermanent { get; }
        int DurationRemainingMS { get; }
        int DurationTotalMS { get; }

        IModifier OnTicking(int parentIndex, int deltaMS);
        IModifier OnTakingDamage(MutatingWorld world, int attackerIndex, int parentIndex, ref float amount);
        IModifier OnDealingDamage(MutatingWorld world, int attackedIndex, int parentIndex, ref float amount);
        IModifier OnBeingHealed(MutatingWorld world, int healerIndex, int parentIndex, ref float amount);
        IModifier OnHealing(MutatingWorld world, int healedIndex, int parentIndex, ref float amount);

        Dictionary<string, object> Serialize();
    }
}