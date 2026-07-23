using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null)
            return;

        Vector3 dir = cam.transform.position - transform.position;
        dir.y = 0f;

        transform.forward = -dir.normalized;
    }
}