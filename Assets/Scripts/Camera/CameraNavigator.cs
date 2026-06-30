using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraNavigator : MonoBehaviour
{
    [SerializeField] private Transform mainCameraTransform;
    [SerializeField] private Transform overviewAnchor;
    [SerializeField] private Transform workTableAnchor;
    [SerializeField] private Transform shelvesAnchor;
    [SerializeField] private Transform counterAnchor;
    [SerializeField] private Transform personalCornerAnchor;
    [SerializeField] private Transform forestExitAnchor;
    [SerializeField, Min(0f)] private float transitionDuration = 0.65f;
    [SerializeField] private bool enableDebugNumberKeys = true;
    [SerializeField, Range(0f, 179f)] private float maxYaw = 90f;
    [SerializeField, Range(0f, 89f)] private float maxPitchUp = 35f;
    [SerializeField, Range(0f, 89f)] private float maxPitchDown = 35f;
    [SerializeField, FormerlySerializedAs("mouseSensitivity"), Min(0f)] private float lookSensitivity = 3f;

    private Coroutine transitionRoutine;
    private Transform currentAnchor;
    private float lookYaw;
    private float lookPitch;

    private void Awake()
    {
        if (mainCameraTransform == null && Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    private void Start()
    {
        SnapToAnchor(overviewAnchor);
    }

    private void Update()
    {
        HandleDebugNumberKeys();
        HandleLookAround();
    }

    private void OnValidate()
    {
        transitionDuration = Mathf.Max(0f, transitionDuration);
        maxYaw = Mathf.Clamp(maxYaw, 0f, 179f);
        maxPitchUp = Mathf.Clamp(maxPitchUp, 0f, 89f);
        maxPitchDown = Mathf.Clamp(maxPitchDown, 0f, 89f);
        lookSensitivity = Mathf.Max(0f, lookSensitivity);
    }

    private void HandleDebugNumberKeys()
    {
        if (!enableDebugNumberKeys)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            MoveToOverview();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            MoveToWorkTable();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            MoveToShelves();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            MoveToCounter();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
        {
            MoveToPersonalCorner();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
        {
            MoveToForestExit();
        }
    }

    private void HandleLookAround()
    {
        if (mainCameraTransform == null || currentAnchor == null || transitionRoutine != null)
        {
            return;
        }

        if (!Input.GetMouseButton(1))
        {
            return;
        }

        lookYaw = Mathf.Clamp(lookYaw + Input.GetAxis("Mouse X") * lookSensitivity, -maxYaw, maxYaw);
        lookPitch = Mathf.Clamp(lookPitch + Input.GetAxis("Mouse Y") * lookSensitivity, -maxPitchDown, maxPitchUp);
        ApplyLookRotation();
    }

    public void MoveToOverview()
    {
        MoveToAnchor(overviewAnchor);
    }

    public void MoveToWorkTable()
    {
        MoveToAnchor(workTableAnchor);
    }

    public void MoveToShelves()
    {
        MoveToAnchor(shelvesAnchor);
    }

    public void MoveToCounter()
    {
        MoveToAnchor(counterAnchor);
    }

    public void MoveToPersonalCorner()
    {
        MoveToAnchor(personalCornerAnchor);
    }

    public void MoveToForestExit()
    {
        MoveToAnchor(forestExitAnchor);
    }

    public void MoveToAnchor(Transform targetAnchor)
    {
        if (mainCameraTransform == null || targetAnchor == null)
        {
            return;
        }

        ResetLookOffset();
        currentAnchor = targetAnchor;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (transitionDuration <= 0f)
        {
            SnapToAnchor(targetAnchor);
            return;
        }

        transitionRoutine = StartCoroutine(TransitionToAnchor(targetAnchor));
    }

    private IEnumerator TransitionToAnchor(Transform targetAnchor)
    {
        Vector3 startPosition = mainCameraTransform.position;
        Quaternion startRotation = mainCameraTransform.rotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = t * t * (3f - 2f * t);

            mainCameraTransform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, targetAnchor.position, smoothT),
                Quaternion.Slerp(startRotation, targetAnchor.rotation, smoothT));

            yield return null;
        }

        SnapToAnchor(targetAnchor);
        transitionRoutine = null;
    }

    private void SnapToAnchor(Transform targetAnchor)
    {
        if (mainCameraTransform == null || targetAnchor == null)
        {
            return;
        }

        currentAnchor = targetAnchor;
        ResetLookOffset();
        mainCameraTransform.SetPositionAndRotation(targetAnchor.position, targetAnchor.rotation);
    }

    private void ApplyLookRotation()
    {
        if (mainCameraTransform == null || currentAnchor == null)
        {
            return;
        }

        Vector3 anchorForward = currentAnchor.forward;
        float anchorYaw = Mathf.Atan2(anchorForward.x, anchorForward.z) * Mathf.Rad2Deg;
        float anchorPitch = Mathf.Atan2(
            anchorForward.y,
            new Vector2(anchorForward.x, anchorForward.z).magnitude) * Mathf.Rad2Deg;

        // Yaw is applied around world up so a pitched anchor cannot roll the horizon sideways.
        float finalYaw = anchorYaw + lookYaw;

        // Pitch is clamped separately to keep the camera from flipping over.
        float finalPitch = Mathf.Clamp(anchorPitch + lookPitch, -89f, 89f);
        Vector3 finalForward = DirectionFromYawPitch(finalYaw, finalPitch);

        mainCameraTransform.rotation = Quaternion.LookRotation(finalForward, Vector3.up);
    }

    private Vector3 DirectionFromYawPitch(float yawDegrees, float pitchDegrees)
    {
        float yawRadians = yawDegrees * Mathf.Deg2Rad;
        float pitchRadians = pitchDegrees * Mathf.Deg2Rad;
        float horizontalLength = Mathf.Cos(pitchRadians);

        return new Vector3(
            Mathf.Sin(yawRadians) * horizontalLength,
            Mathf.Sin(pitchRadians),
            Mathf.Cos(yawRadians) * horizontalLength).normalized;
    }

    private void ResetLookOffset()
    {
        lookYaw = 0f;
        lookPitch = 0f;
    }
}
