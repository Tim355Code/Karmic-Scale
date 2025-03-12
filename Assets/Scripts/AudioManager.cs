using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using Random = UnityEngine.Random;

// Generic class that I've used in many of my games, lazily copy pasted and modified :D
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    // Unused since no songs have any real intro and loop sections
    private Music PlayingMusic = Music.NONE;
    private bool StartedLoop = false;

    [SerializeField]
    private AudioSource MusicPlayer;
    [SerializeField]
    private AudioSource SFXPlayer;

    [SerializeField]
    private GameObject InterruptablePrefab;
    [SerializeField]
    private int InterruptableCount = 4;

    [SerializeField]
    private MusicClip[] MusicClips;
    [SerializeField]
    private SoundClip[] SoundEffects;

    // Variables neccessery for all the SFX play modes
    private float CurrentDelayTimer = 0;
    private List<AudioSource> Interruptables = new List<AudioSource>();

    private List<SFX> BusySounds = new List<SFX>();
    private List<float> BusyTimers = new List<float>();
    private Dictionary<SFX, float> MultiBusyTimers = new Dictionary<SFX, float>();

    public delegate void MusicPlayback(float musicLoudness);
    public static MusicPlayback OnMusicPlayback;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        Instance = this;
        for (int i = 0; i < InterruptableCount; i++)
        {
            Interruptables.Add(Instantiate(InterruptablePrefab, transform).GetComponent<AudioSource>());
        }
    }

    void Update()
    {
        // Update busy SFX
        List<int> finishedIndex = new List<int>();
        for (int i = 0; i < BusyTimers.Count; i++)
        {
            BusyTimers[i] -= Time.deltaTime;
            if (BusyTimers[i] < 0)
            {
                finishedIndex.Add(i);
            }
        }

        foreach (int i in finishedIndex)
        {
            BusyTimers.RemoveAt(i);
            BusySounds.RemoveAt(i);
        }

        List<SFX> finishedSounds = new List<SFX>();
        foreach (var key in MultiBusyTimers.Keys.ToList())
        {
            MultiBusyTimers[key] -= Time.deltaTime;
            if (MultiBusyTimers[key] <= 0)
                finishedSounds.Add(key);
        }

        foreach (var key in finishedSounds)
        {
            MultiBusyTimers.Remove(key);
        }

        if (CurrentDelayTimer > 0)
            CurrentDelayTimer -= Time.deltaTime;
    }

    // Unused as I didn't have time to make a pause feature
    void OnGamePauseChanged(bool hasPaused)
    {
        if (hasPaused) MusicPlayer.Pause();
        else MusicPlayer.UnPause();
    }

    // Super simple music switching code, fade out song if one is playing, or just play the next song instantly
    public void PlayMusic(Music music)
    {
        PlayingMusic = music;
        if (MusicPlayer.isPlaying) StartCoroutine(MusicFadeOut(0.5f, music));
        else
        {
            MusicPlayer.clip = MusicPlayer.clip = MusicClips[(int)music].LoopSection;
            MusicPlayer.Play();
        }
    }

    public void PlaySFX(int id)
    {
        SFXPlayer.PlayOneShot(SoundEffects[id].GetClip());
    }

    public void PlaySFX(SFX id, PlayType playType, float volume = 1f)
    {
        if (id == SFX.NONE)
            return;

        switch (playType)
        {
            // Simplest play mode, it just plays the SFX
            case PlayType.UNRESTRICTED:
                {
                    SFXPlayer.PlayOneShot(SoundEffects[(int)id].GetClip(), volume);
                    break;
                }
            // If this SFX is already playing, stop it, and play from the beginning
            case PlayType.INTERRUPTED:
                {
                    if (BusySounds.Contains(id))
                    {
                        AudioClip clip = SoundEffects[(int)id].GetClip();
                        int i = BusySounds.IndexOf(id);

                        BusyTimers[i] = clip.length;

                        Interruptables[i].Stop();
                        Interruptables[i].volume = volume;
                        Interruptables[i].clip = clip;
                        Interruptables[i].Play();
                    }
                    else if (BusySounds.Count < InterruptableCount)
                    {
                        AudioClip clip = SoundEffects[(int)id].GetClip();
                        int index = BusySounds.Count;

                        BusySounds.Add(id);
                        BusyTimers.Add(clip.length);

                        Interruptables[index].Stop();
                        Interruptables[index].volume = volume;
                        Interruptables[index].clip = clip;
                        Interruptables[index].Play();
                    }
                    break;
                }
            // Play the SFX as long as it's not already playing
            case PlayType.SINGLE:
                {
                    if (!BusySounds.Contains(id))
                    {
                        AudioClip clip = SoundEffects[(int)id].GetClip();
                        int index = BusySounds.Count;

                        BusySounds.Add(id);
                        BusyTimers.Add(clip.length);

                        Interruptables[index].clip = clip;
                        Interruptables[index].volume = volume;
                        Interruptables[index].Play();
                    }
                    break;
                }
            // If the SFX has been playing long enough already, interrupt and play again, otherwise let it continue playing without any new plays
            case PlayType.LIMITED:
                {
                    if (!BusySounds.Contains(id) && BusySounds.Count < InterruptableCount)
                    {
                        int index = BusySounds.Count;

                        BusySounds.Add(id);
                        BusyTimers.Add(SoundEffects[(int)id].Time);

                        Interruptables[index].Stop();
                        Interruptables[index].volume = volume;
                        Interruptables[index].clip = SoundEffects[(int)id].GetClip();
                        Interruptables[index].Play();
                    }
                    break;
                }
            // Sets a limit how often a SFX can play
            case PlayType.LIMITED_MULTI:
                {
                    if (!MultiBusyTimers.ContainsKey(id))
                    {
                        var clip = SoundEffects[(int)id];
                        MultiBusyTimers.Add(id, clip.Time + (clip.RandomOffset == 0 ? 0 : Random.Range(0, clip.RandomOffset)));

                        SFXPlayer.PlayOneShot(clip.GetClip(), volume);
                    }
                    break;
                }
        }
    }

    public void FadeOutMusic(float duration = 1f)
    {
        StartCoroutine(MusicFadeOut(duration));
    }

    IEnumerator MusicFadeOut(float duration, Music playAfter = Music.NONE)
    {
        if (!MusicPlayer.isPlaying) yield break;

        var interpolator = new Interpolator<float>(duration, 1, 0);
        while (!interpolator.HasFinished)
        {
            MusicPlayer.volume = interpolator.Update(Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }

        MusicPlayer.Stop();
        MusicPlayer.volume = 1;

        if (playAfter != Music.NONE)
        {
            MusicPlayer.clip = MusicPlayer.clip = MusicClips[(int)playAfter].LoopSection;
            MusicPlayer.Play();
        }
    }

    public void SetMusicPitch(float value)
    {
        MusicPlayer.pitch = value;
    }
}

