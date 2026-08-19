using System;

namespace HordeForge.Core.Data
{
    /// <summary>
    /// Ein Rohstoffgebiet auf der Karte – Wald, Steinvorkommen, Fischgruende und so fort.
    /// Gewinnungsgebaeude duerfen nur im passenden Gebiet stehen; dadurch bekommt die
    /// Karte eine Rolle, statt nur Hintergrund zu sein.
    /// </summary>
    [Serializable]
    public class ResourceZone
    {
        public string DisplayName;
        public BuildingType AllowedBuilding;
        public Vec2 Center;
        public float Radius;

        public ResourceZone()
        {
        }

        public ResourceZone(string displayName, BuildingType allowedBuilding, Vec2 center, float radius)
        {
            DisplayName = displayName;
            AllowedBuilding = allowedBuilding;
            Center = center;
            Radius = radius;
        }

        public bool Contains(Vec2 position)
        {
            return Vec2.SqrDistance(position, Center) <= Radius * Radius;
        }
    }

    [Serializable]
    public class BuildingPlacement
    {
        public BuildingType Type;
        public Vec2 Position;

        public BuildingPlacement()
        {
        }

        public BuildingPlacement(BuildingType type, Vec2 position)
        {
            Type = type;
            Position = position;
        }
    }

    /// <summary>
    /// Aufbau der Testkarte: wo welche Rohstoffe liegen und womit der Spieler startet.
    /// </summary>
    [Serializable]
    public class MapDefinition
    {
        public ResourceZone[] Zones = new ResourceZone[0];
        public BuildingPlacement[] StartingBuildings = new BuildingPlacement[0];

        /// <summary>
        /// Gebiet, in dem dieses Gewinnungsgebaeude stehen darf, oder null.
        /// </summary>
        public ResourceZone FindZoneFor(BuildingType type, Vec2 position)
        {
            for (int i = 0; i < Zones.Length; i++)
            {
                ResourceZone zone = Zones[i];
                if (zone.AllowedBuilding == type && zone.Contains(position))
                {
                    return zone;
                }
            }

            return null;
        }

        public bool HasZoneFor(BuildingType type)
        {
            for (int i = 0; i < Zones.Length; i++)
            {
                if (Zones[i].AllowedBuilding == type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
