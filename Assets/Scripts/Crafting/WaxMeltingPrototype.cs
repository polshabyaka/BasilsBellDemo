using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WaxMeltingPrototype : MonoBehaviour
{
    private enum WaxState
    {
        Solid,
        Melting,
        Melted
    }

    [Header("References")]
    [SerializeField] private FocusModeController focusModeController;
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private Collider waxCupCollider;
    [SerializeField] private Collider heatSourceCollider;
    [SerializeField] private GameObject waxChunkRoot;
    [SerializeField] private Transform waxChunkVisual;
    [SerializeField] private GameObject meltedWaxRoot;
    [SerializeField] private Transform meltedWaxVisual;
    [SerializeField] private GameObject heatVisualRoot;
    [SerializeField] private Transform heatVisual;
    [SerializeField] private Text progressText;

    [Header("Melting")]
    [SerializeField, Min(0.1f)] private float meltDuration = 10f;
    [SerializeField, Range(0.01f, 1f)] private float waxChunkEndScale = 0.18f;
    [SerializeField, Range(0.01f, 1f)] private float meltedWaxStartScale = 0.08f;
    [SerializeField] private Vector3 waxChunkLowerOffset = new Vector3(0f, -0.08f, 0f);

    [Header("Heat Visual")]
    [SerializeField] private bool isHeating;
    [SerializeField, Min(0f)] private float heatPulseSpeed = 5f;
    [SerializeField, Range(0f, 0.5f)] private float heatPulseScale = 0.12f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugResetKey = true;
    [SerializeField] private KeyCode debugResetKey = KeyCode.R;

    private WaxState waxState = WaxState.Solid;
    private Vector3 waxChunkInitialScale = Vector3.one;
    private Vector3 waxChunkInitialLocalPosition;
    private Vector3 meltedWaxInitialScale = Vector3.one;
    private Vector3 heatVisualInitialScale = Vector3.one;
    private float meltingProgress;

    public float Progress => meltingProgress;
    public bool IsMelted => waxState == WaxState.Melted;
    public bool IsHeating => isHeating;

    private void OnValidate()
    {
        meltDuration = Mathf.Max(0.1f, meltDuration);
    }

    private void Awake()
    {
        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }

        if (waxChunkVisual != null)
        {
            waxChunkInitialScale = waxChunkVisual.localScale;
            waxChunkInitialLocalPosition = waxChunkVisual.localPosition;
        }

        if (meltedWaxVisual != null)
        {
            meltedWaxInitialScale = meltedWaxVisual.localScale;
        }

        if (heatVisual != null)
        {
            heatVisualInitialScale = heatVisual.localScale;
        }
    }

    private void Start()
    {
        ResetPrototype();
        SetProgressTextVisible(false);
    }

    private void Update()
    {
        if (!IsFocusActive())
        {
            SetHeating(false);
            SetProgressTextVisible(false);
            return;
        }

        SetProgressTextVisible(true);

        if (enableDebugResetKey && Input.GetKeyDown(debugResetKey))
        {
            ResetPrototype();
            return;
        }

        UpdateInput();

        if (isHeating && waxState != WaxState.Melted)
        {
            AddMeltingProgress(Time.deltaTime / meltDuration);
        }

        UpdateHeatVisual();
    }

    public void ResetPrototype()
    {
        waxState = WaxState.Solid;
        meltingProgress = 0f;
        isHeating = false;
        ApplyVisualState();
        UpdateHeatVisual();
        UpdateProgressText();
    }

    private bool IsFocusActive()
    {
        return focusModeController != null && focusModeController.IsInFocusMode;
    }

    private void UpdateInput()
    {
        if (Input.GetMouseButtonDown(0) && CanToggleHeat())
        {
            SetHeating(!isHeating);
        }
    }

    private bool CanToggleHeat()
    {
        if (interactionCamera == null)
        {
            return false;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        return IsColliderHit(waxCupCollider, ray) || IsColliderHit(heatSourceCollider, ray);
    }

    private static bool IsColliderHit(Collider targetCollider, Ray ray)
    {
        return targetCollider != null && targetCollider.Raycast(ray, out _, 100f);
    }

    private void AddMeltingProgress(float progressAmount)
    {
        if (progressAmount <= 0f)
        {
            return;
        }

        meltingProgress = Mathf.Clamp01(meltingProgress + progressAmount);

        if (meltingProgress >= 1f)
        {
            waxState = WaxState.Melted;
        }

        ApplyVisualState();
        UpdateProgressText();
    }

    private void SetHeating(bool shouldHeat)
    {
        if (isHeating == shouldHeat)
        {
            return;
        }

        isHeating = shouldHeat;

        if (waxState != WaxState.Melted)
        {
            waxState = isHeating ? WaxState.Melting : WaxState.Solid;
        }

        UpdateHeatVisual();
        UpdateProgressText();
    }

    private void ApplyVisualState()
    {
        bool isMelted = waxState == WaxState.Melted;

        if (waxChunkRoot != null)
        {
            waxChunkRoot.SetActive(!isMelted);
        }

        if (waxChunkVisual != null)
        {
            float chunkScale = Mathf.Lerp(1f, waxChunkEndScale, meltingProgress);
            waxChunkVisual.localScale = waxChunkInitialScale * chunkScale;
            waxChunkVisual.localPosition = waxChunkInitialLocalPosition + waxChunkLowerOffset * meltingProgress;
        }

        if (meltedWaxRoot != null)
        {
            meltedWaxRoot.SetActive(isMelted || meltingProgress > 0.02f);
        }

        if (meltedWaxVisual != null)
        {
            float meltedScale = Mathf.Lerp(meltedWaxStartScale, 1f, meltingProgress);
            meltedWaxVisual.localScale = meltedWaxInitialScale * meltedScale;
        }
    }

    private void UpdateHeatVisual()
    {
        if (heatVisualRoot != null && heatVisualRoot.activeSelf != isHeating)
        {
            heatVisualRoot.SetActive(isHeating);
        }

        if (heatVisual == null)
        {
            return;
        }

        if (!isHeating)
        {
            heatVisual.localScale = heatVisualInitialScale;
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.time * heatPulseSpeed) * heatPulseScale;
        heatVisual.localScale = heatVisualInitialScale * pulse;
    }

    private void UpdateProgressText()
    {
        if (progressText == null)
        {
            return;
        }

        progressText.text = waxState == WaxState.Melted
            ? $"Melted wax ready\nHeat: {GetHeatText()}"
            : $"Wax: {Mathf.RoundToInt(meltingProgress * 100f)}%\nHeat: {GetHeatText()}";
    }

    private string GetHeatText()
    {
        return isHeating ? "On" : "Off";
    }

    private void SetProgressTextVisible(bool isVisible)
    {
        if (progressText != null && progressText.gameObject.activeSelf != isVisible)
        {
            progressText.gameObject.SetActive(isVisible);
        }
    }
}
