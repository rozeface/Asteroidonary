using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    // handles any and all sound FX in the menu and general gameplay

    public static SoundPlayer instance = null;
    public AudioSource aSource = null;

    public AudioClip interactSound;
    public AudioClip slashSound;
    public AudioClip sheatheSound;
    public AudioClip unsheatheSound;
    public AudioClip slowSound;
    public AudioClip dmgSound;
    public AudioClip deathSound;
    public AudioClip healthPickupSound;
    public AudioClip explosionSound;
    public AudioClip gemTitanShoot;
    public AudioClip gemShardSmack;
    public AudioClip gemTitanTing;
    public AudioClip gemTitanDamage;
    public AudioClip gemTitanDie;
    public AudioClip angleStun;
    public AudioClip shockwaveSound;
    public List<AudioClip> astSlashSounds = new List<AudioClip>();
    public AudioClip bombTitanDie;
    public AudioClip bombTitanWindup;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            aSource = this.GetComponent<AudioSource>();
            DontDestroyOnLoad(this);
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (PlayerPrefs.GetFloat("soundVolume") != 0) // if volume has been set by player before, change to it
        {
            aSource.volume = PlayerPrefs.GetFloat("soundVolume");
        }
        else aSource.volume = 1f;
    }

    public static void PlaySound(AudioClip clip)
    {
        instance.aSource.PlayOneShot(clip);
    }

    public void InteractSound()
    {
        instance.aSource.PlayOneShot(interactSound);
    }

    public static void PlayInteractSound()
    {
        instance.aSource.PlayOneShot(instance.interactSound);
    }

    public static void PlayAstSlash()
    {
        /*
        float randomVal = Random.value;

        if (randomVal <= .5)
        {
            instance.aSource.PlayOneShot(instance.astSlashSounds[0]);
        }
        else instance.aSource.PlayOneShot(instance.astSlashSounds[1]);
        */
        instance.aSource.PlayOneShot(instance.astSlashSounds[0]);
    }
}
