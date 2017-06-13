using IORPG.Game;
using IORPG.Game.Mutations;
using IORPG.Game.Spells;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using SharpMath2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace IORPG.Networking
{
    public class PlayService : WebSocketBehavior
    {
        private static Rect2 VisibleBounds = new Rect2(-1920 / 2, -1080 / 2, 1920 / 2, 1080 / 2);
        private static Rect2 SpawnBounds = new Rect2(0, 0, 1920, 1080);

        private enum UserState
        {
            NOT_YET_SPAWNED,
            SPAWNING,
            SPAWNED,
            CLOSED
        }

        const int HELLO = 1;
        const int TICK = 2; // baked into Serializer

        const int START_MOVE = 3;
        const int STOP_MOVE = 4;

        const int CAST_SPELL = 5;

        const int CHANGE_TICK_RATE = 6;

        const int DIR_RIGHT = 1;
        const int DIR_LEFT = 2;
        const int DIR_UP = 3;
        const int DIR_DOWN = 4;
        
        private UserState _UserState;
        private Entity Entity;
        private bool _RightPressed;
        private bool _LeftPressed;
        private bool _UpPressed;
        private bool _DownPressed;
        private bool _CastingSpell;

        private int LastMarkersTimestamp;

        private int TicksNotSentCounter;
        private int RequestedTicksPerSentTick;

        public PlayService()
        {
            _UserState = UserState.NOT_YET_SPAWNED;
            Entity = null;
            LastMarkersTimestamp = 0;
            RequestedTicksPerSentTick = 1;
        }

        public void SendTick(World world)
        {
            if(_UserState == UserState.SPAWNED)
            {
                if(RequestedTicksPerSentTick != 1)
                {
                    TicksNotSentCounter++;
                    if(TicksNotSentCounter == RequestedTicksPerSentTick)
                    {
                        TicksNotSentCounter = 0;
                    }else
                    {
                        return;
                    }
                }

                int entIndex;
                if(!world.EntityIDToIndex.TryGetValue(Entity.ID, out entIndex))
                {
                    ForceDisconnect();
                    return;
                }

                Entity = world.Entities[entIndex];
                _CastingSpell = Entity.Spell != null;

                bool sendMarkers = false;
                if(world.Timestamp - LastMarkersTimestamp > 5000)
                {
                    sendMarkers = true;
                    LastMarkersTimestamp = world.Timestamp;
                }
                SendAsync(Serializer.CreateTick(world, VisibleBounds, Entity.Location, Entity, sendMarkers), SendFinished);
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            IWorldMutation mut;
            if(!e.IsText)
            {
                ForceDisconnect();
                return;
            }

            var json = e.Data;
            JArray parsed;
            try
            {
                parsed = JArray.Parse(json);
                var messageType = (int)parsed[0];
                int dir;
                switch (messageType)
                {
                    case HELLO:
                        if (_UserState != UserState.NOT_YET_SPAWNED)
                        {
                            throw new InvalidProgramException($"HELLO recieved when userstate = {_UserState}");
                        }
                        _UserState = UserState.SPAWNING;

                        var hero = (int)parsed[1]["hero"];
                        var name = (string)parsed[1]["name"];
                        name = CleanseName(name);
                        if(name == null)
                        {
                            ForceDisconnect();
                            return;
                        }
                        EntityAttributes entProto;
                        switch(hero)
                        {
                            case (int)UnitType.Warrior:
                                entProto = EntityFactory.WARRIOR_ATTRIBUTES;
                                break;
                            case (int)UnitType.Hunter:
                                entProto = EntityFactory.HUNTER_ATTRIBUTES;
                                break;
                            case (int)UnitType.Priest:
                                entProto = EntityFactory.PRIEST_ATTRIBUTES;
                                break;
                            default:
                                throw new InvalidProgramException($"Weird hero type: {hero}");
                        }
                        mut = new EntityAddedMutation(name, -1, entProto, SpawnBounds, EntityCreated);
                        Program.QueuedMutations[mut.Time].Enqueue(mut);
                        break;
                    case START_MOVE:
                        if (_UserState != UserState.SPAWNED)
                            throw new InvalidProgramException($"STAR_MOVE recieved when userstate={_UserState}");
                        dir = (int)parsed[1];
                        switch(dir)
                        {
                            case DIR_DOWN:
                                _DownPressed = true;
                                break;
                            case DIR_LEFT:
                                _LeftPressed = true;
                                break;
                            case DIR_RIGHT:
                                _RightPressed = true;
                                break;
                            case DIR_UP:
                                _UpPressed = true;
                                break;
                            default:
                                throw new InvalidProgramException($"weird direction: {dir}");
                        }
                        UpdateVelocity();
                        break;
                    case STOP_MOVE:
                        if (_UserState != UserState.SPAWNED)
                            throw new InvalidProgramException($"STOP_MOVE recieved when userstate={_UserState}");
                        dir = (int)parsed[1];
                        switch(dir)
                        {
                            case DIR_DOWN:
                                _DownPressed = false;
                                break;
                            case DIR_LEFT:
                                _LeftPressed = false;
                                break;
                            case DIR_RIGHT:
                                _RightPressed = false;
                                break;
                            case DIR_UP:
                                _UpPressed = false;
                                break;
                            default:
                                throw new InvalidProgramException($"weird direction: {dir}");
                        }
                        UpdateVelocity();
                        break;
                    case CAST_SPELL:
                        if (_UserState != UserState.SPAWNED)
                            throw new InvalidProgramException($"CAST_SPELL recieved when userstate={_UserState}");
                        if (_CastingSpell)
                            break;
                        var spellIndex = (int)parsed[1];
                        switch(Entity.Attributes.UnitType)
                        {
                            case UnitType.Warrior:
                                switch (spellIndex)
                                {
                                    case SpellFactory.PUSH_INDEX:
                                        TryCastPush(parsed);
                                        break;
                                    case SpellFactory.BLOCK_INDEX:
                                        TryCastBlock(parsed);
                                        break;
                                    default:
                                        throw new InvalidProgramException($"unknown spell for warrior: {spellIndex}");
                                }
                                break;
                            case UnitType.Hunter:
                                switch(spellIndex)
                                {
                                    case SpellFactory.SHOOT_INDEX:
                                        TryCastShoot(parsed);
                                        break;
                                    case SpellFactory.DELIBERATE_SHOT_INDEX:
                                        TryCastDeliberateShot(parsed);
                                        break;
                                    default:
                                        throw new InvalidProgramException($"unknown spell for hunter: {spellIndex}");
                                }
                                break;
                            case UnitType.Priest:
                                switch(spellIndex)
                                {
                                    case SpellFactory.LESSER_HEAL_INDEX:
                                        TryCastLesserHeal(parsed);
                                        break;
                                    case SpellFactory.GREATER_HEAL_INDEX:
                                        TryCastGreaterHeal(parsed);
                                        break;
                                    case SpellFactory.HEALING_STRIKE_INDEX:
                                        TryCastHealingStrike(parsed);
                                        break;
                                    default:
                                        throw new InvalidProgramException($"unknown spell for priest: {spellIndex}");
                                }
                                break;
                        }
                        break;
                    case CHANGE_TICK_RATE:
                        bool increase = (bool)parsed[1];

                        if(increase)
                        {
                            RequestedTicksPerSentTick ++;
                        }else
                        {
                            RequestedTicksPerSentTick = Math.Max(RequestedTicksPerSentTick - 1, 1);
                        }
                        break;
                    default:
                        throw new InvalidProgramException($"Weird message type: {messageType}");
                }
            }
            catch(Exception exc)
            {
                // TODO better error handling
                ForceDisconnect();
                return;
            }

        }

        string CleanseName(string name)
        {
            name = name.Trim();
            int indexToStartAt = 0;
            for(indexToStartAt = 0; indexToStartAt < name.Length; indexToStartAt++)
            {
                if (name[indexToStartAt] != '.' && name[indexToStartAt] != ',')
                    break;
            }
            if (indexToStartAt == name.Length)
                return null;
            name = name.Substring(indexToStartAt);
            var nameLower = name.ToLower();

            if (name.Length < 3 || name.Length > 20 || nameLower.Contains("admin") || nameLower.StartsWith("team") || nameLower.StartsWith("[team]"))
            {
                return null;
            }

            return name;
        }

        bool TryGetTargetFromPosition(JToken pos, World currWorld, out Entity targetEnt)
        {
            var checkVec = new Vector2((int)pos["x"], (int)pos["y"]);
            targetEnt = null;
            foreach (var nearbyID in Entity.NearbyEntityIds)
            {
                if (currWorld.EntityIDToIndex.ContainsKey(nearbyID))
                {
                    var nearbyEnt = currWorld.GetByID(nearbyID);
                    if (Polygon2.Contains(nearbyEnt.Attributes.Bounds, nearbyEnt.Location, Rotation2.Zero, checkVec, false))
                    {
                        targetEnt = nearbyEnt;
                        return true;
                    }
                }
            }

            return false;
        }

        bool ManaCheck(Entity caster, int mana)
        {
            return caster.Mana >= mana;
        }

        void TryCastLesserHeal(JToken parsed)
        {
            if (Entity.SpellCooldowns.ContainsKey(SpellFactory.LESSER_HEAL_INDEX))
                return;

            var currWorld = Program.State.World;
            var currEnt = currWorld.GetByID(Entity.ID);
            if (!ManaCheck(currEnt, SpellFactory.LESSER_HEAL_MANA))
                return;
            Entity targetEnt;
            if (!TryGetTargetFromPosition(parsed[2], currWorld, out targetEnt))
                return;
            if (targetEnt.Team != currEnt.Team)
                return;
            if (!SpellFactory.CheckRange(currEnt, targetEnt, SpellFactory.LESSER_HEAL_RANGE))
                return;

            var mut = new EntityCastSpellMutation(Entity.ID, SpellFactory.CreateLesserHeal(targetEnt.ID));
            Program.QueuedMutations[mut.Time].Enqueue(mut);
            _CastingSpell = true;
            UpdateVelocity();
        }

        void TryCastGreaterHeal(JToken parsed)
        {
            if (Entity.SpellCooldowns.ContainsKey(SpellFactory.GREATER_HEAL_INDEX))
                return;

            var currWorld = Program.State.World;
            var currEnt = currWorld.GetByID(Entity.ID);
            if (!ManaCheck(currEnt, SpellFactory.GREATER_HEAL_MANA))
                return;
            Entity targetEnt;
            if (!TryGetTargetFromPosition(parsed[2], currWorld, out targetEnt))
                return;
            if (targetEnt.Team != currEnt.Team)
                return;
            if (!SpellFactory.CheckRange(currEnt, targetEnt, SpellFactory.GREATER_HEAL_RANGE))
                return;

            var mut = new EntityCastSpellMutation(Entity.ID, SpellFactory.CreateGreaterHeal(targetEnt.ID));
            Program.QueuedMutations[mut.Time].Enqueue(mut);
            _CastingSpell = true;
            UpdateVelocity();
        }
        
        void TryCastHealingStrike(JToken parsed)
        {
            if (Entity.SpellCooldowns.ContainsKey(SpellFactory.HEALING_STRIKE_INDEX))
                return;

            var currWorld = Program.State.World;
            var currEnt = currWorld.GetByID(Entity.ID);
            if (!ManaCheck(currEnt, SpellFactory.HEALING_STRIKE_MANA))
                return;
            Vector2 target = new Vector2((float)parsed[2]["x"], (float)parsed[2]["y"]);
            var mind = Polygon2.MinDistance(currEnt.Attributes.Bounds, currEnt.Location, target);
            if (mind != null && mind.Item2 > SpellFactory.HEALING_STRIKE_RANGE)
                return;

            var mut = new EntityCastSpellMutation(Entity.ID, SpellFactory.CreateHealingStrike(target));
            Program.QueuedMutations[mut.Time].Enqueue(mut);
            _CastingSpell = true;
            UpdateVelocity();
        }

        void TryCastShoot(JToken parsed)
        {
            if (Entity.SpellCooldowns.ContainsKey(SpellFactory.SHOOT_INDEX))
                return;

            var currWorld = Program.State.World;
            var currEnt = currWorld.GetByID(Entity.ID);
            if (!ManaCheck(currEnt, SpellFactory.SHOOT_MANA))
                return;
            Entity targetEnt;
            if (!TryGetTargetFromPosition(parsed[2], currWorld, out targetEnt))
                return;
            if (targetEnt.Team == currEnt.Team)
                return;
            if (!SpellFactory.CheckRange(currEnt, targetEnt, SpellFactory.SHOOT_RANGE))
                return;

            var mut = new EntityCastSpellMutation(Entity.ID, SpellFactory.CreateShoot(targetEnt.ID));
            Program.QueuedMutations[mut.Time].Enqueue(mut);
            _CastingSpell = true;
            UpdateVelocity();
        }

        void TryCastDeliberateShot(JToken parsed)
        {
            if (Entity.SpellCooldowns.ContainsKey(SpellFactory.DELIBERATE_SHOT_INDEX))
                return;

            var currWorld = Program.State.World;
            var currEnt = currWorld.GetByID(Entity.ID);
            if (!ManaCheck(currEnt, SpellFactory.DELIBERATE_SHOT_MANA))
                return;
            Entity targetEnt;
            if (!TryGetTargetFromPosition(parsed[2], currWorld, out targetEnt))
                return;
            if (targetEnt.Team == currEnt.Team)
                return;
            if (!SpellFactory.CheckRange(currEnt, targetEnt, SpellFactory.DELIBERATE_SHOT_RANGE))
                return;

            var mut = new EntityCastSpellMutation(Entity.ID, SpellFactory.CreateDeliberateShot(targetEnt.ID));
            Program.QueuedMutations[mut.Time].Enqueue(mut);
            _CastingSpell = true;
            UpdateVelocity();
        }

        void TryCastPush(JToken parsed)
        {
            if (Entity.SpellCooldowns.ContainsKey(SpellFactory.PUSH_INDEX))
                return;

            var currWorld = Program.State.World;
            var currEnt = currWorld.GetByID(Entity.ID);
            if (!ManaCheck(currEnt, SpellFactory.PUSH_MANA))
                return;
            Entity targetEnt;
            if (!TryGetTargetFromPosition(parsed[2], currWorld, out targetEnt))
                return;
            if (targetEnt.Team == currEnt.Team)
                return;
            if (!SpellFactory.CheckRange(currEnt, targetEnt, SpellFactory.PUSH_RANGE))
                return;

            var mut = new EntityCastSpellMutation(Entity.ID, SpellFactory.CreatePush(targetEnt.ID));
            Program.QueuedMutations[mut.Time].Enqueue(mut);
            _CastingSpell = true;
            UpdateVelocity();
        }

        void TryCastBlock(JToken parsed)
        {
            if (Entity.SpellCooldowns.ContainsKey(SpellFactory.BLOCK_INDEX))
                return;

            var currWorld = Program.State.World;
            var currEnt = currWorld.GetByID(Entity.ID);
            if (!ManaCheck(currEnt, SpellFactory.BLOCK_MANA))
                return;

            var mut = new EntityCastSpellMutation(Entity.ID, SpellFactory.CreateBlock());
            Program.QueuedMutations[mut.Time].Enqueue(mut);
            _CastingSpell = true;
            UpdateVelocity();
        }

        protected override void OnClose(CloseEventArgs e)
        {
            OnDisconnect();
        }

        void EntityCreated(Entity ent)
        {
            if(_UserState != UserState.SPAWNING)
            {
                var mut = new EntityRemovedMutation(ent.ID);
                Program.QueuedMutations[mut.Time].Enqueue(mut);
                return;
            }else
            {
                _UserState = UserState.SPAWNED;
                Entity = ent;

                SendTick(Program.State.World);
            }
        }

        void UpdateVelocity()
        {
            var newVelocity = Vector2.Zero;
            if (!_CastingSpell)
            {
                if (_RightPressed)
                    newVelocity.X++;
                if (_LeftPressed)
                    newVelocity.X--;
                if (_UpPressed)
                    newVelocity.Y--;
                if (_DownPressed)
                    newVelocity.Y++;

                newVelocity *= 0.1f;
            }
            var mut = new EntityChangeVelocityMutation(Entity.ID, newVelocity);
            Program.QueuedMutations[mut.Time].Enqueue(mut);
        }

        void SendFinished(bool succ)
        {
            if (!succ)
                ForceDisconnect();
        }

        void ForceDisconnect()
        {
            Sessions.CloseSession(ID);
        }

        void OnDisconnect()
        {
            switch (_UserState)
            {
                case UserState.SPAWNED:
                    var mutation = new EntityRemovedMutation(Entity.ID);
                    Program.QueuedMutations[mutation.Time].Enqueue(mutation);
                    break;
                case UserState.SPAWNING:
                case UserState.NOT_YET_SPAWNED:
                    _UserState = UserState.CLOSED;
                    break;
                case UserState.CLOSED:
                    break;
            }
        }
    }
}
