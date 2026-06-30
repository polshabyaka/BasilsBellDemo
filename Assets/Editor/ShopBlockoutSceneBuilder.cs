using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ShopBlockoutSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/ShopBlockout.unity";
    private const string MaterialsFolder = "Assets/Materials/Blockout";

    [MenuItem("Basil's Bell/Scenes/Create or Regenerate ShopBlockout")]
    public static void CreateShopBlockoutScene()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureProjectFolder("Assets", "Scenes");
        EnsureProjectFolder("Assets", "Materials");
        EnsureProjectFolder("Assets/Materials", "Blockout");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildScene();

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "ShopBlockout Created",
                $"Created and saved the blockout scene at:\n{ScenePath}",
                "OK");
        }
    }

    [MenuItem("Basil's Bell/Scenes/Open ShopBlockout")]
    public static void OpenShopBlockoutScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            EditorUtility.DisplayDialog(
                "ShopBlockout Not Found",
                $"No scene exists at:\n{ScenePath}\n\nRun Create or Regenerate ShopBlockout first.",
                "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static void BuildScene()
    {
        Material floorMaterial = CreateMaterial("MAT_Blockout_Floor", new Color(0.42f, 0.43f, 0.41f));
        Material wallMaterial = CreateMaterial("MAT_Blockout_Walls", new Color(0.68f, 0.66f, 0.60f));
        Material tableMaterial = CreateMaterial("MAT_Blockout_WorkTable", new Color(0.52f, 0.36f, 0.22f));
        Material shelfMaterial = CreateMaterial("MAT_Blockout_Shelves", new Color(0.40f, 0.30f, 0.22f));
        Material counterMaterial = CreateMaterial("MAT_Blockout_Counter", new Color(0.36f, 0.44f, 0.38f));
        Material personalMaterial = CreateMaterial("MAT_Blockout_PersonalCorner", new Color(0.38f, 0.36f, 0.52f));
        Material doorMaterial = CreateMaterial("MAT_Blockout_ForestDoor", new Color(0.30f, 0.42f, 0.32f));
        Material trimMaterial = CreateMaterial("MAT_Blockout_Trim", new Color(0.23f, 0.22f, 0.20f));

        GameObject root = CreateGroup("_Scene");
        GameObject cameras = CreateGroup("_Cameras", root.transform);
        GameObject cameraAnchors = CreateGroup("_CameraAnchors", root.transform);
        CreateGroup("_Hotspots", root.transform);
        GameObject blockout = CreateGroup("_Blockout", root.transform);
        GameObject lighting = CreateGroup("_Lighting", root.transform);
        CreateGroup("_UI", root.transform);
        GameObject systems = CreateGroup("_Systems", root.transform);

        BuildRoom(blockout.transform, floorMaterial, wallMaterial);
        BuildWorkTable(blockout.transform, tableMaterial);
        BuildShelves(blockout.transform, shelfMaterial);
        BuildCounter(blockout.transform, counterMaterial, trimMaterial);
        BuildPersonalCorner(blockout.transform, personalMaterial, trimMaterial);
        BuildForestExit(blockout.transform, doorMaterial, trimMaterial);
        BuildLighting(lighting.transform);
        CameraAnchorSet anchors = BuildCameraAnchors(cameraAnchors.transform);
        Transform mainCameraTransform = BuildMainCamera(cameras.transform);
        BuildCameraNavigator(systems.transform, mainCameraTransform, anchors);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.43f, 0.39f);
    }

    private static void BuildRoom(Transform parent, Material floorMaterial, Material wallMaterial)
    {
        CreateBlock("Floor_Blockout", parent, new Vector3(0f, -0.05f, 0f), new Vector3(10f, 0.1f, 8f), floorMaterial);
        CreateBlock("BackWall_Blockout", parent, new Vector3(0f, 2.45f, 4.05f), new Vector3(10f, 4.9f, 0.1f), wallMaterial);
        CreateBlock("LeftWall_Blockout", parent, new Vector3(-5.05f, 2.45f, 0f), new Vector3(0.1f, 4.9f, 8f), wallMaterial);
        CreateBlock("RightWall_Blockout", parent, new Vector3(5.05f, 2.45f, 0f), new Vector3(0.1f, 4.9f, 8f), wallMaterial);
    }

    private static void BuildWorkTable(Transform parent, Material tableMaterial)
    {
        GameObject group = CreateGroup("WorkTable_Blockout", parent);
        group.transform.localPosition = new Vector3(-1.35f, 0f, 0.05f);

        CreateBlock("WorkTableTop_Blockout", group.transform, new Vector3(0f, 0.9f, 0f), new Vector3(2.8f, 0.22f, 1.45f), tableMaterial);
        CreateBlock("WorkTableLeg_FL_Blockout", group.transform, new Vector3(-1.15f, 0.42f, -0.55f), new Vector3(0.18f, 0.84f, 0.18f), tableMaterial);
        CreateBlock("WorkTableLeg_FR_Blockout", group.transform, new Vector3(1.15f, 0.42f, -0.55f), new Vector3(0.18f, 0.84f, 0.18f), tableMaterial);
        CreateBlock("WorkTableLeg_BL_Blockout", group.transform, new Vector3(-1.15f, 0.42f, 0.55f), new Vector3(0.18f, 0.84f, 0.18f), tableMaterial);
        CreateBlock("WorkTableLeg_BR_Blockout", group.transform, new Vector3(1.15f, 0.42f, 0.55f), new Vector3(0.18f, 0.84f, 0.18f), tableMaterial);
    }

    private static void BuildShelves(Transform parent, Material shelfMaterial)
    {
        GameObject group = CreateGroup("Shelves_Blockout", parent);
        group.transform.localPosition = new Vector3(-3.2f, 0f, 3.72f);

        CreateBlock("ShelvesBackPanel_Blockout", group.transform, new Vector3(0f, 1.55f, 0f), new Vector3(2.1f, 2.2f, 0.16f), shelfMaterial);
        CreateBlock("ShelvesLowerPlank_Blockout", group.transform, new Vector3(0f, 0.75f, -0.18f), new Vector3(2.25f, 0.14f, 0.45f), shelfMaterial);
        CreateBlock("ShelvesMiddlePlank_Blockout", group.transform, new Vector3(0f, 1.35f, -0.18f), new Vector3(2.25f, 0.14f, 0.45f), shelfMaterial);
        CreateBlock("ShelvesUpperPlank_Blockout", group.transform, new Vector3(0f, 1.95f, -0.18f), new Vector3(2.25f, 0.14f, 0.45f), shelfMaterial);
        CreateBlock("ShelvesLeftSide_Blockout", group.transform, new Vector3(-1.1f, 1.35f, -0.18f), new Vector3(0.14f, 2.1f, 0.45f), shelfMaterial);
        CreateBlock("ShelvesRightSide_Blockout", group.transform, new Vector3(1.1f, 1.35f, -0.18f), new Vector3(0.14f, 2.1f, 0.45f), shelfMaterial);
    }

    private static void BuildCounter(Transform parent, Material counterMaterial, Material trimMaterial)
    {
        GameObject group = CreateGroup("Counter_Blockout", parent);
        group.transform.localPosition = new Vector3(2.45f, 0f, -1.25f);

        CreateBlock("CounterBase_Blockout", group.transform, new Vector3(0f, 0.55f, 0f), new Vector3(3.0f, 1.1f, 1.0f), counterMaterial);
        CreateBlock("CounterTop_Blockout", group.transform, new Vector3(0f, 1.16f, 0f), new Vector3(3.25f, 0.18f, 1.15f), trimMaterial);
        CreateBlock("OrderArea_Blockout", group.transform, new Vector3(-0.75f, 1.31f, -0.12f), new Vector3(0.9f, 0.08f, 0.6f), trimMaterial);
    }

    private static void BuildPersonalCorner(Transform parent, Material personalMaterial, Material trimMaterial)
    {
        GameObject group = CreateGroup("PersonalCorner_Blockout", parent);
        group.transform.localPosition = new Vector3(-3.75f, 0f, -2.15f);

        CreateBlock("PersonalCornerRug_Blockout", group.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.8f, 0.04f, 1.4f), personalMaterial);
        CreateBlock("PersonalCornerSeat_Blockout", group.transform, new Vector3(-0.25f, 0.35f, 0.1f), new Vector3(0.75f, 0.7f, 0.75f), trimMaterial);
        CreateBlock("PersonalCornerCrate_Blockout", group.transform, new Vector3(0.65f, 0.28f, 0.35f), new Vector3(0.45f, 0.56f, 0.45f), trimMaterial);
    }

    private static void BuildForestExit(Transform parent, Material doorMaterial, Material trimMaterial)
    {
        GameObject group = CreateGroup("ForestExit_Blockout", parent);
        group.transform.localPosition = new Vector3(3.15f, 0f, 4.0f);

        CreateBlock("ForestDoor_Blockout", group.transform, new Vector3(0f, 1.2f, -0.03f), new Vector3(1.35f, 2.4f, 0.12f), doorMaterial);
        CreateBlock("ForestDoorHeader_Blockout", group.transform, new Vector3(0f, 2.5f, -0.08f), new Vector3(1.65f, 0.18f, 0.2f), trimMaterial);
        CreateBlock("ForestDoorLeftFrame_Blockout", group.transform, new Vector3(-0.78f, 1.25f, -0.08f), new Vector3(0.18f, 2.5f, 0.2f), trimMaterial);
        CreateBlock("ForestDoorRightFrame_Blockout", group.transform, new Vector3(0.78f, 1.25f, -0.08f), new Vector3(0.18f, 2.5f, 0.2f), trimMaterial);
    }

    private static void BuildLighting(Transform parent)
    {
        GameObject lightObject = new GameObject("DirectionalLight_Blockout");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localRotation = Quaternion.Euler(50f, -35f, 0f);

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.88f, 0.72f);
        light.intensity = 1.1f;
    }

    private static CameraAnchorSet BuildCameraAnchors(Transform parent)
    {
        return new CameraAnchorSet
        {
            Overview = CreateCameraAnchor("CameraAnchor_Overview", parent, new Vector3(0f, 4.3f, -7.15f), new Vector3(0f, 1.15f, 0.9f)).transform,
            WorkTable = CreateCameraAnchor("CameraAnchor_WorkTable", parent, new Vector3(-1.35f, 2.35f, -3.25f), new Vector3(-1.35f, 0.9f, 0.05f)).transform,
            Shelves = CreateCameraAnchor("CameraAnchor_Shelves", parent, new Vector3(-2.8f, 2.1f, -0.8f), new Vector3(-3.2f, 1.45f, 3.35f)).transform,
            Counter = CreateCameraAnchor("CameraAnchor_Counter", parent, new Vector3(3.35f, 2.25f, -4.0f), new Vector3(2.45f, 1.05f, -1.25f)).transform,
            PersonalCorner = CreateCameraAnchor("CameraAnchor_PersonalCorner", parent, new Vector3(-3.85f, 1.7f, -3.65f), new Vector3(-3.75f, 0.65f, -2.15f)).transform,
            ForestExit = CreateCameraAnchor("CameraAnchor_ForestExit", parent, new Vector3(2.1f, 2.0f, 0.9f), new Vector3(3.15f, 1.25f, 3.95f)).transform
        };
    }

    private static Transform BuildMainCamera(Transform parent)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, false);
        SetLookAt(cameraObject.transform, new Vector3(0f, 4.3f, -7.15f), new Vector3(0f, 1.15f, 0.9f));

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 45f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.clearFlags = CameraClearFlags.Skybox;

        cameraObject.AddComponent<AudioListener>();

        return cameraObject.transform;
    }

    private static void BuildCameraNavigator(Transform parent, Transform mainCameraTransform, CameraAnchorSet anchors)
    {
        GameObject navigatorObject = CreateGroup("CameraNavigator_System", parent);
        CameraNavigator navigator = navigatorObject.AddComponent<CameraNavigator>();

        SerializedObject serializedNavigator = new SerializedObject(navigator);
        serializedNavigator.FindProperty("mainCameraTransform").objectReferenceValue = mainCameraTransform;
        serializedNavigator.FindProperty("overviewAnchor").objectReferenceValue = anchors.Overview;
        serializedNavigator.FindProperty("workTableAnchor").objectReferenceValue = anchors.WorkTable;
        serializedNavigator.FindProperty("shelvesAnchor").objectReferenceValue = anchors.Shelves;
        serializedNavigator.FindProperty("counterAnchor").objectReferenceValue = anchors.Counter;
        serializedNavigator.FindProperty("personalCornerAnchor").objectReferenceValue = anchors.PersonalCorner;
        serializedNavigator.FindProperty("forestExitAnchor").objectReferenceValue = anchors.ForestExit;
        serializedNavigator.FindProperty("transitionDuration").floatValue = 0.65f;
        serializedNavigator.FindProperty("enableDebugNumberKeys").boolValue = true;
        serializedNavigator.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateCameraAnchor(string name, Transform parent, Vector3 position, Vector3 target)
    {
        GameObject anchor = CreateGroup(name, parent);
        SetLookAt(anchor.transform, position, target);
        return anchor;
    }

    private static GameObject CreateBlock(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localPosition;
        block.transform.localRotation = Quaternion.identity;
        block.transform.localScale = localScale;

        Collider collider = block.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Renderer renderer = block.GetComponent<Renderer>();
        renderer.sharedMaterial = material;

        return block;
    }

    private static GameObject CreateGroup(string name, Transform parent = null)
    {
        GameObject group = new GameObject(name);
        if (parent != null)
        {
            group.transform.SetParent(parent, false);
        }

        return group;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string materialPath = $"{MaterialsFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = name
            };

            ApplyMaterialColor(material, color);
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        ApplyMaterialColor(material, color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyMaterialColor(Material material, Color color)
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

    private static void SetLookAt(Transform transform, Vector3 position, Vector3 target)
    {
        transform.position = position;
        Vector3 direction = target - position;

        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
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

    private struct CameraAnchorSet
    {
        public Transform Overview;
        public Transform WorkTable;
        public Transform Shelves;
        public Transform Counter;
        public Transform PersonalCorner;
        public Transform ForestExit;
    }
}
