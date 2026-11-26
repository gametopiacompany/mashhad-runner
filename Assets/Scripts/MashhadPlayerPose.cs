using UnityEngine;

/// <summary>
/// Procedural jump and slide poses for the controlled character, layered on top of the clip animation.
/// Legs and arms are rotated around the visual's right axis (found by bone name when possible), the whole body gets
/// a pitch arc and squash/stretch. If no leg bones are found the original jump clip is used. Visual only.
/// </summary>
public sealed class MashhadPlayerPose : MonoBehaviour
{
    private const float AirDuration = 0.85f;
    private const float LandDuration = 0.2f;

    private CharacterAnimator animator;
    private Transform root;
    private Transform legL;
    private Transform legR;
    private Transform armL;
    private Transform armR;
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
        FindBones();
        if (animator != null) animator.SuppressJump = legL != null || legR != null;
    }

    private void FindBones()
    {
        Transform[] all = GetComponentsInChildren<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            string n = all[i].name.ToLowerInvariant();
            bool left = n.Contains("left");
            bool right = n.Contains("right");
            if (!left && !right) continue;
            if (n.Contains("leg") && !n.Contains("foot"))
            {
                if (left && legL == null) legL = all[i];
                else if (right && legR == null) legR = all[i];
            }
            else if (n.Contains("arm") && !n.Contains("hand"))
            {
                if (left && armL == null) armL = all[i];
                else if (right && armR == null) armR = all[i];
            }
        }
    }

    private void OnDestroy()
    {
        if (animator != null) animator.SuppressJump = false;
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
        animator.RunSpeedMultiplier = airborne ? 0.25f : 1f;

        float pitch = 0f;
        float stretchY = 1f;
        float legLeftAngle = 0f;
        float legRightAngle = 0f;
        float armAngle = 0f;

        if (airborne)
        {
            float p = Mathf.Clamp01(airTime / AirDuration);
            float launch = 1f - Mathf.SmoothStep(0f, 1f, p / 0.25f);
            float arc = Mathf.Sin(p * Mathf.PI);
            float tuck = Mathf.Sin(Mathf.Clamp01(p / 0.9f) * Mathf.PI);
            pitch = Mathf.Lerp(-6f, 0f, Mathf.Clamp01(p / 0.2f)) + arc * 24f;
            stretchY = 1f + 0.07f * launch - 0.09f * arc;
            legLeftAngle = -75f * tuck;
            legRightAngle = -35f * tuck;
            armAngle = -100f * Mathf.Sin(Mathf.Clamp01(p / 0.8f) * Mathf.PI);
        }
        else if (landTimer > 0f)
        {
            float s = Mathf.Sin(landTimer / LandDuration * Mathf.PI);
            pitch = 10f * s;
            stretchY = 1f - 0.14f * s;
            legLeftAngle = -20f * s;
            legRightAngle = -20f * s;
        }

        pitch = Mathf.Lerp(pitch, -72f, slideBlend);
        stretchY = Mathf.Lerp(stretchY, 0.96f, slideBlend);
        legLeftAngle = Mathf.Lerp(legLeftAngle, -55f, slideBlend);
        legRightAngle = Mathf.Lerp(legRightAngle, -55f, slideBlend);
        armAngle = Mathf.Lerp(armAngle, 50f, slideBlend);
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

        Vector3 axis = transform.right;
        RotateBone(legL, legLeftAngle, axis);
        RotateBone(legR, legRightAngle, axis);
        RotateBone(armL, armAngle, axis);
        RotateBone(armR, armAngle, axis);
    }

    private static void RotateBone(Transform bone, float angle, Vector3 axis)
    {
        if (bone == null || Mathf.Abs(angle) < 0.1f) return;
        bone.rotation = Quaternion.AngleAxis(angle, axis) * bone.rotation;
    }
}
