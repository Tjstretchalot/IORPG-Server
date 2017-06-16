using IORPG.Game;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using SharpMath2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Networking
{
    public class Serializer
    {
        static Dictionary<string, object> EntityToDict(Entity e)
        {
            var result = new Dictionary<string, object>();
            result.Add("id", e.ID);
            result.Add("hero", e.Attributes.UnitType);
            result.Add("name", e.Name);
            result.Add("team", e.Team);

            var translate = new Dictionary<string, object>();
            translate.Add("x", (int)e.Location.X);
            translate.Add("y", (int)e.Location.Y);
            result.Add("translate", translate);

            if (e.Modifiers.Count > 0) {
                var mods = new List<object>(e.Modifiers.Count);
                foreach (var mod in e.Modifiers)
                {
                    var ser = mod.Serialize();
                    if (ser != null)
                        mods.Add(ser);
                }
                if(mods.Count > 0)
                {
                    result.Add("modifiers", mods);
                }
            }

            result.Add("health", (int)((e.Health * 100.0f) / e.Attributes.MaxHealth));
            if (e.Attributes.MaxMana == 0)
                result.Add("mana", 100);
            else
                result.Add("mana", (int)((e.Mana * 100.0f) / e.Attributes.MaxMana));

            if(e.Spell != null)
            {
                result.Add("spell_progress", (int)(100 * (((double)e.Spell.TotalCastTimeMS - e.Spell.CastTimeRemainingMS) / e.Spell.TotalCastTimeMS)));
            }

            return result;
        }

        private static List<object> CreateMinimapMarkers(World world, Entity user)
        {
            var result = new List<object>();
            var team = world.Teams[user.Team];
            foreach (var teamMemberID in team.Members)
            {
                if (teamMemberID == user.ID)
                    continue;

                var teamMemberEnt = world.GetByID(teamMemberID);

                var tmp = new Dictionary<string, object>();
                tmp["translate"] = new Dictionary<string, object>
                {
                    { "x", (int)teamMemberEnt.Location.X }, { "y", (int)teamMemberEnt.Location.Y }
                };

                tmp["color"] = "#0A0";
                tmp["radius"] = 2;

                result.Add(tmp);
            }
            return result;
        }

        public static string CreateTick(World world, Rect2 bounds, Vector2 boundsLoc, Entity user, bool markers)
        {
            // this is already precalculated so is O(n) time, where n is the number of *nearby* entities
            var entities = new[] { user.ID }.Concat(user.NearbyEntityIds).Select((id) => world.GetByID(id));
            var result = new Dictionary<string, object>();

            result.Add("interpolation_factor", Program.GAME_SPEED_MULTIPLIER);
            result.Add("timestamp", world.Timestamp);
            result.Add("width", world.Width);
            result.Add("height", world.Height);
            
            result.Add("me", EntityToDict(user));

            var entities_arr = new List<Dictionary<string, object>>();
            foreach(var e in entities)
            {
                entities_arr.Add(EntityToDict(e));
            }

            if(markers)
                result.Add("minimap_markers", CreateMinimapMarkers(world, user));

            result.Add("entities", entities_arr);

            var wrapped = new List<object>();
            wrapped.Add(2);
            wrapped.Add(result);
            return JsonConvert.SerializeObject(wrapped);
        }
    }
}
