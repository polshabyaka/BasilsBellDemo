using UnityEngine;
using UnityEngine.UI;

public class MortarGrindingPrototype : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FocusModeController focusModeController;
    [SerializeField] private GameObject rawHerbRoot;
    [SerializeField] private GameObject powderRoot;
    [SerializeField] private Text progressText;

    [Header("Progress Tuning")]
    [SerializeField, Range(0f, 1f)] private float minEffectiveRadius = 0.25f;
    [SerializeField, Range(0f, 1f)] private float maxEffectiveRadius = 0.95f;
    [Tooltip("Progress added per degree of useful circular pestle motion.")]
    [SerializeField, Min(0f)] private float angularProgressMultiplier = 0.00055f;
    [SerializeField, Min(0.01f)] private float maxProgressPerSecond = 0.18f;
    [SerializeField, Range(0f, 1f)] private float directionChangePenalty = 0.2f;
    [SerializeField, Min(0f)] private float minAngularDeltaToCount = 0.5f;

    [Header("Visual Feedback")]
    [SerializeField, Range(0.05f, 1f)] private float rawHerbEndScale = 0.25f;
    [SerializeField, Range(0.01f, 1f)] private float powderStartScale = 0.15f;
    [SerializeField] private bool showMotionHint = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugResetKey = true;
    [SerializeField] private KeyCode debugResetKey = KeyCode.R;

    private Vector3 rawHerbInitialScale = Vector3.one;
    private Vector3 powderInitialScale = Vector3.one;
    private float grindingProgress;
    private bool isComplete;
    private bool hasPreviousContact;
    private float previousAngleDegrees;
    private int angularDirection;
    private string motionHint = "Move in circles";

    public float Progress => grindingProgress;
    public bool IsComplete => isComplete;

    private void OnValidate()
    {
        maxEffectiveRadius = Mathf.Max(minEffectiveRadius + 0.01f, maxEffectiveRadius);
        angularProgressMultiplier = Mathf.Max(0f, angularProgressMultiplier);
        maxProgressPerSecond = Mathf.Max(0.01f, maxProgressPerSecond);
        minAngularDeltaToCount = Mathf.Max(0f, minAngularDeltaToCount);
    }

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
        if (!isFocusActive)
        {
            ResetAngularTracking();
        }

        if (isFocusActive && enableDebugResetKey && Input.GetKeyDown(debugResetKey))
        {
            ResetPrototype();
        }
    }

    public void BeginGrindingStroke(Vector3 contactPosition, Transform grindArea, float grindRadiusX, float grindRadiusZ)
    {
        ResetAngularTracking();

        if (TryGetAngularSample(contactPosition, grindArea, grindRadiusX, grindRadiusZ, out float angleDegrees, out float normalizedRadius) &&
            normalizedRadius >= minEffectiveRadius)
        {
            previousAngleDegrees = angleDegrees;
            hasPreviousContact = true;
        }

        motionHint = "Move in circles";
        UpdateProgressText();
    }

    public void RegisterGrindingContact(Vector3 contactPosition, Transform grindArea, float grindRadiusX, float grindRadiusZ)
    {
        if (!IsFocusActive() || isComplete)
        {
            return;
        }

        if (!TryGetAngularSample(contactPosition, grindArea, grindRadiusX, grindRadiusZ, out float angleDegrees, out float normalizedRadius))
        {
            motionHint = "Move in circles";
            UpdateProgressText();
            return;
        }

        float radiusQuality = CalculateRadiusQuality(normalizedRadius);
        if (radiusQuality <= 0f)
        {
            motionHint = "Too close to center";
            hasPreviousContact = false;
            UpdateProgressText();
            return;
        }

        if (!hasPreviousContact)
        {
            previousAngleDegrees = angleDegrees;
            hasPreviousContact = true;
            motionHint = "Move in circles";
            UpdateProgressText();
            return;
        }

        float signedAngularDelta = Mathf.DeltaAngle(previousAngleDegrees, angleDegrees);
        float angularDelta = Mathf.Abs(signedAngularDelta);
        previousAngleDegrees = angleDegrees;

        if (angularDelta < minAngularDeltaToCount)
        {
            motionHint = "Move in circles";
            UpdateProgressText();
            return;
        }

        int newDirection = signedAngularDelta > 0f ? 1 : -1;
        bool changedDirection = angularDirection != 0 && newDirection != angularDirection;
        angularDirection = newDirection;

        float uncappedProgress = angularDelta * angularProgressMultiplier * radiusQuality;
        float cappedProgress = Mathf.Min(uncappedProgress, maxProgressPerSecond * Mathf.Max(Time.deltaTime, 0.0001f));

        if (changedDirection)
        {
            cappedProgress *= directionChangePenalty;
        }

        AddProgress(cappedProgress);
        motionHint = changedDirection ? "Keep a steady circle" : "Good circular motion";
        UpdateProgressText();
    }

    public void EndGrindingStroke()
    {
        ResetAngularTracking();
    }

    public void ResetPrototype()
    {
        grindingProgress = 0f;
        isComplete = false;
        motionHint = "Move in circles";
        ResetAngularTracking();
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

    private bool TryGetAngularSample(
        Vector3 contactPosition,
        Transform grindArea,
        float grindRadiusX,
        float grindRadiusZ,
        out float angleDegrees,
        out float normalizedRadius)
    {
        angleDegrees = 0f;
        normalizedRadius = 0f;

        if (grindArea == null || grindRadiusX <= 0f || grindRadiusZ <= 0f)
        {
            return false;
        }

        Vector3 localPoint = Quaternion.Inverse(grindArea.rotation) * (contactPosition - grindArea.position);
        Vector2 normalizedPoint = new Vector2(localPoint.x / grindRadiusX, localPoint.z / grindRadiusZ);
        normalizedRadius = normalizedPoint.magnitude;
        angleDegrees = Mathf.Atan2(normalizedPoint.y, normalizedPoint.x) * Mathf.Rad2Deg;
        return true;
    }

    private float CalculateRadiusQuality(float normalizedRadius)
    {
        if (normalizedRadius < minEffectiveRadius)
        {
            return 0f;
        }

        float fullStrengthRadius = Mathf.Min(maxEffectiveRadius, minEffectiveRadius + 0.15f);
        float quality = fullStrengthRadius > minEffectiveRadius
            ? Mathf.InverseLerp(minEffectiveRadius, fullStrengthRadius, normalizedRadius)
            : 1f;

        if (normalizedRadius > maxEffectiveRadius && maxEffectiveRadius < 1f)
        {
            float edgeAmount = Mathf.InverseLerp(maxEffectiveRadius, 1f, Mathf.Min(normalizedRadius, 1f));
            quality *= Mathf.Lerp(1f, 0.55f, edgeAmount);
        }

        return Mathf.Clamp01(quality);
    }

    private void AddProgress(float progressAmount)
    {
        if (progressAmount <= 0f)
        {
            return;
        }

        grindingProgress = Mathf.Clamp01(grindingProgress + progressAmount);

        if (grindingProgress >= 1f)
        {
            isComplete = true;
        }

        ApplyVisualState();
    }

    private void ResetAngularTracking()
    {
        hasPreviousContact = false;
        angularDirection = 0;
    }

    private void UpdateProgressText()
    {
        if (progressText == null)
        {
            return;
        }

        progressText.text = isComplete
            ? "Ground herb ready"
            : GetProgressText();
    }

    private string GetProgressText()
    {
        string percentageText = $"Grinding: {Mathf.RoundToInt(grindingProgress * 100f)}%";
        return showMotionHint && !string.IsNullOrEmpty(motionHint)
            ? $"{percentageText}\n{motionHint}"
            : percentageText;
    }

    private void SetProgressTextVisible(bool isVisible)
    {
        if (progressText != null && progressText.gameObject.activeSelf != isVisible)
        {
            progressText.gameObject.SetActive(isVisible);
        }
    }
}
