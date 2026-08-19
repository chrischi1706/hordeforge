using System;

namespace HordeForge.Core.Data
{
    /// <summary>
    /// Globale Stellschrauben der Simulation. Alles, was Balancing betrifft, steht
    /// hier oder in <see cref="WaveSettings"/> – nicht verstreut im Code.
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        public int StartingPopulation = 40;

        /// <summary>Obergrenze, damit Nachwuchs die Simulation nicht sprengt.</summary>
        public int MaxPopulation = 160;

        /// <summary>Nahrungsbedarf pro Buerger und Minute.</summary>
        public float FoodPerCitizenPerMinute = 1f;

        /// <summary>
        /// Ab diesem Nahrungsvorrat waechst die Bevoelkerung. Ohne Nachwuchs waere der
        /// Endlos-Loop nicht durchhaltbar, mit ihm bekommt Nahrung eine echte Rolle.
        /// </summary>
        public int GrowthFoodThreshold = 60;

        /// <summary>Neue Buerger pro Minute, solange der Vorrat ueber der Schwelle liegt.</summary>
        public float PopulationGrowthPerMinute = 2f;

        /// <summary>Unter diesem Vorrat zeigt das HUD eine Hungerwarnung.</summary>
        public int FoodWarningThreshold = 20;

        public int InventoryCapacity = 20;

        /// <summary>Spielbarer Kartenradius um das Rathaus.</summary>
        public float MapRadius = 70f;

        /// <summary>Mindestabstand zwischen zwei Gebaeudemittelpunkten.</summary>
        public float BuildingSpacing = 1.0f;

        /// <summary>Startbestand, damit sofort etwas gebaut werden kann.</summary>
        public ResourceAmount[] StartingResources = new ResourceAmount[0];
    }

    /// <summary>
    /// Steuert Wellenbudget, Eskalation und Elitewellen.
    /// </summary>
    [Serializable]
    public class WaveSettings
    {
        /// <summary>Budget der ersten Welle.</summary>
        public int BaseBudget = 100;

        /// <summary>
        /// Budget = BaseBudget * Growth^(Welle-1), gerundet auf
        /// <see cref="BudgetRounding"/>. Ergibt 100 / 140 / 200 / 270 / 380 ...
        /// und laesst sich mit einem Wert nachziehen.
        /// </summary>
        public float BudgetGrowth = 1.38f;

        public int BudgetRounding = 10;

        /// <summary>Wahrscheinlichkeit, dass eine Welle eine Elitewelle wird.</summary>
        public float EliteChance = 0.15f;

        /// <summary>Vor dieser Welle gibt es keine Elite – die erste Welle soll fair sein.</summary>
        public int EliteMinWave = 3;

        public float EliteBudgetMultiplier = 1.5f;
        public float EliteHealthMultiplier = 1.5f;
        public float EliteDamageMultiplier = 1.25f;

        /// <summary>Vorbereitungszeit zwischen den Wellen. Kann uebersprungen werden.</summary>
        public float PreparationSeconds = 90f;

        /// <summary>
        /// Vor der ersten Welle deutlich mehr Zeit: die Siedlung startet ohne
        /// Verteidigung, und die Produktionsketten brauchen einen Moment, bis sie
        /// laufen. Ein Durchlauf mit der normalen Zeit endete zuverlaessig in Welle 1.
        /// </summary>
        public float FirstWavePreparationSeconds = 210f;

        /// <summary>
        /// Ein Gegnertyp taucht erst auf, wenn das Wellenbudget ein Vielfaches seiner
        /// Kosten erreicht. Verhindert, dass Welle 1 aus einem einzelnen Troll besteht,
        /// und staffelt die Typen von allein: Ork-Krieger ab Welle 4, Schattenreiter
        /// ab Welle 5, Trolle ab Welle 8.
        /// </summary>
        public float EnemyUnlockBudgetFactor = 4f;

        /// <summary>Aus wievielen Himmelsrichtungen eine Horde hoechstens kommt.</summary>
        public int MaxSpawnDirections = 2;

        /// <summary>Abstand zwischen zwei Gegnern beim Einsickern.</summary>
        public float SpawnIntervalSeconds = 0.35f;

        /// <summary>Entfernung der Spawnpunkte vom Rathaus.</summary>
        public float SpawnDistance = 62f;
    }
}
