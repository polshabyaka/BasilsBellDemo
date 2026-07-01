using UnityEngine;
using UnityEngine.UI;

public class FocusModeController : MonoBehaviour
{
    [SerializeField] private CameraNavigator cameraNavigator;
    [SerializeField] private Transform workTableAnchor;
    [SerializeField] private Transform workTableFocusAnchor;
    [SerializeField] private GameObject roomHotspotsRoot;
    [SerializeField] private Button workTableFocusButton;
    [SerializeField] private Button leaveFocusButton;

    private bool isInFocusMode;

    public bool IsInFocusMode => isInFocusMode;

    private void Start()
    {
        SetFocusModeActive(false);
        UpdateButtonVisibility();
    }

    private void Update()
    {
        UpdateButtonVisibility();
    }

    public void EnterWorkTableFocus()
    {
        if (cameraNavigator == null || workTableFocusAnchor == null)
        {
            return;
        }

        isInFocusMode = true;
        SetRoomHotspotsActive(false);
        cameraNavigator.SetLookAroundEnabled(false);
        cameraNavigator.SetDebugNumberKeysBlocked(true);
        cameraNavigator.MoveToAnchor(workTableFocusAnchor);
        UpdateButtonVisibility();
    }

    public void LeaveFocus()
    {
        isInFocusMode = false;
        SetRoomHotspotsActive(true);

        if (cameraNavigator != null)
        {
            cameraNavigator.SetLookAroundEnabled(true);
            cameraNavigator.SetDebugNumberKeysBlocked(false);

            if (workTableAnchor != null)
            {
                cameraNavigator.MoveToAnchor(workTableAnchor);
            }
        }

        UpdateButtonVisibility();
    }

    private void SetFocusModeActive(bool isFocused)
    {
        isInFocusMode = isFocused;
        SetRoomHotspotsActive(!isFocused);

        if (cameraNavigator != null)
        {
            cameraNavigator.SetLookAroundEnabled(!isFocused);
            cameraNavigator.SetDebugNumberKeysBlocked(isFocused);
        }
    }

    private void SetRoomHotspotsActive(bool isActive)
    {
        if (roomHotspotsRoot != null)
        {
            roomHotspotsRoot.SetActive(isActive);
        }
    }

    private void UpdateButtonVisibility()
    {
        bool canEnterWorkFocus = !isInFocusMode &&
            cameraNavigator != null &&
            workTableAnchor != null &&
            cameraNavigator.CurrentAnchor == workTableAnchor &&
            !cameraNavigator.IsTransitioning;

        SetButtonVisible(workTableFocusButton, canEnterWorkFocus);
        SetButtonVisible(leaveFocusButton, isInFocusMode);
    }

    private static void SetButtonVisible(Button button, bool isVisible)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(isVisible);
        button.interactable = isVisible;
    }
}
