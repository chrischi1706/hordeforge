using System.IO;
using HordeForge.Core;
using HordeForge.Core.Data;
using HordeForge.Core.Save;
using HordeForge.Unity.Content;
using HordeForge.Unity.UI;
using HordeForge.Unity.View;
using UnityEngine;

namespace HordeForge.Unity
{
    /// <summary>
    /// Einziger Einstiegspunkt der Szene.
    ///
    /// Die Szene enthaelt bewusst nur ein Objekt mit dieser Komponente – Kamera, Licht,
    /// Karte und HUD entstehen zur Laufzeit. Dadurch gibt es keine Prefabs und keine
    /// Szenenverdrahtung, die kaputtgehen koennte.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Inhalte")]
        [Tooltip("Optional. Ohne Asset laufen die Standardwerte aus DefaultContent.")]
        [SerializeField]
        private GameContentAsset _content;

        [Header("Simulation")]
        [Tooltip("Groesster Zeitschritt pro Bild. Verhindert Spruenge nach einem Hänger.")]
        [SerializeField]
        private float _maxDeltaTime = 0.1f;

        [Tooltip("0 = zufaelliger Seed bei jedem Start.")]
        [SerializeField]
        private int _seed;

        private GameSimulation _sim;
        private CameraRig _camera;
        private WorldView _world;
        private PlayerController _controller;
        private GameHud _hud;

        public GameSimulation Simulation
        {
            get { return _sim; }
        }

        private string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, "hordeforge-save.json"); }
        }

        private void Awake()
        {
            SetUpLighting();

            _sim = new GameSimulation(ResolveConfig(), _seed);
            _sim.StartNewGame();

            Transform root = transform;
            _camera = new CameraRig(root, _sim.Config.Settings.MapRadius);
            _world = new WorldView(_sim, root);
            _controller = new PlayerController(_sim, _camera, _world);
            _hud = new GameHud(_sim, _controller, root, NewGame, SaveGame, LoadGame);

            _world.Sync();
            _hud.Refresh();
        }

        private GameConfig ResolveConfig()
        {
            if (_content != null && _content.IsUsable)
            {
                return _content.Config;
            }

            if (_content != null)
            {
                Debug.LogWarning(
                    "[HordeForge] Das hinterlegte Inhalte-Asset ist leer. "
                    + "Es werden die Standardwerte verwendet. "
                    + "Ueber HordeForge / Inhalte-Asset erzeugen laesst es sich befuellen.");
            }

            return DefaultContent.CreateConfig();
        }

        private void SetUpLighting()
        {
            GameObject lightObject = new GameObject("Sun");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.None;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.50f);
        }

        private void Update()
        {
            float deltaTime = Mathf.Min(Time.deltaTime, _maxDeltaTime);

            _controller.Update(deltaTime);
            _camera.Update(deltaTime, UiBuilder.IsPointerOverUi());

            _sim.Tick(deltaTime);

            _world.Sync();
            _hud.Refresh();
        }

        // ------------------------------------------------------------------
        // Menuebefehle
        // ------------------------------------------------------------------

        private void NewGame()
        {
            _sim.StartNewGame();
            _controller.ClearSelection();
            _controller.CancelMode();
            _controller.SetStatus("Neue Partie gestartet.");
            _hud.InvalidateAll();
        }

        private void SaveGame()
        {
            try
            {
                SaveSystem.SaveToFile(_sim, SavePath);
                _sim.Log.Info("Spielstand gespeichert.");
                _controller.SetStatus("Gespeichert unter " + SavePath);
            }
            catch (IOException exception)
            {
                Debug.LogError("[HordeForge] Speichern fehlgeschlagen: " + exception.Message);
                _controller.SetStatus("Speichern fehlgeschlagen: " + exception.Message);
            }
        }

        private void LoadGame()
        {
            try
            {
                if (!SaveSystem.TryLoadFromFile(_sim, SavePath))
                {
                    _controller.SetStatus("Kein Spielstand gefunden.");
                    return;
                }

                _controller.ClearSelection();
                _controller.CancelMode();
                _hud.InvalidateAll();
                _sim.Log.Info("Spielstand geladen.");
                _controller.SetStatus("Spielstand geladen.");
            }
            catch (IOException exception)
            {
                Debug.LogError("[HordeForge] Laden fehlgeschlagen: " + exception.Message);
                _controller.SetStatus("Laden fehlgeschlagen: " + exception.Message);
            }
        }
    }
}
