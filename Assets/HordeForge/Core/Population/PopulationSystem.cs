using System;
using HordeForge.Core.Data;
using HordeForge.Core.Economy;

namespace HordeForge.Core.Population
{
    /// <summary>
    /// Bevoelkerung als reine Zahl – keine Namen, kein Alter, kein Geschlecht.
    /// Es zaehlt nur, wieviele Buerger es gibt und wieviele davon gerade gebunden sind.
    ///
    /// Jede Bindung laeuft ueber <see cref="TryReserve"/> / <see cref="Release"/>,
    /// egal ob Produktionsgebaeude, Turmbesatzung oder Truppe. Dadurch kann die
    /// Zahl freier Buerger nie negativ werden und Umverteilung wirkt sofort.
    /// </summary>
    public sealed class PopulationSystem
    {
        private readonly GameConfig _config;
        private readonly ResourceLedger _ledger;

        private float _foodBuffer;
        private float _growthProgress;

        public event Action Changed;

        public PopulationSystem(GameConfig config, ResourceLedger ledger)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (ledger == null)
            {
                throw new ArgumentNullException("ledger");
            }

            _config = config;
            _ledger = ledger;
            Total = config.Settings.StartingPopulation;
        }

        public int Total { get; private set; }

        /// <summary>Buerger, die einem Gebaeude oder einer Truppe zugewiesen sind.</summary>
        public int Reserved { get; private set; }

        public int Free
        {
            get { return Total - Reserved; }
        }

        /// <summary>Es fehlt Nahrung. Loest im MVP eine Warnung aus, kein Sterben.</summary>
        public bool IsStarving { get; private set; }

        /// <summary>Angefangene Nahrungseinheit, die noch nicht aufgebraucht ist.</summary>
        public float FoodBuffer
        {
            get { return _foodBuffer; }
        }

        public float FoodPerMinute
        {
            get { return Total * _config.Settings.FoodPerCitizenPerMinute; }
        }

        /// <summary>
        /// Bindet Buerger. Gibt false zurueck und aendert nichts, wenn nicht genug
        /// freie Buerger vorhanden sind.
        /// </summary>
        public bool TryReserve(int count)
        {
            if (count <= 0)
            {
                return true;
            }

            if (Free < count)
            {
                return false;
            }

            Reserved += count;
            RaiseChanged();
            return true;
        }

        public void Release(int count)
        {
            if (count <= 0)
            {
                return;
            }

            Reserved -= count;
            if (Reserved < 0)
            {
                Reserved = 0;
            }

            RaiseChanged();
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            ConsumeFood(deltaTime);
            GrowPopulation(deltaTime);
        }

        private void ConsumeFood(float deltaTime)
        {
            float required = Total * _config.Settings.FoodPerCitizenPerMinute * deltaTime / 60f;
            _foodBuffer -= required;

            while (_foodBuffer < 0f)
            {
                int gained = _ledger.ConsumeOneFoodUnit();
                if (gained <= 0)
                {
                    // Nichts mehr im Lager: Warnzustand, aber niemand stirbt.
                    _foodBuffer = 0f;
                    SetStarving(true);
                    return;
                }

                _foodBuffer += gained;
            }

            SetStarving(_ledger.FoodStock < _config.Settings.FoodWarningThreshold);
        }

        private void GrowPopulation(float deltaTime)
        {
            GameSettings settings = _config.Settings;
            if (IsStarving
                || Total >= settings.MaxPopulation
                || settings.PopulationGrowthPerMinute <= 0f
                || _ledger.FoodStock < settings.GrowthFoodThreshold)
            {
                return;
            }

            _growthProgress += settings.PopulationGrowthPerMinute * deltaTime / 60f;
            while (_growthProgress >= 1f && Total < settings.MaxPopulation)
            {
                _growthProgress -= 1f;
                Total++;
                RaiseChanged();
            }
        }

        private void SetStarving(bool value)
        {
            if (IsStarving == value)
            {
                return;
            }

            IsStarving = value;
            RaiseChanged();
        }

        // ------------------------------------------------------------------
        // Persistenz
        // ------------------------------------------------------------------

        /// <summary>
        /// Setzt den Zustand beim Laden. <paramref name="reserved"/> wird danach von
        /// den Gebaeude- und Truppensystemen beim Wiederherstellen erneut aufgebaut.
        /// </summary>
        public void Restore(int total, float foodBuffer, bool starving)
        {
            Total = total < 0 ? 0 : total;
            Reserved = 0;
            _foodBuffer = foodBuffer;
            _growthProgress = 0f;
            IsStarving = starving;
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            Action handler = Changed;
            if (handler != null)
            {
                handler();
            }
        }
    }
}
