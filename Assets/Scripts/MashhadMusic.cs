using UnityEngine;

/// <summary>
/// Background music generated at runtime (no audio asset, no download): a fast chase track at 136 BPM with a
/// four-on-the-floor kick, snare, hats, a rolling pumped bass, an arpeggiated hook in Hijaz mode with echo and a
/// noise riser before every 8-bar loop. It plays while the game scene is active, ducks when paused, and has a MUSIC
/// on/off button (or the M key). The choice is remembered.
/// </summary>
public sealed class MashhadMusic : MonoBehaviour
{
    private const int SampleRate = 22050;
    private const string PrefKey = "MashhadMusicEnabled";
    private const float FullVolume = 0.6f;
    private const float PausedVolume = 0.2f;

    private sealed class Track
    {
        private const double Bpm = 136.0;
        private const int MaxVoices = 10;
        private const float TwoPi = 6.2831853f;

        private static readonly float[] LeadCents = { 0f, 100f, 400f, 500f, 700f, 800f, 1000f, 1200f, 1300f, 1600f, 1700f, 1900f, 2000f, 2200f };
        private static readonly float[] BassCents = { 0f, 100f, 400f, 500f, 700f };
        private static readonly int[] BassRoots = { 0, 0, 1, 0, 0, 0, 3, 2 };
        private static readonly bool[] BassHit =
        {
            true, false, true, true, false, true, false, true,
            true, false, true, false, true, true, false, true
        };
        private static readonly int[][] Hooks =
        {
            new int[] { 0, -1, 3, -1, 4, 3, 2, -1, 0, -1, 3, 4, 5, 4, 3, 2 },
            new int[] { 7, -1, 6, 5, 4, -1, 5, 4, 3, -1, 2, 3, 4, -1, 2, 0 }
        };

        private readonly float[] voiceFreq = new float[MaxVoices];
        private readonly float[] voiceTime = new float[MaxVoices];
        private readonly float[] voiceAmp = new float[MaxVoices];
        private readonly int[] voiceKind = new int[MaxVoices];
        private readonly float[] echo = new float[(int)(SampleRate * 60.0 / Bpm / 4.0 * 3.0)];
        private readonly double samplesPerStep = SampleRate * 60.0 / Bpm / 4.0;
        private double stepCounter;
        private int step;
        private int echoPos;
        private float kickTime = 99f;
        private float kickPhase;
        private float snareTime = 99f;
        private float snareAmp;
        private float hatTime = 99f;
        private float hatAmp;
        private float hatPrevious;
        private float riserLevel;
        private float riserPrevious;
        private float bassFilter;
        private uint noiseState = 22695477u;

        public Track()
        {
            for (int i = 0; i < MaxVoices; i++) voiceTime[i] = 99f;
            stepCounter = samplesPerStep;
        }

        private static float LeadFreq(int degree)
        {
            return 293.66f * (float)System.Math.Pow(2.0, LeadCents[degree] / 1200.0);
        }

        private static float BassFreq(int index)
        {
            return 73.42f * (float)System.Math.Pow(2.0, BassCents[index] / 1200.0);
        }

        private float Noise()
        {
            noiseState = noiseState * 1664525u + 1013904223u;
            return ((noiseState >> 9) & 0xFFFFu) / 32768f - 1f;
        }

        private void Trigger(float freq, float amp, int kind)
        {
            int slot = 0;
            float oldest = -1f;
            for (int v = 0; v < MaxVoices; v++)
            {
                if (voiceTime[v] >= 3f)
                {
                    slot = v;
                    break;
                }
                if (voiceTime[v] > oldest)
                {
                    oldest = voiceTime[v];
                    slot = v;
                }
            }
            voiceFreq[slot] = freq;
            voiceAmp[slot] = amp;
            voiceKind[slot] = kind;
            voiceTime[slot] = 0f;
        }

        private void AdvanceStep()
        {
            int s16 = step % 16;
            int bar = (step / 16) % 8;

            if (s16 % 4 == 0 || (bar == 7 && s16 >= 14)) kickTime = 0f;
            if (s16 == 4 || s16 == 12 || (bar == 7 && s16 >= 13))
            {
                snareTime = 0f;
                snareAmp = (bar == 7 && s16 >= 13) ? 0.7f : 1f;
            }
            if (s16 % 2 == 0)
            {
                hatTime = 0f;
                hatAmp = (s16 % 4 == 2) ? 1f : 0.6f;
            }
            else if (bar >= 4)
            {
                hatTime = 0f;
                hatAmp = 0.35f;
            }

            if (BassHit[s16]) Trigger(BassFreq(BassRoots[bar]), 0.34f, 1);

            int[] hook = Hooks[bar < 4 ? 0 : 1];
            int note = hook[s16];
            if (note >= 0) Trigger(LeadFreq(note), 0.16f, 0);

            riserLevel = bar == 7 ? (s16 + 1) / 16f : 0f;
            step++;
        }

