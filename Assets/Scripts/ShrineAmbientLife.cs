using UnityEngine;

/// <summary>
/// Animates a small low-poly pigeon flock around the shrine dome and a soft glint on the finial.
/// Visual only. Updates stop when the camera is far away or the game is paused.
/// </summary>
public sealed class ShrineAmbientLife : MonoBehaviour
{
    public Vector3 orbitCenter = new Vector3(0f, 18f, 0f);
    public float activeDistance = 140f;
    public float glintAmount = 0.22f;

    private Transform[] birds;
    private Transform[] leftWings;
    private Transform[] rightWings;
    private Transform glint;
    private Vector3 glintScale = Vector3.one;
    private float[] phase;
    private float[] radius;
    private float[] speed;
    private float[] height;
    private Camera viewCamera;
    private float nextCheck;
    private float time;
    private bool active = true;

    public void Setup(Transform[] birdRoots, Transform[] left, Transform[] right, Transform glintTransform)
    {
        birds = birdRoots;
        leftWings = left;
        rightWings = right;
        glint = glintTransform;
        if (glint != null) glintScale = glint.localScale;

        int count = birds.Length;
        phase = new float[count];
        radius = new float[count];
        speed = new float[count];
        height = new float[count];
        for (int i = 0; i < count; i++)
        {
            phase[i] = i * 2.4f;
            radius[i] = 9f + (i % 3) * 2.2f;
            speed[i] = 0.32f + (i % 4) * 0.05f;
            height[i] = (i % 3) * 0.9f;
        }
        Apply();
    }

    private void Update()
    {
        if (birds == null) return;

        if (Time.unscaledTime >= nextCheck)
        {
            nextCheck = Time.unscaledTime + 0.5f;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera != null)
            {
                active = (transform.position - viewCamera.transform.position).sqrMagnitude < activeDistance * activeDistance;
            }
        }

        if (!active || Time.deltaTime <= 0f) return;
        time += Time.deltaTime;
        Apply();
    }

    private void Apply()
    {
        for (int i = 0; i < birds.Length; i++)
        {
            if (birds[i] == null) continue;

            float angle = time * speed[i] + phase[i];
            float bob = Mathf.Sin(time * 0.7f + phase[i]) * 0.7f;
            birds[i].localPosition = orbitCenter + new Vector3(Mathf.Cos(angle) * radius[i], height[i] + bob, Mathf.Sin(angle) * radius[i]);

            Vector3 forward = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            birds[i].localRotation = Quaternion.LookRotation(forward, Vector3.up);

            float flap = Mathf.Sin(time * 12f + phase[i]) * 38f;
            if (leftWings[i] != null) leftWings[i].localRotation = Quaternion.Euler(0f, 0f, -flap);
            if (rightWings[i] != null) rightWings[i].localRotation = Quaternion.Euler(0f, 0f, flap);
        }

        if (glint != null)
        {
            glint.localScale = glintScale * (1f + Mathf.Sin(time * 2.2f) * glintAmount);
        }
    }
}
