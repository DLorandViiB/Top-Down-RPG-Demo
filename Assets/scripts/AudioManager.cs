using UnityEngine;
using UnityEngine.Audio;
using System;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    public AudioSource musicSource; // For background music
    public AudioSource sfxSource;   // For sound effects

    [Header("Sound Library")]
    public Sound[] sounds; // List of all sounds in the game

    void Awake()
    {
        // SINGLETON PATTERN
        // This ensures there is only ever ONE AudioManager
        if (instance == null)
        {
            instance = this;
            // We don't need DontDestroyOnLoad here if this script is 
            // attached to your existing "Managers" object that already has it.
            // But adding it doesn't hurt.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If we load a scene that accidentally has another AudioManager, kill it.
            Destroy(gameObject);
        }
    }

    // Call this to play music (e.g. "MainTheme")
    public void PlayMusic(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Music: " + name + " not found!");
            return;
        }

        // If this song is already playing, don't restart it (Good for scene transitions)
        if (musicSource.clip == s.clip && musicSource.isPlaying) return;

        musicSource.clip = s.clip;
        musicSource.loop = true;
        musicSource.volume = s.volume;
        musicSource.Play();
    }

    // Call this for Sound Effects (e.g. "Attack", "Jump")
    public void PlaySFX(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("SFX: " + name + " not found!");
            return;
        }

        // Randomize pitch slightly for variety (optional but sounds good)
        sfxSource.pitch = s.pitch * UnityEngine.Random.Range(0.95f, 1.05f);
        sfxSource.volume = s.volume;
        sfxSource.PlayOneShot(s.clip);
    }

    // Helper to stop music (e.g. game over)
    public void StopMusic()
    {
        musicSource.Stop();
    }
}

// A simple class to hold sound data in the Inspector
[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.5f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}