        public void Read(float[] data)
        {
            const float dt = 1f / SampleRate;
            for (int i = 0; i < data.Length; i++)
            {
                if (stepCounter >= samplesPerStep)
                {
                    stepCounter -= samplesPerStep;
                    AdvanceStep();
                }
                stepCounter += 1.0;

                float duck = 1f;
                if (kickTime < 0.25f) duck = 1f - 0.7f * (float)System.Math.Exp(-kickTime * 14f);

                float leadSum = 0f;
                float bassSum = 0f;
                for (int v = 0; v < MaxVoices; v++)
                {
                    float t = voiceTime[v];
                    if (t >= 3f) continue;
                    float ph = TwoPi * voiceFreq[v] * t;
                    if (voiceKind[v] == 0)
                    {
                        float env = (float)System.Math.Exp(-t * 8f);
                        if (env < 0.001f)
                        {
                            voiceTime[v] = 99f;
                            continue;
                        }
                        float tone = (float)System.Math.Sin(ph)
                            + 0.5f * (float)System.Math.Sin(2f * ph)
                            + 0.33f * (float)System.Math.Sin(3f * ph)
                            + 0.2f * (float)System.Math.Sin(5f * ph);
                        leadSum += voiceAmp[v] * env * tone;
                    }
                    else
                    {
                        float env = (1f - (float)System.Math.Exp(-t * 80f)) * (float)System.Math.Exp(-t * 4.5f);
                        if (env < 0.001f && t > 0.05f)
                        {
                            voiceTime[v] = 99f;
                            continue;
                        }
                        float tone = (float)System.Math.Sin(ph)
                            + 0.5f * (float)System.Math.Sin(2f * ph)
                            + 0.33f * (float)System.Math.Sin(3f * ph)
                            + 0.25f * (float)System.Math.Sin(4f * ph);
                        bassSum += voiceAmp[v] * env * tone;
                    }
                    voiceTime[v] = t + dt;
                }

                bassFilter += (bassSum - bassFilter) * 0.3f;
                float s = bassFilter * duck + leadSum * (0.5f + 0.5f * duck);

                float delayed = echo[echoPos];
                echo[echoPos] = leadSum + delayed * 0.35f;
                echoPos++;
                if (echoPos >= echo.Length) echoPos = 0;
                s += delayed * 0.45f;

                if (kickTime < 0.35f)
                {
                    float env = (float)System.Math.Exp(-kickTime * 10f);
                    float freq = 48f + 120f * (float)System.Math.Exp(-kickTime * 28f);
                    kickPhase += TwoPi * freq * dt;
                    if (kickPhase > TwoPi) kickPhase -= TwoPi;
                    s += 0.85f * env * (float)System.Math.Sin(kickPhase);
                    kickTime += dt;
                }

                if (snareTime < 0.2f)
                {
                    float env = (float)System.Math.Exp(-snareTime * 22f);
                    s += snareAmp * env * (0.3f * Noise() + 0.22f * (float)System.Math.Sin(TwoPi * 190f * snareTime));
                    snareTime += dt;
                }

                if (hatTime < 0.05f)
                {
                    float env = (float)System.Math.Exp(-hatTime * 80f);
                    float n = Noise();
                    float hp = n - hatPrevious * 0.9f;
                    hatPrevious = n;
                    s += 0.12f * hatAmp * env * hp;
                    hatTime += dt;
                }

                if (riserLevel > 0f)
                {
                    float n = Noise();
                    float hp = n - riserPrevious * 0.8f;
                    riserPrevious = n;
                    s += 0.1f * riserLevel * riserLevel * hp;
                }

                data[i] = (float)System.Math.Tanh(s * 1.2f) * 0.85f;
            }
        }
    }

    private AudioSource source;
    private readonly Track track = new Track();
    private bool musicOn = true;
    private bool gameActive;
    private float nextCheck;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<MashhadMusic>() != null) return;
        var host = new GameObject("MR_Music");
        DontDestroyOnLoad(host);
        host.AddComponent<MashhadMusic>();
    }

    private void Awake()
    {
        musicOn = PlayerPrefs.GetInt(PrefKey, 1) != 0;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.clip = AudioClip.Create("MR_Music", SampleRate * 8, 1, SampleRate, true, track.Read);
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextCheck)
        {
            nextCheck = Time.unscaledTime + 1f;
            gameActive = FindObjectOfType<RunnerGame>() != null;
            if (gameActive)
            {
                EnsureListener();
                if (!source.isPlaying) source.Play();
            }
        }

        if (Input.GetKeyDown(KeyCode.M)) Toggle();

        float target = (musicOn && gameActive) ? (Time.timeScale > 0f ? FullVolume : PausedVolume) : 0f;
        source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * 1.5f);
    }

    private static void EnsureListener()
    {
        if (FindObjectOfType<AudioListener>() != null) return;
        Camera cam = Camera.main;
        if (cam != null) cam.gameObject.AddComponent<AudioListener>();
    }

    private void Toggle()
    {
        musicOn = !musicOn;
        PlayerPrefs.SetInt(PrefKey, musicOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnGUI()
    {
        if (!gameActive) return;
        Matrix4x4 old = GUI.matrix;
        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1080f, Screen.height / 1920f), 0.75f, 1.35f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        float screenWidth = Screen.width / scale;
        if (GUI.Button(new Rect(screenWidth - 190f, 112f, 166f, 64f), musicOn ? "MUSIC ON" : "MUSIC OFF"))
        {
            Toggle();
        }
        GUI.matrix = old;
    }
}
