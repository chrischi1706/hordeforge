using System;
using System.Collections.Generic;
using HordeForge.Core.Data;

namespace HordeForge.Core.Save
{
    /// <summary>
    /// Flacher Abzug des Spielstands. Bewusst eigene DTOs statt der Laufzeitobjekte,
    /// damit sich das Speicherformat unabhaengig von internen Umbauten entwickeln kann.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public int Seed;
        public float ElapsedTime;

        public List<ResourceEntry> Resources = new List<ResourceEntry>();

        public int PopulationTotal;
        public float FoodBuffer;
        public bool Starving;

        public List<BuildingState> Buildings = new List<BuildingState>();
        public List<SquadState> Squads = new List<SquadState>();
        public List<ItemState> Inventory = new List<ItemState>();

        public int WaveNumber = 1;
        public WavePhase Phase = WavePhase.Preparation;
        public float PreparationRemaining;
    }

    [Serializable]
    public class ResourceEntry
    {
        public ResourceType Resource;
        public int Amount;
    }

    [Serializable]
    public class BuildingState
    {
        public int Id;
        public BuildingType Type;
        public float X;
        public float Y;
        public int Workers;
        public float Health;
        public int RecipeIndex;
    }

    [Serializable]
    public class SquadState
    {
        public int Id;
        public string Name;
        public float X;
        public float Y;
        public List<UnitState> Units = new List<UnitState>();
    }

    [Serializable]
    public class UnitState
    {
        public UnitType Type;
        public float Health;
    }

    [Serializable]
    public class ItemState
    {
        public string Id;
        public ItemType Type;
    }
}
