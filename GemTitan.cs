using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EZCameraShake;

public class GemTitan : MonoBehaviour
{
    public List<GameObject> shootPoints = new List<GameObject>(); // assign top to bottom in editor
    [SerializeField] GameObject shardClusterObj = null;

    private bool spawnedCluster = false;
    [SerializeField] float maxShootDelay;
    [SerializeField] float minShootDelay;
    float shootDelay;
    float startDelay;
    private float shotTimer;

    Rigidbody2D rb;
    public float moveSpeed; // how fast gem titan moves
    float startMoveSpeed;
    bool adjusted = false;

    PlayerMovement player;

    public int maxHealth; // how many shards must hit for gem titan to die
    public int currentHealth;
    Slider healthBar = null;

    bool dead = false;
    public Animator anim;
    bool initialized = false;
    [SerializeField] GameObject particleParent; // disable when dead

    private void Start()
    {
        rb = this.GetComponent<Rigidbody2D>();
        player = PlayerMovement.instance;

        currentHealth = maxHealth;
        anim = transform.Find("SpriteObj").GetComponent<Animator>();

        foreach (Transform child in transform.Find("ShootPoints").transform)
        {
            shootPoints.Add(child.gameObject);
        }

        healthBar = transform.Find("HealthBar").GetComponent<Slider>();
        healthBar.maxValue = maxHealth;

        startMoveSpeed = moveSpeed;

        shootDelay = RandomShootDelay();
        startDelay = shootDelay;
    }

    private void FixedUpdate()
    {
        rb.velocity = -Vector2.right * moveSpeed;

        SlowMo();
    }

    private void Update()
    {
        if (!dead)
        {
            if (!spawnedCluster)
            {
                SpawnClusterAtRandom(0, shootPoints.Count);
                shotTimer = RandomShootDelay();
                startDelay = shotTimer;
                spawnedCluster = true;
            }
            else
            {
                shotTimer -= Time.deltaTime;

                if (shotTimer <= 0)
                {
                    spawnedCluster = false;
                }
            }

            healthBar.value = currentHealth;
        }
    }

    void SpawnClusterAtRandom(int rangeMin, int rangeMax) // spawns/shoots a shard from random shoot point
    {
        int randPoint = Random.Range(rangeMin, rangeMax);

        // shard direction based on left from object's rotation
        Vector3 dir = shootPoints[randPoint].transform.TransformDirection(Vector3.left);
        GameObject cluster = Instantiate(shardClusterObj, shootPoints[randPoint].transform.position, Quaternion.identity);
        cluster.transform.SetParent(transform.parent);
        RandomSize(cluster);
        cluster.GetComponent<ShardCluster>().travelDir = dir;

        SoundPlayer.PlaySound(SoundPlayer.instance.gemTitanShoot);
    }

    void RandomSize(GameObject obj)
    {
        float randVal = Random.Range(1f, 1.2f);
        obj.transform.localScale = new Vector3(randVal, randVal, 0f);
    }

    private void CollisionChecks(Collider2D collision) // to be checked on trigger enter AND exit
    {
        if (collision != null && !dead)
        {
            if (collision.tag == "Player" && collision.GetComponent<PlayerMovement>().attacking)
            {
                // could play sound here of hitting armor (indicates player can't damage bc of armor)
                SoundPlayer.PlaySound(SoundPlayer.instance.gemTitanTing);
                player.ReceiveStun(1f);
            }

            if (collision.tag == "DamageZone") // got past player, auto death
            {
                GameManager.Damage(3);
                anim.SetTrigger("Destroy");
                SoundPlayer.PlaySound(SoundPlayer.instance.gemTitanDie);
                moveSpeed = 0f;
                this.GetComponent<CircleCollider2D>().enabled = false;
                Destroy(gameObject, 3f);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CollisionChecks(collision);
    }

    public void TakeDamage(int amt)
    {
        currentHealth -= amt;
    }

    void SlowMo()
    {
        if (SlowMoController.SlowMoCheck()) // should slowmo
        {
            if (!adjusted)
            {
                moveSpeed /= GameManager.regularSlowMo;
                shootDelay *= 2f;
                adjusted = true;
            }

            // slowly return speed back to normal
            if (moveSpeed <= startMoveSpeed)
            {
                moveSpeed += player.speedIncrement * Time.deltaTime;
            }
            if (shootDelay >= startDelay)
            {
                shootDelay -= .001f * Time.deltaTime;
            }
        }
        else // shouldnt slowmo
        {
            if (adjusted)
            {
                moveSpeed = startMoveSpeed;
                shootDelay = startDelay;
                adjusted = false;
            }
        }
    }

    float RandomShootDelay()
    {
        return Random.Range(minShootDelay, maxShootDelay);
    }

    public void DeathTasks()
    {
        if (!initialized)
        {
            dead = true;
            particleParent.SetActive(false);
            healthBar.gameObject.SetActive(false);
            anim.SetTrigger("Destroy");
            SoundPlayer.PlaySound(SoundPlayer.instance.gemTitanDie);
            CameraShaker.Instance.ShakeOnce(10f, 15f, .1f, 2.5f);
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            Destroy(gameObject, 4f);

            initialized = true;
        }
    }
}
