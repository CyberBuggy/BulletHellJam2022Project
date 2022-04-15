using System.IO;
using System.Linq;
using UnityEngine;
using System.Reflection;
using BulletFury.Rendering;
using UnityEditor;
using UnityEngine.Rendering;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.Rendering.Universal;
#else
using UnityEngine.Experimental.Rendering.LightweightPipeline;
#endif
public class SetupWindow : EditorWindow
{
    private static bool _hasPipeline;
    private static bool _hasRenderFeature;
    private static ScriptableRendererData _scriptableRenderData;

    private static string Path => $"{Application.persistentDataPath}/bulletfuryinit";

    [InitializeOnLoadMethod]
    public static void Init()
    {
        if (File.Exists(Path)) return;
        File.WriteAllText(Path, "initialised");
        _hasPipeline = false;
        _hasRenderFeature = false;
        var rp = GraphicsSettings.renderPipelineAsset;
#if UNITY_2019_1_OR_NEWER
        if (rp != null && rp is UniversalRenderPipelineAsset pipeline)
        {
            _hasPipeline = true;
            var propertyInfo = pipeline.GetType()
                .GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
            _scriptableRenderData = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[0];

            if (_scriptableRenderData != null &&
                _scriptableRenderData.rendererFeatures.Any(i => i is BulletFuryRenderFeature))
            {
                _hasRenderFeature = true;
            }
        }
#else
        if (rp != null && rp is LightweightRenderPipelineAsset pipeline) 
        {
            _hasPipeline = true;
            var propertyInfo =
 pipeline.GetType(  ).GetField( "m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic );
            _scriptableRenderData = ((ScriptableRendererData[]) propertyInfo?.GetValue( pipeline ))?[0];

            if (_scriptableRenderData != null && _scriptableRenderData.rendererFeatures.Any(i => i is BulletFuryRenderFeature))
            {
                _hasRenderFeature = true;
            }
        }
#endif
        ShowWindow();
    }

    [MenuItem("Window/BulletFury/Setup")]
    public static void ShowWindow()
    {
        GetWindow<SetupWindow>(false, "BulletFury Setup").Show();
    }

    private void OnGUI()
    {
        var bold = new GUIStyle(EditorStyles.boldLabel)
        {
            wordWrap = true
        };
        EditorGUILayout.LabelField("Welcome to the BulletFury Demo", bold);
        /* 
         EditorGUILayout.LabelField("Welcome to BulletFury", bold);
         */
        EditorGUILayout.LabelField("This window will help make sure you can see the bullets.");
        EditorGUILayout.LabelField("Don't forget to use a material with the \"BulletFury/Unlit\" shader!", bold);
        EditorGUILayout.LabelField(
            "This enables GPU instancing, which the whole asset is built around. You can check out BulletFury/Rendering/BulletShader.shader to see what it does. You're welcome to write your own shader and use that, but be aware that it must support GPU instancing!");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "You can only make builds of your game with this asset for the duration of Bullet Hell Jam. You'll still be able to use the asset and create a game - but you'll need to purchase it on the Asset Store to export the project for any platform.");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Just to reiterate:");
        EditorGUILayout.LabelField("THIS IS A DEMO VERSION, NOT FOR COMMERCIAL USE", bold);
        EditorGUILayout.Space();
        var rp = GraphicsSettings.renderPipelineAsset;
        if (rp != null && rp is UniversalRenderPipelineAsset pipeline)
        {
            _hasPipeline = true;
            var propertyInfo = pipeline.GetType()
                .GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
            _scriptableRenderData = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[0];

            if (_scriptableRenderData != null &&
                _scriptableRenderData.rendererFeatures.Any(i => i is BulletFuryRenderFeature))
                _hasRenderFeature = true;
        }

        //if (_hasRenderFeature) Close();

        EditorStyles.label.wordWrap = true;
        var style = EditorStyles.label;
        var oldColor = style.normal.textColor;
        style.normal.textColor = _hasPipeline ? Color.green : Color.red;

        GUILayout.Label("Pipeline", bold);
        GUILayout.Label(_hasPipeline ? "You're using a render pipeline :)" : "You're not using URP!", style);
        EditorGUILayout.Space();
        style.normal.textColor = oldColor;
        if (!_hasPipeline)
        {
            GUILayout.Label("Please use URP, this asset will not work without.", EditorStyles.label);
            if (GUILayout.Button("Click here to open the Unity Manual"))
                Application.OpenURL(
                    "https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@11.0/manual/");

            return;
        }

        GUILayout.Label("Render Feature", bold);
        style.normal.textColor = _hasRenderFeature ? Color.green : Color.red;
        GUILayout.Label(
            _hasRenderFeature
                ? "The render feature has been added, you're good to go :)"
                : "The render feature hasn't been added :(", style);
        EditorGUILayout.Space();
        style.normal.textColor = oldColor;
        if (!_hasRenderFeature)
        {
            GUILayout.Label(
                "The render feature hasn't been added to the renderer - you won't see any bullets without it.",
                EditorStyles.label);
            if (GUILayout.Button("Add the BulletFury render feature for me please"))
            {
                var feature = CreateInstance<BulletFuryRenderFeature>();
                feature.name = "BulletFuryRenderFeature";
                AssetDatabase.AddObjectToAsset(feature, rp);
                _scriptableRenderData.rendererFeatures.Add(feature);
            }
        }
    }
}