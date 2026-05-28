using UnityEngine;

// Script ini dipasang di BubbleCanvas
// Fungsinya: bubble selalu menghadap kamera
public class BubbleFaceCamera : MonoBehaviour
{
    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
    }

    void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        // Hadapkan bubble ke kamera tanpa miring
        transform.LookAt(
            transform.position + _cam.transform.rotation * Vector3.forward,
            _cam.transform.rotation * Vector3.up
        );
    }
}