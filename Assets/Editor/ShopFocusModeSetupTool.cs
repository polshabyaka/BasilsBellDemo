using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ShopFocusModeSetupTool
{
    private const string ScenePath = "Assets/Scenes/ShopBlockout.unity";
    private const string CameraAnchorsRootName = "_CameraAnchors";
    private const string SystemsRootName = "_Systems";
    private const string HotspotsRootName = "_Hotspots";
    private const string UiRootName = "_UI";
    private const string CameraNavigatorObjectName = "CameraNavigator_System";
    private const string FocusControllerObjectName = "FocusModeController_System";
    private const string WorkTableAnchorName = "CameraAnchor_WorkTable";
    private const string WorkTableFocusAnchorName = "CameraAnchor_WorkTableFocus";
    private const string CanvasName = "ShopUI_Canvas";
    private const string WorkButtonName = "WorkTableFocusButton";
    private const string LeaveButtonName = "LeaveFocusButton";
    private const string EventSystemName = "EventSystem";

    [MenuItem("Basil's Bell/Scenes/Setup WorkTable Focus Mode")]
    public static void SetupWorkTableFocusMode()
    {
        if (!OpenShopBlockoutIfNeeded())
        {
            return;
        }

        Transform cameraAnchorsRoot = FindRequiredTransform(CameraAnchorsRootName);
        Transform systemsRoot = FindRequiredTransform(SystemsRootName);
        Transform hotspotsRoot = FindRequiredTransform(HotspotsRootName);
        Transform uiRoot = FindRequiredTransform(UiRootName);
        Transform workTableAnchor = FindRequiredTransform(WorkTableAnchorName);
        CameraNavigator cameraNavigator = FindCameraNavigator();

        if (cameraAnchorsRoot == null ||
            systemsRoot == null ||
            hotspotsRoot == null ||
            uiRoot == null ||
            workTableAnchor == null ||
            cameraNavigator == null)
        {
            return;
        }

        Transform workTableFocusAnchor = CreateOrUpdateWorkTableFocusAnchor(cameraAnchorsRoot);
        FocusModeController focusModeController = CreateOrUpdateFocusModeController(systemsRoot);
        Canvas canvas = EnsureCanvas(uiRoot);
        Button workButton = CreateOrUpdateButton(uiRoot, canvas, WorkButtonName, "Work", new Vector2(24f, -76f), false);
        Button leaveButton = CreateOrUpdateButton(uiRoot, canvas, LeaveButtonName, "Leave", new Vector2(24f, -24f), false);
        if (workButton == null || leaveButton == null)
        {
            return;
        }

        EnsureEventSystem(uiRoot);
        ConfigureButtonAction(workButton, focusModeController, nameof(FocusModeController.EnterWorkTableFocus));
        ConfigureButtonAction(leaveButton, focusModeController, nameof(FocusModeController.LeaveFocus));
        ConfigureFocusModeController(
            focusModeController,
            cameraNavigator,
            workTableAnchor,
            workTableFocusAnchor,
            hotspotsRoot.gameObject,
            workButton,
            leaveButton);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.objects = new Object[]
        {
            focusModeController.gameObject,
            workTableFocusAnchor.gameObject,
            workButton.gameObject,
            leaveButton.gameObject
        };

        EditorUtility.DisplayDialog(
            "WorkTable Focus Mode Ready",
            "Created or updated the WorkTable focus anchor, focus controller, and prototype UI buttons.",
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
            "WorkTable Focus Setup Failed",
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
                "WorkTable Focus Setup Failed",
                $"Could not find a {nameof(CameraNavigator)} component in the scene.",
                "OK");
        }

        return cameraNavigator;
    }

    private static Transform CreateOrUpdateWorkTableFocusAnchor(Transform cameraAnchorsRoot)
    {
        Transform anchor = FindChildRecursive(cameraAnchorsRoot, WorkTableFocusAnchorName);
        GameObject anchorObject;

        if (anchor == null)
        {
            anchorObject = new GameObject(WorkTableFocusAnchorName);
            Undo.RegisterCreatedObjectUndo(anchorObject, $"Create {WorkTableFocusAnchorName}");
        }
        else
        {
            anchorObject = anchor.gameObject;
            Undo.RecordObject(anchorObject.transform, $"Update {WorkTableFocusAnchorName}");
        }

        anchorObject.transform.SetParent(cameraAnchorsRoot, true);
        SetLookAt(
            anchorObject.transform,
            new Vector3(-1.35f, 2.35f, -1.15f),
            new Vector3(-1.35f, 0.9f, 0.05f));

        return anchorObject.transform;
    }

    private static FocusModeController CreateOrUpdateFocusModeController(Transform systemsRoot)
    {
        Transform controllerTransform = FindChildRecursive(systemsRoot, FocusControllerObjectName);
        GameObject controllerObject;

        if (controllerTransform == null)
        {
            controllerObject = new GameObject(FocusControllerObjectName);
            Undo.RegisterCreatedObjectUndo(controllerObject, $"Create {FocusControllerObjectName}");
            controllerObject.transform.SetParent(systemsRoot, false);
        }
        else
        {
            controllerObject = controllerTransform.gameObject;
            Undo.RecordObject(controllerObject.transform, $"Update {FocusControllerObjectName}");
            controllerObject.transform.SetParent(systemsRoot, false);
        }

        FocusModeController focusModeController = controllerObject.GetComponent<FocusModeController>();
        if (focusModeController == null)
        {
            focusModeController = Undo.AddComponent<FocusModeController>(controllerObject);
        }

        return focusModeController;
    }

    private static Canvas EnsureCanvas(Transform uiRoot)
    {
        Canvas existingCanvas = uiRoot.GetComponentInChildren<Canvas>(true);
        if (existingCanvas != null)
        {
            ConfigureCanvas(existingCanvas);
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
        ConfigureCanvas(canvas);
        return canvas;
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
        Undo.RecordObject(canvas, "Configure Shop UI Canvas");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler canvasScaler = canvas.GetComponent<CanvasScaler>();
        if (canvasScaler == null)
        {
            canvasScaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
        }

        Undo.RecordObject(canvasScaler, "Configure Shop UI Canvas Scaler");
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
        }
    }

    private static Button CreateOrUpdateButton(
        Transform uiRoot,
        Canvas canvas,
        string buttonName,
        string labelText,
        Vector2 anchoredPosition,
        bool isVisible)
    {
        Transform existingButton = FindChildRecursive(uiRoot, buttonName);
        GameObject buttonObject;

        if (existingButton == null)
        {
            buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            Undo.RegisterCreatedObjectUndo(buttonObject, $"Create {buttonName}");
            buttonObject.name = buttonName;
        }
        else
        {
            buttonObject = existingButton.gameObject;
        }

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (buttonRect == null)
        {
            EditorUtility.DisplayDialog(
                "WorkTable Focus Setup Failed",
                $"Found {buttonName}, but it is not a Unity UI object. Rename it or remove it, then run setup again.",
                "OK");
            return null;
        }

        Undo.RecordObject(buttonObject.transform, $"Update {buttonName}");
        buttonObject.transform.SetParent(canvas.transform, false);
        ConfigureButtonRect(buttonRect, anchoredPosition);

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            button = Undo.AddComponent<Button>(buttonObject);
        }

        Image buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage == null)
        {
            buttonImage = Undo.AddComponent<Image>(buttonObject);
        }

        Undo.RecordObject(button, $"Configure {buttonName}");
        ConfigureButtonLabel(buttonObject, labelText);
        button.targetGraphic = buttonImage;
        button.interactable = isVisible;
        buttonObject.SetActive(isVisible);

        return button;
    }

    private static void ConfigureButtonRect(RectTransform buttonRect, Vector2 anchoredPosition)
    {
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(120f, 40f);
        buttonRect.localScale = Vector3.one;
    }

    private static void ConfigureButtonLabel(GameObject buttonObject, string labelText)
    {
        Text label = buttonObject.GetComponentInChildren<Text>(true);
        if (label == null)
        {
            GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            Undo.RegisterCreatedObjectUndo(labelObject, $"Create {buttonObject.name} Text");
            labelObject.transform.SetParent(buttonObject.transform, false);
            label = labelObject.GetComponent<Text>();
        }

        Undo.RecordObject(label, $"Configure {buttonObject.name} Text");
        label.text = labelText;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.raycastTarget = false;

        if (label.font == null)
        {
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (label.font == null)
            {
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private static void ConfigureButtonAction(Button button, FocusModeController focusModeController, string methodName)
    {
        if (button == null || focusModeController == null)
        {
            return;
        }

        Undo.RecordObject(button, $"Configure {button.name} OnClick");

        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (button.onClick.GetPersistentTarget(i) == focusModeController &&
                IsFocusModeMethod(button.onClick.GetPersistentMethodName(i)))
            {
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            }
        }

        if (methodName == nameof(FocusModeController.EnterWorkTableFocus))
        {
            UnityEventTools.AddPersistentListener(button.onClick, focusModeController.EnterWorkTableFocus);
        }
        else if (methodName == nameof(FocusModeController.LeaveFocus))
        {
            UnityEventTools.AddPersistentListener(button.onClick, focusModeController.LeaveFocus);
        }

        EditorUtility.SetDirty(button);
    }

    private static bool IsFocusModeMethod(string methodName)
    {
        return methodName == nameof(FocusModeController.EnterWorkTableFocus) ||
            methodName == nameof(FocusModeController.LeaveFocus);
    }

    private static void ConfigureFocusModeController(
        FocusModeController focusModeController,
        CameraNavigator cameraNavigator,
        Transform workTableAnchor,
        Transform workTableFocusAnchor,
        GameObject hotspotsRoot,
        Button workButton,
        Button leaveButton)
    {
        SerializedObject serializedController = new SerializedObject(focusModeController);
        serializedController.FindProperty("cameraNavigator").objectReferenceValue = cameraNavigator;
        serializedController.FindProperty("workTableAnchor").objectReferenceValue = workTableAnchor;
        serializedController.FindProperty("workTableFocusAnchor").objectReferenceValue = workTableFocusAnchor;
        serializedController.FindProperty("roomHotspotsRoot").objectReferenceValue = hotspotsRoot;
        serializedController.FindProperty("workTableFocusButton").objectReferenceValue = workButton;
        serializedController.FindProperty("leaveFocusButton").objectReferenceValue = leaveButton;
        serializedController.ApplyModifiedProperties();
        EditorUtility.SetDirty(focusModeController);
    }

    private static void EnsureEventSystem(Transform uiRoot)
    {
        if (Object.FindObjectOfType<EventSystem>(true) != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject(
            EventSystemName,
            typeof(EventSystem),
            typeof(StandaloneInputModule));

        Undo.RegisterCreatedObjectUndo(eventSystemObject, $"Create {EventSystemName}");
        eventSystemObject.transform.SetParent(uiRoot, false);
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

    private static void SetLookAt(Transform transform, Vector3 position, Vector3 target)
    {
        Undo.RecordObject(transform, $"Position {transform.name}");
        transform.position = position;
        Vector3 direction = target - position;

        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
