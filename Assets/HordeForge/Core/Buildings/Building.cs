using HordeForge.Core.Data;

namespace HordeForge.Core.Buildings
{
    /// <summary>
    /// Eine gebaute Instanz auf der Karte. Haelt nur Laufzeitzustand – alle
    /// Balancewerte kommen aus <see cref="Definition"/>.
    /// </summary>
    public sealed class Building
    {
        public Building(int id, BuildingDefinition definition, Vec2 position)
        {
            Id = id;
            Definition = definition;
            Position = position;
            Health = definition.MaxHealth;
            Status = definition.HasRecipes ? ProductionStatus.NoWorkers : ProductionStatus.Idle;
        }

        public int Id { get; private set; }

        public BuildingDefinition Definition { get; private set; }

        public Vec2 Position { get; internal set; }

        public float Health { get; internal set; }

        public int AssignedWorkers { get; internal set; }

        public int ActiveRecipeIndex { get; internal set; }

        /// <summary>Fortschritt des laufenden Zyklus, 0..1.</summary>
        public float CycleProgress { get; internal set; }

        /// <summary>Inputs sind gebucht und der Zyklus laeuft.</summary>
        public bool CycleRunning { get; internal set; }

        public ProductionStatus Status { get; internal set; }

        /// <summary>Welcher Input fehlt, wenn <see cref="Status"/> WaitingForInput ist.</summary>
        public ResourceType MissingInput { get; internal set; }

        public float AttackCooldown { get; internal set; }

        public BuildingType Type
        {
            get { return Definition.Type; }
        }

        public string DisplayName
        {
            get { return Definition.DisplayName; }
        }

        public bool IsAlive
        {
            get { return Health > 0f; }
        }

        public int MaxWorkers
        {
            get { return Definition.WorkerSlots; }
        }

        /// <summary>
        /// Lineare Skalierung der Leistung ueber die Besatzung: 2/2 = 100 %,
        /// 1/2 = 50 %, 0/2 = 0 %. Gebaeude ohne Besatzungsplaetze laufen immer voll.
        /// </summary>
        public float Efficiency
        {
            get
            {
                if (Definition.WorkerSlots <= 0)
                {
                    return 1f;
                }

                if (!IsAlive)
                {
                    return 0f;
                }

                return (float)AssignedWorkers / Definition.WorkerSlots;
            }
        }

        public bool IsFullyStaffed
        {
            get { return Definition.WorkerSlots <= 0 || AssignedWorkers >= Definition.WorkerSlots; }
        }

        public RecipeDefinition ActiveRecipe
        {
            get
            {
                if (!Definition.HasRecipes)
                {
                    return null;
                }

                int index = ActiveRecipeIndex;
                if (index < 0 || index >= Definition.Recipes.Length)
                {
                    index = 0;
                }

                return Definition.Recipes[index];
            }
        }

        /// <summary>
        /// Theoretischer Ausstoss bei aktueller Besatzung, fuer die Anzeige
        /// "+X Bretter/min" im Gebaeude-UI.
        /// </summary>
        public float OutputPerMinute
        {
            get
            {
                RecipeDefinition recipe = ActiveRecipe;
                if (recipe == null || recipe.CycleSeconds <= 0f
                    || recipe.Outputs == null || recipe.Outputs.Length == 0)
                {
                    return 0f;
                }

                return recipe.Outputs[0].Amount / recipe.CycleSeconds * 60f * Efficiency;
            }
        }

        public ResourceType PrimaryOutput
        {
            get
            {
                RecipeDefinition recipe = ActiveRecipe;
                if (recipe == null || recipe.Outputs == null || recipe.Outputs.Length == 0)
                {
                    return ResourceType.None;
                }

                return recipe.Outputs[0].Resource;
            }
        }

        /// <summary>Verteidigungsanlage mit Besatzung und intaktem Zustand.</summary>
        public bool CanOperate
        {
            get { return IsAlive && (Definition.WorkerSlots <= 0 || AssignedWorkers > 0); }
        }
    }
}
