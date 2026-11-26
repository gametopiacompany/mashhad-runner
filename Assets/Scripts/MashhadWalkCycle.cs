using UnityEngine;

/// <summary>Swings legs and arms of a pedestrian in step with its real speed. Visual only.</summary>
public sealed class MashhadWalkCycle : MonoBehaviour
{
    public float legAngle = 28f;
    public float armAngle = 20f;
    public float radiansPerMeter = 2.6f;
    public float activeDistance = 60f;

    private Transform legL;
    private Transform legR;
    private Transform armL;
    private Transform armR;
    private Camera viewCamera;
    private Vector3 lastPosition;
    private float phase;
    private float nextLookup;

    private void Start()
    {
        legL = transform.Find("LegPivot_L");
        legR = transform.Find("LegPivot_R");
        armL = transform.Find("ArmPivot_L");
        armR = transform.Find("ArmPivot_R");
        lastPosition = transform.position;
        phase = (GetInstanceID() & 1023) * 0.05f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 position = transform.position;
        float speed = (position - lastPosition).magnitude / dt;
        lastPosition = position;

        if (viewCamera == null && Time.time >= nextLookup)
        {
            viewCamera = Camera.main;
            nextLookup = Time.time + 1f;
        }
        if (viewCamera != null && (position - viewCamera.transform.position).sqrMagnitude > activeDistance * activeDistance) return;

        phase += speed * radiansPerMeter * dt;
        float amount = Mathf.Clamp01(speed / 1.1f);
        float swing = Mathf.Sin(phase) * amount;
        SetAngle(legL, swing * legAngle);
        SetAngle(legR, -swing * legAngle);
        SetAngle(armL, -swing * armAngle);
        SetAngle(armR, swing * armAngle);
    }

    private static void SetAngle(Transform t, float angle)
    {
        if (t != null) t.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }
}
