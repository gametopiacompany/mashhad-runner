using UnityEngine;

/// <summary>
/// Background music generated at runtime (no audio asset, no download): a Shur-style melody on a plucked voice,
/// a low drone, a bass and a simple frame-drum pattern with a soft echo. It plays while the game scene is active,
/// ducks when paused, and has a MUSIC on/off button (or the M key). The choice is remembered.
/// </summary>
public sealed class MashhadMusic : MonoBehaviour
{
    private const int SampleRate = 22050;
    private const string PrefKey = "MashhadMusicEnabled";
    private const float FullVolume = 0.5f;
    private const float PausedVolume = 0.2f;

    private sealed class Track
    {
        private const double Bpm = 92.0;
        private const int MaxVoices = 8;
        private const float TwoPi = 6.2831853f;

        private static readonly float[] Cents = { 0f, 150f, 300f, 500f, 700f, 800f, 1000f, 1200f, 1350f, 1500f };
        private static readonly int[][] Phrases =
        {
            new int[] { 4, -1, 5, 4, 3, 2, 3, 1, 0, -1, 1, 2, 3, -1, 2, -1 },
            new int[] { 4, 5, 6, 5, 4, 3, 4, 2, 3, 4, 3, 2, 1, 0, -1, -1 },
            new int[] { 7, -1, 6, 5, 4, -1, 5, 4, 3, 2, 3, 4, 2, 1, 0, -1 },
            new int[] { 0, 1, 2, 3, 2, 1, 2, 0, 1, 2, 3, 4, 3, 2, 1, -1 }
        };

        private readonly float[] voiceFreq = new float[MaxVoices];
        private readonly float[] voiceTime = new float[MaxVoices];
        private readonly float[] voiceAmp = new float[MaxVoices];
        private readonly int[] voiceKind = new int[MaxVoices];
        private readonly float[] delay = new float[(int)(SampleRate * 0.31f)];
        private readonly double samplesPerStep = SampleRate * 60.0 / Bpm / 2.0;
        private double stepCounter;
        private int step;
        private int delayPos;
        private float dumTime = 99f;
        private float dumPhase;
        private float takTime = 99f;
        private float takAmp;
        private float previousNoise;
        private float dronePhaseA;
        private float dronePhaseB;
        private float tremPhase;
        private uint noiseState = 22695477u;

        public Track()
        {
            for (int i = 0; i < MaxVoices; i++) voiceTime[i] = 99f;
            stepCounter = samplesPerStep;
        }

        private static float NoteFreq(int degree)
        {
            return 293.66f * (float)System.Math.Pow(2.0, Cents[degree] / 1200.0);
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
                if (voiceTime[v] >= 4f)
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
            int phrase = (step / 16) % Phrases.Length;
            int note = Phrases[phrase][s16];
            if (note >= 0) Trigger(NoteFreq(note), 0.28f, 0);

            if (s16 % 8 == 0)
            {
                float bass = ((step / 8) % 4 == 3) ? 110f : 73.42f;
                Trigger(bass, 0.22f, 1);
            }

            int d = step % 8;
            if (d == 0 || d == 3 || d == 4) dumTime = 0f;
            if (d == 2 || d == 5 || d == 6 || d == 7)
            {
                takTime = 0f;
                takAmp = d == 7 ? 0.5f : 1f;
            }
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

                float s = 0f;
                for (int v = 0; v < MaxVoices; v++)
                {
                    float t = voiceTime[v];
                    if (t >= 4f) continue;
                    float ph = TwoPi * voiceFreq[v] * t;
                    float env;
                    float tone;
                    if (voiceKind[v] == 0)
                    {
                        env = (float)System.Math.Exp(-t * 4.2f);
                        tone = (float)System.Math.Sin(ph)
                            + 0.45f * (float)System.Math.Sin(2f * ph) * (float)System.Math.Exp(-t * 3f)
                            + 0.2f * (float)System.Math.Sin(3f * ph) * (float)System.Math.Exp(-t * 7f);
                    }
                    else
                    {
                        env = (1f - (float)System.Math.Exp(-t * 30f)) * (float)System.Math.Exp(-t * 1.4f);
                        tone = (float)System.Math.Sin(ph) + 0.3f * (float)System.Math.Sin(2f * ph);
                    }
                    if (env < 0.0008f && t > 0.05f)
                    {
                        voiceTime[v] = 99f;
                        continue;
                    }
                    s += voiceAmp[v] * env * tone;
                    voiceTime[v] = t + dt;
                }

                dronePhaseA += TwoPi * 73.42f * dt;
                if (dronePhaseA > TwoPi) dronePhaseA -= TwoPi;
                dronePhaseB += TwoPi * 110f * dt;
                if (dronePhaseB > TwoPi) dronePhaseB -= TwoPi;
                tremPhase += TwoPi * 0.12f * dt;
                if (tremPhase > TwoPi) tremPhase -= TwoPi;
                float trem = 0.7f + 0.3f * (float)System.Math.Sin(tremPhase);
                s += 0.045f * trem * ((float)System.Math.Sin(dronePhaseA) + 0.6f * (float)System.Math.Sin(dronePhaseB));

                if (dumTime < 0.45f)
                {
                    float env = (float)System.Math.Exp(-dumTime * 12f);
                    float freq = 58f + 95f * (float)System.Math.Exp(-dumTime * 22f);
                    dumPhase += TwoPi * freq * dt;
                    if (dumPhase > TwoPi) dumPhase -= TwoPi;
                    s += 0.5f * env * (float)System.Math.Sin(dumPhase);
                    dumTime += dt;
                }

                if (takTime < 0.15f)
                {
                    float env = (float)System.Math.Exp(-takTime * 42f);
                    float n = Noise();
                    float hp = n - previousNoise * 0.85f;
                    previousNoise = n;
                    s += 0.16f * takAmp * env * hp;
                    takTime += dt;
                }

                float echo = delay[delayPos];
                float mixed = s + echo * 0.3f;
                delay[delayPos] = s + echo * 0.38f;
                delayPos++;
                if (delayPos >= delay.Length) delayPos = 0;
                data[i] = (float)System.Math.Tanh(mixed * 1.1f) * 0.8f;
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
