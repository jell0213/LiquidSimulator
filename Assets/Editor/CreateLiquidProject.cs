using System.IO;
using LiquidSimulator;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CreateLiquidProject
{
    const string ScenePath = "Assets/Scenes/LiquidSandbox.unity";
    static CreateLiquidProject()
    {
        EditorApplication.delayCall += EnsureProject;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }
    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path == ScenePath) Show2DView();
    }
    static void Show2DView()
    {
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null) return;
        view.in2DMode = true;
        view.LookAt(Vector3.zero, Quaternion.identity, 6f, true, true);
        view.Repaint();
    }
    static void EnsureCamera(Scene scene)
    {
        Camera camera = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Camera candidate in root.GetComponentsInChildren<Camera>(true))
                if (candidate.CompareTag("MainCamera")) { camera = candidate; break; }
        if (camera == null)
        {
            var obj = new GameObject("Main Camera", typeof(Camera));
            Undo.RegisterCreatedObjectUndo(obj, "Create 2D camera");
            SceneManager.MoveGameObjectToScene(obj, scene);
            obj.tag = "MainCamera";
            camera = obj.GetComponent<Camera>();
        }
        Undo.RecordObject(camera, "Configure 2D camera");
        Undo.RecordObject(camera.transform, "Position 2D camera");
        camera.orthographic = true;
        camera.orthographicSize = 5.8f;
        camera.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.045f, 0.09f);
        EditorSceneManager.MarkSceneDirty(scene);
    }
    [MenuItem("Liquid Lab/Open sandbox in 2D")]
    public static void OpenSandboxIn2D()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists(ScenePath)) { EnsureProject(); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        EnsureCamera(scene);
        Show2DView();
        // Existing scene changes remain reviewable and undoable; save explicitly with Ctrl/Cmd+S.
    }
    public static void EnsureProject()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (File.Exists(ScenePath))
        {
            if (SceneManager.GetActiveScene().path == ScenePath) Show2DView();
            return;
        }
        Directory.CreateDirectory("Assets/Settings"); Directory.CreateDirectory("Assets/Scenes");
        var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>("Assets/Settings/LiquidRenderer2D.asset");
        if (renderer == null) { renderer = ScriptableObject.CreateInstance<Renderer2DData>(); AssetDatabase.CreateAsset(renderer,"Assets/Settings/LiquidRenderer2D.asset"); }
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/LiquidURP.asset");
        if (pipeline == null) { pipeline = UniversalRenderPipelineAsset.Create(renderer); AssetDatabase.CreateAsset(pipeline,"Assets/Settings/LiquidURP.asset"); }
        GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
        // This project intentionally uses the built-in input backend for mouse and uGUI.
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input = settings.FindProperty("activeInputHandler");
        if (input != null) { input.intValue = 0; settings.ApplyModifiedPropertiesWithoutUndo(); }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Liquid Lab", typeof(LiquidSandbox));
        EnsureCamera(scene);
        Show2DView();
        EditorSceneManager.SaveScene(scene,ScenePath);
        EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene(ScenePath,true)};
        AssetDatabase.SaveAssets();
    }
}
