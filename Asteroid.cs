using TMPro;
using UnityEngine;

public class Asteroid : MonoBehaviour
{
    Rigidbody2D rb = null;

    // script serves as the base/foundation for all asteroids
    private float horizontalSpeed = 0f;
    [SerializeField] float maxHorizontalSpeed = 0f;
    [SerializeField] float minHorizontalSpeed = 0f;

    private float rotateSpeed = 0f;
    [SerializeField] float maxRotateSpeed = 150f;
    [SerializeField] float minRotateSpeed = 60f;

    private float chanceVertical = 0; // chance of asteroid having verticality to feel more natural
    private float verticalSpeed = 0f; // randomized in Start()
    private int upOrDown = 0;

    private PlayerMovement player = null;
    [HideInInspector] public bool slashed = false; // if the asteroid has been slashed by player
    [HideInInspector] public bool exploding = false; // keeps it from damaging while playing explosion animation
    const int baseReward = 10; // how many pts each asteroid awards at base

    private GameObject spriteObj = null;
    [HideInInspector] public SpriteRenderer sr = null;
    private Animator anim = null;

    private bool happenedYet = false;
    private bool didTasks = false;
    private bool addedPoints = false;

    private float startSpeed = 0;
    private float startRot = 0;
    private float startVert = 0;

    //[HideInInspector] public bool selected = false; // whether selected in player ultimate

    private GameObject kunaiHit; // to prevent multiple kunai from blowing up on same asteroid
    private bool adjusted = false;

    private void Awake()
    {
        spriteObj = transform.Find("Sprite").gameObject;
        anim = spriteObj.GetComponent<Animator>();

        if (GameObject.FindWithTag("Player"))
        {
            player = PlayerMovement.instance;
        }

        sr = transform.GetComponentInChildren<SpriteRenderer>();

        rb = this.GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // random speeds for more natural feeling
        horizontalSpeed = Random.Range(minHorizontalSpeed, maxHorizontalSpeed);
        rotateSpeed = Random.Range(minRotateSpeed, maxRotateSpeed);


        float chance = .50f; // 50-50 chance for the asteroid to rotate clockwise vs counter clockwise
        int dir; // left or right

        if (Random.value <= chance)
        {
            dir = 1;
        }
        else
        {
            dir = -1;
        }

        rotateSpeed *= dir;

        // verticality
        chanceVertical = Random.value;
        upOrDown = Random.Range(0, 3);

        verticalSpeed = Random.Range(20, 60); 
         // randomized vertical speed, sometimes stays on screen/is a threat, sometimes not a threat

        startSpeed = horizontalSpeed;
        startRot = rotateSpeed;
        startVert = verticalSpeed;

        //selected = false;
    }

    private void FixedUpdate()
    {
        if (!exploding)
        {
            rb.velocity = -transform.right * horizontalSpeed;
            spriteObj.transform.Rotate(transform.forward * rotateSpeed * Time.fixedDeltaTime); // rotate the sprite over time
        }

        if (chanceVertical <= .2)
        {
            //private bool haveLogged; // debugging

            if (upOrDown == 1)
            {
                rb.velocity = (-transform.right * horizontalSpeed) + (transform.up * verticalSpeed);
            }
            else
            {
                rb.velocity = (-transform.right * horizontalSpeed) + (-transform.up * verticalSpeed);
            }
        }

        if (player != null)
        {
            if (slashed && player.sheathed) // wait until player sheathes to destroy
            {
                if (!didTasks) // performance purposes since in update, only want to call this code one time
                {
                    PlayerDestroyTasks();

                    didTasks = true;
                }
                Destroy(gameObject, 3f);
            }
        }
    }

    private void Update()
    {
        SlowMo();
    }

    void SlowMo()
    {
        if (player != null)
        {
            if (SlowMoController.SlowMoCheck()) // should slowmo
            {
                if (!adjusted)
                {
                    horizontalSpeed /= GameManager.regularSlowMo;
                    verticalSpeed /= GameManager.regularSlowMo;
                    rotateSpeed /= GameManager.regularSlowMo / 2f;
                    adjusted = true;
                }

                // slowly return speed back to normal
                if (horizontalSpeed <= startSpeed)
                {
                    horizontalSpeed += player.speedIncrement * Time.deltaTime;
                }
                if (verticalSpeed <= startVert)
                {
                    verticalSpeed += player.speedIncrement * Time.deltaTime;
                }
                if (rotateSpeed <= startRot)
                {
                    rotateSpeed += (player.speedIncrement / 2f) * Time.deltaTime;
                }
            }
            else // shouldnt slowmo
            {
                if (adjusted)
                {
                    horizontalSpeed = startSpeed;
                    verticalSpeed = startVert;
                    rotateSpeed = startRot;
                    adjusted = false;
                }
            }
        }
    }

    void PlayerDestroyTasks()
    {
        /*
        if (player.ultCharges < PlayerMovement.MAX_CHARGES)
        {
            player.ultCharges++;

            if (player.ultCharges == PlayerMovement.MAX_CHARGES)
            {
                player.sr.color = new Color(0, 255, 0); // turn player green to indicate readiness to ult
            }
        }
        */

        anim.SetTrigger("Destroy");
        rb.constraints = RigidbodyConstraints2D.FreezeAll; // stop movement for explosion
        exploding = true;
    }

    void BarrierDestroyTasks()
    {
        anim.SetTrigger("Destroy");
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        rotateSpeed = 0f;
        Destroy(gameObject, 2f);
    }

    public void FirstSlashTasks() // tasks to perform when first slashed
    {
        if (!happenedYet) // so that combo isn't added to too many times through trigger enter/exit (anything we want to only happen once)
        {
            slashed = true;
            SoundPlayer.PlayAstSlash();
            anim.SetTrigger("Slashed");
            player.SpawnSlashParticles(transform.position, Vector3.one);
            player.currentCombo++;
            SoundPlayer.instance.aSource.pitch += .075f * Time.deltaTime;
            RealTimeScoreDisplay.instance.UpdatePendingScore(baseReward * player.multiplier);

            happenedYet = true;
        }
    }

    void CollisionChecks(Collider2D collision)
    {
        if (collision != null)
        {
            if (collision.tag == "Player" &&  collision.GetComponent<PlayerMovement>().attacking) // if player is colliding while attacking
            {
                FirstSlashTasks();
                // animate Asteroid slightly cracking here
            }

            if (transform.tag != "Asteroid" && collision.tag == transform.tag) // the kunai that is meant to destroy this asteroid, same tags assigned in playermovement script
            {
                if (kunaiHit == null)
                {
                    kunaiHit = collision.gameObject; // only adds 1 kunai to be destroyed
                }

                Destroy(kunaiHit);
                GameManager.instance.playerScore++;

                Destroy(gameObject); //do animations for Asteroid breaking apart etc here
            }

            if (collision.tag == "DamageZone")
            {
                BarrierDestroyTasks();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) // primary collision checking
    {
        CollisionChecks(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)  // secondary collision checking in case player is in asteroid when start attack
    {
        CollisionChecks(collision);
    }
}