// Enum of all SFX
[Serializable, InspectorOrder(InspectorSort.ByName, InspectorSortDirection.Ascending)]
public enum SFX
{
    NONE = -1,
    SHOOT = 0,
    WOOSH = 1,
    POT_BREAK = 2,
    BEAD = 3,
    PLAYER_HIT = 4,
    ENEMY_HIT = 5,
    ENEMY_DEATH = 6,
    GATE_OPEN = 7,
    GATE_CLOSE = 8,
    FIRE_0 = 9,
    FIRE_1 = 10,
    CHEST_OPEN = 11,
    ITEM_GET = 12,
    CARD_TURN = 13,
    HEAL = 14,
    EXPLOSION = 15,
    HEALTH_UP = 16,
    POTION_PICK_UP = 17,
    POTION_USE = 18,
    SCALE_TIP = 19,
    FUMO = 20,
    PURCHASE = 21,
    FLAME = 22,
    CHOIR = 23,
    BEAM = 24,
    BUCKET_BREAK = 25
}

// Enum of all the music in the game
public enum Music
{
    NONE = -1,
    MENU = 0,
    FLOOR = 1,
    BOSS = 2,
    BOSS_OVER = 3,
    FLOOR_2 = 4,
    FINAL_BOSS = 5,
    DIALOGUE_CREEPY = 6
}

// Enum of ways to play SFX
public enum PlayType
{
    UNRESTRICTED,
    INTERRUPTED,
    SINGLE,
    LIMITED,
    LIMITED_MULTI
}

// Storage classes for music and SFX
[Serializable]
public class MusicClip
{
    public AudioClip IntroSection;
    public AudioClip LoopSection;
}

[Serializable]
public class SoundClip
{
    public string Name;

    public AudioClip[] Clips;
    public float Time = 0.1f;
    public float RandomOffset = 0f;

    public AudioClip GetClip()
    {
        if (Clips.Length == 1)
            return Clips[0];

        return Clips[Random.Range(0, Clips.Length)];
    }
}