using Microsoft.Xna.Framework;
using SharpMath2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Game
{
    public class EntityFactory
    {
        private static int RADIUS = 10;
        private static int WARRIOR_SIZE = 96;
        private static int HUNTER_SIZE = 80;
        private static int PRIEST_SIZE = 76;

        private static Polygon2 WARRIOR_BOUNDS = new Polygon2(new[] {
            new Vector2(RADIUS, 0), new Vector2(WARRIOR_SIZE - RADIUS, 0), new Vector2(WARRIOR_SIZE, RADIUS),
            new Vector2(WARRIOR_SIZE, WARRIOR_SIZE - RADIUS), new Vector2(WARRIOR_SIZE - RADIUS, WARRIOR_SIZE), new Vector2(RADIUS, WARRIOR_SIZE),
            new Vector2(0, WARRIOR_SIZE - RADIUS), new Vector2(0, RADIUS) });
        private static Polygon2 HUNTER_BOUNDS = new Polygon2(new[] {
            new Vector2(RADIUS, 0), new Vector2(HUNTER_SIZE - RADIUS, 0), new Vector2(HUNTER_SIZE, RADIUS),
            new Vector2(HUNTER_SIZE, HUNTER_SIZE - RADIUS), new Vector2(HUNTER_SIZE - RADIUS, HUNTER_SIZE), new Vector2(RADIUS, HUNTER_SIZE),
            new Vector2(0, HUNTER_SIZE - RADIUS), new Vector2(0, RADIUS) });
        private static Polygon2 PRIEST_BOUNDS = new Polygon2(new[] {
            new Vector2(RADIUS, 0), new Vector2(PRIEST_SIZE - RADIUS, 0), new Vector2(PRIEST_SIZE, RADIUS),
            new Vector2(PRIEST_SIZE, PRIEST_SIZE - RADIUS), new Vector2(PRIEST_SIZE - RADIUS, PRIEST_SIZE), new Vector2(RADIUS, PRIEST_SIZE),
            new Vector2(0, PRIEST_SIZE - RADIUS), new Vector2(0, RADIUS) });

        public static EntityAttributes WARRIOR_ATTRIBUTES = new EntityAttributes(UnitType.Warrior, WARRIOR_BOUNDS, 0.01f, 100, 50, 0.0005f);
        public static EntityAttributes HUNTER_ATTRIBUTES = new EntityAttributes(UnitType.Hunter, HUNTER_BOUNDS, 0.015f, 50, 50, 0.0005f);
        public static EntityAttributes PRIEST_ATTRIBUTES = new EntityAttributes(UnitType.Priest, PRIEST_BOUNDS, 0.01f, 50, 100, 0.002f);
    }
}
