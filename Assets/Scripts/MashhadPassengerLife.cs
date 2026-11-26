using System.Collections.Generic;
using UnityEngine;

/// <summary>Small idle behaviours for people waiting at BRT stations. Visual only.</summary>
public sealed class MashhadPassengerLife : MonoBehaviour
{
    public enum Mode { Phone, Paper, Standing }

    public Mode mode = Mode.Standing;
    public bool seated;
    public float activeDistance = 60f;

    private Transform head;
    private Transform hair;
    private Transform phone;
    private Transform screen;
    private Transform legL;
    private Transform legR;
    private readonly List<Transform> paperParts = new List<Transform>();
    private readonly List<Quaternion> paperRots = new List<Quaternion>();
    private readonly List<Vector3> paperScales = new List<Vector3>();
    private readonly List<Transform> shins = new List<Transform>();
    private Vector3 headPos;
    private Vector3 hairPos;
    private Vector3 phonePos;
    private Vector3 screenPos;
    private Quaternion headRot = Quaternion.identity;
    private Quaternion hairRot = Quaternion.identity;
    private Camera viewCamera;
    private float nextLookup;
    private float time;
    private float seed;

    private void Start()
    {
        head = transform.Find("Head");
        hair = transform.Find("Hair");
        phone = transform.Find("Phone");
        screen = transform.Find("Screen");
        legL = transform.Find("LegPivot_L");
        legR = transform.Find("LegPivot_R");
        if (head != null) { headPos = head.localPosition; headRot = head.localRotation; }
        if (hair != null) { hairPos = hair.localPosition; hairRot = hair.localRotation; }
        if (phone != null) phonePos = phone.localPosition;
        if (screen != null) screenPos = screen.localPosition;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform c = transform.GetChild(i);
            if (c.name == "Newspaper" || c.name == "Headline" || c.name == "Text")
            {
                paperParts.Add(c);
                paperRots.Add(c.localRotation);
                paperScales.Add(c.localScale);
            }
            else if (c.name == "Shin")
            {
                shins.Add(c);
            }
        }
        seed = (GetInstanceID() & 1023) * 0.37f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (viewCamera == null && Time.time >= nextLookup)
        {
            viewCamera = Camera.main;
            nextLookup = Time.time + 1f;
        }
        if (viewCamera != null && (transform.position - viewCamera.transform.position).sqrMagnitude > activeDistance * activeDistance) return;

        time += dt;
        float t = time + seed;
        float headPitch = 0f;
        float headYaw = 0f;

        switch (mode)
        {
            case Mode.Phone:
            {
                float p = Mathf.Repeat(t, 5.5f) / 5.5f;
                float glance = p > 0.8f ? Mathf.Sin((p - 0.8f) / 0.2f * Mathf.PI) : 0f;
                headPitch = -glance * 22f;
                headYaw = glance * 12f;
                float scroll = Mathf.Sin(t * 9f) * 0.004f;
                if (phone != null) phone.localPosition = phonePos + new Vector3(0f, scroll, 0f);
                if (screen != null) screen.localPosition = screenPos + new Vector3(0f, scroll, 0f);
                break;
            }
            case Mode.Paper:
            {
                float sway = Mathf.Sin(t * 0.9f) * 3f;
                float turn = Mathf.Repeat(t, 9f);
                float flip = turn < 0.6f ? Mathf.Sin(turn / 0.6f * Mathf.PI) : 0f;
                for (int i = 0; i < paperParts.Count; i++)
                {
                    paperParts[i].localRotation = paperRots[i] * Quaternion.Euler(sway, 0f, 0f);
                    Vector3 s = paperScales[i];
                    paperParts[i].localScale = new Vector3(s.x * (1f - 0.75f * flip), s.y, s.z);
                }
                headPitch = Mathf.Sin(t * 0.6f) * 3f;
                break;
            }
            default:
            {
                headYaw = Mathf.Sin(t * 0.55f) * 28f;
                float shift = Mathf.Sin(t * 0.8f) * 2.5f;
                SetPitch(legL, shift);
                SetPitch(legR, -shift);
                break;
            }
        }

        if (seated && shins.Count > 0)
        {
            shins[0].localRotation = Quaternion.Euler(Mathf.Sin(t * 5f) * 6f, 0f, 0f);
        }

        Quaternion delta = Quaternion.Euler(headPitch, headYaw, 0f);
        if (head != null) head.localRotation = delta * headRot;
        if (hair != null)
        {
            hair.localPosition = headPos + delta * (hairPos - headPos);
            hair.localRotation = delta * hairRot;
        }
    }

    private static void SetPitch(Transform pivot, float angle)
    {
        if (pivot != null) pivot.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }
}
