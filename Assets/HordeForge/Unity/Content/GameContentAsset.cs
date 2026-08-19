using HordeForge.Core.Data;
using UnityEngine;

namespace HordeForge.Unity.Content
{
    /// <summary>
    /// Macht den kompletten Balancing-Datensatz im Inspector editierbar.
    ///
    /// Die Definitionen der Core-Assembly sind gewoehnliche <c>[Serializable]</c>-Klassen,
    /// deshalb kann Unity sie unveraendert serialisieren – es braucht keine Spiegelung
    /// der Felder und kein Umkopieren.
    ///
    /// Ist im <see cref="GameBootstrap"/> kein Asset hinterlegt, laeuft das Spiel mit den
    /// Werten aus <see cref="DefaultContent"/>. Ein Asset erzeugt man ueber das Menue
    /// "HordeForge / Inhalte-Asset erzeugen".
    /// </summary>
    [CreateAssetMenu(fileName = "HordeForgeContent", menuName = "HordeForge/Inhalte", order = 0)]
    public sealed class GameContentAsset : ScriptableObject
    {
        [SerializeField]
        private GameConfig _config = new GameConfig();

        public GameConfig Config
        {
            get { return _config; }
        }

        [ContextMenu("Auf Standardwerte zuruecksetzen")]
        public void ResetToDefaults()
        {
            _config = DefaultContent.CreateConfig();
        }

        /// <summary>
        /// Prueft grob, ob das Asset befuellt ist. Ein frisch angelegtes, leeres Asset
        /// wuerde die Simulation sonst beim ersten Nachschlagen scheitern lassen.
        /// </summary>
        public bool IsUsable
        {
            get
            {
                return _config != null
                       && _config.Resources != null && _config.Resources.Length > 0
                       && _config.Buildings != null && _config.Buildings.Length > 0
                       && _config.Units != null && _config.Units.Length > 0
                       && _config.Enemies != null && _config.Enemies.Length > 0
                       && _config.Items != null && _config.Items.Length > 0;
            }
        }
    }
}
