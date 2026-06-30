using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShopHotspotSetupTool
{
    private const string ScenePath = "Assets/Scenes/ShopBlockout.unity";
    private const string HotspotsRootName = "_Hotspots";
    private const string CameraNavigatorObjectName = "CameraNavigator_System";
    private const string MaterialsFolder = "Assets/Materials/Blockout";
    private const string HotspotMaterialPath = MaterialsFolder + "/MAT_Blockout_Hotspot_Debug.mat";

    [MenuItem("Basil's Bell/Scenes/Setup Shop Hotspots")]
    public static void SetupShopHotspots()
    {
        if (!OpenShopBlockoutIfNeeded())
        {
            return;
        }

        Transform hotspotsRoot = FindRequiredTransform(HotspotsRootName);
        CameraNavigator cameraNavigator = FindCameraNavigator();
        Transform workTableAnchor = FindRequiredTransform("CameraAnchor_WorkTable");
        Transform shelvesAnchor = FindRequiredTransform("CameraAnchor_Shelves");
        Transform counterAnchor = FindRequiredTransform("CameraAnchor_Counter");
        Transform personalCornerAnchor = FindRequiredTransform("CameraAnchor_PersonalCorner");
        Transform forestExitAnchor = FindRequiredTransform("CameraAnchor_ForestExit");

        if (hotspotsRoot == null ||
            cameraNavigator == null ||
            workTableAnchor == null ||
            shelvesAnchor == null ||
            counterAnchor == null ||
            personalCornerAnchor == null ||
            forestExitAnchor == null)
        {
            return;
        }

        Material hotspotMaterial = GetOrCreateHotspotMaterial();
        List<GameObject> updatedHotspots = new List<GameObject>
        {
            CreateOrUpdateHotspot("Hotspot_WorkTable", hotspotsRoot, cameraNavigator, workTableAnchor, new Vector3(-1.35f, 1.25f, 0.05f), new Vector3(3.0f, 0.35f, 1.6f), hotspotMaterial),
            CreateOrUpdateHotspot("Hotspot_Shelves", hotspotsRoot, cameraNavigator, shelvesAnchor, new Vector3(-3.2f, 1.45f, 3.3f), new Vector3(2.4f, 2.2f, 0.5f), hotspotMaterial),
            CreateOrUpdateHotspot("Hotspot_Counter", hotspotsRoot, cameraNavigator, counterAnchor, new Vector3(2.45f, 1.3f, -1.25f), new Vector3(3.3f, 0.35f, 1.2f), hotspotMaterial),
            CreateOrUpdateHotspot("Hotspot_PersonalCorner", hotspotsRoot, cameraNavigator, personalCornerAnchor, new Vector3(-3.75f, 0.5f, -2.15f), new Vector3(1.9f, 0.25f, 1.5f), hotspotMaterial),
            CreateOrUpdateHotspot("Hotspot_ForestExit", hotspotsRoot, cameraNavigator, forestExitAnchor, new Vector3(3.15f, 1.25f, 3.85f), new Vector3(1.7f, 2.5f, 0.3f), hotspotMaterial)
        };

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.objects = updatedHotspots.ToArray();

        EditorUtility.DisplayDialog(
            "Shop Hotspots Ready",
            "Created or updated the five camera hotspots under _Hotspots.",
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
            "Shop Hotspot Setup Failed",
            $"Could not find required object:\n{objectName}",
            "OK");
        return null;
    }

    private static CameraNavigator FindCameraNavigator()
    {
        GameObject navigatorObject = GameObject.Find(CameraNavigatorObjectName);
        CameraNavigator cameraNavigator = navigatorObject != null
            ? navigatorObject.GetComponent<CameraNavigator>()
            : null;

        if (cameraNavigator == null)
        {
            cameraNavigator = Object.FindObjectOfType<CameraNavigator>(true);
        }

        if (cameraNavigator == null)
        {
            EditorUtility.DisplayDialog(
                "Shop Hotspot Setup Failed",
                $"Could not find a {nameof(CameraNavigator)} component in the scene.",
                "OK");
        }

        return cameraNavigator;
    }

    private static GameObject CreateOrUpdateHotspot(
        string hotspotName,
        Transform parent,
        CameraNavigator cameraNavigator,
        Transform targetAnchor,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject hotspot = FindHotspot(hotspotName, parent);
        if (hotspot == null)
        {
            hotspot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(hotspot, $"Create {hotspotName}");
            hotspot.name = hotspotName;
        }

        Undo.RecordObject(hotspot.transform, $"Update {hotspotName}");
        hotspot.transform.SetParent(parent, false);
        hotspot.transform.localPosition = localPosition;
        hotspot.transform.localRotation = Quaternion.identity;
        hotspot.transform.localScale = localScale;

        EnsureCubeVisual(hotspot, material);
        EnsureBoxCollider(hotspot);
        ConfigureCameraHotspot(hotspot, cameraNavigator, targetAnchor);

        return hotspot;
    }

    private static GameObject FindHotspot(string hotspotName, Transform expectedParent)
    {
        Transform directChild = expectedParent.Find(hotspotName);
        if (directChild != null)
        {
            return directChild.gameObject;
        }

        return GameObject.Find(hotspotName);
    }

    private static void EnsureCubeVisual(GameObject hotspot, Material material)
    {
        MeshFilter meshFilter = hotspot.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = hotspot.AddComponent<MeshFilter>();
        }

        if (meshFilter.sharedMesh == null)
        {
            GameObject temporaryCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meshFilter.sharedMesh = temporaryCube.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temporaryCube);
        }

        MeshRenderer meshRenderer = hotspot.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = hotspot.AddComponent<MeshRenderer>();
        }

        meshRenderer.sharedMaterial = material;
    }

    private static void EnsureBoxCollider(GameObject hotspot)
    {
        BoxCollider boxCollider = hotspot.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = hotspot.AddComponent<BoxCollider>();
        }

        boxCollider.center = Vector3.zero;
        boxCollider.size = Vector3.one;
    }

    private static void ConfigureCameraHotspot(GameObject hotspot, CameraNavigator cameraNavigator, Transform targetAnchor)
    {
        CameraHotspot cameraHotspot = hotspot.GetComponent<CameraHotspot>();
        if (cameraHotspot == null)
        {
            cameraHotspot = hotspot.AddComponent<CameraHotspot>();
        }

        SerializedObject serializedHotspot = new SerializedObject(cameraHotspot);
        serializedHotspot.FindProperty("cameraNavigator").objectReferenceValue = cameraNavigator;
        serializedHotspot.FindProperty("targetAnchor").objectReferenceValue = targetAnchor;
        serializedHotspot.ApplyModifiedProperties();
    }

    private static Material GetOrCreateHotspotMaterial()
    {
        EnsureProjectFolder("Assets", "Materials");
        EnsureProjectFolder("Assets/Materials", "Blockout");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(HotspotMaterialPath);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        material = new Material(shader)
        {
            name = "MAT_Blockout_Hotspot_Debug"
        };

        SetMaterialColor(material, new Color(0.95f, 0.78f, 0.25f));
        AssetDatabase.CreateAsset(material, HotspotMaterialPath);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
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
