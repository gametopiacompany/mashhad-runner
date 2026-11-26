using UnityEngine;

/// <summary>
/// Adds a procedural jump arc, landing squash and a low backward slide pose on top of the clip animation of the
/// controlled character. Visual only: it never touches the player root or collision.
/// </summary>
public sealed class MashhadPlayerPose : MonoBehaviour
{
    private const float AirDuration = 0.85f;
    private const float LandDuration = 0.2f;

    private CharacterAnimator animator;
    private Transform root;
    private Vector3 baseScale = Vector3.one;
    private Quaternion lastApplied = Quaternion.identity;
    private Quaternion lastDelta = Quaternion.identity;
    private bool hasApplied;
    private bool wasAirborne;
    private float airTime;
    private float landTimer;
    private float slideBlend;

    private void Start()
    {
        animator = GetComponent<CharacterAnimator>();
        root = transform.parent;
        baseScale = transform.localScale;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || root == null || animator == null || !animator.IsReady) return;

        bool airborne = root.position.y > 0.08f;
        if (airborne && !wasAirborne) airTime = 0f;
        if (airborne) airTime += dt;
        if (!airborne && wasAirborne) landTimer = LandDuration;
        if (landTimer > 0f) landTimer -= dt;
        wasAirborne = airborne;

        bool sliding = !airborne && animator.State == CharacterState.Slide;
        slideBlend = Mathf.MoveTowards(slideBlend, sliding ? 1f : 0f, dt / (sliding ? 0.08f : 0.16f));

        float pitch = 0f;
        float stretchY = 1f;
        if (airborne)
        {
            float p = Mathf.Clamp01(airTime / AirDuration);
            float launch = 1f - Mathf.SmoothStep(0f, 1f, p / 0.25f);
            float arc = Mathf.Sin(p * Mathf.PI);
            pitch = Mathf.Lerp(-6f, 0f, Mathf.Clamp01(p / 0.2f)) + arc * 24f;
            stretchY = 1f + 0.07f * launch - 0.09f * arc;
        }
        else if (landTimer > 0f)
        {
            float s = Mathf.Sin(landTimer / LandDuration * Mathf.PI);
            pitch = 10f * s;
            stretchY = 1f - 0.14f * s;
        }

        pitch = Mathf.Lerp(pitch, -72f, slideBlend);
        stretchY = Mathf.Lerp(stretchY, 0.96f, slideBlend);
        float stretchXZ = 1f / Mathf.Sqrt(stretchY);

        Quaternion current = transform.localRotation;
        Quaternion baseRotation = (hasApplied && Quaternion.Angle(current, lastApplied) < 0.01f)
            ? current * Quaternion.Inverse(lastDelta)
            : current;
        Quaternion delta = Quaternion.Euler(pitch, 0f, 0f);
        Quaternion applied = baseRotation * delta;
        transform.localRotation = applied;
        lastApplied = applied;
        lastDelta = delta;
        hasApplied = true;

        transform.localPosition = new Vector3(0f, 0.12f * slideBlend, 0.85f * slideBlend);
        transform.localScale = new Vector3(baseScale.x * stretchXZ, baseScale.y * stretchY, baseScale.z * stretchXZ);
    }
}
