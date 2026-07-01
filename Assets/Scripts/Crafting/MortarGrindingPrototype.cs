using UnityEngine;
using UnityEngine.UI;

public class MortarGrindingPrototype : MonoBehaviour
{
    [SerializeField] private FocusModeController focusModeController;
    [SerializeField] private GameObject rawHerbRoot;
    [SerializeField] private GameObject powderRoot;
    [SerializeField] private Text progressText;
    [SerializeField, Min(0f)] private float progressPerMeter = 0.65f;
    [SerializeField, Range(0.05f, 1f)] private float rawHerbEndScale = 0.25f;
    [SerializeField, Range(0.01f, 1f)] private float powderStartScale = 0.15f;
    [SerializeField] private bool enableDebugResetKey = true;
    [SerializeField] private KeyCode debugResetKey = KeyCode.R;

    private Vector3 rawHerbInitialScale = Vector3.one;
    private Vector3 powderInitialScale = Vector3.one;
    private float grindingProgress;
    private bool isComplete;

    public float Progress => grindingProgress;
    public bool IsComplete => isComplete;

    private void Awake()
    {
        if (rawHerbRoot != null)
        {
            rawHerbInitialScale = rawHerbRoot.transform.localScale;
        }

        if (powderRoot != null)
        {
            powderInitialScale = powderRoot.transform.localScale;
        }
    }

    private void Start()
    {
        ResetPrototype();
    }

    private void Update()
    {
        bool isFocusActive = IsFocusActive();
        SetProgressTextVisible(isFocusActive);

        if (isFocusActive && enableDebugResetKey && Input.GetKeyDown(debugResetKey))
        {
            ResetPrototype();
        }
    }

    public void RegisterGrindingMovement(float movementDistance)
    {
        if (!IsFocusActive() || isComplete || movementDistance <= 0.001f)
        {
            return;
        }

        grindingProgress = Mathf.Clamp01(grindingProgress + movementDistance * progressPerMeter);

        if (grindingProgress >= 1f)
        {
            isComplete = true;
        }

        ApplyVisualState();
        UpdateProgressText();
    }

    public void ResetPrototype()
    {
        grindingProgress = 0f;
        isComplete = false;
        ApplyVisualState();
        UpdateProgressText();
    }

    private bool IsFocusActive()
    {
        return focusModeController != null && focusModeController.IsInFocusMode;
    }

    private void ApplyVisualState()
    {
        if (rawHerbRoot != null)
        {
            rawHerbRoot.SetActive(!isComplete);
            if (!isComplete)
            {
                rawHerbRoot.transform.localScale = rawHerbInitialScale * Mathf.Lerp(1f, rawHerbEndScale, grindingProgress);
            }
        }

        if (powderRoot != null)
        {
            bool shouldShowPowder = isComplete || grindingProgress > 0.05f;
            powderRoot.SetActive(shouldShowPowder);
            if (shouldShowPowder)
            {
                powderRoot.transform.localScale = powderInitialScale * Mathf.Lerp(powderStartScale, 1f, grindingProgress);
            }
        }
    }

    private void UpdateProgressText()
    {
        if (progressText == null)
        {
            return;
        }

        progressText.text = isComplete
            ? "Ground herb ready"
            : $"Grinding: {Mathf.RoundToInt(grindingProgress * 100f)}%";
    }

    private void SetProgressTextVisible(bool isVisible)
    {
        if (progressText != null && progressText.gameObject.activeSelf != isVisible)
        {
            progressText.gameObject.SetActive(isVisible);
        }
    }
}
