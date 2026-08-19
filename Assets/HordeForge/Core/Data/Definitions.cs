using System;

namespace HordeForge.Core.Data
{
    /// <summary>
    /// Alle Definitionen sind schlichte <c>[Serializable]</c>-Klassen mit
    /// oeffentlichen Feldern. Dadurch kann Unity sie unveraendert in
    /// ScriptableObjects serialisieren, waehrend die Core-Assembly selbst
    /// vollstaendig Unity-frei bleibt.
    /// </summary>
    [Serializable]
    public class ResourceDefinition
    {
        public ResourceType Type;
        public string DisplayName;
        public ResourceCategory Category;

        /// <summary>
        /// Wieviel Nahrung eine Einheit dieser Ressource deckt. 0 = keine Nahrung.
        /// Brot saettigt mehr als Fisch, deshalb datengetrieben statt fest verdrahtet.
        /// </summary>
        public int FoodValue;

        public ResourceDefinition()
        {
        }

        public ResourceDefinition(
            ResourceType type, string displayName, ResourceCategory category, int foodValue = 0)
        {
            Type = type;
            DisplayName = displayName;
            Category = category;
            FoodValue = foodValue;
        }

        public bool IsFood
        {
            get { return FoodValue > 0; }
        }
    }

    /// <summary>
    /// Ein Produktionsrezept. Gebaeude koennen mehrere besitzen; der Spieler waehlt
    /// das aktive aus. Das loest sowohl die Spinnerei (Baumwolle ODER Wolle) als auch
    /// die Schmiede (Speer/Schwert/Schild/...) ohne Sonderfaelle im Code.
    /// </summary>
    [Serializable]
    public class RecipeDefinition
    {
        public string DisplayName;
        public ResourceAmount[] Inputs;
        public ResourceAmount[] Outputs;

        /// <summary>Dauer eines Zyklus bei voller Besetzung, in Sekunden.</summary>
        public float CycleSeconds;

        public RecipeDefinition()
        {
            Inputs = ResourceAmountExtensions.Empty;
            Outputs = ResourceAmountExtensions.Empty;
        }

        public RecipeDefinition(
            string displayName, float cycleSeconds, ResourceAmount[] inputs, ResourceAmount[] outputs)
        {
            DisplayName = displayName;
            CycleSeconds = cycleSeconds;
            Inputs = inputs ?? ResourceAmountExtensions.Empty;
            Outputs = outputs ?? ResourceAmountExtensions.Empty;
        }
    }

    [Serializable]
    public class BuildingDefinition
    {
        public BuildingType Type;
        public string DisplayName;
        public BuildingCategory Category;

        public ResourceAmount[] BuildCost;

        /// <summary>Arbeits- bzw. Besatzungsplaetze. 0 = laeuft ohne Bevoelkerung.</summary>
        public int WorkerSlots;

        public int MaxHealth;

        /// <summary>Grundflaeche und Trefferradius in Weltmetern.</summary>
        public float Radius;

        public RecipeDefinition[] Recipes;

        // --- Nur fuer BuildingCategory.Defense relevant --------------------
        public float AttackDamage;
        public float AttackRange;
        public float AttackInterval;
        public ResourceType AmmoResource;
        public int AmmoPerShot;

        public BuildingDefinition()
        {
            BuildCost = ResourceAmountExtensions.Empty;
            Recipes = new RecipeDefinition[0];
            AmmoResource = ResourceType.None;
            Radius = 1.5f;
        }

        public bool HasRecipes
        {
            get { return Recipes != null && Recipes.Length > 0; }
        }

        public bool IsCore
        {
            get { return Category == BuildingCategory.Core; }
        }

        public bool CanAttack
        {
            get { return AttackDamage > 0f && AttackRange > 0f; }
        }

        /// <summary>
        /// Verteidigungsanlagen und Produktionsgebaeude binden Bevoelkerung; Mauern nicht.
        /// </summary>
        public bool NeedsCrew
        {
            get { return WorkerSlots > 0; }
        }
    }

    [Serializable]
    public class UnitDefinition
    {
        public UnitType Type;
        public string DisplayName;

        public int MaxHealth;
        public float AttackDamage;
        public float AttackRange;
        public float AttackInterval;
        public float MoveSpeed;

        /// <summary>
        /// Wie weit die Einheit ihren Formationsplatz verlassen darf, um einen Gegner
        /// zu erreichen. Haelt Nahkaempfer nuetzlich, ohne dass Truppen davonlaufen.
        /// </summary>
        public float LeashRadius;

        public ResourceType AmmoResource;
        public int AmmoPerShot;

        /// <summary>Ausruestung und Munition, die beim Aufstellen verbraucht werden.</summary>
        public ResourceAmount[] RecruitCost;

        /// <summary>
        /// Nahrungsbedarf beim Aufstellen. Wird aus dem gemeinsamen Nahrungsvorrat
        /// gedeckt (Brot oder Fisch), deshalb kein fester Ressourcentyp.
        /// </summary>
        public int FoodCost;

        public UnitDefinition()
        {
            RecruitCost = ResourceAmountExtensions.Empty;
            AmmoResource = ResourceType.None;
        }

        public bool IsRanged
        {
            get { return AmmoResource != ResourceType.None && AmmoPerShot > 0; }
        }
    }

    [Serializable]
    public class EnemyDefinition
    {
        public EnemyType Type;
        public string DisplayName;

        public int MaxHealth;
        public float MoveSpeed;
        public float AttackDamage;
        public float AttackInterval;
        public float AttackRange;

        /// <summary>Ork-Krieger und Trolle sind vor allem fuer Gebaeude gefaehrlich.</summary>
        public float BuildingDamageMultiplier;

        /// <summary>Preis im Wellenbudget.</summary>
        public int BudgetCost;

        public float Radius;

        public EnemyDefinition()
        {
            BuildingDamageMultiplier = 1f;
            Radius = 0.6f;
        }
    }

    [Serializable]
    public class ItemDefinition
    {
        public ItemType Type;
        public string DisplayName;

        public ItemDefinition()
        {
        }

        public ItemDefinition(ItemType type, string displayName)
        {
            Type = type;
            DisplayName = displayName;
        }
    }
}
