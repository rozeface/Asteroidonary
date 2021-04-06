using System.Collections;
using UnityEngine;

public class ShardCluster : MonoBehaviour
{
    [SerializeField] int maxHealth;
    int health;
    bool dead = false;

    Rigidbody2D rb;

    bool adjusted = false;
    float moveSpd;
    float startMoveSpd;
    [SerializeField] float maxMoveSpd;
    [SerializeField] float minMoveSpd;
    [SerializeField] GameObject clusterRotationObj;
    float rotSpd;
    float startRotSpd;
    [SerializeField] float maxRotSpd;
    [SerializeField] float minRotSpd;
    [HideInInspector] public Vector3 travelDir; // received from gemTitan based on spawn point this cluster came from

    PlayerMovement player;

    private void Start()
    {
        rb = this.GetComponent<Rigidbody2D>();
        health = maxHealth;

        moveSpd = RandomSpd(minMoveSpd, maxMoveSpd);
        startMoveSpd = moveSpd;
        rotSpd = RandomSpd(minRotSpd, maxRotSpd);
        startRotSpd = rotSpd;

        player = PlayerMovement.instance;

        foreach(Transform child in clusterRotationObj.transform)
        {
            child.GetComponent<Rigidbody2D>().isKinematic = true;
        }


        Destroy(gameObject, 20f);
    }

    private void FixedUpdate()
    {
        if (!dead)
        {
            transform.Rotate(clusterRotationObj.transform.forward * rotSpd * Time.fixedDeltaTime);
            rb.velocity = moveSpd * travelDir;
            SlowMo();
        }
        else // dead
        {
            if (transform.childCount == 0)
            {
                Destroy(gameObject);
            }
        }
    }

    float RandomSpd(float min, float max)
    {
        return Random.Range(min, max);
    }

    void SlowMo()
    {
            if (SlowMoController.SlowMoCheck()) // should slowmo
            {
                if (!adjusted)
                {
                    moveSpd /= GameManager.regularSlowMo;
                    rotSpd /= GameManager.regularSlowMo;
                    adjusted = true;
                }

                // slowly return speed back to normal
                if (moveSpd <= startMoveSpd)
                {
                    moveSpd += player.speedIncrement * 3f * Time.deltaTime;
                }
                if (rotSpd <= startRotSpd)
                {
                    rotSpd += player.speedIncrement * Time.deltaTime;
                }
            }
        else // shouldnt slowmo
        {
            if (adjusted)
            {
                moveSpd = startMoveSpd;
                rotSpd = startRotSpd;
                adjusted = false;
            }
        }
    }

    void DeathTasks()
    {
        dead = true;
        rb.velocity = Vector2.zero;
        SoundPlayer.PlayAstSlash();
        player.SpawnSlashParticles(transform.position, new Vector3(.6f, .6f, 1f));

        // shard individual scripts to take over
        foreach (Transform child in clusterRotationObj.transform)
        {
            child.GetComponent<GemShard>().enabled = true;
            StartCoroutine(DelayShardVincibility(child));
            child.GetComponent<Rigidbody2D>().isKinematic = false;
            float randScale = Random.Range(.7f, 1f);
            child.transform.localScale = new Vector3(randScale, randScale, 1f);
        }

        this.GetComponent<BoxCollider2D>().enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null && collision.tag == "Player" && player.attacking)
        {
            health -= 1;

            if (health <= 0)
            {
                DeathTasks();
            }
        }

        if (collision.tag == "DamageZone")
        {
            GameManager.Damage(1);
        }
    }

    IEnumerator DelayShardVincibility(Transform child)
    {
        yield return new WaitForSecondsRealtime(.5f);
        child.GetComponent<BoxCollider2D>().enabled = true;
        child.GetComponent<GemShard>().vincible = true;
    }
}
