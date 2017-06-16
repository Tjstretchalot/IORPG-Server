using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game
{
    public delegate IModifier ModifierOnThingHappened(IModifier me, MutatingWorld world, int otherEntID, int parentID, ref float amount);

    public class LinqModifier : IModifier
    {
        public bool IsPermanent { get; private set; }
        public int DurationRemainingMS { get; private set; }
        public int DurationTotalMS { get; private set; }

        private readonly Func<IModifier, int, int, IModifier> OnTickingDelegate;
        private readonly ModifierOnThingHappened OnTakingDamageDelegate;
        private readonly ModifierOnThingHappened OnDealingDamageDelegate;
        private readonly ModifierOnThingHappened OnBeingHealedDelegate;
        private readonly ModifierOnThingHappened OnHealingDelegate;
        private readonly Func<Dictionary<string, object>> SerializeDelegate;

        public LinqModifier(bool permanent, int durationTotal, int durationRemaining,
            Func<IModifier, int, int, IModifier> OnTicking = null, 
            ModifierOnThingHappened OnTakingDamage = null, 
            ModifierOnThingHappened OnDealingDamage = null,
            ModifierOnThingHappened OnBeingHealed = null,
            ModifierOnThingHappened OnHealing = null,
            Func<Dictionary<string, object>> Serialize = null
            )
        {
            IsPermanent = permanent;
            DurationRemainingMS = durationRemaining;
            DurationTotalMS = durationTotal;

            OnTickingDelegate = OnTicking;
            OnTakingDamageDelegate = OnTakingDamage;
            OnDealingDamageDelegate = OnDealingDamage;
            OnBeingHealedDelegate = OnBeingHealed;
            OnHealingDelegate = OnHealing;
            SerializeDelegate = Serialize;
        }

        public IModifier OnTicking(int parentID, int deltaMS)
        {
            return OnTickingDelegate == null ? this : OnTickingDelegate(this, parentID, deltaMS);
        }

        public IModifier OnTakingDamage(MutatingWorld world, int attackerID, int parentID, ref float amount)
        {
            return OnTakingDamageDelegate == null ? this : OnTakingDamageDelegate(this, world, attackerID, parentID, ref amount);
        }

        public IModifier OnDealingDamage(MutatingWorld world, int attackedID, int parentID, ref float amount)
        {
            return OnDealingDamageDelegate == null ? this : OnDealingDamageDelegate(this, world, attackedID, parentID, ref amount);
        }

        public IModifier OnBeingHealed(MutatingWorld world, int healerID, int parentID, ref float amount)
        {
            return OnBeingHealedDelegate == null ? this : OnBeingHealedDelegate(this, world, healerID, parentID, ref amount);
        }

        public IModifier OnHealing(MutatingWorld world, int healedID, int parentID, ref float amount)
        {
            return OnHealingDelegate == null ? this : OnHealingDelegate(this, world, healedID, parentID, ref amount);
        }

        public Dictionary<string, object> Serialize()
        {
            return SerializeDelegate == null ? null : SerializeDelegate();
        }

    }
}
