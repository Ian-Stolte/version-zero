using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public bool playEffects;

    [Header("Editable")]
    public Sound[] music;
    public Sound[] uiSFX;
    public Sound[] worldSFX;
    public Sound[] programSFX;
    public Sound[] combatSFX;
    private List<Sound> sfx = new List<Sound>();

    [Header("Don't edit")]
    public AudioSource[] audios;

    public List<Sound> currentSongs;


    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        foreach (Sound s in uiSFX)
            sfx.Add(s);
        foreach (Sound s in worldSFX)
            sfx.Add(s);
        foreach (Sound s in programSFX)
            sfx.Add(s);
        foreach (Sound s in combatSFX)
            sfx.Add(s);

        foreach (Sound s in music)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            //play once to reset
            float storedVol = s.volume;
            s.volume = 0;
            s.source.Play();
            s.source.Stop();
            s.volume = storedVol;
        }
        foreach (Sound s in sfx)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
        audios = gameObject.GetComponents<AudioSource>();
    }


    void Start()
    {
        int sceneNum = 0;
        int.TryParse(SceneManager.GetActiveScene().name.Substring(6), out sceneNum);
        if (SceneManager.GetActiveScene().name.Contains("Final"))
        {
            Play("Area Final");
            StartCoroutine(StartFade("Area Final", 1, 0.25f));
        }
        else if (sceneNum >= 13)
        {
            Play("Area 3");
            StartCoroutine(StartFade("Area 3", 2, 0.25f));
        }
        else if (sceneNum >= 7)
        {
            Play("Area 2");
            StartCoroutine(StartFade("Area 2", 2, 0.4f));
        }
        else if (sceneNum > 0)
        {
            Play("Area 1");
            StartCoroutine(StartFade("Area 1", 2, 0.25f));
        }
    }


    public IEnumerator FadeOutAll(float duration)
    {
        List<string> songsToFade = new List<string>();
        foreach (Sound s in music)
        {
            if (s.source.volume != 0)
            {
                StartCoroutine(StartFade(s.name, duration, 0));
                songsToFade.Add(s.name);
            }
        }
        yield return new WaitForSeconds(duration);
        foreach (Sound s in music)
        {
            if (songsToFade.Contains(s.name))
                s.source.Stop();
        }
    }

    public IEnumerator QuietAll(float duration, float n)
    {
        foreach (Sound s in music)
        {
            if (s.source.volume != 0)
                StartCoroutine(StartFade(s.name, duration, s.source.volume * n));
        }
        yield return new WaitForSeconds(duration);
        foreach (Sound s in music)
        {
            if (s.source.volume != 0)
                StartCoroutine(StartFade(s.name, duration, s.source.volume / n));
        }

    }

    public void Play(string name)
    {
        Sound s = sfx.Find(sound => sound.name == name);
        if (s == null)
        {
            s = Array.Find(music, sound => sound.name == name);
            if (s != null)
                currentSongs.Add(s);
        }
        if (s == null)
        {
            Debug.LogError("Sound: " + name + " not found!");
            return;
        }
        s.source.Play();
    }

    public void Stop(string name)
    {
        Sound s = sfx.Find(sound => sound.name == name);
        if (s == null)
            s = Array.Find(music, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogError("Sound: " + name + " not found!");
            return;
        }
        currentSongs.Remove(s);
        s.source.Stop();
    }

    public IEnumerator StartFade(string name, float duration, float end)
    {
        Sound s = sfx.Find(sound => sound.name == name);
        if (s == null)
            s = Array.Find(music, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogError("Sound: " + name + " not found!");
            yield break;
        }

        float currentTime = 0;
        float start = s.source.volume;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            s.source.volume = Mathf.Lerp(start, end, currentTime / duration);
            yield return null;
        }

        //if (end == 0)
        //    s.source.Stop();
    }


    //CONTENT
    public void KillBoss1()
    {
        Stop("Boss 1");
        Play("Walking Turret Die");
        Play("Area 1");
        StartCoroutine(StartFade("Area 1", 2, 0.2f));
    }

    public void KillBoss2()
    {
        Stop("Boss 2");
        Play("Walking Turret Die");
        Play("Area 2");
        StartCoroutine(StartFade("Area 2", 2, 0.2f));
    }


    public IEnumerator Area2()
    {
        StartCoroutine(StartFade("Area 1", 3, 0f));
        yield return new WaitForSeconds(2f);
        Stop("Area 1");

        yield return new WaitForSeconds(2f);
        Play("Area 2");
        StartCoroutine(StartFade("Area 2", 2, 0.4f));
        yield return new WaitForSeconds(1f);
    }
    
    public IEnumerator Area3()
    {
        StartCoroutine(StartFade("Area 2", 3, 0f));
        yield return new WaitForSeconds(2f);
        Stop("Area 2");

        yield return new WaitForSeconds(2f);
        Play("Area 3");
        StartCoroutine(StartFade("Area 3", 2, 0.2f));
        yield return new WaitForSeconds(1f);
    }
}


[System.Serializable]
public class Sound
{
    public string name;

    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume;
    [Range(-3f, 3f)]
    public float pitch;

    public bool loop;

    public AudioSource source;
}