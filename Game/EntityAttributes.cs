using SharpMath2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game
{
    /// <summary>
    /// Describe things about an entity that rarely change between units of the same type
    /// </summary>
    public class EntityAttributes
    {
        public readonly UnitType UnitType;
        public readonly Polygon2 Bounds;
        public readonly float Speed;
        public readonly int MaxHealth;
        public readonly int MaxMana;
        public readonly float ManaRegen;

        public EntityAttributes(UnitType unitType, Polygon2 bounds, float speed, int maxHealth, int maxMana, float manaRegen)
        {
            UnitType = unitType;
            Bounds = bounds;
            Speed = speed;
            MaxHealth = maxHealth;
            MaxMana = maxMana;
            ManaRegen = manaRegen;
        }
    }
}
