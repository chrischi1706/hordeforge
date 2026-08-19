using System;

namespace HordeForge.Core
{
    /// <summary>
    /// Geometrie der Testkarte. Das Rathaus steht im Ursprung, die Horden kommen
    /// aus den vier Himmelsrichtungen.
    /// </summary>
    public static class MapLayout
    {
        public static readonly SpawnDirection[] AllDirections =
        {
            SpawnDirection.North,
            SpawnDirection.East,
            SpawnDirection.South,
            SpawnDirection.West
        };

        public static Vec2 DirectionVector(SpawnDirection direction)
        {
            switch (direction)
            {
                case SpawnDirection.North:
                    return new Vec2(0f, 1f);
                case SpawnDirection.East:
                    return new Vec2(1f, 0f);
                case SpawnDirection.South:
                    return new Vec2(0f, -1f);
                default:
                    return new Vec2(-1f, 0f);
            }
        }

        public static string DisplayName(SpawnDirection direction)
        {
            switch (direction)
            {
                case SpawnDirection.North:
                    return "Norden";
                case SpawnDirection.East:
                    return "Osten";
                case SpawnDirection.South:
                    return "Sueden";
                default:
                    return "Westen";
            }
        }

        public static Vec2 SpawnPoint(SpawnDirection direction, float distance)
        {
            return DirectionVector(direction) * distance;
        }

        /// <summary>
        /// Streut die Gegner quer zur Anmarschrichtung, damit eine Horde als Front
        /// erscheint und nicht als Kolonne aus einem einzigen Punkt.
        /// </summary>
        public static Vec2 SpawnPosition(
            SpawnDirection direction, float distance, Random random, float spread)
        {
            Vec2 forward = DirectionVector(direction);
            Vec2 sideways = new Vec2(-forward.Y, forward.X);
            float offset = (float)(random.NextDouble() * 2.0 - 1.0) * spread;
            return forward * distance + sideways * offset;
        }
    }
}
