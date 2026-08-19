using System.IO;
using HordeForge.Unity;
using HordeForge.Unity.Content;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HordeForge.EditorTools
{
    /// <summary>
    /// Erzeugt Szene und Inhalte-Asset durch Unity selbst.
    ///
    /// Der Grund: eine Szenendatei oder ein ScriptableObject von Hand als YAML zu
    /// schreiben ist fehleranfaellig, weil dort Guids und interne Formatversionen
    /// stimmen muessen. Ein Editor-Skript erzeugt beides garantiert korrekt.
    /// </summary>
    public static class ProjectSetup
    {
        private const string DataFolder = "Assets/HordeForge/Data";
        private const string ContentPath = DataFolder + "/HordeForgeContent.asset";
        private const string SceneFolder = "Assets/HordeForge/Scenes";
        private const string ScenePath = SceneFolder + "/HordeForge.unity";

        private const string AutoSetupDoneKey = "HordeForge.AutoSetupDone";

        [MenuItem("HordeForge/Projekt einrichten (Szene und Inhalte)", false, 0)]
        public static void SetUpProject()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            GameContentAsset content = CreateOrUpdateContentAsset(true);
            CreateScene(content);
            RegisterSceneInBuildSettings();
            ApplyPlayerSettings();

            EditorSceneManager.OpenScene(ScenePath);

            Debug.Log(
                "[HordeForge] Einrichtung fertig.\n"
                + "Szene: " + ScenePath + "\n"
                + "Inhalte: " + ContentPath + "\n"
                + "Auf Play druecken zum Spielen.");
        }

        [MenuItem("HordeForge/Szene oeffnen", false, 1)]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath))
            {
                SetUpProject();
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
        }

        [MenuItem("HordeForge/Inhalte-Asset auf Standardwerte zuruecksetzen", false, 20)]
        public static void ResetContentAsset()
        {
            CreateOrUpdateContentAsset(true);
            Debug.Log("[HordeForge] Inhalte-Asset auf die Werte aus DefaultContent gesetzt.");
        }

        // ------------------------------------------------------------------

        private static GameContentAsset CreateOrUpdateContentAsset(bool resetValues)
        {
            EnsureFolder(DataFolder);

            GameContentAsset content =
                AssetDatabase.LoadAssetAtPath<GameContentAsset>(ContentPath);

            bool created = false;
            if (content == null)
            {
                content = ScriptableObject.CreateInstance<GameContentAsset>();
                AssetDatabase.CreateAsset(content, ContentPath);
                created = true;
            }

            if (created || resetValues || !content.IsUsable)
            {
                content.ResetToDefaults();
                EditorUtility.SetDirty(content);
            }

            AssetDatabase.SaveAssets();
            return content;
        }

        private static void CreateScene(GameContentAsset content)
        {
            EnsureFolder(SceneFolder);

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject bootstrapObject = new GameObject("HordeForge");
            GameBootstrap bootstrap = bootstrapObject.AddComponent<GameBootstrap>();

            // Das Feld ist privat, damit es zur Laufzeit niemand umbiegt – im Editor
            // wird es ueber SerializedObject gesetzt.
            SerializedObject serialized = new SerializedObject(bootstrap);
            SerializedProperty contentProperty = serialized.FindProperty("_content");
            if (contentProperty != null)
            {
                contentProperty.objectReferenceValue = content;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void RegisterSceneInBuildSettings()
        {
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;

            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].path == ScenePath)
                {
                    return;
                }
            }

            EditorBuildSettingsScene[] updated =
                new EditorBuildSettingsScene[existing.Length + 1];
            existing.CopyTo(updated, 0);
            updated[existing.Length] = new EditorBuildSettingsScene(ScenePath, true);

            EditorBuildSettings.scenes = updated;
        }

        private static void ApplyPlayerSettings()
        {
            // Bestimmt unter Linux den Pfad von Application.persistentDataPath.
            PlayerSettings.companyName = "HordeForge";
            PlayerSettings.productName = "HordeForge";
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        // ------------------------------------------------------------------
        // Einmalige Einrichtung beim ersten Oeffnen
        // ------------------------------------------------------------------

        [InitializeOnLoadMethod]
        private static void ScheduleAutoSetup()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        /// <summary>
        /// Richtet ein frisch geklontes Projekt beim ersten Oeffnen selbst ein, damit
        /// nach dem Klonen sofort auf Play gedrueckt werden kann. Laeuft nur, solange es
        /// noch keine Szene gibt, und fasst eine bestehende Einrichtung nie an.
        /// </summary>
        private static void TryAutoSetup()
        {
            if (SessionState.GetBool(AutoSetupDoneKey, false))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutoSetup;
                return;
            }

            SessionState.SetBool(AutoSetupDoneKey, true);

            if (File.Exists(ScenePath) || Application.isPlaying)
            {
                return;
            }

            try
            {
                GameContentAsset content = CreateOrUpdateContentAsset(false);
                CreateScene(content);
                RegisterSceneInBuildSettings();
                ApplyPlayerSettings();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[HordeForge] Szene und Inhalte wurden angelegt. "
                    + "Die Szene " + ScenePath + " ist geoeffnet – auf Play druecken.");
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(
                    "[HordeForge] Automatische Einrichtung fehlgeschlagen: " + exception.Message
                    + "\nBitte einmal HordeForge / Projekt einrichten aufrufen.");
            }
        }
    }
}
