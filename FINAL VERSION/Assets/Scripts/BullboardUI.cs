// Purpose: Keeps a UI element facing the camera for readable world-space display.
using UnityEngine;

public class BillboardUI : MonoBehaviour
{

    void LateUpdate()
    {
        if (Camera.main == null) return;

        transform.forward = Camera.main.transform.forward;
    }
}

