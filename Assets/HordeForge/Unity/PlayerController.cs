using HordeForge.Core;
using HordeForge.Core.Buildings;
using HordeForge.Core.Data;
using HordeForge.Core.Military;
using HordeForge.Unity.UI;
using HordeForge.Unity.View;
using UnityEngine;

namespace HordeForge.Unity
{
    public enum InteractionMode
    {
        Select = 0,
        PlaceBuilding = 1,
        PlaceSquad = 2,
        MoveSquad = 3
    }

    /// <summary>
    /// Maus- und Tastatursteuerung: auswaehlen, bauen, Truppen setzen und verlegen.
    ///
    /// Klicks werden gegen eine mathematische Bodenebene gerechnet und anschliessend
    /// wird das naechste Objekt gesucht. Das kommt ohne Collider aus und trifft auch
    /// dann zuverlaessig, wenn Gebaeude dicht beieinander stehen.
    /// </summary>
    public sealed class PlayerController
    {
        private const float SquadPickRadius = 4f;
        private const float BuildingPickPadding = 1.5f;

        private readonly GameSimulation _sim;
        private readonly CameraRig _camera;
        private readonly WorldView _world;

        private GameObject _placementGhost;
        private float _statusTimer;

        public PlayerController(GameSimulation simulation, CameraRig camera, WorldView world)
        {
            _sim = simulation;
            _camera = camera;
            _world = world;
        }

        public InteractionMode Mode { get; private set; }

        public BuildingType PendingBuilding { get; private set; }

        public Building SelectedBuilding { get; private set; }

        public Squad SelectedSquad { get; private set; }

        public string StatusMessage { get; private set; }

        public void Update(float deltaTime)
        {
            DropDeadSelection();
            HandleKeyboard();

            bool overUi = UiBuilder.IsPointerOverUi();
            if (!overUi && Input.GetMouseButtonDown(0))
            {
                HandlePrimaryClick();
            }

            if (Input.GetMouseButtonDown(1))
            {
                CancelMode();
            }

            UpdatePlacementGhost(overUi);
            UpdateSelectionMarker();

            if (_statusTimer > 0f)
            {
                _statusTimer -= deltaTime;
                if (_statusTimer <= 0f)
                {
                    StatusMessage = null;
                }
            }
        }

        // ------------------------------------------------------------------
        // Modi
        // ------------------------------------------------------------------

        public void BeginBuildingPlacement(BuildingType type)
        {
            Mode = InteractionMode.PlaceBuilding;
            PendingBuilding = type;
            SetStatus(_sim.Config.GetBuilding(type).DisplayName
                      + " platzieren – Linksklick setzt, Rechtsklick bricht ab.");
        }

        public void BeginSquadPlacement()
        {
            Mode = InteractionMode.PlaceSquad;
            SetStatus("Position fuer die neue Truppe waehlen.");
        }

        public void BeginSquadMove()
        {
            if (SelectedSquad == null)
            {
                return;
            }

            Mode = InteractionMode.MoveSquad;
            SetStatus("Neue Verteidigungsposition fuer " + SelectedSquad.Name + " waehlen.");
        }

        public void CancelMode()
        {
            if (Mode == InteractionMode.Select)
            {
                ClearSelection();
                return;
            }

            Mode = InteractionMode.Select;
            PendingBuilding = BuildingType.None;
            SetStatus(null);
        }

        public void ClearSelection()
        {
            SelectedBuilding = null;
            SelectedSquad = null;
        }

        public void SelectSquad(Squad squad)
        {
            SelectedSquad = squad;
            SelectedBuilding = null;
        }

        public void SetStatus(string message, float seconds = 4f)
        {
            StatusMessage = message;
            _statusTimer = message == null ? 0f : seconds;
        }

        // ------------------------------------------------------------------
        // Eingabe
        // ------------------------------------------------------------------

