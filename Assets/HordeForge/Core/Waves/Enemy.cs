using HordeForge.Core.Data;

namespace HordeForge.Core.Waves
{
    /// <summary>
    /// Ein Gegner der aktuellen Horde. Existiert nur waehrend einer Welle – es gibt
    /// keine gegnerische Wirtschaft, keine Lager und keine Basen.
    /// </summary>
    public sealed class Enemy
    {
        public Enemy(int id, EnemyDefinition definition, Vec2 position, bool elite,
            float healthMultiplier, float damageMultiplier)
        {
            Id = id;
            Definition = definition;
            Position = position;
            IsElite = elite;
            MaxHealth = definition.MaxHealth * (elite ? healthMultiplier : 1f);
            Health = MaxHealth;
            DamageMultiplier = elite ? damageMultiplier : 1f;
        }

        public int Id { get; private set; }

        public EnemyDefinition Definition { get; private set; }

        public Vec2 Position { get; internal set; }

        public float Health { get; internal set; }

        public float MaxHealth { get; private set; }

        public bool IsElite { get; private set; }

        public float DamageMultiplier { get; private set; }

        public float AttackCooldown { get; internal set; }

        public EnemyType Type
        {
            get { return Definition.Type; }
        }

        public bool IsAlive
        {
            get { return Health > 0f; }
        }

        public float DamageAgainstUnits
        {
            get { return Definition.AttackDamage * DamageMultiplier; }
        }

        public float DamageAgainstBuildings
        {
            get
            {
                return Definition.AttackDamage
                       * DamageMultiplier
                       * Definition.BuildingDamageMultiplier;
            }
        }
    }
}
