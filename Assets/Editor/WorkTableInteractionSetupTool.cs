using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WorkTableInteractionSetupTool
{
    private const string ScenePath = "Assets/Scenes/ShopBlockout.unity";
    private const string WorkTableRootName = "WorkTable_Blockout";
    private const string PrototypeRootName = "WorkTableFocus_Prototype";
    private const string MortarName = "Mortar_Blockout";
    private const string MortarGrindAreaName = "MortarGrindArea";
    private const string PestleContactPointName = "PestleContactPoint";
    private const string PestleVisualRootName = "PestleVisualRoot";
    private const string PestleVisualName = "Pestle_Visual";
    private const string PestleHandleGuideName = "PestleHandleGuide_RightHand";
    private const string UiRootName = "_UI";
    private const string CanvasName = "ShopUI_Canvas";
    private const string ProgressTextName = "MortarProgressText";
    private const string MaterialsFolder = "Assets/Materials/Blockout";
    private const string MortarMaterialPath = MaterialsFolder + "/MAT_Blockout_Mortar_Debug.mat";
    private const string PestleMaterialPath = MaterialsFolder + "/MAT_Blockout_Pestle_Debug.mat";
    private const string GrindAreaMaterialPath = MaterialsFolder + "/MAT_Blockout_GrindArea_Debug.mat";

    [MenuItem("Basil's Bell/Scenes/Setup WorkTable Mortar Prototype")]
    public static void SetupWorkTableMortarPrototype()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Mortar Prototype Setup Skipped",
                "Exit Play Mode before updating the WorkTable mortar prototype.",
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
        Material mortarMaterial = GetOrCreateMaterial(MortarMaterialPath, "MAT_Blockout_Mortar_Debug", new Color(0.54f, 0.53f, 0.49f));
        Material pestleMaterial = GetOrCreateMaterial(PestleMaterialPath, "MAT_Blockout_Pestle_Debug", new Color(0.34f, 0.28f, 0.22f));
        Material grindAreaMaterial = GetOrCreateTransparentMaterial(GrindAreaMaterialPath, "MAT_Blockout_GrindArea_Debug", new Color(0.25f, 0.7f, 1f, 0.25f));

        GameObject mortar = CreateOrUpdatePrimitive(
            MortarName,
            prototypeRoot,
            PrimitiveType.Cylinder,
            new Vector3(0f, 1.11f, 0f),
            Quaternion.identity,
            new Vector3(0.72f, 0.10f, 0.72f),
            mortarMaterial,
            true,
            false);

        GameObject grindArea = CreateOrUpdatePrimitive(
            MortarGrindAreaName,
            prototypeRoot,
            PrimitiveType.Cylinder,
            new Vector3(0f, 1.24f, 0f),
            Quaternion.identity,
            new Vector3(0.64f, 0.012f, 0.48f),
            grindAreaMaterial,
            true,
            true);

        Transform contactPoint = CreateOrUpdateLocator(
            prototypeRoot,
            PestleContactPointName,
            new Vector3(0f, 1.24f, 0f),
            Quaternion.identity);

        Transform visualRoot = CreateOrUpdateLocator(
            prototypeRoot,
            PestleVisualRootName,
            contactPoint.localPosition,
            Quaternion.identity);

        Transform handleGuide = CreateOrUpdateLocator(
            prototypeRoot,
            PestleHandleGuideName,
            new Vector3(0.58f, 1.96f, -0.08f),
            Quaternion.identity);

        GameObject pestleVisual = CreateOrUpdatePestleVisual(prototypeRoot, visualRoot, pestleMaterial);
        Text progressText = CreateOrUpdateProgressText(uiRoot);
        if (progressText == null)
        {
            return;
        }

        MortarPestleInteraction interaction = ConfigureMortarPestleInteraction(
            visualRoot.gameObject,
            grindArea.transform,
            contactPoint,
            visualRoot,
            pestleVisual.transform,
            handleGuide,
            grindArea.GetComponent<Collider>(),
            focusModeController,
            mainCamera,
            progressText);

        DisableExtraMortarInteractions(prototypeRoot, interaction);
        DisableLegacyObjects(prototypeRoot, visualRoot, pestleVisual.transform);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Selection.objects = new Object[]
        {
            mortar,
            grindArea,
            contactPoint.gameObject,
            visualRoot.gameObject,
            pestleVisual,
            handleGuide.gameObject,
            interaction
        };

        EditorUtility.DisplayDialog(
            "WorkTable Mortar Prototype Ready",
            "Created or updated the mortar grind area, pestle contact point, right-hand guide, and pestle visual.",
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
            "Mortar Prototype Setup Failed",
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

    private static Transform CreateOrUpdateLocator(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Quaternion localRotation)
    {
        Transform existingTransform = parent.Find(objectName);
        GameObject locatorObject;

        if (existingTransform == null)
        {
            locatorObject = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(locatorObject, $"Create {objectName}");
            locatorObject.transform.SetParent(parent, false);
        }
        else
        {
            locatorObject = existingTransform.gameObject;
            Undo.RecordObject(locatorObject.transform, $"Update {objectName}");
        }

        locatorObject.SetActive(true);
        locatorObject.transform.localPosition = localPosition;
        locatorObject.transform.localRotation = localRotation;
        locatorObject.transform.localScale = Vector3.one;
        return locatorObject.transform;
    }

    private static GameObject CreateOrUpdatePestleVisual(Transform prototypeRoot, Transform visualRoot, Material material)
    {
        Transform existingVisual = visualRoot.Find(PestleVisualName);
        Transform looseVisual = FindChildRecursive(prototypeRoot, PestleVisualName);
        Transform legacyPestle = prototypeRoot.Find("Pestle_Blockout");
        GameObject visualObject;

        if (existingVisual != null)
        {
            visualObject = existingVisual.gameObject;
        }
        else if (looseVisual != null)
        {
            visualObject = looseVisual.gameObject;
        }
        else if (legacyPestle != null)
        {
            visualObject = legacyPestle.gameObject;
            Undo.RecordObject(visualObject, "Rename Pestle_Blockout");
            visualObject.name = PestleVisualName;
        }
        else
        {
            visualObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(visualObject, $"Create {PestleVisualName}");
            visualObject.name = PestleVisualName;
        }

        Undo.RecordObject(visualObject.transform, $"Update {PestleVisualName}");
        visualObject.SetActive(true);
        visualObject.transform.SetParent(visualRoot, false);
        visualObject.transform.localPosition = Vector3.up * 0.52f;
        visualObject.transform.localRotation = Quaternion.identity;
        visualObject.transform.localScale = new Vector3(0.06f, 0.52f, 0.06f);

        EnsurePrimitiveVisual(visualObject, PrimitiveType.Cylinder, material);
        EnsureBoxCollider(visualObject);
        return visualObject;
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
                "Mortar Prototype Setup Failed",
                $"Found {ProgressTextName}, but it is not a Unity UI object. Rename it or remove it, then run setup again.",
                "OK");
            return null;
        }

        Undo.RecordObject(textRect, $"Position {ProgressTextName}");
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = new Vector2(24f, -168f);
        textRect.sizeDelta = new Vector2(280f, 44f);
        textRect.localScale = Vector3.one;

        Undo.RecordObject(progressText, $"Configure {ProgressTextName}");
        progressText.text = "Grinding: 0%";
        progressText.fontSize = 30;
        progressText.alignment = TextAnchor.MiddleLeft;
        progressText.color = Color.black;
        progressText.raycastTarget = false;

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

    private static MortarPestleInteraction ConfigureMortarPestleInteraction(
        GameObject interactionObject,
        Transform grindArea,
        Transform contactPoint,
        Transform visualRoot,
        Transform pestleVisual,
        Transform handleGuide,
        Collider grindCollider,
        FocusModeController focusModeController,
        Camera mainCamera,
        Text progressText)
    {
        MortarPestleInteraction interaction = interactionObject.GetComponent<MortarPestleInteraction>();
        if (interaction == null)
        {
            interaction = Undo.AddComponent<MortarPestleInteraction>(interactionObject);
        }

        SerializedObject serializedInteraction = new SerializedObject(interaction);
        serializedInteraction.FindProperty("focusModeController").objectReferenceValue = focusModeController;
        serializedInteraction.FindProperty("interactionCamera").objectReferenceValue = mainCamera;
        serializedInteraction.FindProperty("mortarGrindArea").objectReferenceValue = grindArea;
        serializedInteraction.FindProperty("pestleContactPoint").objectReferenceValue = contactPoint;
        serializedInteraction.FindProperty("pestleVisualRoot").objectReferenceValue = visualRoot;
        serializedInteraction.FindProperty("pestleVisual").objectReferenceValue = pestleVisual;
        serializedInteraction.FindProperty("pestleHandleGuideRightHand").objectReferenceValue = handleGuide;
        serializedInteraction.FindProperty("grindInputCollider").objectReferenceValue = grindCollider;
        serializedInteraction.FindProperty("progressText").objectReferenceValue = progressText;
        serializedInteraction.FindProperty("grindRadiusX").floatValue = 0.32f;
        serializedInteraction.FindProperty("grindRadiusZ").floatValue = 0.24f;
        serializedInteraction.FindProperty("minTiltAboveHorizontal").floatValue = 50f;
        serializedInteraction.FindProperty("maxTiltAboveHorizontal").floatValue = 75f;
        serializedInteraction.FindProperty("rightHandLeanOffset").vector3Value = new Vector3(0.58f, 0.72f, -0.08f);
        serializedInteraction.FindProperty("pestleLength").floatValue = 1.04f;
        serializedInteraction.FindProperty("pestleRadius").floatValue = 0.06f;
        serializedInteraction.FindProperty("progressPerMeter").floatValue = 0.65f;
        serializedInteraction.ApplyModifiedProperties();
        EditorUtility.SetDirty(interaction);

        return interaction;
    }

    private static void DisableExtraMortarInteractions(Transform prototypeRoot, MortarPestleInteraction activeInteraction)
    {
        MortarPestleInteraction[] interactions = prototypeRoot.GetComponentsInChildren<MortarPestleInteraction>(true);
        foreach (MortarPestleInteraction interaction in interactions)
        {
            Undo.RecordObject(interaction, $"Update {nameof(MortarPestleInteraction)}");
            interaction.enabled = interaction == activeInteraction;
        }
    }

    private static void DisableLegacyObjects(Transform prototypeRoot, Transform visualRoot, Transform pestleVisual)
    {
        DisableChildIfPresent(prototypeRoot, "MortarGrindingPivot");
        DisableChildIfPresent(prototypeRoot, "Pestle_Pivot");
        DisableChildIfPresent(prototypeRoot, "PestleRestPoint");
        DisableChildIfPresent(prototypeRoot, "MortarGrindCenter");

        Transform legacyPestle = prototypeRoot.Find("Pestle_Blockout");
        if (legacyPestle != null && legacyPestle != pestleVisual && legacyPestle != visualRoot)
        {
            Undo.RecordObject(legacyPestle.gameObject, "Disable Pestle_Blockout");
            legacyPestle.gameObject.SetActive(false);
        }
    }

    private static void DisableChildIfPresent(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            return;
        }

        Undo.RecordObject(child.gameObject, $"Disable {childName}");
        child.gameObject.SetActive(false);
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

    private static Material GetOrCreateTransparentMaterial(string materialPath, string materialName, Color color)
    {
        Material material = GetOrCreateMaterial(materialPath, materialName, color);

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
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
        material.renderQueue = (int)RenderQueue.Transparent;
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
