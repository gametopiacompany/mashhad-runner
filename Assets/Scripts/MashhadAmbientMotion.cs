using UnityEngine;

public class MashhadAmbientMotion : MonoBehaviour
{
    public enum MotionType { Idle, Walk, BusIdle, Pulse }
    public MotionType motionType = MotionType.Idle;
    public Vector3 travelDirection = Vector3.forward;
    public float travelDistance = 1.8f;
    public float speed = 0.65f;
    public float amplitude = 0.045f;
    public float distantUpdateDistance = 45f;
    public float distantUpdateInterval = 0.1f;

    private Transform cachedTransform;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startScale;
    private Camera viewCamera;
    private float elapsed;
    private float phase;
    private float nextUpdate;
    private float nextCameraLookup;

    private void Start()
    {
        cachedTransform = transform;
        startPosition = cachedTransform.localPosition;
        startRotation = cachedTransform.localRotation;
        startScale = cachedTransform.localScale;
        phase = (GetInstanceID() & 1023) * 0.017f;
        nextUpdate = Time.time;
        RefreshCamera();
    }

    private void RefreshCamera()
    {
        viewCamera = Camera.main;
        nextCameraLookup = Time.time + 1f;
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f) return;
        elapsed += Time.deltaTime;
        if (Time.time < nextUpdate) return;
        if (viewCamera == null && Time.time >= nextCameraLookup) RefreshCamera();

        bool distant = false;
        if (viewCamera != null)
        {
            float threshold = Mathf.Max(0f, distantUpdateDistance);
            distant = (cachedTransform.position - viewCamera.transform.position).sqrMagnitude > threshold * threshold;
        }
        nextUpdate = Time.time + (distant ? Mathf.Max(0.02f, distantUpdateInterval) : 0f);

        float t = elapsed * Mathf.Max(0f, speed);
        Vector3 position = startPosition;
        Quaternion rotation = startRotation;
        Vector3 scale = startScale;

        switch (motionType)
        {
            case MotionType.Idle:
                position.y += (Mathf.Sin(t * 1.7f + phase) - Mathf.Sin(phase)) * amplitude;
                rotation *= Quaternion.Euler(0f, (Mathf.Sin(t + phase) - Mathf.Sin(phase)) * 1.1f, (Mathf.Sin(t * 0.8f + phase) - Mathf.Sin(phase)) * 0.6f);
                break;
            case MotionType.Walk:
                float radius = Mathf.Max(0f, travelDistance) * 0.5f;
                Vector3 direction = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector3.forward;
                float along = Mathf.Sin(t) * radius;
                position += direction * along;
                position.y += Mathf.Abs(Mathf.Sin(t * 5f)) * 0.025f * Mathf.Abs(Mathf.Cos(t));
                float facing = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, -0.2f, Mathf.Cos(t)));
                rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0f, facing * 180f, 0f);
                break;
            case MotionType.BusIdle:
                position.y += (Mathf.Sin(t * 1.2f + phase) - Mathf.Sin(phase)) * 0.006f;
                rotation *= Quaternion.Euler(0f, 0f, (Mathf.Sin(t * 1.2f + phase) - Mathf.Sin(phase)) * 0.09f);
                break;
            case MotionType.Pulse:
                scale = startScale * (1f + (Mathf.Sin(t * 2f + phase) - Mathf.Sin(phase)) * 0.04f);
                break;
        }

        cachedTransform.localPosition = position;
        cachedTransform.localRotation = rotation;
        cachedTransform.localScale = scale;
    }
}
