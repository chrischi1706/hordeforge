// Minimal nachgebildete Unity-APIs – ausschliesslich als Kompilierhilfe.
//
// Dieses Verzeichnis liegt bewusst ausserhalb von Assets/, Unity sieht es also nie.
// Zweck: Der Unity-Layer von HordeForge laesst sich damit ohne installierten Editor
// uebersetzen. Das findet Tippfehler, falsche Signaturen und veraltete Aufrufe in die
// Core-Assembly. Es ersetzt keinen Test im echten Editor, faengt aber die Fehlerklasse
// ab, die sonst erst beim ersten Oeffnen des Projekts auffaellt.
//
// Nur die tatsaechlich benutzten Mitglieder sind vorhanden.

using System;

namespace UnityEngine
{
    public class Object
    {
        public string name;

        public static void Destroy(Object target)
        {
        }

        public static void DestroyImmediate(Object target)
        {
        }
    }

    public class Component : Object
    {
        public Transform transform { get; set; }
        public GameObject gameObject { get; set; }

        public T GetComponent<T>() where T : Component
        {
            return default(T);
        }
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
    }

    public class MonoBehaviour : Behaviour
    {
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject
        {
            return default(T);
        }
    }

    public class GameObject : Object
    {
        public GameObject()
        {
        }

        public GameObject(string name)
        {
        }

        public GameObject(string name, params Type[] components)
        {
        }

        public Transform transform { get; set; }
        public int layer { get; set; }
        public string tag { get; set; }
        public bool activeSelf { get; set; }

        public T AddComponent<T>() where T : Component
        {
            return default(T);
        }

        public T GetComponent<T>() where T : Component
        {
            return default(T);
        }

        public void SetActive(bool value)
        {
        }

        public static GameObject CreatePrimitive(PrimitiveType type)
        {
            return null;
        }
    }

    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public int childCount { get; set; }

        public void SetParent(Transform parent)
        {
        }

        public void SetParent(Transform parent, bool worldPositionStays)
        {
        }

