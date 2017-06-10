using IORPG.Game.Spells;
using IORPG.Util;
using Microsoft.Xna.Framework;
using SharpMath2;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game
{
    public class Entity
    {
        public readonly int ID;
        public readonly string Name;
        public readonly int Team;
        public readonly EntityAttributes Attributes;
        public readonly Vector2 Location;
        public readonly Vector2 Velocity;
        public readonly float Health;
        public readonly float Mana;
        public readonly SpellInfo Spell;
        public readonly ImmutableDictionary<int, int> SpellCooldowns;
        public readonly ImmutableHashSet<int> NearbyEntityIds;
        public readonly ImmutableList<IModifier> Modifiers;

        public Entity(int id, int team, string name, EntityAttributes attributes, Vector2 location, Vector2 velocity, float health, float mana, SpellInfo spell, ImmutableDictionary<int, int> cooldowns, ImmutableHashSet<int> nearbyIds,
            ImmutableList<IModifier> modifiers)
        {
            ID = id;
            Team = team;
            Name = name;
            Attributes = attributes;
            Location = location;
            Velocity = velocity;
            Health = health;
            Mana = mana;
            Spell = spell;
            SpellCooldowns = cooldowns;
            NearbyEntityIds = nearbyIds;
            Modifiers = modifiers;
        }

        public Entity(Entity copy, int? id = null, int? team = null, Maybe<string> name = default(Maybe<string>), Maybe<EntityAttributes> attrib = default(Maybe<EntityAttributes>),
            Vector2? location = null, Vector2? velocity = null, float? health = null, float? mana = null, 
            Maybe<SpellInfo> spell = default(Maybe<SpellInfo>),
            Maybe<ImmutableDictionary<int, int>> cooldowns = default(Maybe<ImmutableDictionary<int, int>>),
            Maybe<ImmutableHashSet<int>> nearby = default(Maybe<ImmutableHashSet<int>>),
            Maybe<ImmutableList<IModifier>> modifiers = default(Maybe<ImmutableList<IModifier>>)) 
            : this(
                  id.HasValue ? id.Value : copy.ID,
                  team.HasValue ? team.Value : copy.Team,
                  name.HasValue ? name.Value : copy.Name,
                  attrib.HasValue ? attrib.Value : copy.Attributes,
                  location.HasValue ? location.Value : copy.Location,
                  velocity.HasValue ? velocity.Value : copy.Velocity,
                  health.HasValue ? health.Value : copy.Health,
                  mana.HasValue ? mana.Value : copy.Mana,
                  spell.HasValue ? spell.Value : copy.Spell,
                  cooldowns.HasValue ? cooldowns.Value : copy.SpellCooldowns,
                  nearby.HasValue ? nearby.Value : copy.NearbyEntityIds,
                  modifiers.HasValue ? modifiers.Value : copy.Modifiers
                  )
        {

        }
    }
}
