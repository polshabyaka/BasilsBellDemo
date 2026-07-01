using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MortarPestleInteraction : MonoBehaviour
{
    [SerializeField] private FocusModeController focusModeController;
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private Transform mortarGrindArea;
    [SerializeField] private Transform pestleContactPoint;
    [SerializeField] private Transform pestleVisualRoot;
    [SerializeField] private Transform pestleVisual;
    [SerializeField] private Transform pestleHandleGuideRightHand;
    [SerializeField] private Collider grindInputCollider;
    [SerializeField] private MortarGrindingPrototype grindingPrototype;
    [SerializeField] private Text progressText;
    [SerializeField, Min(0.01f)] private float grindRadiusX = 0.32f;
    [SerializeField, Min(0.01f)] private float grindRadiusZ = 0.24f;
    [SerializeField, Range(1f, 89f)] private float minTiltAboveHorizontal = 50f;
    [SerializeField, Range(1f, 89f)] private float maxTiltAboveHorizontal = 75f;
    [SerializeField] private Vector3 rightHandLeanOffset = new Vector3(0.58f, 0.72f, -0.08f);
    [SerializeField, Min(0.05f)] private float pestleLength = 1.04f;
    [SerializeField, Min(0.01f)] private float pestleRadius = 0.06f;
    [SerializeField, Min(0f)] private float progressPerMeter = 0.65f;

    private bool isDragging;
    private float grindingProgress;
    private Vector3 lastContactPosition;

    private void OnValidate()
    {
        grindRadiusX = Mathf.Max(0.01f, grindRadiusX);
        grindRadiusZ = Mathf.Max(0.01f, grindRadiusZ);
        maxTiltAboveHorizontal = Mathf.Max(minTiltAboveHorizontal, maxTiltAboveHorizontal);
        pestleLength = Mathf.Max(0.05f, pestleLength);
        pestleRadius = Mathf.Max(0.01f, pestleRadius);
        progressPerMeter = Mathf.Max(0f, progressPerMeter);
    }

    private void Awake()
    {
        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }

        if (grindInputCollider == null && mortarGrindArea != null)
        {
            grindInputCollider = mortarGrindArea.GetComponentInChildren<Collider>();
        }
    }

    private void Start()
    {
        ResetContactToCenter();
        UpdatePestlePose();
        UpdateProgressText();
        SetProgressTextVisible(false);
    }

    private void Update()
    {
        if (!IsFocusActive())
        {
            isDragging = false;
            if (grindingPrototype == null)
            {
                SetProgressTextVisible(false);
            }
            return;
        }

        if (grindingPrototype == null)
        {
            SetProgressTextVisible(true);
        }

        if (Input.GetMouseButtonDown(0) && CanStartDrag())
        {
            isDragging = true;
            lastContactPosition = GetContactPosition();
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            MoveContactPointFromMouse();
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    private bool IsFocusActive()
    {
        return focusModeController != null && focusModeController.IsInFocusMode;
    }

    private bool CanStartDrag()
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
        Collider visualCollider = pestleVisual != null ? pestleVisual.GetComponentInChildren<Collider>() : null;

        return (grindInputCollider != null && grindInputCollider.Raycast(ray, out _, 100f)) ||
            (visualCollider != null && visualCollider.Raycast(ray, out _, 100f));
    }

    private void MoveContactPointFromMouse()
    {
        if (!TryGetMousePointOnGrindPlane(out Vector3 planePoint))
        {
            return;
        }

        Vector3 nextContactPosition = ClampToGrindArea(planePoint);
        MoveContactPoint(nextContactPosition);
    }

    private bool TryGetMousePointOnGrindPlane(out Vector3 point)
    {
        point = Vector3.zero;

        if (interactionCamera == null || mortarGrindArea == null)
        {
            return false;
        }

        Plane grindPlane = new Plane(mortarGrindArea.up, mortarGrindArea.position);
        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);

        if (!grindPlane.Raycast(ray, out float enter))
        {
            return false;
        }

        point = ray.GetPoint(enter);
        return true;
    }

    private Vector3 ClampToGrindArea(Vector3 worldPoint)
    {
        Vector3 localPoint = Quaternion.Inverse(mortarGrindArea.rotation) * (worldPoint - mortarGrindArea.position);
        Vector2 normalized = new Vector2(localPoint.x / grindRadiusX, localPoint.z / grindRadiusZ);

        if (normalized.sqrMagnitude > 1f)
        {
            normalized.Normalize();
        }

        Vector3 clampedLocal = new Vector3(
            normalized.x * grindRadiusX,
            0f,
            normalized.y * grindRadiusZ);

        // Use position and rotation only so the locator's scale can stay a visual/debug aid.
        return mortarGrindArea.position + mortarGrindArea.rotation * clampedLocal;
    }

    private void MoveContactPoint(Vector3 nextContactPosition)
    {
        if (pestleContactPoint == null)
        {
            return;
        }

        float movementDistance = Vector3.Distance(lastContactPosition, nextContactPosition);
        pestleContactPoint.position = nextContactPosition;
        UpdatePestlePose();

        if (isDragging)
        {
            AddProgress(movementDistance);
        }

        lastContactPosition = nextContactPosition;
    }

    private void UpdatePestlePose()
    {
        if (pestleVisualRoot == null || pestleContactPoint == null)
        {
            return;
        }

        Vector3 contactPosition = pestleContactPoint.position;
        Vector3 guidePosition = GetRightHandGuidePosition();
        Vector3 horizontalDirection = Vector3.ProjectOnPlane(guidePosition - contactPosition, Vector3.up);

        if (horizontalDirection.sqrMagnitude < 0.0001f)
        {
            horizontalDirection = Vector3.right;
        }

        horizontalDirection.Normalize();

        float verticalOffset = Mathf.Max(0.01f, guidePosition.y - contactPosition.y);
        float horizontalDistance = Mathf.Max(0.01f, Vector3.Distance(
            Vector3.ProjectOnPlane(guidePosition, Vector3.up),
            Vector3.ProjectOnPlane(contactPosition, Vector3.up)));
        float tiltAboveHorizontal = Mathf.Atan2(verticalOffset, horizontalDistance) * Mathf.Rad2Deg;
        tiltAboveHorizontal = Mathf.Clamp(tiltAboveHorizontal, minTiltAboveHorizontal, maxTiltAboveHorizontal);

        float tiltRadians = tiltAboveHorizontal * Mathf.Deg2Rad;
        Vector3 pestleDirection = horizontalDirection * Mathf.Cos(tiltRadians) + Vector3.up * Mathf.Sin(tiltRadians);

        pestleVisualRoot.SetPositionAndRotation(
            contactPosition,
            Quaternion.FromToRotation(Vector3.up, pestleDirection.normalized));

        ConfigureVisualShape();
    }

    private Vector3 GetRightHandGuidePosition()
    {
        if (pestleHandleGuideRightHand != null)
        {
            return pestleHandleGuideRightHand.position;
        }

        if (mortarGrindArea != null)
        {
            return mortarGrindArea.position + mortarGrindArea.rotation * rightHandLeanOffset;
        }

        return GetContactPosition() + rightHandLeanOffset;
    }

    private Vector3 GetContactPosition()
    {
        if (pestleContactPoint != null)
        {
            return pestleContactPoint.position;
        }

        return mortarGrindArea != null ? mortarGrindArea.position : transform.position;
    }

    private void ResetContactToCenter()
    {
        if (pestleContactPoint == null || mortarGrindArea == null)
        {
            return;
        }

        pestleContactPoint.position = mortarGrindArea.position;
        lastContactPosition = pestleContactPoint.position;
    }

    private void ConfigureVisualShape()
    {
        if (pestleVisual == null)
        {
            return;
        }

        pestleVisual.localPosition = Vector3.up * (pestleLength * 0.5f);
        pestleVisual.localRotation = Quaternion.identity;
        pestleVisual.localScale = new Vector3(pestleRadius, pestleLength * 0.5f, pestleRadius);
    }

    private void AddProgress(float movementDistance)
    {
        if (movementDistance <= 0.001f)
        {
            return;
        }

        if (grindingPrototype != null)
        {
            grindingPrototype.RegisterGrindingMovement(movementDistance);
            return;
        }

        grindingProgress = Mathf.Clamp01(grindingProgress + movementDistance * progressPerMeter);
        UpdateProgressText();
    }

    private void UpdateProgressText()
    {
        if (progressText != null)
        {
            progressText.text = $"Grinding: {Mathf.RoundToInt(grindingProgress * 100f)}%";
        }
    }

    private void SetProgressTextVisible(bool isVisible)
    {
        if (progressText != null && progressText.gameObject.activeSelf != isVisible)
        {
            progressText.gameObject.SetActive(isVisible);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (mortarGrindArea == null)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        const int SegmentCount = 40;
        Vector3 previousPoint = GetEllipsePoint(0);

        for (int i = 1; i <= SegmentCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / SegmentCount;
            Vector3 nextPoint = GetEllipsePoint(angle);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }

        if (pestleContactPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(pestleContactPoint.position, 0.035f);
        }
    }

    private Vector3 GetEllipsePoint(float angleRadians)
    {
        Vector3 localPoint = new Vector3(
            Mathf.Cos(angleRadians) * grindRadiusX,
            0f,
            Mathf.Sin(angleRadians) * grindRadiusZ);

        return mortarGrindArea.position + mortarGrindArea.rotation * localPoint;
    }
}