        private void HandleKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelMode();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _sim.Waves.StartWaveNow();
            }
        }

        private void HandlePrimaryClick()
        {
            Vector3 groundPoint;
            if (!_camera.TryGetGroundPoint(Input.mousePosition, out groundPoint))
            {
                return;
            }

            Vec2 point = Visuals.ToMap(groundPoint);

            switch (Mode)
            {
                case InteractionMode.PlaceBuilding:
                    PlaceBuilding(point);
                    break;
                case InteractionMode.PlaceSquad:
                    PlaceSquad(point);
                    break;
                case InteractionMode.MoveSquad:
                    MoveSquad(point);
                    break;
                default:
                    SelectAt(point);
                    break;
            }
        }

        private void PlaceBuilding(Vec2 point)
        {
            Building placed;
            PlacementResult result = _sim.Buildings.TryPlace(PendingBuilding, point, out placed);

            SetStatus(Texts.Describe(result));

            if (result != PlacementResult.Success)
            {
                return;
            }

            SelectedBuilding = placed;
            SelectedSquad = null;

            // Mit gedrueckter Umschalttaste laesst sich eine Mauer am Stueck ziehen.
            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                Mode = InteractionMode.Select;
                PendingBuilding = BuildingType.None;
            }
        }

        private void PlaceSquad(Vec2 point)
        {
            Squad squad = _sim.Squads.CreateEmptySquad(ClampToMap(point));
            SelectSquad(squad);
            Mode = InteractionMode.Select;
            SetStatus(squad.Name + " aufgestellt. Jetzt Einheiten hinzufuegen.");
        }

        private void MoveSquad(Vec2 point)
        {
            if (SelectedSquad == null)
            {
                Mode = InteractionMode.Select;
                return;
            }

            _sim.Squads.MoveSquad(SelectedSquad, ClampToMap(point));
            Mode = InteractionMode.Select;
            SetStatus(SelectedSquad.Name + " bezieht Stellung.");
        }

        private Vec2 ClampToMap(Vec2 point)
        {
            float radius = _sim.Config.Settings.MapRadius;
            return point.Magnitude > radius
                ? Vec2.ClampToRadius(point, Vec2.Zero, radius)
                : point;
        }

        /// <summary>
        /// Waehlt das naechste Gebaeude oder die naechste Truppe. Was naeher dran ist,
        /// gewinnt; daneben geklickt hebt die Auswahl auf.
        /// </summary>
        private void SelectAt(Vec2 point)
        {
            Building bestBuilding = null;
            float bestBuildingDistance = float.MaxValue;

            for (int i = 0; i < _sim.Buildings.Buildings.Count; i++)
            {
                Building candidate = _sim.Buildings.Buildings[i];
                float distance = Vec2.Distance(point, candidate.Position);
                if (distance <= candidate.Definition.Radius + BuildingPickPadding
                    && distance < bestBuildingDistance)
                {
                    bestBuildingDistance = distance;
                    bestBuilding = candidate;
                }
            }

            Squad bestSquad = null;
            float bestSquadDistance = float.MaxValue;

            for (int i = 0; i < _sim.Squads.Squads.Count; i++)
            {
                Squad candidate = _sim.Squads.Squads[i];
                float distance = Vec2.Distance(point, candidate.Position);
                if (distance <= SquadPickRadius && distance < bestSquadDistance)
                {
                    bestSquadDistance = distance;
                    bestSquad = candidate;
                }
            }

            if (bestSquad != null && bestSquadDistance <= bestBuildingDistance)
            {
                SelectSquad(bestSquad);
                return;
            }

            if (bestBuilding != null)
            {
                SelectedBuilding = bestBuilding;
                SelectedSquad = null;
                return;
            }

            ClearSelection();
        }

        /// <summary>Zerstoerte oder aufgeloeste Auswahl darf nicht haengen bleiben.</summary>
        private void DropDeadSelection()
        {
            if (SelectedBuilding != null && !SelectedBuilding.IsAlive)
            {
                SelectedBuilding = null;
            }

            if (SelectedBuilding != null && _sim.Buildings.GetById(SelectedBuilding.Id) == null)
            {
                SelectedBuilding = null;
            }

            if (SelectedSquad != null && _sim.Squads.GetById(SelectedSquad.Id) == null)
            {
                SelectedSquad = null;
            }
        }

        // ------------------------------------------------------------------
        // Darstellung
        // ------------------------------------------------------------------

        private void UpdateSelectionMarker()
        {
            if (SelectedBuilding != null)
            {
                _world.ShowSelection(SelectedBuilding.Position, SelectedBuilding.Definition.Radius);
            }
            else if (SelectedSquad != null)
            {
                _world.ShowSelection(SelectedSquad.Position, 3.5f);
            }
            else
            {
                _world.HideSelection();
            }
        }

        private void UpdatePlacementGhost(bool pointerOverUi)
        {
            bool placing = Mode == InteractionMode.PlaceBuilding
                           || Mode == InteractionMode.PlaceSquad
                           || Mode == InteractionMode.MoveSquad;

            if (!placing || pointerOverUi)
            {
                if (_placementGhost != null)
                {
                    _placementGhost.SetActive(false);
                }

                return;
            }

            Vector3 groundPoint;
            if (!_camera.TryGetGroundPoint(Input.mousePosition, out groundPoint))
            {
                return;
            }

            EnsureGhost();
            _placementGhost.SetActive(true);

            Vec2 point = Visuals.ToMap(groundPoint);
            bool valid = true;
            float radius = 3.5f;

            if (Mode == InteractionMode.PlaceBuilding)
            {
                BuildingDefinition definition = _sim.Config.GetBuilding(PendingBuilding);
                radius = definition.Radius;
                valid = _sim.Buildings.CanPlace(PendingBuilding, point) == PlacementResult.Success;
            }
            else
            {
                valid = point.Magnitude <= _sim.Config.Settings.MapRadius;
            }

            _placementGhost.transform.localPosition = new Vector3(point.X, 0.08f, point.Y);
            _placementGhost.transform.localScale = new Vector3(radius * 2.2f, 0.02f, radius * 2.2f);
            Visuals.SetColor(
                _placementGhost, valid ? Visuals.PlacementValid : Visuals.PlacementInvalid);
        }

        private void EnsureGhost()
        {
            if (_placementGhost != null)
            {
                return;
            }

            _placementGhost = Visuals.CreatePrimitive(
                PrimitiveType.Cylinder, null, "PlacementGhost",
                Vector3.zero, new Vector3(4f, 0.02f, 4f), Visuals.PlacementValid);
        }
    }
}
