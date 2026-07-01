using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ShopDebugVisualSetupTool
{
    private const string ScenePath = "Assets/Scenes/ShopBlockout.unity";
    private const string HotspotsRootName = "_Hotspots";
    private const string UiRootName = "_UI";
    private const string MaterialsFolder = "Assets/Materials/Blockout";
    private const string HotspotMaterialPath = MaterialsFolder + "/MAT_Blockout_Hotspot_Debug.mat";
    private const int PrototypeButtonTextSize = 32;

    [MenuItem("Basil's Bell/Scenes/Setup Debug Visuals")]
    public static void SetupDebugVisuals()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Debug Visual Setup Skipped",
                "Exit Play Mode before updating scene debug visuals.",
                "OK");
            return;
        }

        if (!OpenShopBlockoutIfNeeded())
        {
            return;
        }

        Transform hotspotsRoot = FindRequiredTransform(HotspotsRootName);
        Transform uiRoot = FindRequiredTransform(UiRootName);
        if (hotspotsRoot == null || uiRoot == null)
        {
            return;
        }

        Material hotspotMaterial = GetOrCreateTransparentHotspotMaterial();
        int hotspotCount = UpdateHotspotVisuals(hotspotsRoot, hotspotMaterial);
        int buttonCount = UpdatePrototypeButtons(uiRoot);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Debug Visuals Ready",
            $"Updated {hotspotCount} hotspot renderer(s) and {buttonCount} UI button(s).",
            "OK");
    }

    private static bool OpenShopBlockoutIfNeeded()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path == ScenePath)
        {
            return true;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            EditorUtility.DisplayDialog(
                "ShopBlockout Missing",
                $"Could not find the scene at:\n{ScenePath}",
                "OK");
            return false;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return false;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return true;
    }

    private static Transform FindRequiredTransform(string objectName)
    {
        GameObject foundObject = GameObject.Find(objectName);
        if (foundObject != null)
        {
            return foundObject.transform;
        }

        EditorUtility.DisplayDialog(
            "Debug Visual Setup Failed",
            $"Could not find required object:\n{objectName}",
            "OK");
        return null;
    }

    private static int UpdateHotspotVisuals(Transform hotspotsRoot, Material hotspotMaterial)
    {
        Renderer[] hotspotRenderers = hotspotsRoot.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer hotspotRenderer in hotspotRenderers)
        {
            Undo.RecordObject(hotspotRenderer, $"Update {hotspotRenderer.name} Debug Material");
            hotspotRenderer.sharedMaterial = hotspotMaterial;
            EditorUtility.SetDirty(hotspotRenderer);
        }

        return hotspotRenderers.Length;
    }

    private static int UpdatePrototypeButtons(Transform uiRoot)
    {
        Button[] buttons = uiRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            ConfigureButtonRect(button);
            ConfigureButtonLabels(button);
            EditorUtility.SetDirty(button);
        }

        return buttons.Length;
    }

    private static void ConfigureButtonRect(Button button)
    {
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect == null)
        {
            return;
        }

        Undo.RecordObject(buttonRect, $"Resize {button.name}");
        buttonRect.sizeDelta = new Vector2(180f, 60f);

        if (button.name == "WorkTableFocusButton")
        {
            SetTopLeftButtonPosition(buttonRect, new Vector2(24f, -96f));
        }
        else if (button.name == "LeaveFocusButton" || button.name == "BackButton")
        {
            SetTopLeftButtonPosition(buttonRect, new Vector2(24f, -24f));
        }
    }

    private static void SetTopLeftButtonPosition(RectTransform buttonRect, Vector2 anchoredPosition)
    {
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.localScale = Vector3.one;
    }

    private static void ConfigureButtonLabels(Button button)
    {
        Text[] labels = button.GetComponentsInChildren<Text>(true);
        foreach (Text label in labels)
        {
            Undo.RecordObject(label, $"Update {button.name} Text");
            label.fontSize = PrototypeButtonTextSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 24;
            label.resizeTextMaxSize = PrototypeButtonTextSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;

            RectTransform labelRect = label.GetComponent<RectTransform>();
            if (labelRect != null)
            {
                Undo.RecordObject(labelRect, $"Resize {button.name} Text");
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            EditorUtility.SetDirty(label);
        }
    }

    private static Material GetOrCreateTransparentHotspotMaterial()
    {
        EnsureProjectFolder("Assets", "Materials");
        EnsureProjectFolder("Assets/Materials", "Blockout");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(HotspotMaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = "MAT_Blockout_Hotspot_Debug"
            };

            ConfigureTransparentMaterial(material);
            AssetDatabase.CreateAsset(material, HotspotMaterialPath);
        }
        else
        {
            ConfigureTransparentMaterial(material);
            EditorUtility.SetDirty(material);
        }

        return material;
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        Color debugColor = new Color(1f, 0.78f, 0.15f, 0.32f);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", debugColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", debugColor);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private static void EnsureProjectFolder(string parentFolder, string folderName)
    {
        string folderPath = $"{parentFolder}/{folderName}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
    }
}
