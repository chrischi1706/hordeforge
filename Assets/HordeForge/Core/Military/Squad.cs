using System.Collections.Generic;
using System.Text;
using HordeForge.Core.Data;

namespace HordeForge.Core.Military
{
    /// <summary>
    /// Die taktische Einheit des Spielers. Kann gemischt sein – etwa 5 Bogenschuetzen,
    /// 3 Speertraeger und 2 Schwertkaempfer – und wird als Gruppe platziert.
    /// </summary>
    public sealed class Squad
    {
        private readonly List<Unit> _units = new List<Unit>();

        /// <summary>Reihenbreite der Aufstellung.</summary>
        private const int UnitsPerRow = 5;

        private const float UnitSpacing = 1.3f;

        public Squad(int id, string name, Vec2 position)
        {
            Id = id;
            Name = name;
            Position = position;
        }

        public int Id { get; private set; }

        public string Name { get; internal set; }

        /// <summary>Der Ankerpunkt der Truppe. Hier bleibt sie stationiert.</summary>
        public Vec2 Position { get; internal set; }

        public IReadOnlyList<Unit> Units
        {
            get { return _units; }
        }

        public int Count
        {
            get { return _units.Count; }
        }

        public bool IsEmpty
        {
            get { return _units.Count == 0; }
        }

        public int CountOf(UnitType type)
        {
            int total = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Type == type)
                {
                    total++;
                }
            }

            return total;
        }

        /// <summary>Etwa "5x Bogenschuetze, 3x Speertraeger" fuer das Truppen-UI.</summary>
        public string Composition
        {
            get
            {
                if (_units.Count == 0)
                {
                    return "leer";
                }

                UnitType[] order = { UnitType.Archer, UnitType.Spearman, UnitType.Swordsman };
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < order.Length; i++)
                {
                    int count = CountOf(order[i]);
                    if (count == 0)
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(count).Append("x ").Append(DisplayNameOf(order[i]));
                }

                return builder.ToString();
            }
        }

        private string DisplayNameOf(UnitType type)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Type == type)
                {
                    return _units[i].Definition.DisplayName;
                }
            }

            return type.ToString();
        }

        internal void AddUnit(Unit unit)
        {
            _units.Add(unit);
            RebuildFormation(false);

            // Frisch Rekrutierte stellen sich sofort richtig auf, statt uebereinander
            // auf dem Ankerpunkt zu stehen.
            unit.Position = unit.FormationSlot;
        }

        internal bool RemoveUnit(Unit unit)
        {
            bool removed = _units.Remove(unit);
            if (removed)
            {
                // Die Verbliebenen ruecken auf, ohne aus einem laufenden Gefecht
                // herausgerissen zu werden.
                RebuildFormation(false);
            }

            return removed;
        }

        internal void ClearUnits()
        {
            _units.Clear();
        }

        internal void SetPosition(Vec2 position)
        {
            Position = position;
            RebuildFormation(true);
        }

        /// <summary>
        /// Verteilt die Einheiten in Reihen um den Ankerpunkt.
        /// </summary>
        /// <param name="snapToSlots">
        /// True, wenn der Spieler die Truppe bewusst verlegt hat – dann stellen sich
        /// die Einheiten direkt neu auf. False bei Zu- und Abgaengen, damit niemand
        /// mitten im Gefecht an seinen Formationsplatz zurueckspringt.
        /// </param>
        internal void RebuildFormation(bool snapToSlots)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                int row = i / UnitsPerRow;
                int column = i % UnitsPerRow;
                float offsetX = (column - (UnitsPerRow - 1) * 0.5f) * UnitSpacing;
                float offsetY = -row * UnitSpacing;

                Vec2 slot = new Vec2(Position.X + offsetX, Position.Y + offsetY);
                _units[i].FormationSlot = slot;
                _units[i].Position = snapToSlots
                    ? slot
                    : Vec2.ClampToRadius(_units[i].Position, slot, _units[i].Definition.LeashRadius);
            }
        }
    }
}
