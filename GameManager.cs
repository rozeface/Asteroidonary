using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// universal coding rules to follow given the circumstances (specific to this game):
// ALWAYS parent any spawned objects to another object that is under the game canvas to ensure proper scaling

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;

    [Header("Player Refs")]
    public int playerScore = 0;
    public int currencyEarned;
    private PlayerMovement player;
    [HideInInspector] public int highScore = 0;
    public int playerCurrency;
    [SerializeField] ParticleSystem barrierDeathParticles = null;
    [SerializeField] ParticleSystem playerDeathPartciles = null;
    [SerializeField] float deathPause = 0f;
    bool deathPauseInit = false;
    [SerializeField] float gameOverCanvasDelay = 0f;

    public static float regularSlowMo = 32f; // how much to slow time by when player attacking
    // public static float ultSlowMo = 32f; // how much to slow time by when player is using ult **ult has been removed**

    private int waveReached;
    [HideInInspector] public int highestWave;

    [Header("Health Refs")]
    //public SpriteRenderer healthSr;
    //public Sprite[] healthSprites;
    public int currentHealth = 3;
    public int maxHealth = 4; // naturally 3 hearts, 4th one is a shield
    [SerializeField] GameObject barrier;
    [SerializeField] GameObject crackLights1;
    [SerializeField] GameObject crackLights2;
    [SerializeField] GameObject armorLights;
    public bool gameOver = false;

    [Header("UI Refs")]
    public GameObject pauseCanvas;
    [SerializeField] Button pauseButton;
    public GameObject gameOverCanvas;
    [SerializeField] GameObject fadeObj;

    [Header("Ad Ref")]
    [SerializeField] GameObject adManagerObj;
    AdManager adManager;

    private void Awake()
    {
        if (instance == null) // if we dont have instance of game manager, assign instance
            instance = this;
        else if (instance != this) // if instance already assigned, destroy this one
            Destroy(gameObject);

        adManager = adManagerObj.GetComponent<AdManager>();
    }

    private void Start()
    {
        LoadData();
        player = PlayerMovement.instance;

        if (!player.invincible)
        {
            currentHealth = maxHealth;
        }
        else currentHealth = 9999;

        MusicPlayer.PlayIntro();
        fadeObj.SetActive(true);
    }

    void SaveData()
    {
        PlayerPrefs.SetInt("savedFirstRun", 1);
        PlayerPrefs.SetInt("highScore", instance.highScore);
        PlayerPrefs.SetInt("highWave", instance.highestWave);
        PlayerPrefs.SetInt("currency", PlayerPrefs.GetInt("currency") + instance.currencyEarned);
    }
    void LoadData()
    {
        instance.highScore = PlayerPrefs.GetInt("highScore");
        instance.highestWave = PlayerPrefs.GetInt("highWave");
        instance.playerCurrency = PlayerPrefs.GetInt("currency");
    }
    public static void EndGame()
    {
        if (!instance.gameOver)
        {
            //instance.healthSr.sprite = null;
            MusicPlayer.instance.aSource.Stop();

            if (!instance.deathPauseInit)
            {
                instance.player.isPaused = true;
                instance.StartCoroutine(instance.Unpause());
                instance.deathPauseInit = true;
            }
            instance.PostDeathPause();

            instance.player.scoreToAdd = 0;
            instance.waveReached = GameObject.FindWithTag("ASpawner").GetComponent<AsteroidSpawner>().currentWave;
            instance.Invoke("ActivateGameOverCanvas", instance.gameOverCanvasDelay);

            if (instance.playerScore > instance.highScore)
            {
                instance.highScore = instance.playerScore;
            }

            if (instance.waveReached > instance.highestWave)
            {
                instance.highestWave = instance.waveReached;
            }

            instance.currencyEarned = instance.playerScore / 10;
            Debug.Log("currency earned: " + instance.currencyEarned);

            instance.SaveData();

            instance.gameOver = true;
        }
    }

    IEnumerator Unpause()
    {
        yield return new WaitForSecondsRealtime(deathPause);
        player.isPaused = false;
        SoundPlayer.PlaySound(SoundPlayer.instance.deathSound);
        instance.barrierDeathParticles.Play();
        instance.playerDeathPartciles.Play();
        instance.barrier.SetActive(false);
        instance.player.sr.enabled = false;
    }

    void PostDeathPause()
    {
        instance.player.canMove = false;
        instance.player.rb.velocity = Vector2.zero;
        instance.player.GetComponent<BoxCollider2D>().enabled = false;
    }

    void ActivateGameOverCanvas()
    {
        instance.gameOverCanvas.SetActive(true);
        instance.pauseButton.interactable = false;

        if (PlayerPrefs.GetInt("playAd") == 1)
        {
            instance.adManager.PlayInterstitialAd();
        }
        PlayAdEveryOtherTime();
    }

    void PlayAdEveryOtherTime()
    {
        if (PlayerPrefs.GetInt("playAd") == 0)
        {
            PlayerPrefs.SetInt("playAd", 1);
        }
        else if (PlayerPrefs.GetInt("playAd") == 1)
            PlayerPrefs.SetInt("playAd", 0);
    }

    public static void NewGame()
    {
        SceneManager.LoadScene("MainScene");
        MusicPlayer.PlayIntro(); // restart music
    }

    public static void Damage(int amt)
    {
        if (instance.currentHealth > 0)
        {
            instance.currentHealth -= amt;

            SoundPlayer.PlaySound(SoundPlayer.instance.dmgSound);

            instance.AdjustHealthSprites();

            if (instance.currentHealth <= 0)
            {
                EndGame();
            }
        }
    }

    public static void AddPoints(int pts)
    {
        instance.playerScore += pts;
    }

    public static void AddRewardCurrency()
    {
        /*
        int rewardAmt = (instance.playerScore / 10) * 2;
        PlayerPrefs.SetInt("currency", PlayerPrefs.GetInt("currency") + rewardAmt);
        instance.currencyEarned += rewardAmt;
        Debug.Log("Amount rewarded by ad: " + rewardAmt);
        */

        int rewardAmt = (int)(instance.currencyEarned * .1f) + 20;
        PlayerPrefs.SetInt("currency", PlayerPrefs.GetInt("currency") + rewardAmt);
        Debug.Log("Amount rewarded by ad: " + rewardAmt);
        instance.currencyEarned += rewardAmt;
    }

    public static void Heal()
    {
        if (instance.currentHealth <= instance.maxHealth) // = included as player can obtain an extra heart/shield
        {
            instance.currentHealth++;

            instance.AdjustHealthSprites();
        }
    }

    void AdjustHealthSprites()
    {
        // health tracking
        if (instance.currentHealth == 1)
        {
            //healthSr.sprite = healthSprites[0];
            crackLights2.SetActive(true);
            crackLights1.SetActive(true);
        }
        if (instance.currentHealth == 2)
        {
            //healthSr.sprite = healthSprites[1];
            crackLights1.SetActive(true);
            crackLights2.SetActive(false);
        }
        if (instance.currentHealth == 3)
        {
            //healthSr.sprite = healthSprites[2];
            crackLights1.SetActive(false);
            crackLights2.SetActive(false);
            armorLights.SetActive(false);
        }
        if (instance.currentHealth == 4) // shield/bonus health
        {
            armorLights.SetActive(true);
        }
    }

    /*
    public void AdjustDashSprites()
    {
        // dash tracking
        if (player.currentDashes == 0)
        {
            dashSr.sprite = null;
        }
        if (player.currentDashes == 1)
        {
            dashSr.sprite = dashSprites[0];
        }
        if (player.currentDashes == 2)
        {
            dashSr.sprite = dashSprites[1];
        }
        if (player.currentDashes == 3)
        {
            dashSr.sprite = dashSprites[2];
        }
    }
    */

    public static Vector2 Rotate(Vector2 v, float angle) // serves to rotate vectors with custom angles
    {
        float radian = angle * Mathf.Deg2Rad;
        float _x = v.x * Mathf.Cos(radian) - v.y * Mathf.Sin(radian);
        float _y = v.x * Mathf.Sin(radian) + v.y * Mathf.Cos(radian);
        return new Vector2(_x, _y);
    }

    public static void RotateTowardsDir(GameObject obj, Vector3 dir) // serves to rotate an object towards a direction
    {
        dir.Normalize();
        if (dir != Vector3.zero && dir.x > 0) // travelling right, sprite is normal so rotate forward
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            obj.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        else if (dir.x < 0) // travelling left, sprite is flipped, rotate towards opposite direction
        {
            float angle = Mathf.Atan2(-dir.y, -dir.x) * Mathf.Rad2Deg;
            obj.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    public static void PlayAnimFromStart(Animator anim, string animName)
    {
        anim.Play(animName, -1, 0f); // code set up to restart animation state back to beginning each time it is triggered
    }

    public void PauseGame()
    {
        player.isPaused = true;
        pauseCanvas.SetActive(true);
        MusicPlayer.instance.aSource.Pause();
        SoundPlayer.instance.aSource.Stop();
    }

    public void ResumeGame()
    {
        player.isPaused = false;
        pauseCanvas.SetActive(false);
        MusicPlayer.instance.aSource.UnPause();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("MainScene");
        MusicPlayer.PlayIntro();
    }

    public void QuitToMenu()
    {
        // save only SOME data, player loses progress if they quit early, initialize score etc.
        SceneManager.LoadScene("MainMenu");
        MusicPlayer.PlayMenuMusic();
    }

    public void InteractSound()
    {
        SoundPlayer.PlayInteractSound();
    }
}
