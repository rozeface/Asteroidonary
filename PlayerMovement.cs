using UnityEngine;
using UnityEngine.UI;
using EZCameraShake;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("Debugging/Testing")]
    public bool invincible = false;
    [SerializeField] bool pcTesting = false;
    [SerializeField] bool cursorOn;

    private GameObject dTrigger = null; // deceleration trigger
    [Header("Deceleration Trigger")]
    public bool decelerate = false;
    [SerializeField] float decelSpeed = 0f;
    public float minSpeed = 0f;

    [Header("Slow Motion")]
    AudioSource music = null;
    public float maxSlowMoTime = 0f;
    [SerializeField] float minVolumeDec = 0f; // min amt to decrease current volume by when enter slow mo
    float startVol = 0f;
    [SerializeField] float volInc = 0f; // to gradually return volume to normal
    public float speedIncrement = 0f; // how fast objects should return to normal speed
    bool adjustedAudio = false;
    [HideInInspector] public float timeSinceSheathed = 0f;

    AsteroidSpawner astSpawner = null;
    [SerializeField] float spawnSlowAmt = 0f; // how much to slow spawning down initially
    [SerializeField] float spawnInc = 0f; // increment to gradually return spawning to normal speed

    [Header("Player Refs & Vars")]
    public static PlayerMovement instance;
    public Rigidbody2D rb = null;
    private Camera mainCam = null;
    [SerializeField] GameObject playerSpriteObj = null;
    public Animator playerAnimator = null;
    [SerializeField] GameObject playerRot = null; // animation is relative to parent object and bc position changes on sprite object this object was added to handle all rotation
    [HideInInspector] public SpriteRenderer sr;
    public GameObject frontGhost = null;
    public GameObject backGhost = null;

    [SerializeField] GameObject slashParticlesPrefab = null;
    [SerializeField] ParticleSystem sheatheParticles = null;
    [SerializeField] ParticleSystem teleportParticles = null;
    [SerializeField] ParticleSystem stunParticles = null;
    public bool stunned = false;
    [HideInInspector] public ParticleSystem currentTrail = null;
    [SerializeField] ParticleSystem worldMovementParticles = null;

    // pausing/time scale adjustments/debugging
    [HideInInspector] public float currentTimeScale = 0f; // if there is ever slow motion, for game manager to reference
    private bool timeChanged = false;
    [HideInInspector] public bool isPaused = false;

    // touch controls
    [HideInInspector] public bool canMove = true;
    private Touch touch;
    private Vector3 startPos = Vector3.zero;
    private Vector3 dragPos = Vector3.zero;
    private Vector3 releasePos = Vector3.zero;
    [HideInInspector] public Vector2 dragDir = Vector2.zero;

    //[SerializeField] float minTravelDist = 30f;
    [HideInInspector] public Vector3 travelDirection = Vector3.zero;
    private Vector3 targetVelocity = Vector3.zero; // velocity given to player depending on length of finger drag

    private bool hasDragged = false;
    [HideInInspector] public bool alterSpeed = false; // brings rigidbody modification into fixedupdate
    private bool finished = true;


    // player can progressively slash more often as they continue in a combo without sheathing
    public float slashDelay = 0f; // what the player's slash delay starts out as
    [SerializeField] float delayDecrementStep = 0f; // how much to decrease the delay by after each slash
    public float currentDelay = 0f;
    private float timeSinceSlash = 0f; // time since player last slashed
    public float preSlashDelay = 0f;
    [SerializeField] float preDelayDecrement = 0f;
    public float currentPreDelay = 0f;

    // slash system
    [SerializeField] float slashSpeed = 2000f;

    // combo system
    [Header("Combo/Multiplier System")]
    [SerializeField] TextMeshProUGUI multiplierTxt = null;
    Animator multAnimator;
    [SerializeField] Slider multiplierBar = null;
    [HideInInspector] public int currentCombo = 0;
    [HideInInspector] public int multiplier = 0;
    [SerializeField] int quantity1 = 0;
    [SerializeField] int quantity2 = 0;
    [SerializeField] int quantity3 = 0;
    [SerializeField] int maxQuantity = 0;
    [SerializeField] int multiplier1 = 0;
    [SerializeField] int multiplier2 = 0;
    [SerializeField] int multiplier3 = 0;
    [SerializeField] int maxMultiplier = 0;
    [HideInInspector] public int scoreToAdd; // proxy for RealTimeScoreDisplay to deliver current score to be added

    // player states
    [HideInInspector] public bool attacking = false; // if player is attacking
    [HideInInspector] public bool startSheathing = false; // begin to sheathe weapon
    [HideInInspector] public bool sheathed = false; // if the weapon is sheathed

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        astSpawner = GameObject.FindWithTag("ASpawner").GetComponent<AsteroidSpawner>();

        playerAnimator = playerSpriteObj.GetComponent<Animator>();
        sr = playerSpriteObj.GetComponent<SpriteRenderer>();
        currentTrail = transform.Find("TrailTester").GetComponent<ParticleSystem>();

        music = GameObject.FindWithTag("MusicPlayer").GetComponent<AudioSource>();
    }

    private void Start()
    {
        this.transform.localPosition = new Vector2(-Camera.main.pixelWidth / 2f + 520f, 0); // always want player to start at left center of screen
        startPos = transform.position;
        sheathed = true;

        rb = GetComponent<Rigidbody2D>();

        dTrigger = GameObject.FindWithTag("Decelerate");
        dTrigger.SetActive(false);

        mainCam = Camera.main;

        currentDelay = slashDelay;
        currentPreDelay = preSlashDelay;

        GameManager.instance.playerScore = 0;

        if (pcTesting)
        {
            if (!cursorOn)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Confined;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        multAnimator = multiplierTxt.GetComponent<Animator>();
    }

    private void Update()
    {
        if (!isPaused)
        {
            Time.timeScale = 1f;

            if (!timeChanged)
            {
                Time.timeScale = currentTimeScale;
                timeChanged = true;
            }

            if (canMove)
            {
                currentTimeScale = 1f; // normal time scale when not in ultimate

                if (Input.touchCount == 1) // 1 finger input, > 0/multi finger causes bugs
                {
                    touch = Input.GetTouch(0);

                    if (touch.phase == TouchPhase.Moved) // start and drag
                    {
                        if (Time.time - timeSinceSlash >= currentDelay) // if delay has been waited out
                        {
                            Dragging();
                        }
                    }

                    if (touch.phase == TouchPhase.Ended) // release drag
                    {
                        if (hasDragged)
                        {
                            ReleaseDrag();
                        }
                    }
                }

                // MOUSE CONTROLS
                if (pcTesting)
                {
                    if (Input.GetMouseButton(0)) // hold left click
                    {
                        if (Time.time - timeSinceSlash >= currentDelay) // if delay has been waited out
                        {
                            Dragging();
                        }
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        if (hasDragged)
                        {
                            ReleaseDrag();
                        }
                    }
                }
            }

            if (sheathed)
            {
                Time.timeScale = 1f;
                if (!hasDragged)
                {
                    IdleAnim(playerAnimator);
                }
            }
            else // not sheathed
            {
                if (travelDirection.x >= 0)
                {
                    RotateSpriteRight();
                }
                else RotateSpriteLeft();

                AudioSpeedLogic();
            }

            if (playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("PlayerSheathe")) // if sheathe animation clip starts playing
            {
                if (!finished) // ensures this code only gets ran once
                {
                    //Debug.Log("player sheathe animation happening");
                    sheathed = true;
                    attacking = false;
                    sheatheParticles.Play();
                    currentDelay = slashDelay; // reset the slash delay
                    currentPreDelay = preSlashDelay; // reset pre slash delay
                    SoundPlayer.instance.aSource.pitch = 1f;
                    SoundPlayer.instance.aSource.Stop(); // stops the slow mo sound from overlapping

                    if (currentCombo > 0)
                    {
                        // play explosion sound here so it doesn't overlap w multiple asteroid explosions
                        SoundPlayer.PlaySound(SoundPlayer.instance.explosionSound);
                        CameraShaker.Instance.ShakeOnce(10f * currentCombo, 20f * currentCombo, 0f, .2f * currentCombo);
                        GameManager.AddPoints(scoreToAdd);
                        scoreToAdd = 0;
                        currentCombo = 0;
                    }

                    SoundPlayer.PlaySound(SoundPlayer.instance.sheatheSound);

                    timeSinceSheathed = 0f;
                    adjustedAudio = false;
                    music.volume = startVol;

                    var vo = worldMovementParticles.GetComponent<ParticleSystem>().velocityOverLifetime;
                    vo.x = -400f;

                    astSpawner.spawnDelay = astSpawner.startSpawnDelay;

                    finished = true;
                }
            }

            ComboMultiplierLogic();
        }
        else // paused
        {
            Time.timeScale = 0;
        }
    }

    private void FixedUpdate()
    {
        if (alterSpeed)
        {
            rb.velocity = targetVelocity; // make sure to only ever adjust velocity/physics in fixed update
            alterSpeed = false;
        }

        if (decelerate)
        {
            DeceleratePlayer();
        }
    }

    void Dragging()
    {
        if (!TappingButton()) // only do dragging if we aren't touching button
        {
            hasDragged = true;

            startPos = rb.position;
            startPos.z = 0f; // keeps touch from going out of screen

            if (!pcTesting)
            {
                dragPos = mainCam.ScreenToWorldPoint(touch.position);
            }
            else dragPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            dragPos.z = 0f;

            dragDir = dragPos - startPos;
            dragDir.Normalize();

            FaceDirection();
            //GameManager.RotateTowardsDir(playerSpriteObj, dragDir); // live rotation towards finger dragging

            ReadyingAnim(playerAnimator);
        }
    }

    void ReleaseDrag()
    {
        if (!pcTesting)
        {
            releasePos = mainCam.ScreenToWorldPoint(touch.position);
        }
        else releasePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        releasePos.z = 0f;

        travelDirection = releasePos - startPos;
        travelDirection.z = 0;
        travelDirection.Normalize();

        dTrigger.SetActive(false);
        decelerate = false; // ensure there is no deceleration

        // StartCoroutine(SpawnDecelTrigger()); // after a delay, spawn trigger on player
        Invoke("SpawnDecelTrigger", .001f);

        targetVelocity = slashSpeed * travelDirection;
        Invoke("PreSlashDelay", currentPreDelay);

        sheathed = false;
        attacking = true;

        SlashingAnim(playerAnimator);

        dragPos = Vector3.zero;

        if (currentDelay > 0)
        {
            currentDelay -= delayDecrementStep;
        }
        else // avoids negative numbers/decimals
        {
            currentDelay = 0f;
        }
        timeSinceSlash = Time.time;

        if (currentPreDelay > 0)
        {
            currentPreDelay -= preDelayDecrement;
        }
        else
        {
            currentPreDelay = 0f;
        }

        FaceDirection();
        RotateSpriteTowardsTravel(dragDir);
        frontGhost.SetActive(true);
        teleportParticles.Play();

        var vo = worldMovementParticles.GetComponent<ParticleSystem>().velocityOverLifetime;
        vo.x = -200f;

        hasDragged = false;
    }

    void DeceleratePlayer()
    {
        rb.velocity -= decelSpeed * rb.velocity * Time.deltaTime; // rapidly decelerates player
        startSheathing = true;
        alterSpeed = false;

        frontGhost.SetActive(false);
        backGhost.SetActive(true);
        // should use GameManager.PlayAnimFromStart and fade the ghost sprites in and out instead of activating and deactivating

        if (rb.velocity.magnitude <= minSpeed) // if below speed threshold, stop decelrating
        {
            backGhost.SetActive(false);
            currentTrail.Stop();
            finished = false;
            decelerate = false;
        }
    }
    
    private void PreSlashDelay()
    {
        alterSpeed = true;
        currentTrail.Play(); // start trail play here
        SoundPlayer.PlaySound(SoundPlayer.instance.slashSound); // slash sound here to be visually accurate
    }
    
    private void SpawnDecelTrigger()
    {
        dTrigger.SetActive(true);
        dTrigger.transform.position = rb.position;
    }
    
    void ComboMultiplierLogic()
    {
        if (currentCombo < quantity1)
        {
            multiplier = 1;
            multiplierBar.maxValue = quantity1;
            multiplierBar.value = currentCombo;
            multAnimator.SetBool("Mult1", true);
            multAnimator.SetBool("Mult2", false);
            multAnimator.SetBool("Mult3", false);
            multAnimator.SetBool("Mult4", false);
            multAnimator.SetBool("Mult5", false);
        }
        else if (currentCombo >= quantity1 && currentCombo < quantity2)
        {
            multiplier = multiplier1;
            multiplierBar.maxValue = quantity2 - quantity1;
            multiplierBar.value = currentCombo - quantity1;
            multAnimator.SetBool("Mult1", false);
            multAnimator.SetBool("Mult2", true);
        }
        else if (currentCombo >= quantity2 && currentCombo < quantity3)
        {
            multiplier = multiplier2;
            multiplierBar.maxValue = quantity3 - quantity2;
            multiplierBar.value = currentCombo - quantity2;
            multAnimator.SetBool("Mult1", false);
            multAnimator.SetBool("Mult2", false);
            multAnimator.SetBool("Mult3", true);
        }
        else if (currentCombo >= quantity3 && currentCombo < maxQuantity)
        {
            multiplier = multiplier3;
            multiplierBar.maxValue = maxQuantity - quantity3;
            multiplierBar.value = currentCombo - quantity3;
            multAnimator.SetBool("Mult1", false);
            multAnimator.SetBool("Mult2", false);
            multAnimator.SetBool("Mult3", false);
            multAnimator.SetBool("Mult4", true);
        }
        else
        {
            multiplier = maxMultiplier;
            multiplierBar.maxValue = 1;
            multiplierBar.value = 1;
            multAnimator.SetBool("Mult1", false);
            multAnimator.SetBool("Mult2", false);
            multAnimator.SetBool("Mult3", false);
            multAnimator.SetBool("Mult4", false);
            multAnimator.SetBool("Mult5", true);
        }

        multiplierTxt.text = multiplier.ToString() + "x";
    }

    void FaceDirection() // function serves to ensure the player is facing the proper direction
    {
        if (dragDir.x < 0) // looking left
        {
            RotateSpriteLeft();
        }
        else // looking right
        {
            RotateSpriteRight();
        }
    }

    void AudioSpeedLogic()
    {
        timeSinceSheathed += Time.deltaTime;

        if (!adjustedAudio) // initial audio adjustment
        {
            startVol = music.volume;
            music.volume -= minVolumeDec;

            FirstSlashTasks();

            adjustedAudio = true;
        }

        if (timeSinceSheathed <= maxSlowMoTime)
        {
            // adjust audio here
            if (music.volume <= startVol)
            {
                music.volume += volInc * Time.deltaTime;
            }
            else music.volume = startVol;

            IncSpawnSpeed();
        }
        else // too long, return to normal audio
        {
            music.volume = startVol;
            astSpawner.spawnDelay = astSpawner.startSpawnDelay;
        }
    }

    void FirstSlashTasks()
    {
        // this only plays on the first slash of player's slash comboing
        SoundPlayer.PlaySound(SoundPlayer.instance.unsheatheSound);
        SoundPlayer.PlaySound(SoundPlayer.instance.slowSound);
        astSpawner.spawnDelay /= (1f / spawnSlowAmt); // slow the spawn speed/increase spawn delay for the duration of slashing
    }

    void IncSpawnSpeed()
    {
        if (astSpawner.spawnDelay >= astSpawner.startSpawnDelay)
        {
            astSpawner.spawnDelay -= spawnInc * Time.deltaTime;
        }
        else astSpawner.spawnDelay = astSpawner.startSpawnDelay;
    }

    public static void IdleAnim(Animator anim)
    {
        // future note, do switch to use different animations per different player skin
        anim.SetBool("Idle", true);
        anim.SetBool("Readying", false);
        anim.SetBool("Slashing", false);
        anim.SetBool("Sheathe", false);
    }
    public static void ReadyingAnim(Animator anim)
    {
        anim.SetBool("Readying", true);
        anim.SetBool("Idle", false);
        anim.SetBool("Slashing", false);
        anim.SetBool("Sheathe", false);
    }
    public static void SlashingAnim(Animator anim) // takes animator to animate and direction player is going
    {
        GameManager.PlayAnimFromStart(anim, "PlayerSlashing");
    }
    public static void SheatheAnim(Animator anim)
    {
        anim.SetBool("Sheathe", true);
        anim.SetBool("Idle", false);
        anim.SetBool("Readying", false);
        anim.SetBool("Slashing", false);
    }

    void RotateSpriteLeft()
    {
        playerRot.transform.localRotation = Quaternion.Euler(sr.transform.eulerAngles.x, 180, sr.transform.eulerAngles.z);
    }
    void RotateSpriteRight()
    {
        playerRot.transform.localRotation = Quaternion.Euler(sr.transform.eulerAngles.x, 0, sr.transform.eulerAngles.z);
    }
    void RotateSpriteTowardsTravel(Vector3 dir) // serves to rotate an object towards a direction
    {
        dir.Normalize();
        if (dir != Vector3.zero && dir.x > 0) // travelling right, sprite is normal so rotate forward
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            playerRot.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        else if (dir.x < 0) // travelling left, sprite is flipped, rotate towards opposite direction
        {
            float angle = Mathf.Atan2(-dir.y, -dir.x) * Mathf.Rad2Deg;
            playerRot.transform.rotation = Quaternion.AngleAxis(angle, -Vector3.forward);
        }
    }

    public void ReceiveStun(float duration) // proxy for the coroutine below
    {
        //StartCoroutine(Stun(duration));
        stunned = true;
        SoundPlayer.PlaySound(SoundPlayer.instance.angleStun);
        canMove = false;
        stunParticles.Play();
        // stun anim play

        Invoke("RemoveStun", duration);
    }

    void RemoveStun()
    {
        canMove = true;
        stunParticles.Stop();
        // stun anim stop
        stunned = false;
    }
    /*
    IEnumerator Stun(float duration)
    {
        stunned = true;
        SoundPlayer.PlaySound(SoundPlayer.instance.angleStun);
        canMove = false;
        stunParticles.Play();
        // stun anim play
        yield return new WaitForSecondsRealtime(duration);
        canMove = true;
        stunParticles.Stop();
        // stun anim stop
        stunned = false;
    }
    */
    public void HitPause(float duration)
    {
        //StartCoroutine(HitPauseCR(duration));
    }
    /*
    IEnumerator HitPauseCR(float duration)
    {
        isPaused = true;
        yield return new WaitForSecondsRealtime(duration);
        isPaused = false;
    }
    */
    public void SpawnSlashParticles(Vector3 pos, Vector3 scale)
    {
        GameObject sp = Instantiate(slashParticlesPrefab, pos, Quaternion.identity, transform.parent);

        if (dragDir.x >= 0) // slashing right, particles move right
        {
            sp.transform.eulerAngles = new Vector3(Random.Range(0f, 180f), sp.transform.eulerAngles.y, sp.transform.eulerAngles.z);
        }
        else // slashing left, particles move left
            sp.transform.eulerAngles = new Vector3(Random.Range(0f, 180f), 180f, sp.transform.eulerAngles.z);

        sp.transform.localScale = scale;

        sp.GetComponent<ParticleSystem>().Play();
        Destroy(sp, 2f);
    }

    bool TappingButton() // fixes issue of player glitching movement when tapping UI button
    {
        RaycastHit2D hit = Physics2D.Raycast(mainCam.ScreenToWorldPoint(touch.position), Vector2.one);

        if (pcTesting)
        {
            hit = Physics2D.Raycast(mainCam.ScreenToWorldPoint(Input.mousePosition), Vector2.one);
        }

        if (hit.transform.GetComponent<Button>())
        {
            return true;
        }
        else return false;
    }

    private void OnTriggerEnter2D(Collider2D collision) // for more accurate/thorough collision checks on asteroids
    {
        if (collision != null && attacking && collision.GetComponent<Asteroid>()) // if asteroid has not been told it has been slashed yet
        {
            Asteroid ast = collision.GetComponent<Asteroid>();
            if (!ast.slashed)
            {
                ast.FirstSlashTasks();
            }
        }
    }
}

