// Script note: Smooth combat camera that follows the player and tightens framing near the enemy.
// Comment pass: documents responsibilities and key entry points without changing runtime logic.
using UnityEngine;

// Class responsibility: CombatCameraController contains this script's gameplay behavior.
public class CombatCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Mouse Y View Control")]
    public float verticalSensitivity = 0.08f;
    [Range(0f, 1f)] public float tightView = 0f;

    [Header("Normal View")]
    public Vector3 normalPivotOffset = new Vector3(0f, 1.55f, 0f);
    public Vector3 normalLookOffset = new Vector3(0f, 1.45f, 0.6f);
    public float normalDistance = 4.8f;
    public float normalPitch = 14f;

    [Header("Tight Combat View")]
    public Vector3 tightPivotOffset = new Vector3(0.65f, 1.72f, 0f);
    public Vector3 tightLookOffset = new Vector3(-0.12f, 1.65f, 1.6f);
    public float tightDistance = 2.0f;
    public float tightPitch = 6f;

    [Header("Smoothing")]
    public float positionSmooth = 12f;
    public float rotationSmooth = 12f;

    // Runs after Update for camera-facing or smoothed transform work.
    void LateUpdate()
    {
        if (target == null) return;

        // ������ƣ�tightView ����������ƣ�tightView ��С
        tightView = Mathf.Clamp01(tightView + Input.GetAxis("Mouse Y") * verticalSensitivity);

        // ����������ﳯ��
        Quaternion yawRotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);

        // �������ӽǺͽ��ս���ӽ�֮���ֵ
        Vector3 pivotOffset = Vector3.Lerp(normalPivotOffset, tightPivotOffset, tightView);
        Vector3 lookOffset = Vector3.Lerp(normalLookOffset, tightLookOffset, tightView);
        float distance = Mathf.Lerp(normalDistance, tightDistance, tightView);
        float pitch = Mathf.Lerp(normalPitch, tightPitch, tightView);

        // �������㣨Χ����������ϰ벿�֣�
        Vector3 pivot = target.position + yawRotation * pivotOffset;

        // �������λ��
        Quaternion pitchRotation = yawRotation * Quaternion.Euler(pitch, 0f, 0f);
        Vector3 desiredPosition = pivot - pitchRotation * Vector3.forward * distance;

        // ƽ���ƶ�
        float posLerp = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, posLerp);

        // ����Ŀ��㣨ע���������ƫ���ϰ���+ǰ����
        Vector3 lookTarget = target.position + yawRotation * lookOffset;
        Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);

        float rotLerp = 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotLerp);
    }
}