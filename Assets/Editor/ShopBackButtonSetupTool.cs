using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ShopBackButtonSetupTool
{
    private const string ScenePath = "Assets/Scenes/ShopBlockout.unity";
    private const string UiRootName = "_UI";
    private const string CameraNavigatorObjectName = "CameraNavigator_System";
    private const string CanvasName = "ShopUI_Canvas";
    private const string BackButtonName = "BackButton";
    private const string EventSystemName = "EventSystem";

    [MenuItem("Basil's Bell/Scenes/Setup Back Button")]
    public static void SetupBackButton()
    {
        if (!OpenShopBlockoutIfNeeded())
        {
            return;
        }

        Transform uiRoot = FindRequiredTransform(UiRootName);
        CameraNavigator cameraNavigator = FindCameraNavigator();
        if (uiRoot == null || cameraNavigator == null)
        {
            return;
        }

        Canvas canvas = EnsureCanvas(uiRoot);
        Button backButton = CreateOrUpdateBackButton(uiRoot, canvas);
        if (backButton == null)
        {
            return;
        }

        EnsureEventSystem(uiRoot);
        ConfigureBackButton(backButton, cameraNavigator);
        AssignBackButtonToNavigator(cameraNavigator, backButton);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = backButton.gameObject;

        EditorUtility.DisplayDialog(
            "Back Button Ready",
            "Created or updated BackButton under _UI. It is hidden by default and reserved for focused interaction modes.",
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
            "Back Button Setup Failed",
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
                "Back Button Setup Failed",
                $"Could not find a {nameof(CameraNavigator)} component in the scene.",
                "OK");
        }

        return cameraNavigator;
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

    private static Button CreateOrUpdateBackButton(Transform uiRoot, Canvas canvas)
    {
        Transform existingBackButton = FindChildRecursive(uiRoot, BackButtonName);
        GameObject backButtonObject;

        if (existingBackButton == null)
        {
            backButtonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            Undo.RegisterCreatedObjectUndo(backButtonObject, $"Create {BackButtonName}");
            backButtonObject.name = BackButtonName;
        }
        else
        {
            backButtonObject = existingBackButton.gameObject;
        }

        RectTransform buttonRect = backButtonObject.GetComponent<RectTransform>();
        if (buttonRect == null)
        {
            EditorUtility.DisplayDialog(
                "Back Button Setup Failed",
                $"Found {BackButtonName}, but it is not a Unity UI object. Rename it or remove it, then run setup again.",
                "OK");
            return null;
        }

        Undo.RecordObject(backButtonObject.transform, $"Update {BackButtonName}");
        backButtonObject.transform.SetParent(canvas.transform, false);
        ConfigureButtonRect(buttonRect);

        Button backButton = backButtonObject.GetComponent<Button>();
        if (backButton == null)
        {
            backButton = Undo.AddComponent<Button>(backButtonObject);
        }

        Image buttonImage = backButtonObject.GetComponent<Image>();
        if (buttonImage == null)
        {
            buttonImage = Undo.AddComponent<Image>(backButtonObject);
        }

        Undo.RecordObject(backButton, $"Configure {BackButtonName}");
        ConfigureButtonLabel(backButtonObject);

        backButton.targetGraphic = buttonImage;
        backButton.interactable = false;
        backButtonObject.SetActive(false);

        return backButton;
    }

    private static void ConfigureButtonRect(RectTransform buttonRect)
    {
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = new Vector2(24f, -24f);
        buttonRect.sizeDelta = new Vector2(120f, 40f);
        buttonRect.localScale = Vector3.one;
    }

    private static void ConfigureButtonLabel(GameObject backButtonObject)
    {
        Text label = backButtonObject.GetComponentInChildren<Text>(true);
        if (label == null)
        {
            GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            Undo.RegisterCreatedObjectUndo(labelObject, "Create BackButton Text");
            labelObject.transform.SetParent(backButtonObject.transform, false);
            label = labelObject.GetComponent<Text>();
        }

        Undo.RecordObject(label, "Configure BackButton Text");
        label.text = "Back";
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

    private static void ConfigureBackButton(Button backButton, CameraNavigator cameraNavigator)
    {
        Undo.RecordObject(backButton, "Configure BackButton OnClick");

        for (int i = backButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (backButton.onClick.GetPersistentTarget(i) == cameraNavigator &&
                IsNavigatorLeaveMethod(backButton.onClick.GetPersistentMethodName(i)))
            {
                UnityEventTools.RemovePersistentListener(backButton.onClick, i);
            }
        }

        UnityEventTools.AddPersistentListener(backButton.onClick, cameraNavigator.LeaveFocus);
        EditorUtility.SetDirty(backButton);
    }

    private static bool IsNavigatorLeaveMethod(string methodName)
    {
        return methodName == nameof(CameraNavigator.GoBack) ||
            methodName == nameof(CameraNavigator.LeaveFocus);
    }

    private static void AssignBackButtonToNavigator(CameraNavigator cameraNavigator, Button backButton)
    {
        SerializedObject serializedNavigator = new SerializedObject(cameraNavigator);
        SerializedProperty backButtonProperty = serializedNavigator.FindProperty("backButton");
        if (backButtonProperty == null)
        {
            EditorUtility.DisplayDialog(
                "Back Button Setup Failed",
                $"Could not find the {nameof(backButton)} field on {nameof(CameraNavigator)}.",
                "OK");
            return;
        }

        backButtonProperty.objectReferenceValue = backButton;
        serializedNavigator.ApplyModifiedProperties();
        EditorUtility.SetDirty(cameraNavigator);
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
}
