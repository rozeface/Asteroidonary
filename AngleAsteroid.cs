using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class AngleAsteroid : MonoBehaviour
{
    // player must slash this asteroid at a proper angle AND hit it at a proper spot or else it will not break (betweem left crit and right crit)
    // future note: could have made this a sub class of asteroid in orer to keep same variables like horizontalSpeed etc for easier adjustments and less code duplication

    private Rigidbody2D rb = null;

    [SerializeField] List<GameObject> critPoints = new List<GameObject>(); // bottom of range in element 0, middle of range in element 1, top of range in element 2
    [SerializeField] int pointReward = 0;

    Animator animator = null;

    // bottom and top of the directional range in which player needs to come in at
    private Vector3 leftDir = Vector3.zero;
    private Vector3 rightDir = Vector3.zero;

    // direction player DID come in at
    private Vector3 playerDir = Vector3.zero;
    private PlayerMovement player = null;
    bool hit = false; // whether player has struck a good hit
    bool exploding = false;

    [SerializeField] float startHorizSpeed = 0f; // base speed when not slashing
    private float currentSpeed = 0f;
    [SerializeField] float startRotateSpeed = 0f; // base rotate speed when not slashing
    private float rotateSpeed = 0f;
    private bool adjusted = false;

    [SerializeField] float hitPauseDuration = .2f; // how long to pause player as they hit at wrong angle
    private bool hitPause = false;
    [SerializeField] float stunDuration = 0f; // how long until player can control themselves again

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        currentSpeed = startHorizSpeed; // default value
        rotateSpeed = startRotateSpeed; // default

        if (GameObject.FindWithTag("Player"))
        {
            player = PlayerMovement.instance;
        }

        animator = transform.Find("AngleSprite").GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        if (!exploding)
        {
            rb.velocity = -Vector2.right * currentSpeed;
            transform.Rotate(transform.forward * rotateSpeed * Time.fixedDeltaTime);
        }

        if (hitPause)
        {
            player.HitPause(hitPauseDuration);
            player.ReceiveStun(stunDuration);
            hitPause = false;
        }
    }

    private void Update()
    {
        leftDir = (critPoints[0].transform.position - critPoints[1].transform.position).normalized; // left vector/direction (bottom of range)
        rightDir = (critPoints[2].transform.position - critPoints[1].transform.position).normalized; // right vector/direction (top of range)

        SlowMo();

        if (hit)
        {
            if (player.sheathed) // if player has hit this asteroid, wait for sheathe and explode
            {
                exploding = true;
                rb.velocity = Vector2.zero;
                animator.SetTrigger("Destroy");
                Destroy(gameObject, 3f);
            }
        }
    }

    void SlowMo()
    {
        if (player != null)
        {
            if (SlowMoController.SlowMoCheck()) // should slowmo
            {
                if (!adjusted)
                {
                    currentSpeed /= GameManager.regularSlowMo;
                    rotateSpeed /= GameManager.regularSlowMo;
                    adjusted = true;
                }


                // slowly return speed back to normal
                if (currentSpeed <= startHorizSpeed)
                {
                    currentSpeed += player.speedIncrement * Time.deltaTime;
                }
                if (rotateSpeed <= startRotateSpeed)
                {
                    rotateSpeed += (player.speedIncrement/ 2f) * Time.deltaTime;
                }
            }
            else // shouldnt slowmo
            {
                if (adjusted)
                {
                    currentSpeed = startHorizSpeed;
                    rotateSpeed = startRotateSpeed;
                    adjusted = false;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null && !hit)
        {
            if (collision.tag == "Player" && collision.GetComponent<PlayerMovement>().attacking)
            {
                playerDir = player.dragDir.normalized; // store angle/direction player came in at

                float dist = DistanceLineSegmentPoint(leftDir, rightDir, playerDir);
                //Debug.Log(dist);

                if (dist >= 1.80f || dist <= .20f) // player slash is definitely proper/in between the crit zone (or very close)
                {
                    player.currentCombo += 1;
                    SoundPlayer.PlayAstSlash();
                    RealTimeScoreDisplay.instance.UpdatePendingScore(pointReward * player.multiplier);
                    animator.SetTrigger("Hit");
                    hit = true;
                }
                else // not good angle
                {
                    player.rb.velocity = Vector2.zero;
                    hitPause = true;
                }
            }

            if (collision.tag == "DamageZone")
            {
                BarrierDestroyTasks();
            }
        }
    }

    // Distance to point (p) from line segment (end points a b)
    float DistanceLineSegmentPoint(Vector3 a, Vector3 b, Vector3 p) // a distance of ~2 has proven to be indicative of a "perfect" asteroid slice
    {
        // If a == b line segment is a point and will cause a divide by zero in the line segment test.
        // Instead return distance from a
        if (a == b)
            return Vector3.Distance(a, p);

        // Line segment to point distance equation
        Vector3 ba = b - a;
        Vector3 pa = a - p;
        return (pa - ba * (Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba))).magnitude;
    }


    void BarrierDestroyTasks()
    {
        animator.SetTrigger("Destroy");
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        rotateSpeed = 0f;
        Destroy(gameObject, 2f);
    }
}
