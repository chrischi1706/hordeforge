// Nachbildung der uGUI- und UnityEditor-APIs, die HordeForge benutzt.
// Siehe Kopfkommentar in UnityEngineStubs.cs.

using System;
using UnityEngine.SceneManagement;

namespace UnityEngine.UI
{
    public class Graphic : UnityEngine.EventSystems.UIBehaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform { get; set; }
    }

    public class MaskableGraphic : Graphic
    {
    }

    public class Image : MaskableGraphic
    {
    }

    public class Text : MaskableGraphic
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public bool supportRichText { get; set; }
    }

    public struct ColorBlock
    {
        public Color normalColor;
        public Color highlightedColor;
        public Color pressedColor;
        public Color disabledColor;
        public float colorMultiplier;
    }

    public class Selectable : UnityEngine.EventSystems.UIBehaviour
    {
        public bool interactable { get; set; }
        public Graphic targetGraphic { get; set; }
        public ColorBlock colors { get; set; }
    }

    public class Button : Selectable
    {
        public UnityEngine.Events.UnityEvent onClick { get; set; }
    }

    public class ScrollRect : UnityEngine.EventSystems.UIBehaviour
    {
        public enum MovementType
        {
            Unrestricted = 0,
            Elastic = 1,
            Clamped = 2
        }

        public bool horizontal { get; set; }
        public bool vertical { get; set; }
        public MovementType movementType { get; set; }
        public float scrollSensitivity { get; set; }
        public RectTransform viewport { get; set; }
        public RectTransform content { get; set; }
    }

    public class Mask : UnityEngine.EventSystems.UIBehaviour
    {
    }

    public class RectMask2D : UnityEngine.EventSystems.UIBehaviour
    {
    }

    public class CanvasScaler : UnityEngine.EventSystems.UIBehaviour
    {
        public enum ScaleMode
        {
            ConstantPixelSize = 0,
            ScaleWithScreenSize = 1,
            ConstantPhysicalSize = 2
        }

        public enum ScreenMatchMode
        {
            MatchWidthOrHeight = 0,
            Expand = 1,
            Shrink = 2
        }

        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
    }

    public class GraphicRaycaster : UnityEngine.EventSystems.UIBehaviour
    {
    }

    public class LayoutElement : UnityEngine.EventSystems.UIBehaviour
    {
        public float minHeight { get; set; }
        public float preferredHeight { get; set; }
        public float minWidth { get; set; }
        public float preferredWidth { get; set; }
        public bool ignoreLayout { get; set; }
    }

    public class LayoutGroup : UnityEngine.EventSystems.UIBehaviour
    {
        public RectOffset padding { get; set; }
        public TextAnchor childAlignment { get; set; }
    }

    public class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
        public bool childForceExpandWidth { get; set; }
        public bool childForceExpandHeight { get; set; }
    }

    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup
    {
    }

    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup
    {
    }

    public class ContentSizeFitter : UnityEngine.EventSystems.UIBehaviour
    {
        public enum FitMode
        {
            Unconstrained = 0,
            MinSize = 1,
            PreferredSize = 2
        }

        public FitMode horizontalFit { get; set; }
        public FitMode verticalFit { get; set; }
    }
}

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItem : Attribute
    {
        public MenuItem(string itemName)
        {
        }

        public MenuItem(string itemName, bool isValidateFunction)
        {
        }

        public MenuItem(string itemName, bool isValidateFunction, int priority)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class InitializeOnLoadMethodAttribute : Attribute
    {
    }

    public static class AssetDatabase
    {
        public static bool IsValidFolder(string path)
        {
            return false;
        }

        public static string CreateFolder(string parentFolder, string newFolderName)
        {
            return string.Empty;
        }

        public static void CreateAsset(UnityEngine.Object asset, string path)
        {
        }

        public static void SaveAssets()
        {
        }

        public static void Refresh()
        {
        }

        public static T LoadAssetAtPath<T>(string assetPath) where T : UnityEngine.Object
        {
            return default(T);
        }
    }

    public static class EditorUtility
    {
        public static void SetDirty(UnityEngine.Object target)
        {
        }
    }

    public static class EditorApplication
    {
        public delegate void CallbackFunction();

        public static CallbackFunction delayCall { get; set; }
        public static bool isCompiling { get; set; }
        public static bool isUpdating { get; set; }
    }

    public static class SessionState
    {
        public static bool GetBool(string key, bool defaultValue)
        {
            return defaultValue;
        }

        public static void SetBool(string key, bool value)
        {
        }
    }

    public class EditorBuildSettingsScene
    {
        public EditorBuildSettingsScene(string path, bool enabled)
        {
        }

        public string path { get; set; }
        public bool enabled { get; set; }
    }

    public static class EditorBuildSettings
    {
        public static EditorBuildSettingsScene[] scenes { get; set; }
    }

    public static class PlayerSettings
    {
        public static string companyName { get; set; }
        public static string productName { get; set; }
    }

    public class SerializedProperty
    {
        public UnityEngine.Object objectReferenceValue { get; set; }
    }

    public class SerializedObject
    {
        public SerializedObject(UnityEngine.Object target)
        {
        }

        public SerializedProperty FindProperty(string propertyPath)
        {
            return null;
        }

        public bool ApplyModifiedPropertiesWithoutUndo()
        {
            return false;
        }
    }
}

namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup
    {
        EmptyScene = 0,
        DefaultGameObjects = 1
    }

    public enum NewSceneMode
    {
        Single = 0,
        Additive = 1
    }

    public static class EditorSceneManager
    {
        public static Scene NewScene(NewSceneSetup setup, NewSceneMode mode)
        {
            return new Scene();
        }

        public static bool SaveScene(Scene scene, string dstScenePath)
        {
            return false;
        }

        public static Scene OpenScene(string scenePath)
        {
            return new Scene();
        }

        public static bool MarkSceneDirty(Scene scene)
        {
            return false;
        }

        public static bool SaveCurrentModifiedScenesIfUserWantsTo()
        {
            return true;
        }
    }
}
