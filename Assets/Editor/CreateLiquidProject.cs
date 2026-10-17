using System.IO;
using LiquidSimulator;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class CreateLiquidProject
{
    const string ScenePath = "Assets/Scenes/LiquidSandbox.unity";
    static CreateLiquidProject() => EditorApplication.delayCall += EnsureProject;
    [MenuItem("Liquid Lab/Create or open sandbox")]
    public static void EnsureProject()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (File.Exists(ScenePath)) return;
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
        EditorSceneManager.SaveScene(scene,ScenePath);
        EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene(ScenePath,true)};
        AssetDatabase.SaveAssets();
    }
}
