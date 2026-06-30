using System.Collections;
using UnityEngine;

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

    private Coroutine transitionRoutine;

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

        mainCameraTransform.SetPositionAndRotation(targetAnchor.position, targetAnchor.rotation);
    }
}
