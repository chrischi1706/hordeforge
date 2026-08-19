using HordeForge.Core.Data;

namespace HordeForge.Core.Military
{
    /// <summary>
    /// Ein einzelner Soldat. Der Spieler steuert ihn nie direkt – er gehoert immer
    /// zu einer Truppe. Eigene Kampfwerte behaelt er trotzdem, damit ein
    /// Bogenschuetze in einer gemischten Truppe weiterhin auf Distanz schiesst.
    /// </summary>
    public sealed class Unit
    {
        public Unit(int id, UnitDefinition definition, Vec2 position)
        {
            Id = id;
            Definition = definition;
            Health = definition.MaxHealth;
            Position = position;
            FormationSlot = position;
        }

        public int Id { get; private set; }

        public UnitDefinition Definition { get; private set; }

        public float Health { get; internal set; }

        /// <summary>Aktuelle Position. Weicht nur im Rahmen der Leine vom Formationsplatz ab.</summary>
        public Vec2 Position { get; internal set; }

        /// <summary>Der Platz in der Formation, zu dem die Einheit zurueckkehrt.</summary>
        public Vec2 FormationSlot { get; internal set; }

        public float AttackCooldown { get; internal set; }

        /// <summary>Id des aktuellen Ziels, oder 0. Nur fuer die Anzeige.</summary>
        public int TargetEnemyId { get; internal set; }

        public UnitType Type
        {
            get { return Definition.Type; }
        }

        public bool IsAlive
        {
            get { return Health > 0f; }
        }
    }
}
