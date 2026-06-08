// Script note: Keeps world-space UI facing the main camera, usually for health bars or floating prompts.
// Comment pass: documents responsibilities and key entry points without changing runtime logic.
using UnityEngine;

// Class responsibility: BillboardUI contains this script's gameplay behavior.
public class BillboardUI : MonoBehaviour
{
    // Runs after Update for camera-facing or smoothed transform work.
    void LateUpdate()
    {
        if (Camera.main == null) return;

        transform.forward = Camera.main.transform.forward;
    }
}