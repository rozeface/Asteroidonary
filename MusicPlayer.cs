using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicPlayer : MonoBehaviour
{
    // handles all music playing for gameplay and menu

    public static MusicPlayer instance = null;
    public AudioSource aSource = null;

    public List<AudioClip> menuSongs = new List<AudioClip>();
    public AudioClip cosmicIntro;
    public AudioClip cosmicLoop;

    bool inMenu = false;
    bool started = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            aSource = this.GetComponent<AudioSource>();
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (PlayerPrefs.GetFloat("musicVolume") != 0) // if the volume has been altered and stored from player before
        {
            aSource.volume = PlayerPrefs.GetFloat("musicVolume");
        }
        else aSource.volume = 1f; // standard volume

        if (SceneManager.GetActiveScene().buildIndex == 0) // if this is the menu scene (make sure to refresh integer comparison if order of scenes changes)
        {
            RandomMenuMusic();
            inMenu = true;
        }
        else
        {
            PlayIntro();
            inMenu = false;
        }
    }

    private void Update()
    {
        SwitchToLoop();
    }

    public static void PlaySong(AudioClip clip)
    {
        instance.aSource.clip = clip;
        instance.aSource.Play();
    }

    void RandomMenuMusic()
    {
        aSource.clip = menuSongs[Random.Range(0, menuSongs.Count)];
        aSource.Play();
    }

    public static void PlayIntro() // returns from boss music back to general gameplay music
    {
        instance.aSource.loop = false;
        instance.aSource.clip = instance.cosmicIntro;
        instance.aSource.Play();
    }

    void PlayLoop()
    {
        instance.aSource.loop = true;
        instance.aSource.clip = instance.cosmicLoop;
        instance.aSource.Play();
    }

    void SwitchToLoop()
    {
        if (!inMenu)
        {
            if (!instance.aSource.isPlaying && !GameManager.instance.gameOver && !PlayerMovement.instance.isPaused) // if music is not playing, and game is not over or paused, play/start loop.
            {
                PlayLoop();
            }
        }
    }

    public static void PlayMenuMusic()
    {
        instance.aSource.clip = instance.menuSongs[Random.Range(0, instance.menuSongs.Count)];
        instance.aSource.Play();
    }
}