/*                  ***OUTDATED/DEPRECATED***
**did away with finite dashes for now**
private const int MAX_DASHES = 3;
[HideInInspector] public int currentDashes;
private float refillTimer = 0; // tracks when last refill happened
public float refillDelay = .3f; // how fast dashes refill when sheathed
private bool refilledDash = false;
// ult
public const int MAX_CHARGES = 10; // how many asteroids must be destroyed for player to be able to ult
[Header("Player Ult")]
public int ultCharges = 0;
public bool inUltimate = false;

[SerializeField] GameObject trailPrefab = null;
private GameObject playerTrail = null;

private int tapCount = 0;
private float tapTimer = 0f;  // tracks when last tap happened
[SerializeField] int fingersRequired = 0; // how many fingers player must use to tap & enter ult
private int tapsRequired = 2; // x consecutive taps to enter ult
private bool startTimer = false;
[SerializeField] float tapSpeed = 0f; // how fast need to double tap to enter ult

[SerializeField] float ultDuration = 3f; // how long in ult
private float ultTimer = 0f;
[HideInInspector] public float coolDownTimer = 0f; // stores cool down
private float ultCoolDown = 10f; // duration of cooldown

private List<Asteroid> asteroids = new List<Asteroid>(); // list of selected asteroids in ult
private int currentIndex = 0; // to traverse the asteroid list
[SerializeField] int maxSelectableAst = 0; // how many asteroids can be selected in ultimate 
    // (ENSURE THERE ARE TAGS FOR EACH # of selectable asteroids)

[SerializeField] GameObject kunaiPrefab = null;
[SerializeField] float kunaiSpeed = 0f;
private float kunaiTimer = 0f; // keeps track of last kunai throw
[SerializeField] float throwDelay = 0f; // how long between each throw
private bool spawnedKunai = false;

void ThrowKunai(int i) // throws kunai at each selected asteroid in order of selection
{
    // changing tag of asteroid so it matches kunai and they can ONLY collide/destroy each other
    // fixes bug where kunai destroys meteors out of order if in same path of fire
    asteroids[i].tag = i.ToString();

    GameObject kunai = Instantiate(kunaiPrefab, transform.position, Quaternion.identity);
    kunai.transform.SetParent(transform);
    kunai.tag = i.ToString(); // matching kunai tag to asteroid tag

    Vector3 dir = asteroids[i].transform.position - transform.position; // dir from player to asteroid
    dir.Normalize(); // length of vector to 1 so distance doesn't affect speed

    // face player to direction of the throw
    if (dir.x >= 0) // to the right
    {
        RotateSpriteRight();
    }
    if (dir.x < 0)
    {
        RotateSpriteLeft();
    }

    // send kunai in direction of asteroid
    kunai.GetComponent<Rigidbody2D>().velocity = kunaiSpeed * dir;

    // rotate kunai sprite towards direction of travel
    float spriteAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    kunai.transform.Find("Sprite").transform.rotation = Quaternion.AngleAxis(spriteAngle, Vector3.forward);

    kunaiTimer = throwDelay;
}

void ExitUlt()
{
    Time.timeScale = 1f;

    //lineRenderer.enabled = true;
    ultCharges = 0;
    sr.color = new Color(255, 255, 255); // back to normal color

    asteroids.Clear(); // reset list & for next ult
    currentIndex = 0;

    astSpawner.spawnDelay = initSpawnSpeed;
    //Debug.Log("The spawn speed is back to " + astSpawner.spawnDelay);

    canMove = true;

    inUltimate = false;
}

public void EnterUltimate()
{
    inUltimate = true;
    canMove = false;

    ultTimer = ultDuration;
    coolDownTimer = ultCoolDown;

    RotateSpriteRight();
    rb.velocity = Vector2.zero;

    //lineRenderer.enabled = false;

    IdleAnim(playerAnimator);

    dTrigger.transform.position = rb.position; // in the case we use ultimate during a slash, make sure we teleport trigger here to avoid bugs

    initSpawnSpeed = astSpawner.spawnDelay; // fetch current spawn speed as we enter ult
    astSpawner.spawnDelay *= 4; // increase the delay between ast spawning
    //Debug.Log("init spawn speed was: " + initSpawnSpeed + ". The new, slowed down speed is " + astSpawner.spawnDelay);
}

void Ultimate() // actual ultimate mechanics
{
    ultTimer -= Time.deltaTime;

    if (Input.touchCount == 1)
    {
        touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved) // if finger touched screen or dragged
        {
            RaycastHit2D hit = Physics2D.Raycast(mainCam.ScreenToWorldPoint(touch.position), -Vector2.up);

            Asteroid currentAsteroid = null; // currently selected asteroid

            if (hit.collider != null)
            {
                if (hit.collider.tag == "Asteroid")
                {
                    // place crosshair or something indicative of selection ?

                    // ASTEROID SCRIPT IS ON PARENT OF COLLIDER OBJECT
                    currentAsteroid = hit.collider.transform.GetComponentInParent<Asteroid>();

                    // if haven't already selected asteroid & haven't selected max, add it to list
                    if (currentAsteroid != null && !currentAsteroid.selected && asteroids.Count <= maxSelectableAst)
                    {
                        asteroids.Add(currentAsteroid);

                        Debug.DrawLine(rb.position, currentAsteroid.transform.position, Color.red, 2f);

                        currentAsteroid.selected = true; 
                    }
                }

                if (hit.collider.tag == "BombTitan")
                {

                }

                if (hit.collider.tag == "GemTitan")
                {

                }
            }
        }
    }

    if (ultTimer <= 0) // duration of ult has been reached, throw kunai and exit
    {
        if (asteroids.Count > 0 && currentIndex < asteroids.Count)
        {
            if (kunaiTimer <= 0)
            {
                if (!spawnedKunai) // only need to run the code 1 time, right when we throw it
                                   // fixes bug of player getting stuck in ult when kunai hits wrong meteor/out of order
                {
                    if (asteroids[currentIndex] != null)
                    {
                        ThrowKunai(currentIndex);
                        currentIndex++;
                    }
                    else currentIndex++; // still traverse forward

                    spawnedKunai = true;
                }
            }
            else
            {
                kunaiTimer -= Time.deltaTime;
                spawnedKunai = false;
            }
        }
        else // thrown all kunai at selected asteroids, exit ult
        {
            // stop the throwing animation
            Invoke("ExitUlt", .3f); // using invoke to ensure the kunai have all hit their targets
        }
    }
}

**dashes are no longer finite**
void DashRefill(bool r1, bool r2, bool r3) // dash refills
{
    if (!refilledDash)
    {
        if (r1) // if we should be refilling dash 1
        {
            currentDashes++;
            canMove = true; // player can move when they have atleast 1 dash

            GameManager.PlaySound(GameManager.instance.dashRefills[0]);
        }

        if (r2) // if we should be refilling dash 2
        {
            currentDashes++;

            GameManager.PlaySound(GameManager.instance.dashRefills[1]);
        }

        if (r3) // if we should be refilling dash 3
        {
            currentDashes++;

            GameManager.PlaySound(GameManager.instance.dashRefills[2]);
        }

        refillTimer = refillDelay;

        GameManager.instance.AdjustDashSprites();

        refilledDash = true;
    }
}

void IdleRefill() // general refill system to refill all dashes, based on timer
{
    if (refillTimer <= 0)
    {
        if (currentDashes == 0)
        {
            DashRefill(true, false, false);
        }

        if (currentDashes == 1)
        {
            DashRefill(false, true, false);
        }

        if (currentDashes == 2)
        {
            DashRefill(false, false, true);
        }
    }
    else
    {
        refillTimer -= Time.deltaTime;

        refilledDash = false;
    }
}

public void ComboRefill() // combo refill system, adds a singular dash refill
{
    if (currentDashes == 0)
    {
        DashRefill(true, false, false);
    }
    else if (currentDashes == 1)
    {
        DashRefill(false, true, false);
    }
    else if (currentDashes == 2)
    {
        DashRefill(false, false, true);
    }
}

 inside of update
            if (Input.touchCount == fingersRequired && sheathed) // if player taps with number of req fingers to enter ult & not moving, allow player to enter ult
            {
                touch = Input.GetTouch(fingersRequired - 1);

                if (touch.phase == TouchPhase.Began) // fingers hit screen
                {
                    tapCount++;

                    if (tapCount == 1) // after one 2finger tap, start timer
                    {
                        startTimer = true;
                    }

                    if (tapTimer <= tapSpeed && tapCount == tapsRequired) // if time since finger tap is fast enough
                    {
                        if (ultCharges >= MAX_CHARGES && coolDownTimer <= 0 && !inUltimate) // if player can ultimate
                        {
                            EnterUltimate();
                            Debug.Log("entering ult.");
                        }

                        tapTimer = 0f;
                        tapCount = 0;
                        startTimer = false;
                    }

                    if (tapTimer > tapSpeed) // if timer has gone too long, didn't tap fast enough
                    {
                        //Debug.Log("took too long");

                        tapTimer = 0f;
                        tapCount = 0;
                        startTimer = false;
                    }
                }
            }
            */
