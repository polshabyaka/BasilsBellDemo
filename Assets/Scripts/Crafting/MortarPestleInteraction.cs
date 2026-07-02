using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MortarPestleInteraction : MonoBehaviour
{
    private enum PestleState
    {
        Resting,
        PickingUp,
        Grinding,
        Returning
    }

    [Header("References")]
    [SerializeField] private FocusModeController focusModeController;
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private Transform mortarGrindArea;
    [SerializeField] private Transform mortarGrindCenter;
    [SerializeField] private Transform pestleContactPoint;
    [SerializeField] private Transform pestleVisualRoot;
    [SerializeField] private Transform pestleVisual;
    [SerializeField] private Transform pestleHandleGuideRightHand;
    [SerializeField] private Transform pestleRestPoint;
    [SerializeField] private Transform pestleWorkPosePoint;
    [SerializeField] private Collider grindInputCollider;
    [SerializeField] private MortarGrindingPrototype grindingPrototype;
    [SerializeField] private Text progressText;

    [Header("Grind Area")]
    [Tooltip("Visible bowl/opening radius on local X before padding is applied.")]
    [SerializeField, Min(0.01f)] private float grindRadiusX = 0.32f;
    [Tooltip("Visible bowl/opening radius on local Z before padding is applied.")]
    [SerializeField, Min(0.01f)] private float grindRadiusZ = 0.24f;
    [Tooltip("Inset from the visible bowl radius so the pestle does not grind through the rim.")]
    [SerializeField, Min(0f)] private float grindAreaPadding = 0.04f;

    [Header("Input Mapping")]
    [Tooltip("Centers grinding input on the camera-projected MortarGrindCenter so cursor circles match the visible bowl.")]
    [SerializeField] private bool useScreenSpaceGrindingInput = true;

    [Header("Pestle Pose")]
    [SerializeField, Range(1f, 89f)] private float minTiltAboveHorizontal = 50f;
    [SerializeField, Range(1f, 89f)] private float maxTiltAboveHorizontal = 75f;
    [SerializeField] private Vector3 rightHandLeanOffset = new Vector3(0.58f, 0.72f, -0.08f);
    [Tooltip("Visual-only length of the pestle cylinder. Gameplay uses the contact point and grind area.")]
    [SerializeField, Min(0.05f)] private float pestleLength = 0.78f;
    [Tooltip("Visual-only radius of the pestle cylinder. Gameplay uses the contact point and grind area.")]
    [SerializeField, Min(0.01f)] private float pestleRadius = 0.095f;
    [Tooltip("Used only if no MortarGrindingPrototype is assigned.")]
    [SerializeField, Min(0f)] private float fallbackGrindProgressPerUnit = 0.14f;

    [Header("Pickup / Return")]
    [SerializeField, Min(0f)] private float pickupDuration = 0.22f;
    [SerializeField, Min(0f)] private float returnDuration = 0.3f;
    [SerializeField, Min(0f)] private float grindingStartGraceDuration = 0.06f;

    private PestleState pestleState = PestleState.Resting;
    private float grindingProgress;
    private Vector3 lastContactPosition;
    private Vector3 transitionStartPosition;
    private Quaternion transitionStartRotation = Quaternion.identity;
    private float transitionElapsed;
    private Vector3 dragPlaneOffset;
    private float grindingInputGraceTimer;

    private void OnValidate()
    {
        grindRadiusX = Mathf.Max(0.01f, grindRadiusX);
        grindRadiusZ = Mathf.Max(0.01f, grindRadiusZ);
        grindAreaPadding = Mathf.Max(0f, grindAreaPadding);
        maxTiltAboveHorizontal = Mathf.Max(minTiltAboveHorizontal, maxTiltAboveHorizontal);
        pestleLength = Mathf.Max(0.05f, pestleLength);
        pestleRadius = Mathf.Max(0.01f, pestleRadius);
        fallbackGrindProgressPerUnit = Mathf.Max(0f, fallbackGrindProgressPerUnit);
        pickupDuration = Mathf.Max(0f, pickupDuration);
        returnDuration = Mathf.Max(0f, returnDuration);
        grindingStartGraceDuration = Mathf.Max(0f, grindingStartGraceDuration);
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
        ConfigureVisualShape();
        SnapToRest();
        UpdateProgressText();
        SetProgressTextVisible(false);
    }

    private void Update()
    {
        if (!IsFocusActive())
        {
            if (pestleState != PestleState.Resting)
            {
                EndGrindingStrokeIfNeeded();
                SnapToRest();
            }

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

        switch (pestleState)
        {
            case PestleState.Resting:
                UpdateResting();
                break;
            case PestleState.PickingUp:
                UpdatePickingUp();
                break;
            case PestleState.Grinding:
                UpdateGrinding();
                break;
            case PestleState.Returning:
                UpdateReturning();
                break;
        }
    }

    private bool IsFocusActive()
    {
        return focusModeController != null && focusModeController.IsInFocusMode;
    }

    private Transform GetGrindCenterTransform()
    {
        return mortarGrindCenter != null ? mortarGrindCenter : mortarGrindArea;
    }

    private float GetEffectiveGrindRadiusX()
    {
        return Mathf.Max(0.01f, grindRadiusX - grindAreaPadding);
    }

    private float GetEffectiveGrindRadiusZ()
    {
        return Mathf.Max(0.01f, grindRadiusZ - grindAreaPadding);
    }

    private void UpdateResting()
    {
        if (Input.GetMouseButtonDown(0) && CanStartDrag())
        {
            BeginPickup();
        }
    }

    private void UpdatePickingUp()
    {
        if (!Input.GetMouseButton(0))
        {
            BeginReturn();
            return;
        }

        Vector3 workPosition = GetPickupTargetPosition();
        Quaternion workRotation = GetGrindingRotation(workPosition);

        if (UpdateTransition(workPosition, workRotation, pickupDuration))
        {
            SetPestleRootPose(workPosition, workRotation);
            pestleState = PestleState.Grinding;
            InitializeGrindingInputFromCurrentPose(workPosition);
        }
    }

    private void UpdateGrinding()
    {
        if (!Input.GetMouseButton(0))
        {
            BeginReturn();
            return;
        }

        bool wasInInputGrace = grindingInputGraceTimer > 0f;
        MoveContactPointFromMouse();

        if (wasInInputGrace)
        {
            grindingInputGraceTimer = Mathf.Max(0f, grindingInputGraceTimer - Time.deltaTime);

            if (grindingInputGraceTimer <= 0f && grindingPrototype != null)
            {
                grindingPrototype.BeginGrindingStroke(
                    lastContactPosition,
                    GetGrindCenterTransform(),
                    GetEffectiveGrindRadiusX(),
                    GetEffectiveGrindRadiusZ());
            }
        }
    }

    private void UpdateReturning()
    {
        if (UpdateTransition(GetRestPosition(), GetRestRotation(), returnDuration))
        {
            SnapToRest();
        }
    }

    private void BeginPickup()
    {
        transitionStartPosition = GetPestleRootPosition();
        transitionStartRotation = GetPestleRootRotation();
        transitionElapsed = 0f;
        pestleState = PestleState.PickingUp;
    }

    private void BeginReturn()
    {
        EndGrindingStrokeIfNeeded();
        grindingInputGraceTimer = 0f;
        transitionStartPosition = GetPestleRootPosition();
        transitionStartRotation = GetPestleRootRotation();
        transitionElapsed = 0f;
        pestleState = PestleState.Returning;
    }

    private void EndGrindingStrokeIfNeeded()
    {
        if (pestleState == PestleState.Grinding && grindingPrototype != null)
        {
            grindingPrototype.EndGrindingStroke();
        }
    }

    private bool UpdateTransition(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        transitionElapsed += Time.deltaTime;
        float normalizedTime = duration <= 0f ? 1f : Mathf.Clamp01(transitionElapsed / duration);
        float smoothTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

        SetPestleRootPose(
            Vector3.Lerp(transitionStartPosition, targetPosition, smoothTime),
            Quaternion.Slerp(transitionStartRotation, targetRotation, smoothTime));

        return normalizedTime >= 1f;
    }

    private void InitializeGrindingInputFromCurrentPose(Vector3 contactPosition)
    {
        lastContactPosition = contactPosition;
        grindingInputGraceTimer = grindingStartGraceDuration;
        dragPlaneOffset = Vector3.zero;

        if (TryGetMousePointOnGrindPlane(out Vector3 mousePlanePoint))
        {
            Transform grindCenter = GetGrindCenterTransform();
            Vector3 planeNormal = grindCenter != null ? grindCenter.up : Vector3.up;
            dragPlaneOffset = Vector3.ProjectOnPlane(contactPosition - mousePlanePoint, planeNormal);
        }
        else
        {
            dragPlaneOffset = Vector3.zero;
        }

        if (grindingPrototype != null)
        {
            grindingPrototype.BeginGrindingStroke(
                contactPosition,
                GetGrindCenterTransform(),
                GetEffectiveGrindRadiusX(),
                GetEffectiveGrindRadiusZ());
        }
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

        Collider visualCollider = pestleVisual != null ? pestleVisual.GetComponentInChildren<Collider>() : null;
        if (visualCollider == null)
        {
            return false;
        }

        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        return visualCollider.Raycast(ray, out _, 100f);
    }

    private void MoveContactPointFromMouse()
    {
        if (useScreenSpaceGrindingInput && TryGetScreenSpaceContactPoint(out Vector3 screenContactPoint))
        {
            MoveContactPoint(screenContactPoint);
            return;
        }

        if (!TryGetMousePointOnGrindPlane(out Vector3 planePoint))
        {
            return;
        }

        Vector3 nextContactPosition = ClampToGrindArea(planePoint + dragPlaneOffset);
        MoveContactPoint(nextContactPosition);
    }

    private Vector3 GetPickupTargetPosition()
    {
        if (useScreenSpaceGrindingInput && TryGetScreenSpaceContactPoint(out Vector3 screenContactPoint))
        {
            return screenContactPoint;
        }

        return GetWorkPosePosition();
    }

    private bool TryGetScreenSpaceContactPoint(out Vector3 contactPoint)
    {
        contactPoint = Vector3.zero;

        // Center mouse input on Camera.WorldToScreenPoint(MortarGrindCenter.position),
        // then solve that screen delta back into mortar-local X/Z movement.
        if (!TryGetScreenSpaceGrindBasis(
            out Transform grindCenter,
            out float effectiveRadiusX,
            out float effectiveRadiusZ,
            out Vector2 screenCenter,
            out Vector2 screenRightAxis,
            out Vector2 screenForwardAxis))
        {
            return false;
        }

        Vector2 mousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        if (!TryGetNormalizedScreenGrindPosition(
            mousePosition,
            screenCenter,
            screenRightAxis,
            screenForwardAxis,
            out Vector2 normalizedPosition))
        {
            return false;
        }

        if (normalizedPosition.sqrMagnitude > 1f)
        {
            normalizedPosition.Normalize();
        }

        Vector3 localContactPosition = new Vector3(
            normalizedPosition.x * effectiveRadiusX,
            0f,
            normalizedPosition.y * effectiveRadiusZ);

        contactPoint = grindCenter.position + grindCenter.rotation * localContactPosition;
        return true;
    }

    private bool TryGetScreenSpaceGrindBasis(
        out Transform grindCenter,
        out float effectiveRadiusX,
        out float effectiveRadiusZ,
        out Vector2 screenCenter,
        out Vector2 screenRightAxis,
        out Vector2 screenForwardAxis)
    {
        grindCenter = GetGrindCenterTransform();
        effectiveRadiusX = GetEffectiveGrindRadiusX();
        effectiveRadiusZ = GetEffectiveGrindRadiusZ();
        screenCenter = Vector2.zero;
        screenRightAxis = Vector2.zero;
        screenForwardAxis = Vector2.zero;

        if (interactionCamera == null || grindCenter == null)
        {
            return false;
        }

        Vector3 worldCenter = grindCenter.position;
        Vector3 centerScreenPoint = interactionCamera.WorldToScreenPoint(worldCenter);
        Vector3 rightScreenPoint = interactionCamera.WorldToScreenPoint(worldCenter + grindCenter.right * effectiveRadiusX);
        Vector3 forwardScreenPoint = interactionCamera.WorldToScreenPoint(worldCenter + grindCenter.forward * effectiveRadiusZ);

        if (centerScreenPoint.z <= 0f || rightScreenPoint.z <= 0f || forwardScreenPoint.z <= 0f)
        {
            return false;
        }

        screenCenter = new Vector2(centerScreenPoint.x, centerScreenPoint.y);
        screenRightAxis = new Vector2(rightScreenPoint.x, rightScreenPoint.y) - screenCenter;
        screenForwardAxis = new Vector2(forwardScreenPoint.x, forwardScreenPoint.y) - screenCenter;

        float determinant = GetScreenBasisDeterminant(screenRightAxis, screenForwardAxis);
        return screenRightAxis.sqrMagnitude > 1f
            && screenForwardAxis.sqrMagnitude > 1f
            && Mathf.Abs(determinant) > 0.001f;
    }

    private bool TryGetNormalizedScreenGrindPosition(
        Vector2 screenPosition,
        Vector2 screenCenter,
        Vector2 screenRightAxis,
        Vector2 screenForwardAxis,
        out Vector2 normalizedPosition)
    {
        normalizedPosition = Vector2.zero;
        float determinant = GetScreenBasisDeterminant(screenRightAxis, screenForwardAxis);

        if (Mathf.Abs(determinant) <= 0.001f)
        {
            return false;
        }

        Vector2 screenDelta = screenPosition - screenCenter;
        normalizedPosition = new Vector2(
            (screenDelta.x * screenForwardAxis.y - screenDelta.y * screenForwardAxis.x) / determinant,
            (screenRightAxis.x * screenDelta.y - screenRightAxis.y * screenDelta.x) / determinant);
        return true;
    }

    private float GetScreenBasisDeterminant(Vector2 screenRightAxis, Vector2 screenForwardAxis)
    {
        return screenRightAxis.x * screenForwardAxis.y - screenRightAxis.y * screenForwardAxis.x;
    }

    private bool TryGetMousePointOnGrindPlane(out Vector3 point)
    {
        point = Vector3.zero;

        Transform grindCenter = GetGrindCenterTransform();
        if (interactionCamera == null || grindCenter == null)
        {
            return false;
        }

        Plane grindPlane = new Plane(grindCenter.up, grindCenter.position);
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
        Transform grindCenter = GetGrindCenterTransform();
        if (grindCenter == null)
        {
            return worldPoint;
        }

        float effectiveRadiusX = GetEffectiveGrindRadiusX();
        float effectiveRadiusZ = GetEffectiveGrindRadiusZ();
        Vector3 localPoint = Quaternion.Inverse(grindCenter.rotation) * (worldPoint - grindCenter.position);
        Vector2 normalized = new Vector2(localPoint.x / effectiveRadiusX, localPoint.z / effectiveRadiusZ);

        if (normalized.sqrMagnitude > 1f)
        {
            normalized.Normalize();
        }

        Vector3 clampedLocal = new Vector3(
            normalized.x * effectiveRadiusX,
            0f,
            normalized.y * effectiveRadiusZ);

        // Use position and rotation only so the locator's scale can stay a visual/debug aid.
        return grindCenter.position + grindCenter.rotation * clampedLocal;
    }

    private void MoveContactPoint(Vector3 nextContactPosition)
    {
        float movementDistance = Vector3.Distance(lastContactPosition, nextContactPosition);
        SetGrindingPose(nextContactPosition);

        if (pestleState == PestleState.Grinding)
        {
            AddProgress(nextContactPosition, movementDistance);
        }

        lastContactPosition = nextContactPosition;
    }

    private void SetGrindingPose(Vector3 contactPosition)
    {
        SetPestleRootPose(contactPosition, GetGrindingRotation(contactPosition));
    }

    private Quaternion GetGrindingRotation(Vector3 contactPosition)
    {
        Vector3 guidePosition = GetRightHandGuidePosition(contactPosition);
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
        return Quaternion.FromToRotation(Vector3.up, pestleDirection.normalized);
    }

    private Vector3 GetRightHandGuidePosition(Vector3 contactPosition)
    {
        if (pestleHandleGuideRightHand != null)
        {
            return pestleHandleGuideRightHand.position;
        }

        Transform grindCenter = GetGrindCenterTransform();
        if (grindCenter != null)
        {
            return grindCenter.position + grindCenter.rotation * rightHandLeanOffset;
        }

        return contactPosition + rightHandLeanOffset;
    }

    private Vector3 GetWorkPosePosition()
    {
        if (pestleWorkPosePoint != null)
        {
            return pestleWorkPosePoint.position;
        }

        Transform grindCenter = GetGrindCenterTransform();
        return grindCenter != null ? grindCenter.position : GetPestleRootPosition();
    }

    private Vector3 GetRestPosition()
    {
        return pestleRestPoint != null ? pestleRestPoint.position : GetPestleRootPosition();
    }

    private Quaternion GetRestRotation()
    {
        return pestleRestPoint != null ? pestleRestPoint.rotation : GetPestleRootRotation();
    }

    private Vector3 GetPestleRootPosition()
    {
        return pestleVisualRoot != null ? pestleVisualRoot.position : transform.position;
    }

    private Quaternion GetPestleRootRotation()
    {
        return pestleVisualRoot != null ? pestleVisualRoot.rotation : transform.rotation;
    }

    private void SnapToRest()
    {
        grindingInputGraceTimer = 0f;
        dragPlaneOffset = Vector3.zero;
        SetPestleRootPose(GetRestPosition(), GetRestRotation());
        lastContactPosition = GetPestleRootPosition();
        pestleState = PestleState.Resting;
    }

    private void SetPestleRootPose(Vector3 position, Quaternion rotation)
    {
        if (pestleVisualRoot != null)
        {
            pestleVisualRoot.SetPositionAndRotation(position, rotation);
        }

        if (pestleContactPoint != null)
        {
            pestleContactPoint.position = position;
        }

        ConfigureVisualShape();
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

    private void AddProgress(Vector3 contactPosition, float movementDistance)
    {
        if (movementDistance <= 0.001f)
        {
            return;
        }

        if (grindingPrototype != null)
        {
            if (grindingInputGraceTimer > 0f)
            {
                return;
            }

            grindingPrototype.RegisterGrindingContact(
                contactPosition,
                GetGrindCenterTransform(),
                GetEffectiveGrindRadiusX(),
                GetEffectiveGrindRadiusZ());
            return;
        }

        if (grindingInputGraceTimer > 0f)
        {
            return;
        }

        grindingProgress = Mathf.Clamp01(grindingProgress + movementDistance * fallbackGrindProgressPerUnit);
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
        Transform grindCenter = GetGrindCenterTransform();
        if (grindCenter == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        DrawEllipseGizmo(grindCenter, grindRadiusX, grindRadiusZ);

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.95f);
        DrawEllipseGizmo(grindCenter, GetEffectiveGrindRadiusX(), GetEffectiveGrindRadiusZ());

        if (grindingPrototype != null)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.75f);
            DrawEllipseGizmo(
                grindCenter,
                GetEffectiveGrindRadiusX() * grindingPrototype.MinEffectiveRadius,
                GetEffectiveGrindRadiusZ() * grindingPrototype.MinEffectiveRadius);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(grindCenter.position, 0.025f);

        if (pestleContactPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(pestleContactPoint.position, 0.035f);
        }
    }

    private void DrawEllipseGizmo(Transform center, float radiusX, float radiusZ)
    {
        const int SegmentCount = 40;
        Vector3 previousPoint = GetEllipsePoint(center, radiusX, radiusZ, 0);

        for (int i = 1; i <= SegmentCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / SegmentCount;
            Vector3 nextPoint = GetEllipsePoint(center, radiusX, radiusZ, angle);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }

    private Vector3 GetEllipsePoint(Transform center, float radiusX, float radiusZ, float angleRadians)
    {
        Vector3 localPoint = new Vector3(
            Mathf.Cos(angleRadians) * radiusX,
            0f,
            Mathf.Sin(angleRadians) * radiusZ);

        return center.position + center.rotation * localPoint;
    }
}
