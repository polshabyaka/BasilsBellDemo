using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WorkTableWaxSetupTool
{
    private const string ScenePath = "Assets/Scenes/ShopBlockout.unity";
    private const string WorkTableRootName = "WorkTable_Blockout";
    private const string PrototypeRootName = "WorkTableWax_Prototype";
    private const string CupName = "WaxMeltingCup_Blockout";
    private const string WaxChunkName = "WaxChunk_Blockout";
    private const string MeltedWaxName = "MeltedWax_Blockout";
    private const string HeatSourceName = "HeatSource_Blockout";
    private const string UiRootName = "_UI";
    private const string CanvasName = "ShopUI_Canvas";
    private const string ProgressTextName = "WaxMeltingProgressText";
    private const string MaterialsFolder = "Assets/Materials/Blockout";
    private const string CupMaterialPath = MaterialsFolder + "/MAT_Blockout_WaxCup_Debug.mat";
    private const string WaxChunkMaterialPath = MaterialsFolder + "/MAT_Blockout_WaxChunk_Debug.mat";
    private const string MeltedWaxMaterialPath = MaterialsFolder + "/MAT_Blockout_MeltedWax_Debug.mat";
    private const string HeatSourceMaterialPath = MaterialsFolder + "/MAT_Blockout_HeatSource_Debug.mat";

    [MenuItem("Basil's Bell/Scenes/Setup WorkTable Wax Prototype")]
    public static void SetupWorkTableWaxPrototype()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Wax Prototype Setup Skipped",
                "Exit Play Mode before updating the WorkTable wax prototype.",
                "OK");
            return;
        }

        if (!OpenShopBlockoutIfNeeded())
        {
            return;
        }

        Transform workTableRoot = FindRequiredTransform(WorkTableRootName);
        Transform uiRoot = FindRequiredTransform(UiRootName);
        FocusModeController focusModeController = Object.FindObjectOfType<FocusModeController>(true);
        Camera mainCamera = FindMainCamera();

        if (workTableRoot == null || uiRoot == null || focusModeController == null || mainCamera == null)
        {
            if (focusModeController == null)
            {
                ShowMissingObjectDialog(nameof(FocusModeController));
            }

            if (mainCamera == null)
            {
                ShowMissingObjectDialog("Main Camera");
            }

            return;
        }

        Transform prototypeRoot = CreateOrUpdatePrototypeRoot(workTableRoot);
        Material cupMaterial = GetOrCreateMaterial(CupMaterialPath, "MAT_Blockout_WaxCup_Debug", new Color(0.48f, 0.56f, 0.60f));
        Material waxChunkMaterial = GetOrCreateMaterial(WaxChunkMaterialPath, "MAT_Blockout_WaxChunk_Debug", new Color(0.94f, 0.86f, 0.58f));
        Material meltedWaxMaterial = GetOrCreateMaterial(MeltedWaxMaterialPath, "MAT_Blockout_MeltedWax_Debug", new Color(1f, 0.72f, 0.22f));
        Material heatSourceMaterial = GetOrCreateMaterial(HeatSourceMaterialPath, "MAT_Blockout_HeatSource_Debug", new Color(0.86f, 0.26f, 0.14f));

        GameObject heatSource = CreateOrUpdatePrimitive(
            HeatSourceName,
            prototypeRoot,
            PrimitiveType.Cylinder,
            new Vector3(0.78f, 1.075f, 0.18f),
            Quaternion.identity,
            new Vector3(0.56f, 0.025f, 0.56f),
            heatSourceMaterial,
            true,
            true);

        GameObject cup = CreateOrUpdatePrimitive(
            CupName,
            prototypeRoot,
            PrimitiveType.Cylinder,
            new Vector3(0.78f, 1.19f, 0.18f),
            Quaternion.identity,
            new Vector3(0.42f, 0.105f, 0.42f),
            cupMaterial,
            true,
            true);

        GameObject waxChunk = CreateOrUpdatePrimitive(
            WaxChunkName,
            prototypeRoot,
            PrimitiveType.Cube,
            new Vector3(0.78f, 1.34f, 0.18f),
            Quaternion.Euler(0f, 22f, 0f),
            new Vector3(0.25f, 0.13f, 0.20f),
            waxChunkMaterial,
            false,
            false);

        GameObject meltedWax = CreateOrUpdatePrimitive(
            MeltedWaxName,
            prototypeRoot,
            PrimitiveType.Cylinder,
            new Vector3(0.78f, 1.305f, 0.18f),
            Quaternion.identity,
            new Vector3(0.34f, 0.01f, 0.34f),
            meltedWaxMaterial,
            false,
            false);
        meltedWax.SetActive(false);

        Text progressText = CreateOrUpdateProgressText(uiRoot);
        if (progressText == null)
        {
            return;
        }

        WaxMeltingPrototype waxPrototype = ConfigureWaxPrototype(
            prototypeRoot.gameObject,
            focusModeController,
            mainCamera,
            cup.GetComponent<Collider>(),
            heatSource.GetComponent<Collider>(),
            waxChunk,
            waxChunk.transform,
            meltedWax,
            meltedWax.transform,
            progressText);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Selection.objects = new Object[]
        {
            prototypeRoot.gameObject,
            cup,
            waxChunk,
            meltedWax,
            heatSource,
            progressText,
            waxPrototype
        };

        EditorUtility.DisplayDialog(
            "WorkTable Wax Prototype Ready",
            "Created or updated the wax cup, wax chunk, melted wax, heat source, and melting progress UI.",
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

        ShowMissingObjectDialog(objectName);
        return null;
    }

    private static void ShowMissingObjectDialog(string objectName)
    {
        EditorUtility.DisplayDialog(
            "Wax Prototype Setup Failed",
            $"Could not find required object:\n{objectName}",
            "OK");
    }

    private static Camera FindMainCamera()
    {
        if (Camera.main != null)
        {
            return Camera.main;
        }

        GameObject cameraObject = GameObject.Find("Main Camera");
        return cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
    }

    private static Transform CreateOrUpdatePrototypeRoot(Transform workTableRoot)
    {
        Transform existingRoot = workTableRoot.Find(PrototypeRootName);
        GameObject rootObject;

        if (existingRoot == null)
        {
            rootObject = new GameObject(PrototypeRootName);
            Undo.RegisterCreatedObjectUndo(rootObject, $"Create {PrototypeRootName}");
            rootObject.transform.SetParent(workTableRoot, false);
        }
        else
        {
            rootObject = existingRoot.gameObject;
            Undo.RecordObject(rootObject.transform, $"Update {PrototypeRootName}");
        }

        rootObject.SetActive(true);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        return rootObject.transform;
    }

    private static GameObject CreateOrUpdatePrimitive(
        string objectName,
        Transform parent,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Material material,
        bool keepCollider,
        bool forceBoxCollider)
    {
        Transform existingTransform = parent.Find(objectName);
        GameObject primitiveObject;

        if (existingTransform == null)
        {
            primitiveObject = GameObject.CreatePrimitive(primitiveType);
            Undo.RegisterCreatedObjectUndo(primitiveObject, $"Create {objectName}");
            primitiveObject.name = objectName;
        }
        else
        {
            primitiveObject = existingTransform.gameObject;
        }

        Undo.RecordObject(primitiveObject.transform, $"Update {objectName}");
        primitiveObject.SetActive(true);
        primitiveObject.transform.SetParent(parent, false);
        primitiveObject.transform.localPosition = localPosition;
        primitiveObject.transform.localRotation = localRotation;
        primitiveObject.transform.localScale = localScale;
        EnsurePrimitiveVisual(primitiveObject, primitiveType, material);

        if (!keepCollider)
        {
            RemoveCollider(primitiveObject);
        }
        else if (forceBoxCollider)
        {
            EnsureBoxCollider(primitiveObject);
        }
        else if (primitiveObject.GetComponent<Collider>() == null)
        {
            Undo.AddComponent<BoxCollider>(primitiveObject);
        }

        return primitiveObject;
    }

    private static void EnsurePrimitiveVisual(GameObject targetObject, PrimitiveType primitiveType, Material material)
    {
        MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = targetObject.GetComponent<MeshRenderer>();

        if (meshFilter == null || meshRenderer == null || meshFilter.sharedMesh == null)
        {
            GameObject temporaryPrimitive = GameObject.CreatePrimitive(primitiveType);

            if (meshFilter == null)
            {
                meshFilter = Undo.AddComponent<MeshFilter>(targetObject);
            }

            if (meshRenderer == null)
            {
                meshRenderer = Undo.AddComponent<MeshRenderer>(targetObject);
            }

            meshFilter.sharedMesh = temporaryPrimitive.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temporaryPrimitive);
        }

        Undo.RecordObject(meshRenderer, $"Set {targetObject.name} Material");
        meshRenderer.sharedMaterial = material;
    }

    private static void EnsureBoxCollider(GameObject targetObject)
    {
        Collider existingCollider = targetObject.GetComponent<Collider>();
        if (existingCollider != null && !(existingCollider is BoxCollider))
        {
            Undo.DestroyObjectImmediate(existingCollider);
        }

        BoxCollider boxCollider = targetObject.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = Undo.AddComponent<BoxCollider>(targetObject);
        }

        boxCollider.center = Vector3.zero;
        boxCollider.size = Vector3.one;
    }

    private static void RemoveCollider(GameObject targetObject)
    {
        Collider collider = targetObject.GetComponent<Collider>();
        if (collider != null)
        {
            Undo.DestroyObjectImmediate(collider);
        }
    }

    private static Text CreateOrUpdateProgressText(Transform uiRoot)
    {
        Canvas canvas = EnsureCanvas(uiRoot);
        Transform existingText = FindChildRecursive(canvas.transform, ProgressTextName);
        GameObject textObject;

        if (existingText == null)
        {
            textObject = new GameObject(ProgressTextName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            Undo.RegisterCreatedObjectUndo(textObject, $"Create {ProgressTextName}");
            textObject.transform.SetParent(canvas.transform, false);
        }
        else
        {
            textObject = existingText.gameObject;
        }

        Text progressText = textObject.GetComponent<Text>();
        if (progressText == null)
        {
            progressText = Undo.AddComponent<Text>(textObject);
        }

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        if (textRect == null)
        {
            EditorUtility.DisplayDialog(
                "Wax Prototype Setup Failed",
                $"Found {ProgressTextName}, but it is not a Unity UI object. Rename it or remove it, then run setup again.",
                "OK");
            return null;
        }

        Undo.RecordObject(textRect, $"Position {ProgressTextName}");
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = new Vector2(24f, -260f);
        textRect.sizeDelta = new Vector2(420f, 72f);
        textRect.localScale = Vector3.one;

        Undo.RecordObject(progressText, $"Configure {ProgressTextName}");
        progressText.text = "Wax melting: 0%";
        progressText.fontSize = 30;
        progressText.alignment = TextAnchor.MiddleLeft;
        progressText.color = Color.black;
        progressText.raycastTarget = false;
        progressText.horizontalOverflow = HorizontalWrapMode.Overflow;
        progressText.verticalOverflow = VerticalWrapMode.Overflow;

        if (progressText.font == null)
        {
            progressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (progressText.font == null)
            {
                progressText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        textObject.SetActive(false);
        return progressText;
    }

    private static Canvas EnsureCanvas(Transform uiRoot)
    {
        Canvas existingCanvas = uiRoot.GetComponentInChildren<Canvas>(true);
        if (existingCanvas != null)
        {
            return existingCanvas;
        }

        GameObject canvasObject = new GameObject(
            CanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Undo.RegisterCreatedObjectUndo(canvasObject, $"Create {CanvasName}");
        canvasObject.transform.SetParent(uiRoot, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static WaxMeltingPrototype ConfigureWaxPrototype(
        GameObject prototypeRoot,
        FocusModeController focusModeController,
        Camera mainCamera,
        Collider cupCollider,
        Collider heatSourceCollider,
        GameObject waxChunkRoot,
        Transform waxChunkVisual,
        GameObject meltedWaxRoot,
        Transform meltedWaxVisual,
        Text progressText)
    {
        WaxMeltingPrototype waxPrototype = prototypeRoot.GetComponent<WaxMeltingPrototype>();
        if (waxPrototype == null)
        {
            waxPrototype = Undo.AddComponent<WaxMeltingPrototype>(prototypeRoot);
        }

        SerializedObject serializedPrototype = new SerializedObject(waxPrototype);
        serializedPrototype.FindProperty("focusModeController").objectReferenceValue = focusModeController;
        serializedPrototype.FindProperty("interactionCamera").objectReferenceValue = mainCamera;
        serializedPrototype.FindProperty("waxCupCollider").objectReferenceValue = cupCollider;
        serializedPrototype.FindProperty("heatSourceCollider").objectReferenceValue = heatSourceCollider;
        serializedPrototype.FindProperty("waxChunkRoot").objectReferenceValue = waxChunkRoot;
        serializedPrototype.FindProperty("waxChunkVisual").objectReferenceValue = waxChunkVisual;
        serializedPrototype.FindProperty("meltedWaxRoot").objectReferenceValue = meltedWaxRoot;
        serializedPrototype.FindProperty("meltedWaxVisual").objectReferenceValue = meltedWaxVisual;
        serializedPrototype.FindProperty("progressText").objectReferenceValue = progressText;
        serializedPrototype.FindProperty("meltDuration").floatValue = 5.5f;
        serializedPrototype.FindProperty("waxChunkEndScale").floatValue = 0.18f;
        serializedPrototype.FindProperty("meltedWaxStartScale").floatValue = 0.08f;
        serializedPrototype.FindProperty("waxChunkLowerOffset").vector3Value = new Vector3(0f, -0.08f, 0f);
        serializedPrototype.FindProperty("enableDebugResetKey").boolValue = true;
        serializedPrototype.ApplyModifiedProperties();
        EditorUtility.SetDirty(waxPrototype);

        return waxPrototype;
    }

    private static Material GetOrCreateMaterial(string materialPath, string materialName, Color color)
    {
        EnsureProjectFolder("Assets", "Materials");
        EnsureProjectFolder("Assets/Materials", "Blockout");

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
                name = materialName
            };

            ApplyMaterialColor(material, color);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            ApplyMaterialColor(material, color);
            EditorUtility.SetDirty(material);
        }

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

    private static void EnsureProjectFolder(string parentFolder, string folderName)
    {
        string folderPath = $"{parentFolder}/{folderName}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform nestedChild = FindChildRecursive(child, childName);
            if (nestedChild != null)
            {
                return nestedChild;
            }
        }

        return null;
    }
}
