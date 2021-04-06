using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AsteroidSpawner : MonoBehaviour
{
    [Header("Debugging/Testing")]
    [SerializeField] bool testing = false;
    [SerializeField] bool startBomb = false;
    [SerializeField] bool startGem = false;

    [Header("Standard Asteroids")]
    [SerializeField] List<GameObject> bombAsteroids = new List<GameObject>();
    [SerializeField] List<GameObject> gemAsteroids = new List<GameObject>();
    [SerializeField] float maxSizeMult;
    [SerializeField] float minSizeMult;

    [SerializeField] float largeChance = 0f; // % chance for large asteroids to spawn
    [SerializeField] float mediumChance = 0f; // % chance for medium asteroids to spawn
    //[SerializeField] float smallChance = 0f; // % chance for small asteroids to spawn // doesnt need var

    [Header("Wave Settings")]
    [SerializeField] bool shouldSpawn = false; // debugging/testing
    private bool spawned = false;

    private List<GameObject> spawnPoints = new List<GameObject>();
    private GameObject currentPoint;
    private bool upperHalf = false; // to switch between grabbing from upper and lower half of list - decreases probability of asteroids spawning ontop of eachother

    private float timeSinceSpawn = 0f;
    public float spawnDelay = 0f;
    [HideInInspector] public float startSpawnDelay = 0f;
    [SerializeField] float decreaseTime = 0f;
    [SerializeField] float minTime = .5f;

    [SerializeField] GameObject waveUI = null;
    private TextMeshProUGUI waveNumText = null;
    private float timeSinceStart = 0f;
    [SerializeField] float waveLength = 0f; // length of each wave (in time)
    [SerializeField] float waveLengthInc = 0f; // how much to increase the length/duration of next wave
    [SerializeField] float maxWaveLength = 0f;
    public int currentWave = 1;
    [SerializeField] float delayBtwnWaves = 0f;

    [Header("Boss Settings")]
    int bossFreq; // how frequent the boss waves happen (value of 5 = once every 5 waves)
    [SerializeField] int minBossFreq; // least amt of waves until boss
    [SerializeField] int maxBossFreq; // max amt of waves until boss
    [SerializeField] bool bossWave = false;
    [SerializeField] float bossDelay = 0f; // how long to wait before spawning boss (prep time)
    private bool spawnedBoss = false; // ensures the boss only gets spawned once, re-initalize when current boss dies
    bool invoked = false;
    [SerializeField] GameObject bombTitan = null;
    [SerializeField] GameObject gemTitan = null;
    private GameObject currentTitan = null;
    private bool checkDeath = false;

    private string currentWaveType = null;
    private string nextWaveType = null;
    private string lastWaveType = null; // to determine which boss should be spawned

    [Header("Background/Wave Transition")]
    [SerializeField] GameObject starsBG = null;
    private float starsHeight = 0f; // height of current object
    private Vector3 bgStartPos = Vector3.zero; // start pos before transition
    private Vector3 bgTargetPos = Vector3.zero; // target pos of where transition should end
    private bool transitioning = false; // whether stars are/should be transitioning to new area
    private float transitionSpeed = 0f;
    [SerializeField] float transitionAccel = 0f; // how fast we want background to accelerate
    bool delayInit = false;

    [Header("Special Settings")]
    [SerializeField] GameObject healthAst = null;
    [SerializeField] float healthPercentChance = 0f; // .2 would be a 20% chance, etc. (needs to be between 0 and 1)
    [SerializeField] float healthDelay = 0f;
    float timeSinceHealth = 0f;

    [SerializeField] GameObject angleAsteroid = null;
    [SerializeField] float anglePercentChance = 0f;
    [SerializeField] float angleDelay = 0f; // time that must be waited before spawning another angle asteroid
    float timeSinceAngle = 0f;

    private void Start()
    {
        startSpawnDelay = spawnDelay;
        waveNumText = waveUI.transform.Find("WaveNumText").GetComponent<TextMeshProUGUI>();
        ShowWaveText();

        foreach (Transform child in transform) // add all spawnpoints to the list
        {
            spawnPoints.Add(child.gameObject);
        }

        currentWave = 0;
        bossFreq = RandomBossFreq();
        PredictNextWave();
        if (!testing)
        {
            SwitchWaveType(nextWaveType);
        }
        else
        {
            if (startBomb)
            {
                SwitchWaveType("bomb");
            }
            if (startGem)
            {
                SwitchWaveType("gem");
            }
        }

        starsHeight = starsBG.GetComponentInChildren<BoxCollider2D>().size.y;

        timeSinceSpawn = 3f;
    }

    private void Update()
    {
        if (!GameManager.instance.gameOver)
        {
            if (shouldSpawn)
            {
                if (!bossWave)
                {
                    // wave system
                    if (timeSinceStart >= waveLength) // wave timer finished, start next wave
                    {
                        NextWave();
                        IncreaseDifficulty();
                    }
                    else // current wave
                    {
                        timeSinceStart += Time.deltaTime;
                    }

                    // spawning system
                    if (timeSinceSpawn <= 0)
                    {
                        SpawnAsteroids();
                    }
                    else
                    {
                        timeSinceSpawn -= Time.deltaTime;
                        timeSinceAngle -= Time.deltaTime;
                        timeSinceHealth -= Time.deltaTime;
                        spawned = false;
                    }

                    if (currentWave == bossFreq)
                    {
                        bossWave = true;
                    }
                    else // not boss wave, store current wave type into lastWaveType all the way up until hit boss wave
                    {
                        lastWaveType = currentWaveType;
                    }
                }

                if (bossWave)
                {
                    if (lastWaveType == "bomb")
                    {
                        if (!invoked)
                        {
                            Invoke("SpawnBombTitan", bossDelay);
                        }

                        currentWaveType = "bombTitan";
                    }

                    if (lastWaveType == "gem")
                    {
                        if (!spawnedBoss)
                        {
                            //StartCoroutine(SpawnTitan(gemTitan));
                            Invoke("SpawnGemTitan", bossDelay);
                        }

                        currentWaveType = "gemTitan";
                    }

                    if ((GameObject.FindWithTag("BombTitan") || GameObject.FindWithTag("GemTitan")) && !checkDeath)
                    {
                        checkDeath = true; // only check for titan death once boss has forsure been spawned
                    }

                    if (checkDeath)
                    {
                        TitanDeathCheck();
                    }
                    // Debug.Log("We are in a boss wave. bomb wave type? " + bombWaveType + ". gem wave type? " + gemWaveType);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (transitioning)
        {
            BackgroundTransition();
        }
    }

    public void NextWave()
    {
        SetUpTransition();
        SwitchWaveType(nextWaveType);
        //Debug.Log("Current: " + currentWaveType);
        currentWave++;
        timeSinceSpawn = delayBtwnWaves; // creates a delay before spawning more asteroids on next wave

        timeSinceStart = 0f;
        PredictNextWave();
    }

    void ShowWaveText()
    {
        waveNumText.text = currentWave.ToString();
        waveUI.GetComponent<Animator>().SetTrigger("ShowText");
        waveNumText.GetComponent<Animator>().SetTrigger("ShowText");
    }

    void SetUpTransition() // function serves to intialize before the actual stars transition (anything we don't want constantly done in update)
    {
        bgStartPos = starsBG.transform.localPosition;
        transitioning = true;
        bgTargetPos = new Vector3(bgStartPos.x, Random.Range(0 - (starsHeight / 2) + Screen.height, 0 + (starsHeight / 2) - Screen.height), bgStartPos.z); // where we want bg to move 
        Invoke("DelayBackgroundTransition", 3f);
    }

    void BackgroundTransition() // move the background to indicate a new wave (inside of update)
    {
        shouldSpawn = false; // pause spawning of any kind until background has reached proper position

        if (delayInit)
        {
            transitionSpeed += transitionAccel * Time.deltaTime;
            starsBG.transform.localPosition = Vector3.Lerp(bgStartPos, bgTargetPos, transitionSpeed);
        }

        if (starsBG.transform.localPosition == bgTargetPos) // once we reach our target position
        {
            transitionSpeed = 0f; // reset transition speed before next transition

            shouldSpawn = true; // resume spawning
            delayInit = false;
            transitioning = false; // stop transitioning
        }
    }

    
    void DelayBackgroundTransition()
    {
        delayInit = true;
        ShowWaveText();
    }
    
    void IncreaseDifficulty()
    {
        // wave difficulty progression
        if (spawnDelay > minTime)
        {
            startSpawnDelay -= decreaseTime;
            spawnDelay = startSpawnDelay;
        }

        if (waveLength <= maxWaveLength)
        {
            waveLength += waveLengthInc;
        }

        // increase speed for asteroids?

        //Debug.Log("reached the end of a wave, now entering wave " + currentWave + ", current speed is " + spawnDelay);
    }

    void SpawnAsteroids()
    {
        RandomSpawnPoint();

        // GENERAL SPAWNING - pick a random prefab considering wave type
        if (!spawned)
        {
            if (currentWaveType == "gem")
            {
                currentPoint = Instantiate(gemAsteroids[RandomAsteroidSize()], spawnPoints[RandomSpawnPoint()].transform.position, Quaternion.identity);
                currentPoint.transform.SetParent(transform);
                RandomScale(currentPoint);
            }

            if (currentWaveType == "bomb")
            {
                currentPoint = Instantiate(bombAsteroids[RandomAsteroidSize()], spawnPoints[RandomSpawnPoint()].transform.position, Quaternion.identity);
                currentPoint.transform.SetParent(transform);
                RandomScale(currentPoint);
            }

            // SPECIAL CASE SPAWNING
            if (Random.value <= healthPercentChance && timeSinceHealth <= 0f)
            {
                GameObject health = Instantiate(healthAst, spawnPoints[RandomSpawnPoint()].transform.position, Quaternion.identity);
                health.transform.SetParent(transform);
                NormalScale(health);
                timeSinceHealth = healthDelay;
            }

            if (Random.value <= anglePercentChance && timeSinceAngle <= 0f)
            {
                GameObject challenge = Instantiate(angleAsteroid, spawnPoints[RandomSpawnPoint()].transform.position, Quaternion.identity);
                challenge.transform.SetParent(transform);
                NormalScale(challenge);
                timeSinceAngle = angleDelay;
            }

            upperHalf = !upperHalf; // flip value so next time called it spawns in opposite half
            spawned = true;
        }

        timeSinceSpawn = spawnDelay;
    }

    int RandomAsteroidSize() // grabs a random sized asteroid based on % chances
    {
        float value = Random.value;
        if (value <= largeChance)
        {
            return 2; // large asteroid
        }
        else if (value <= mediumChance)
        {
            return  1; // medium asteroid
        }
        else // value <= smallChance
        {
            return 0; // small asteroid
        }
    }

    void PredictNextWave() // serves to predict what the next wave type will be before it commences
    {
        int rand = Random.Range(1, 3);
        if (rand == 1)
        {
            nextWaveType = "bomb";
        }
        if (rand == 2)
        {
            nextWaveType = "gem";
        }
    }

    void SwitchWaveType(string next) // changes the current wave type based on the passing of predicted next wave type
    {
        if (next == "bomb")
        {
            currentWaveType = "bomb";
        }
        if (next == "gem")
        {
            currentWaveType = "gem";
        }

        //Debug.Log("Calculating random wave type. Random number is " + rand);
    }

    int RandomBossFreq()
    {
        return currentWave + Random.Range(minBossFreq, maxBossFreq + 1);
    }
    
    void SpawnGemTitan()
    {
        if (!spawnedBoss)
        {
            currentTitan = Instantiate(gemTitan, spawnPoints[spawnPoints.Count / 2].transform.position, Quaternion.identity);
            currentTitan.transform.SetParent(transform);
            currentTitan.transform.localScale = Vector3.one;

            bossFreq = RandomBossFreq(); // rendom frequency until next boss spawn
            spawnedBoss = true;
        }
        invoked = true;
    }
    void SpawnBombTitan()
    {
        if (!spawnedBoss)
        {
            currentTitan = Instantiate(bombTitan, spawnPoints[spawnPoints.Count / 2].transform.position, Quaternion.identity);
            currentTitan.transform.SetParent(transform);
            currentTitan.transform.localScale = Vector3.one;

            bossFreq = RandomBossFreq(); // rendom frequency until next boss spawn
            spawnedBoss = true;
        }
        invoked = true;
    }

    private void TitanDeathCheck()
    {
        if (currentTitan == null) // titans destroyed, end boss wave
        {
            NextWave();
            bossWave = false;
            spawnedBoss = false;
            invoked = false;
            checkDeath = false;
        }
    }

    int RandomSpawnPoint() // grabs a random spawn point (better than simply using random.range as it avoids a repeated position)
    {
        // grabs a random spawn point
        if (upperHalf)
        {
            return Random.Range(0, spawnPoints.Count / 2);
        }
        else
        {
            return Random.Range((spawnPoints.Count / 2) + 1, spawnPoints.Count);
        }
    }

    float RandomTime()
    {
        return Random.Range(0f, waveLength - 5f);
    }
    
    void NormalScale(GameObject obj)
    {
        obj.transform.localScale = Vector3.one;
    }
    void RandomScale(GameObject obj)
    {
        float randomVal = Random.Range(minSizeMult, maxSizeMult);
        obj.transform.localScale = new Vector3(randomVal, randomVal, 1f);
    }
}
