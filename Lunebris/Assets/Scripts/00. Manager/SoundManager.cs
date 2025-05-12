// System
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager
{
    public void BGMPlay(string _clipName, bool isLoop = true)
    {
        GameObject bgmPlayer = GameObject.Find("BGMPlayer");

        AudioMixer audioMixer = GameManager.Resource.Load<AudioMixer>("Sounds", "AudioMixer");
        AudioMixerGroup[] bgmGroup = audioMixer.FindMatchingGroups("BGM");

        if (bgmPlayer != null)
        {
            AudioSource audio = bgmPlayer.GetComponent<AudioSource>();

            AudioClip clip = GameManager.Resource.Load<AudioClip>("Sounds/BGM", _clipName);

            audio.outputAudioMixerGroup = bgmGroup[0]; // Set audio mixer
            audio.loop = isLoop; // Set loop
            audio.clip = clip; // Set Clip

            audio.Play();
        }
        else
        {
            bgmPlayer = new GameObject("BGMPlayer");
            bgmPlayer.AddComponent<AudioSource>();

            AudioSource audio = bgmPlayer.GetComponent<AudioSource>();

            AudioClip clip = GameManager.Resource.Load<AudioClip>("Sounds/BGM", _clipName);

            audio.outputAudioMixerGroup = bgmGroup[0]; // Set audio mixer
            audio.loop = isLoop; // Set loop
            audio.clip = clip; // Set Clip

            audio.Play();
        }
    }

    public void SFXPlay(string _clipName)
    {
        GameObject sfxPlayer = GameObject.Find("SFXPlayer");

        AudioMixer audioMixer = GameManager.Resource.Load<AudioMixer>("Sounds", "AudioMixer");
        AudioMixerGroup[] sfxGroup = audioMixer.FindMatchingGroups("SFX");

        if (sfxPlayer != null)
        {
            List<AudioSource> clipPlayers = GetSFXClips(); // Get Clip Players
            AudioSource audio = null;

            audio = GetEmptyClip(clipPlayers); // Find Empty Clip Player

            AudioClip clip = GameManager.Resource.Load<AudioClip>("Sounds/SFX", _clipName);

            audio.outputAudioMixerGroup = sfxGroup[0]; // Set audio mixer
            audio.clip = clip; // Set Clip
            audio.loop = false; // Set loop

            audio.Play();
        }
        else
        {
            GenerateSFXClipPlayer(); // Generate SFX clip player

            List<AudioSource> clipPlayers = GetSFXClips(); // Get Clip Players
            AudioSource audio = null;

            audio = GetEmptyClip(clipPlayers); // Find Empty Clip Player

            AudioClip clip = GameManager.Resource.Load<AudioClip>("Sounds/SFX", _clipName);

            audio.outputAudioMixerGroup = sfxGroup[0]; // Set audio mixer
            audio.clip = clip; // Set Clip
            audio.loop = false; // Set loop

            audio.Play();
        }
    }

    public void GenerateSFXClipPlayer(int n = 10)
    {
        GameObject sfxPlayer = GameObject.Find("SFXPlayer");

        if (sfxPlayer == null) sfxPlayer = new GameObject("SFXPlayer");

        for (int i = 0; i < n; i++)
        {
            GameObject clipPlayer = new GameObject("Clip Player");
            clipPlayer.AddComponent<AudioSource>();
            clipPlayer.transform.SetParent(sfxPlayer.transform);
        }
    }

    private List<AudioSource> GetSFXClips()
    {
        GameObject sfxPlayer = GameObject.Find("SFXPlayer");

        AudioSource[] clipPlayers = sfxPlayer.GetComponentsInChildren<AudioSource>();
        List<AudioSource> finClipPlayers = new List<AudioSource>();

        for (int i = 1; i < clipPlayers.Length; i++)
        {
            finClipPlayers.Add(clipPlayers[i]);
        }

        return finClipPlayers;
    }

    public AudioSource GetEmptyClip(List<AudioSource> _clipPlayers)
    {
        foreach (AudioSource clipPlayer in _clipPlayers)
        {
            if (!clipPlayer.isPlaying) return clipPlayer;
        }

        GenerateSFXClipPlayer();

        return GetEmptyClip(GetSFXClips()); ;
    }
}