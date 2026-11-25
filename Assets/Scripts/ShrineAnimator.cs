using UnityEngine;

/// <summary>
/// Animates the Imam Reza shrine complex: the golden dome breathes and glows, a halo of sun rays
/// turns behind it, pigeons circle the minarets and golden sparkles rise from the courtyard.
/// All parts are plain transforms created by <see cref="ShrineComplex"/>; nothing needs an Animator.
/// </summary>
public sealed class ShrineAnimator : MonoBehaviour
{
    private static readonly Color DomeGold = new Color(1f, 0.74f, 0.22f);

    public Material domeMaterial;
    public Material finialMaterial;
    public Transform dome;
    public Transform finial;
    public Transform rays;
    public Transform[] pigeons;
    public Transform[] leftWings;
    public Transform[] rightWings;
    public Transform[] sparkles;
    public Light glowLight;
    public Vector3 orbitCenter;
    public Vector3 sparkleOrigin;
    public float sparkleHeight = 26f;

    private Vector3 domeScale = Vector3.one;
    private Vector3 finialScale = Vector3.one;

    private void Start()
    {
        if (dome != null)
        {
            domeScale = dome.localScale;
        }
        if (finial != null)
        {
            finialScale = finial.localScale;
        }
    }

    private void Update()
    {
        float t = Time.time;
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * 1.6f);

        if (domeMaterial != null && domeMaterial.HasProperty("_EmissionColor"))
        {
            domeMaterial.SetColor("_EmissionColor", DomeGold * (0.25f + 0.5f * pulse));
        }

        if (dome != null)
        {
            dome.localScale = domeScale * (1f + 0.007f * Mathf.Sin(t * 1.6f));
        }

        if (finialMaterial != null && finialMaterial.HasProperty("_EmissionColor"))
        {
            float blink = 0.6f + 0.4f * Mathf.Sin(t * 5f);
            finialMaterial.SetColor("_EmissionColor", new Color(1f, 0.9f, 0.5f) * (0.6f + 1.4f * blink));
        }

        if (finial != null)
        {
            finial.localScale = finialScale * (1f + 0.08f * Mathf.Sin(t * 5f));
        }

        if (rays != null)
        {
            rays.localRotation = Quaternion.Euler(0f, 0f, t * 3.5f);
            rays.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(t * 1.1f));
        }

        if (glowLight != null)
        {
            glowLight.intensity = 1.2f + 0.5f * pulse;
        }

        AnimatePigeons(t);
        AnimateSparkles(t);
    }

    private void AnimatePigeons(float t)
    {
        if (pigeons == null)
        {
            return;
        }

        for (int i = 0; i < pigeons.Length; i++)
        {
            var bird = pigeons[i];
            if (bird == null)
            {
                continue;
            }

            float radius = 11f + (i % 4) * 2.6f;
            float speed = (i % 2 == 0 ? 0.32f : -0.27f) * (1f + (i % 3) * 0.12f);
            float angle = t * speed + i * 1.37f;
            var offset = new Vector3(Mathf.Sin(angle) * radius, Mathf.Sin(t * 0.9f + i) * 1.8f + (i % 3) * 2f, Mathf.Cos(angle) * radius);
            bird.position = orbitCenter + offset;

            var tangent = new Vector3(Mathf.Cos(angle), 0f, -Mathf.Sin(angle)) * Mathf.Sign(speed);
            if (tangent.sqrMagnitude > 0.0001f)
            {
                bird.rotation = Quaternion.LookRotation(tangent, Vector3.up);
            }

            float flap = Mathf.Sin(t * 9f + i * 0.8f) * 38f;
            if (leftWings != null && i < leftWings.Length && leftWings[i] != null)
            {
                leftWings[i].localRotation = Quaternion.Euler(0f, 0f, flap);
            }
            if (rightWings != null && i < rightWings.Length && rightWings[i] != null)
            {
                rightWings[i].localRotation = Quaternion.Euler(0f, 0f, -flap);
            }
        }
    }

    private void AnimateSparkles(float t)
    {
        if (sparkles == null)
        {
            return;
        }

        for (int i = 0; i < sparkles.Length; i++)
        {
            var sparkle = sparkles[i];
            if (sparkle == null)
            {
                continue;
            }

            float phase = Mathf.Repeat(t * (0.07f + (i % 4) * 0.012f) + i * 0.173f, 1f);
            float angle = i * 2.399f + t * 0.2f;
            float radius = 5f + (i % 5) * 1.6f;
            sparkle.position = sparkleOrigin + new Vector3(Mathf.Sin(angle) * radius, phase * sparkleHeight, Mathf.Cos(angle) * radius);
            sparkle.localScale = Vector3.one * (Mathf.Sin(phase * Mathf.PI) * 0.45f);
        }
    }

    private void OnDestroy()
    {
        if (domeMaterial != null)
        {
            Destroy(domeMaterial);
        }
        if (finialMaterial != null)
        {
            Destroy(finialMaterial);
        }
    }
}
