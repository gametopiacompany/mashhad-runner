using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public enum CharacterState
{
    Run,
    Jump,
    Slide,
    Hit,
    Idle
}

/// <summary>
/// Plays the animation clips that are embedded in the Kenney character FBX files.
/// No AnimatorController asset is needed: clips are loaded from Resources at runtime
/// and blended through a PlayableGraph. If any required clip is missing, IsReady stays
/// false and the caller keeps using its procedural fallback motion.
/// </summary>
public sealed class CharacterAnimator : MonoBehaviour
{
    private const float FadeTime = 0.12f;

    private sealed class Entry
    {
        public AnimationClip clip;
        public AnimationClipPlayable playable;
        public bool loop;
        public float speed = 1f;
    }

    private static readonly Dictionary<CharacterState, string[]> ClipNames = new Dictionary<CharacterState, string[]>
    {
        { CharacterState.Run, new[] { "sprint", "run", "walk" } },
        { CharacterState.Jump, new[] { "jump" } },
        { CharacterState.Slide, new[] { "crouch" } },
        { CharacterState.Hit, new[] { "fall", "emote-no" } },
        { CharacterState.Idle, new[] { "idle", "static" } }
    };

    private static readonly CharacterState[] States =
    {
        CharacterState.Run,
        CharacterState.Jump,
        CharacterState.Slide,
        CharacterState.Hit,
        CharacterState.Idle
    };

    private readonly Entry[] entries = new Entry[States.Length];
    private float[] weights;
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private CharacterState current = CharacterState.Run;

    public bool IsReady { get; private set; }

    /// <summary>The state that is currently active (read-only).</summary>
    public CharacterState State
    {
        get { return current; }
    }

    /// <summary>Multiplies the playback speed of the looping run clip (1 = authored speed).</summary>
    public float RunSpeedScale { get; set; } = 1f;

    public bool Initialize(string resourcePath)
    {
        if (IsReady)
        {
            return true;
        }

        var clips = Resources.LoadAll<AnimationClip>(resourcePath);
        if (clips == null || clips.Length == 0)
        {
            return false;
        }

        var byName = new Dictionary<string, AnimationClip>();
        foreach (var clip in clips)
        {
            if (clip == null || clip.name.StartsWith("__preview__"))
            {
                continue;
            }
            byName[clip.name.ToLowerInvariant()] = clip;
        }

        var animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }
        animator.applyRootMotion = false;

        graph = PlayableGraph.Create("CharacterAnimator " + name);
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
        mixer = AnimationMixerPlayable.Create(graph, States.Length);
        output.SetSourcePlayable(mixer);
        weights = new float[States.Length];

        for (int i = 0; i < States.Length; i++)
        {
            var clip = FindClip(byName, ClipNames[States[i]]);
            if (clip == null)
            {
                continue;
            }

            var entry = new Entry
            {
                clip = clip,
                playable = AnimationClipPlayable.Create(graph, clip),
                loop = States[i] == CharacterState.Run || States[i] == CharacterState.Idle
            };
            graph.Connect(entry.playable, 0, mixer, i);
            mixer.SetInputWeight(i, 0f);
            entries[i] = entry;
        }

        if (entries[IndexOf(CharacterState.Run)] == null)
        {
            graph.Destroy();
            return false;
        }

        graph.Play();
        IsReady = true;
        current = CharacterState.Run;
        weights[IndexOf(CharacterState.Run)] = 1f;
        mixer.SetInputWeight(IndexOf(CharacterState.Run), 1f);
        return true;
    }

    /// <summary>
    /// Switches to a state. Re-requesting the active state is a no-op, so this can be called every frame.
    /// For non-looping states, <paramref name="duration"/> stretches the clip to last that many seconds.
    /// </summary>
    public void SetState(CharacterState state, float duration = 0f)
    {
        if (!IsReady || state == current)
        {
            return;
        }

        var entry = entries[IndexOf(state)];
        if (entry == null)
        {
            return;
        }

        entry.playable.SetTime(0.0);
        entry.playable.SetDone(false);
        entry.speed = duration > 0.01f && entry.clip.length > 0.01f ? entry.clip.length / duration : 1f;
        current = state;
    }

    private void Update()
    {
        if (!IsReady)
        {
            return;
        }

        float step = Time.deltaTime / FadeTime;
        float total = 0f;
        for (int i = 0; i < States.Length; i++)
        {
            var entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            float target = States[i] == current ? 1f : 0f;
            weights[i] = Mathf.MoveTowards(weights[i], target, step);
            total += weights[i];

            float speed = States[i] == CharacterState.Run ? RunSpeedScale : entry.speed;
            entry.playable.SetSpeed(speed);

            if (entry.loop && entry.clip.length > 0.01f)
            {
                double time = entry.playable.GetTime();
                if (time >= entry.clip.length)
                {
                    entry.playable.SetTime(time % entry.clip.length);
                }
            }
        }

        if (total <= 0.0001f)
        {
            return;
        }

        for (int i = 0; i < States.Length; i++)
        {
            if (entries[i] != null)
            {
                mixer.SetInputWeight(i, weights[i] / total);
            }
        }
    }

    private void OnDestroy()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }

    private static int IndexOf(CharacterState state)
    {
        for (int i = 0; i < States.Length; i++)
        {
            if (States[i] == state)
            {
                return i;
            }
        }
        return 0;
    }

    private static AnimationClip FindClip(Dictionary<string, AnimationClip> byName, string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (byName.TryGetValue(candidate, out var exact))
            {
                return exact;
            }
        }

        foreach (var candidate in candidates)
        {
            foreach (var pair in byName)
            {
                if (pair.Key.Contains(candidate))
                {
                    return pair.Value;
                }
            }
        }
        return null;
    }
}
