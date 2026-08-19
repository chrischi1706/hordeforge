using HordeForge.Core;
using HordeForge.Core.Buildings;
using HordeForge.Core.Data;
using HordeForge.Core.Military;

namespace HordeForge.Unity.UI
{
    /// <summary>
    /// Uebersetzt Ergebniscodes der Simulation in Saetze, die im HUD stehen koennen.
    /// Damit bleibt die Core-Assembly frei von Anzeigetexten.
    /// </summary>
    public static class Texts
    {
        public static string Describe(PlacementResult result)
        {
            switch (result)
            {
                case PlacementResult.Success:
                    return "Gebaut.";
                case PlacementResult.OutOfBounds:
                    return "Das liegt ausserhalb der Karte.";
                case PlacementResult.Overlapping:
                    return "Zu dicht an einem anderen Gebaeude.";
                case PlacementResult.NotAffordable:
                    return "Nicht genug Ressourcen.";
                case PlacementResult.WrongTerrain:
                    return "Dieses Gebaeude braucht das passende Rohstoffgebiet.";
                default:
                    return "Dieses Gebaeude gibt es nicht.";
            }
        }

        public static string Describe(RecruitResult result)
        {
            switch (result)
            {
                case RecruitResult.Success:
                    return "Aufgestellt.";
                case RecruitResult.NotEnoughCitizens:
                    return "Zu wenige freie Buerger.";
                case RecruitResult.NotEnoughEquipment:
                    return "Es fehlt Ausruestung.";
                case RecruitResult.NotEnoughFood:
                    return "Es fehlt Nahrung.";
                case RecruitResult.InvalidCount:
                    return "Ungueltige Anzahl.";
                default:
                    return "Diese Einheit gibt es nicht.";
            }
        }

        public static string Describe(ProductionStatus status, ResourceType missingInput, GameConfig config)
        {
            switch (status)
            {
                case ProductionStatus.Active:
                    return "AKTIV";
                case ProductionStatus.Understaffed:
                    return "UNTERBESETZT";
                case ProductionStatus.NoWorkers:
                    return "KEINE ARBEITER";
                case ProductionStatus.WaitingForInput:
                    return missingInput == ResourceType.None
                        ? "WARTET AUF MATERIAL"
                        : "WARTET AUF " + config.GetResource(missingInput).DisplayName.ToUpperInvariant();
                case ProductionStatus.Inoperable:
                    return "AUSSER BETRIEB";
                default:
                    return "BEREIT";
            }
        }

        public static string Describe(WavePhase phase)
        {
            switch (phase)
            {
                case WavePhase.Preparation:
                    return "Vorbereitung";
                case WavePhase.Active:
                    return "Horde";
                default:
                    return "Niederlage";
            }
        }

        public static string Costs(ResourceAmount[] costs, GameConfig config)
        {
            if (costs == null || costs.Length == 0)
            {
                return "kostenlos";
            }

            string result = "";
            for (int i = 0; i < costs.Length; i++)
            {
                if (i > 0)
                {
                    result += ", ";
                }

                result += costs[i].Amount + " " + config.GetResource(costs[i].Resource).DisplayName;
            }

            return result;
        }

        public static string Timer(float seconds)
        {
            if (seconds < 0f)
            {
                seconds = 0f;
            }

            int total = (int)seconds;
            return (total / 60) + ":" + (total % 60).ToString("00");
        }
    }
}
