namespace HordeForge.Core
{
    /// <summary>
    /// Alles, was gelagert, produziert oder verbraucht werden kann, laeuft ueber
    /// dieses eine Enum – Rohstoffe, Zwischenprodukte, Ausruestung und Munition.
    /// Ein gemeinsames Lager haelt Produktionsketten und Rekrutierungskosten
    /// einheitlich; Unterscheidungen fuer die UI liefert <see cref="ResourceCategory"/>.
    /// </summary>
    public enum ResourceType
    {
        None = 0,

        // Rohstoffe
        Wood = 1,
        Stone = 2,
        Ore = 3,
        Grain = 4,
        Fish = 5,
        Cotton = 6,
        Wool = 7,

        // Verarbeitet
        Planks = 20,
        CutStone = 21,
        Metal = 22,
        Flour = 23,
        Thread = 24,
        Cloth = 25,
        Bread = 26,

        // Werkzeug
        Tool = 40,

        // Ausruestung
        Bow = 60,
        Spear = 61,
        Sword = 62,
        Shield = 63,
        Armor = 64,

        // Munition
        Arrows = 80,
        SiegeAmmo = 81
    }

    public enum ResourceCategory
    {
        Raw = 0,
        Processed = 1,
        Tool = 2,
        Equipment = 3,
        Ammunition = 4
    }

    public enum BuildingCategory
    {
        /// <summary>Rathaus. Geht es verloren, ist die Partie verloren.</summary>
        Core = 0,

        /// <summary>Gewinnt Rohstoffe ohne Input – Holzfaeller, Mine, Feld, ...</summary>
        Gathering = 1,

        /// <summary>Verarbeitet Inputs zu Outputs – Saegewerk, Schmiede, ...</summary>
        Production = 2,

        /// <summary>Greift Gegner an, braucht Besatzung und Munition.</summary>
        Defense = 3,

        /// <summary>Mauer/Tor. Nimmt Schaden, braucht aber keine Besatzung.</summary>
        Fortification = 4,

        /// <summary>Handelskammer. Vorbereitet fuer das spaetere Item-System.</summary>
        Support = 5
    }

    public enum BuildingType
    {
        None = 0,

        TownHall = 1,

        // Rohstoffgewinnung
        LumberCamp = 10,
        Quarry = 11,
        OreMine = 12,
        GrainFarm = 13,
        FishingHut = 14,
        CottonFarm = 15,
        SheepPasture = 16,

        // Verarbeitung
        Sawmill = 30,
        Stonemason = 31,
        Smeltery = 32,
        Mill = 33,
        Bakery = 34,
        Spinnery = 35,
        Weavery = 36,
        Smithy = 37,
        Bowyer = 38,

        // Verteidigung
        Wall = 50,
        Gate = 51,
        ArcherTower = 52,
        Ballista = 53,
        Catapult = 54,

        // Sonstiges
        TradeChamber = 70
    }

    public enum UnitType
    {
        None = 0,
        Archer = 1,
        Spearman = 2,
        Swordsman = 3
    }

    public enum EnemyType
    {
        None = 0,
        Goblin = 1,
        Ork = 2,
        OrkWarrior = 3,
        ShadowRider = 4,
        Troll = 5
    }

    public enum ItemType
    {
        None = 0,
        OldSword = 1,
        BatteredShield = 2,
        SignetRing = 3,
        Crystal = 4,
        Relic = 5,
        AncientDocument = 6
    }

    public enum SpawnDirection
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public enum WavePhase
    {
        /// <summary>Zwischen den Wellen: aufbauen, zuweisen, rekrutieren.</summary>
        Preparation = 0,

        /// <summary>Horde ist unterwegs oder kaempft.</summary>
        Active = 1,

        /// <summary>Rathaus zerstoert.</summary>
        Defeat = 2
    }

    /// <summary>
    /// Warum ein Gebaeude gerade (nicht) produziert. Wird direkt im Gebaeude-UI gezeigt.
    /// </summary>
    public enum ProductionStatus
    {
        Idle = 0,
        Active = 1,

        /// <summary>Keine Arbeiter zugewiesen.</summary>
        NoWorkers = 2,

        /// <summary>Arbeitet, aber nicht auf voller Besetzung.</summary>
        Understaffed = 3,

        /// <summary>Ein Input fehlt – welcher, steht in Building.MissingInput.</summary>
        WaitingForInput = 4,

        /// <summary>Gebaeude zerstoert oder ohne Produktionsrezept.</summary>
        Inoperable = 5
    }
}
