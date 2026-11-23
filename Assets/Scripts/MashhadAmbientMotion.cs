using UnityEngine;

public class MashhadAmbientMotion : MonoBehaviour
{
    public enum MotionType { Idle, Walk, BusIdle, Pulse }
    public MotionType motionType = MotionType.Idle;
    public Vector3 travelDirection = Vector3.forward;
    public float travelDistance = 1.8f;
    public float speed = 0.65f;
    public float amplitude = 0.045f;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float phase;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        phase = (GetInstanceID() & 1023) * 0.017f;
    }

    private void Update()
    {
        float t = Time.time * speed + phase;
        switch (motionType)
        {
            case MotionType.Idle:
                transform.position = startPosition + Vector3.up * (Mathf.Sin(t * 1.7f) * amplitude);
                transform.rotation = startRotation * Quaternion.Euler(0f, Mathf.Sin(t) * 2.2f, Mathf.Sin(t * .8f) * 1.2f);
                break;
            case MotionType.Walk:
                float along = Mathf.PingPong(t * travelDistance, travelDistance) - travelDistance * .5f;
                transform.position = startPosition + travelDirection.normalized * along + Vector3.up * Mathf.Abs(Mathf.Sin(t * 5f)) * .025f;
                transform.rotation = startRotation * Quaternion.Euler(0f, Mathf.Sin(t * 5f) * 3f, 0f);
                break;
            case MotionType.BusIdle:
                transform.position = startPosition + Vector3.up * Mathf.Sin(t * 1.2f) * .012f;
                transform.rotation = startRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.2f) * .18f);
                break;
            case MotionType.Pulse:
                transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 2f) * .08f);
                break;
        }
    }
}
