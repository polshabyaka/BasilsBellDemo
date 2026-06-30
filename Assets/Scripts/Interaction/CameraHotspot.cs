using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CameraHotspot : MonoBehaviour
{
    [SerializeField] private CameraNavigator cameraNavigator;
    [SerializeField] private Transform targetAnchor;

    private void OnMouseDown()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (cameraNavigator == null || targetAnchor == null)
        {
            return;
        }

        cameraNavigator.MoveToAnchor(targetAnchor);
    }
}