        public Transform GetChild(int index)
        {
            return null;
        }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 sizeDelta { get; set; }
    }

    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float magnitude
        {
            get { return (float)Math.Sqrt(x * x + y * y); }
        }

        public Vector2 normalized
        {
            get { return this; }
        }

        public static Vector2 zero
        {
            get { return new Vector2(0f, 0f); }
        }

        public static Vector2 one
        {
            get { return new Vector2(1f, 1f); }
        }

        public static Vector2 operator *(Vector2 a, float b)
        {
            return new Vector2(a.x * b, a.y * b);
        }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float magnitude
        {
            get { return (float)Math.Sqrt(x * x + y * y + z * z); }
        }

        public Vector3 normalized
        {
            get { return this; }
        }

        public static Vector3 zero
        {
            get { return new Vector3(0f, 0f, 0f); }
        }

        public static Vector3 up
        {
            get { return new Vector3(0f, 1f, 0f); }
        }

        public static Vector3 back
        {
            get { return new Vector3(0f, 0f, -1f); }
        }

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3 operator *(Vector3 a, float b)
        {
            return new Vector3(a.x * b, a.y * b, a.z * b);
        }
    }

    public struct Quaternion
    {
        public static Quaternion Euler(float x, float y, float z)
        {
            return new Quaternion();
        }

        public static Vector3 operator *(Quaternion rotation, Vector3 point)
        {
            return point;
        }
    }

    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color(float r, float g, float b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = 1f;
        }

        public Color(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static Color white
        {
            get { return new Color(1f, 1f, 1f); }
        }

        public static Color Lerp(Color a, Color b, float t)
        {
            return a;
        }
    }

    public class Shader : Object
    {
        public static Shader Find(string name)
        {
            return null;
        }
    }

    public class Material : Object
    {
        public Material(Shader shader)
        {
        }

        public Color color { get; set; }
        public int renderQueue { get; set; }

        public void SetFloat(string name, float value)
        {
        }

        public void SetInt(string name, int value)
        {
        }

        public void EnableKeyword(string keyword)
        {
        }

        public void DisableKeyword(string keyword)
        {
        }
    }

    public class Renderer : Component
    {
        public Material sharedMaterial { get; set; }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public bool receiveShadows { get; set; }
    }

    public class Collider : Component
    {
    }

    public class AudioListener : Behaviour
    {
    }

    public enum CameraClearFlags
    {
        Skybox = 1,
        SolidColor = 2,
        Depth = 3,
        Nothing = 4
    }

    public class Camera : Behaviour
    {
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }

        public Ray ScreenPointToRay(Vector3 position)
        {
            return new Ray();
        }
    }

    public enum LightType
    {
        Spot = 0,
        Directional = 1,
        Point = 2
    }

    public enum LightShadows
    {
        None = 0,
        Hard = 1,
        Soft = 2
    }

    public class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public LightShadows shadows { get; set; }
    }

    public static class RenderSettings
    {
        public static Rendering.AmbientMode ambientMode { get; set; }
        public static Color ambientLight { get; set; }
    }

    public enum PrimitiveType
    {
        Sphere = 0,
        Capsule = 1,
        Cylinder = 2,
        Cube = 3,
        Plane = 4,
        Quad = 5
    }

    public struct Ray
    {
        public Vector3 GetPoint(float distance)
        {
            return Vector3.zero;
        }
    }

    public struct Plane
    {
        public Plane(Vector3 normal, Vector3 point)
        {
        }

        public bool Raycast(Ray ray, out float enter)
        {
            enter = 0f;
            return false;
        }
    }

    public static class Mathf
    {
        public static float Abs(float value)
        {
            return Math.Abs(value);
        }

        public static float Min(float a, float b)
        {
            return Math.Min(a, b);
        }

        public static float Max(float a, float b)
        {
            return Math.Max(a, b);
        }

        public static float Clamp(float value, float min, float max)
        {
            return value;
        }

        public static bool Approximately(float a, float b)
        {
            return Math.Abs(a - b) < 0.0001f;
        }

        public static int RoundToInt(float value)
        {
            return (int)Math.Round(value);
        }

        public static int CeilToInt(float value)
        {
            return (int)Math.Ceiling(value);
        }
    }

    public enum KeyCode
    {
        Space = 32,
        Escape = 27,
        A = 97,
        D = 100,
        S = 115,
        W = 119,
        LeftArrow = 276,
        RightArrow = 275,
        UpArrow = 273,
        DownArrow = 274,
        LeftShift = 304,
        RightShift = 303
    }

    public static class Input
    {
        public static Vector3 mousePosition { get; set; }
        public static Vector2 mouseScrollDelta { get; set; }

        public static bool GetKey(KeyCode key)
        {
            return false;
        }

        public static bool GetKeyDown(KeyCode key)
        {
            return false;
        }

        public static bool GetMouseButton(int button)
        {
            return false;
        }

        public static bool GetMouseButtonDown(int button)
        {
            return false;
        }

        public static bool GetMouseButtonUp(int button)
        {
            return false;
        }
    }

    public static class Screen
    {
        public static int width { get; set; }
        public static int height { get; set; }
    }

    public static class Application
    {
        public static bool isFocused { get; set; }
        public static bool isPlaying { get; set; }
        public static string persistentDataPath { get; set; }
    }

    public static class Time
    {
        public static float deltaTime { get; set; }
    }

    public static class Debug
    {
        public static void Log(object message)
        {
        }

        public static void LogWarning(object message)
        {
        }

        public static void LogError(object message)
        {
        }
    }

    public static class LayerMask
    {
        public static int NameToLayer(string layerName)
        {
            return 0;
        }
    }

    public class Font : Object
    {
    }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : Object
        {
            return default(T);
        }
    }

    public class RectOffset
    {
        public RectOffset(int left, int right, int top, int bottom)
        {
        }
    }

    public enum TextAnchor
    {
        UpperLeft = 0,
        UpperCenter = 1,
        UpperRight = 2,
        MiddleLeft = 3,
        MiddleCenter = 4,
        MiddleRight = 5,
        LowerLeft = 6,
        LowerCenter = 7,
        LowerRight = 8
    }

    public enum FontStyle
    {
        Normal = 0,
        Bold = 1,
        Italic = 2,
        BoldAndItalic = 3
    }

    public enum HorizontalWrapMode
    {
        Wrap = 0,
        Overflow = 1
    }

    public enum VerticalWrapMode
    {
        Truncate = 0,
        Overflow = 1
    }

    public enum RenderMode
    {
        ScreenSpaceOverlay = 0,
        ScreenSpaceCamera = 1,
        WorldSpace = 2
    }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeField : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class HeaderAttribute : PropertyAttribute
    {
        public HeaderAttribute(string header)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class TooltipAttribute : PropertyAttribute
    {
        public TooltipAttribute(string tooltip)
        {
        }
    }

    public class PropertyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DisallowMultipleComponent : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CreateAssetMenuAttribute : Attribute
    {
        public string fileName { get; set; }
        public string menuName { get; set; }
        public int order { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ContextMenu : Attribute
    {
        public ContextMenu(string itemName)
        {
        }
    }
}

namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode
    {
        Off = 0,
        On = 1,
        TwoSided = 2,
        ShadowsOnly = 3
    }

    public enum BlendMode
    {
        Zero = 0,
        One = 1,
        SrcAlpha = 5,
        OneMinusSrcAlpha = 10
    }

    public enum AmbientMode
    {
        Skybox = 0,
        Trilight = 1,
        Flat = 3,
        Custom = 4
    }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();

    public class UnityEvent
    {
        public void AddListener(UnityAction call)
        {
        }

        public void RemoveAllListeners()
        {
        }
    }
}

namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour
    {
    }

    public class EventSystem : UIBehaviour
    {
        public static EventSystem current { get; set; }

        public bool IsPointerOverGameObject()
        {
            return false;
        }
    }

    public class BaseInputModule : UIBehaviour
    {
    }

    public class PointerInputModule : BaseInputModule
    {
    }

    public class StandaloneInputModule : PointerInputModule
    {
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        public string name { get; set; }
        public string path { get; set; }
    }
}
