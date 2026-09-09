#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TvAnti.Editor
{
    [InitializeOnLoad]
    public static class TvAntiSetupWizard
    {
        const string ConfigPath = "Assets/TvAnti/TvAntiConfig.asset";
        const string PrefabPath = "Assets/TvAnti/TvAnti.prefab";
        const string SessionKey = "TvAnti.AutoInstalled";

        static TvAntiSetupWizard()
        {
            EditorApplication.delayCall += AutoInstallOnce;
        }

        [MenuItem("TvAnti/Install Scene + Config")]
        public static void InstallMenu()
        {
            Install(true);
        }

        [MenuItem("TvAnti/Create Runtime")]
        public static void CreateRuntimeMenu()
        {
            Install(true);
        }

        static void AutoInstallOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (SessionState.GetBool(SessionKey, false))
                return;
            SessionState.SetBool(SessionKey, true);
            Install(false);
        }

        public static void Install(bool select)
        {
            if (!AssetDatabase.IsValidFolder("Assets/TvAnti"))
                AssetDatabase.CreateFolder("Assets", "TvAnti");

            var config = LoadOrCreateConfig();
            var prefab = LoadOrCreatePrefab(config);
            var instance = EnsureSceneInstance(prefab, config);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (select && instance != null)
                Selection.activeGameObject = instance;

            Debug.Log("[TvAnti] Created Assets/TvAnti/TvAntiConfig.asset, TvAnti.prefab, and scene object.");
        }

        static TvAntiConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<TvAntiConfig>(ConfigPath);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<TvAntiConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            EditorUtility.SetDirty(config);
            return config;
        }

        static GameObject LoadOrCreatePrefab(TvAntiConfig config)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                Wire(existing, config);
                return existing;
            }

            var go = BuildObject(config);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject EnsureSceneInstance(GameObject prefab, TvAntiConfig config)
        {
            var live = Object.FindObjectOfType<TvAnti>();
            if (live != null)
            {
                Wire(live.gameObject, config);
                return live.gameObject;
            }

            var runtime = Object.FindObjectOfType<TvAntiRuntime>();
            if (runtime != null)
            {
                Wire(runtime.gameObject, config);
                return runtime.gameObject;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "TvAnti";
            Undo.RegisterCreatedObjectUndo(instance, "Install TvAnti");
            Wire(instance, config);
            return instance;
        }

        static GameObject BuildObject(TvAntiConfig config)
        {
            var go = new GameObject("TvAnti");
            go.AddComponent<TvAnti>();
            go.AddComponent<TvAntiReporter>();
            go.AddComponent<TvAntiRuntime>();
            go.AddComponent<TvAntiExtendedGuardRunner>();
            go.AddComponent<TvAnti.Networking.TvAntiPlayFab>();
            go.AddComponent<TvAnti.Networking.TvAntiPhotonAdapter>();
            Wire(go, config);
            return go;
        }

        static void Wire(GameObject go, TvAntiConfig config)
        {
            if (go.GetComponent<TvAnti>() == null)
                go.AddComponent<TvAnti>();
            if (go.GetComponent<TvAntiReporter>() == null)
                go.AddComponent<TvAntiReporter>();
            if (go.GetComponent<TvAntiRuntime>() == null)
                go.AddComponent<TvAntiRuntime>();
            if (go.GetComponent<TvAntiExtendedGuardRunner>() == null)
                go.AddComponent<TvAntiExtendedGuardRunner>();

            Assign(go.GetComponent<TvAnti>(), "config", config);
            Assign(go.GetComponent<TvAnti>(), "reporter", go.GetComponent<TvAntiReporter>());
            Assign(go.GetComponent<TvAntiReporter>(), "config", config);
            Assign(go.GetComponent<TvAntiRuntime>(), "config", config);

            var playFab = go.GetComponent<TvAnti.Networking.TvAntiPlayFab>();
            if (playFab != null)
                Assign(playFab, "config", config);

            EditorUtility.SetDirty(go);
        }

        static void Assign(Object target, string field, Object value)
        {
            if (target == null)
                return;
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
                prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
