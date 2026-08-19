using System;
using System.Collections.Generic;
using HordeForge.Core.Data;

namespace HordeForge.Core.Items
{
    /// <summary>
    /// Ein Fundstueck aus einer Elitewelle. Im MVP bewusst ohne Werte, Buffs,
    /// Seltenheiten oder Haltbarkeit – nur Identitaet und Anzeigename.
    /// </summary>
    public sealed class InventoryItem
    {
        public InventoryItem(string id, ItemType type, string displayName)
        {
            Id = id;
            Type = type;
            DisplayName = displayName;
        }

        /// <summary>Eindeutige Id, damit spaeter einzelne Items referenziert werden koennen.</summary>
        public string Id { get; private set; }

        public ItemType Type { get; private set; }

        public string DisplayName { get; private set; }
    }

    /// <summary>
    /// Globales Siedlungsinventar mit fester Slotzahl. Items landen automatisch hier;
    /// es gibt kein Aufheben, kein Charakter- und kein Truppeninventar.
    /// </summary>
    public sealed class InventorySystem
    {
        private readonly GameConfig _config;
        private readonly List<InventoryItem> _items = new List<InventoryItem>();

        public event Action<InventoryItem> ItemAdded;

        /// <summary>Feuert, wenn ein Item wegen vollem Inventar nicht angenommen wurde.</summary>
        public event Action<ItemType> ItemRejected;

        public InventorySystem(GameConfig config)
        {
            _config = config;
        }

        public IReadOnlyList<InventoryItem> Items
        {
            get { return _items; }
        }

        public int Capacity
        {
            get { return _config.Settings.InventoryCapacity; }
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool IsFull
        {
            get { return _items.Count >= Capacity; }
        }

        public bool TryAdd(ItemType type, out InventoryItem item)
        {
            item = null;
            if (type == ItemType.None)
            {
                return false;
            }

            if (IsFull)
            {
                Action<ItemType> rejected = ItemRejected;
                if (rejected != null)
                {
                    rejected(type);
                }

                return false;
            }

            ItemDefinition definition = _config.GetItem(type);
            item = new InventoryItem(Guid.NewGuid().ToString("N"), type, definition.DisplayName);
            _items.Add(item);

            Action<InventoryItem> handler = ItemAdded;
            if (handler != null)
            {
                handler(item);
            }

            return true;
        }

        /// <summary>Belohnung einer geschafften Elitewelle: ein zufaelliges Generic Item.</summary>
        public bool TryAddRandom(Random random, out InventoryItem item)
        {
            item = null;

            ItemDefinition[] definitions = _config.Items;
            if (definitions == null || definitions.Length == 0)
            {
                return false;
            }

            ItemType type = definitions[random.Next(definitions.Length)].Type;
            return TryAdd(type, out item);
        }

        public bool Remove(string id)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Id == id)
                {
                    _items.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            _items.Clear();
        }

        /// <summary>Nur fuer das Laden eines Spielstands.</summary>
        internal void RestoreItem(string id, ItemType type)
        {
            ItemDefinition definition = _config.GetItem(type);
            _items.Add(new InventoryItem(id, type, definition.DisplayName));
        }
    }
}
