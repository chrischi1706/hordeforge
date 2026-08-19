using UnityEngine;

namespace HordeForge.Unity.View
{
    /// <summary>
    /// Schraeg von oben blickende Strategiekamera mit Maus- und Tastatursteuerung.
    ///
    /// Der Blickpunkt liegt auf dem Boden, die Kamera haengt in einem festen Winkel
    /// dahinter. Zoomen aendert nur den Abstand – dadurch bleibt die Perspektive
    /// stabil und man kann weit genug herauszoomen, um die ganze Verteidigung zu sehen.
    /// </summary>
    public sealed class CameraRig
    {
        private const float Pitch = 55f;
        private const float MinDistance = 22f;
        private const float MaxDistance = 165f;
        private const float ZoomSpeed = 14f;
        private const float KeyboardPanSpeed = 22f;
        private const float EdgeScrollMargin = 8f;

        private readonly Transform _pivot;
        private readonly float _mapRadius;

        private float _distance = 95f;
        private Vector3 _dragOrigin;
        private bool _dragging;

        public CameraRig(Transform root, float mapRadius)
        {
            _mapRadius = mapRadius;

            GameObject pivotObject = new GameObject("CameraPivot");
            pivotObject.transform.SetParent(root, false);
            _pivot = pivotObject.transform;

            GameObject cameraObject = new GameObject("MainCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(_pivot, false);

            Camera = cameraObject.AddComponent<Camera>();
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = new Color(0.09f, 0.11f, 0.14f);
            Camera.fieldOfView = 55f;
            Camera.nearClipPlane = 0.5f;
            Camera.farClipPlane = 600f;

            cameraObject.AddComponent<AudioListener>();

            Apply();
        }

        public Camera Camera { get; private set; }

        public Vector3 Focus
        {
            get { return _pivot.position; }
        }

        public void Update(float deltaTime, bool pointerOverUi)
        {
            HandleZoom(pointerOverUi);
            HandleKeyboardPan(deltaTime);
            HandleDragPan();
            Apply();
        }

        public void FocusOn(Vector3 worldPosition)
        {
            _pivot.position = ClampToMap(new Vector3(worldPosition.x, 0f, worldPosition.z));
            Apply();
        }

        private void HandleZoom(bool pointerOverUi)
        {
            if (pointerOverUi)
            {
                return;
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f)
            {
                return;
            }

            // Nah dran feiner zoomen als weit draussen.
            _distance = Mathf.Clamp(
                _distance - scroll * ZoomSpeed * (_distance / 80f + 0.4f),
                MinDistance,
                MaxDistance);
        }

        private void HandleKeyboardPan(float deltaTime)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                horizontal -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                horizontal += 1f;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                vertical -= 1f;
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                vertical += 1f;
            }

            ApplyEdgeScroll(ref horizontal, ref vertical);

            if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f))
            {
                return;
            }

            // Weiter herausgezoomt heisst schneller schwenken.
            float speed = KeyboardPanSpeed * (_distance / 60f) * deltaTime;
            Vector3 move = new Vector3(horizontal, 0f, vertical).normalized * speed;
            _pivot.position = ClampToMap(_pivot.position + move);
        }

        private void ApplyEdgeScroll(ref float horizontal, ref float vertical)
        {
            if (!Application.isFocused)
            {
                return;
            }

            Vector3 mouse = Input.mousePosition;
            if (mouse.x < 0f || mouse.y < 0f || mouse.x > Screen.width || mouse.y > Screen.height)
            {
                return;
            }

            if (mouse.x <= EdgeScrollMargin)
            {
                horizontal -= 1f;
            }
            else if (mouse.x >= Screen.width - EdgeScrollMargin)
            {
                horizontal += 1f;
            }

            if (mouse.y <= EdgeScrollMargin)
            {
                vertical -= 1f;
            }
            else if (mouse.y >= Screen.height - EdgeScrollMargin)
            {
                vertical += 1f;
            }
        }

        private void HandleDragPan()
        {
            // Mittlere Maustaste schiebt die Karte unter dem Cursor mit.
            if (Input.GetMouseButtonDown(2))
            {
                Vector3 origin;
                if (TryGetGroundPoint(Input.mousePosition, out origin))
                {
                    _dragOrigin = origin;
                    _dragging = true;
                }
            }
            else if (Input.GetMouseButtonUp(2))
            {
                _dragging = false;
            }

            if (!_dragging || !Input.GetMouseButton(2))
            {
                return;
            }

            Vector3 current;
            if (!TryGetGroundPoint(Input.mousePosition, out current))
            {
                return;
            }

            _pivot.position = ClampToMap(_pivot.position + (_dragOrigin - current));
        }

        /// <summary>
        /// Schnittpunkt des Mausstrahls mit der Bodenebene. Ohne Collider, damit
        /// Einheiten und Gebaeude die Eingabe nicht abfangen.
        /// </summary>
        public bool TryGetGroundPoint(Vector3 screenPosition, out Vector3 groundPoint)
        {
            groundPoint = Vector3.zero;

            Ray ray = Camera.ScreenPointToRay(screenPosition);
            Plane ground = new Plane(Vector3.up, Vector3.zero);

            float distance;
            if (!ground.Raycast(ray, out distance))
            {
                return false;
            }

            groundPoint = ray.GetPoint(distance);
            return true;
        }

        private Vector3 ClampToMap(Vector3 position)
        {
            Vector2 flat = new Vector2(position.x, position.z);
            float limit = _mapRadius + 10f;
            if (flat.magnitude > limit)
            {
                flat = flat.normalized * limit;
            }

            return new Vector3(flat.x, 0f, flat.y);
        }

        private void Apply()
        {
            Quaternion rotation = Quaternion.Euler(Pitch, 0f, 0f);
            Camera.transform.localRotation = rotation;
            Camera.transform.localPosition = rotation * Vector3.back * _distance;
        }
    }
}
