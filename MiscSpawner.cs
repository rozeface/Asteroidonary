using System.Collections.Generic;
using UnityEngine;

public class MiscSpawner : MonoBehaviour
{
    // spawns misc objects at given points to fly across screen

    private List<GameObject> spawnPoints = new List<GameObject>(); // any child objects will be added to this list
    private GameObject currentSpawn = null;
    [SerializeField] GameObject parentBackground = null; // parent these objects to the background so that when it moves, the misc objects move with it

    [Header("Planets")]
    [SerializeField] List<Sprite> planetSprites = new List<Sprite>(); // all planet sprites to pull from (adjust in editor if develop more sprites)
    [SerializeField] GameObject planetPrefab = null; // planet prefab is one object where only the sprite is changed (not multiple planet objects)
    private GameObject currentPlanet = null;
    private Sprite currentSprite = null; // sprite we will use for the planet that is spawned
    [SerializeField] float planetMinSpawnDelay = 0f; // min time btwn spawning planets
    [SerializeField] float planetMaxSpawnDelay = 0f; // max time btwn spawning planets
    private float currentPlanetDelay = 0f; // current delay before spawning next planet
    private float timeSinceLastPlanet = 0f; // last time we spawned planet

    [Header("Misc Objects")]
    [SerializeField] List<GameObject> miscObjects = new List<GameObject>(); // all misc objects to pull from
    private GameObject currentMiscObj = null;
    [SerializeField] float miscMinSpawnDelay = 0f; // minimum time between spawning misc objects
    [SerializeField] float miscMaxSpawnDelay = 0f; // maximum time between spawning misc objects
    private float currentMiscDelay = 0f; // current time until spawning next misc object
    private float timeSinceLastMisc = 0f; // last time we spawned misc object

    private void Start()
    {
        foreach (Transform child in transform) // all starting child objects are spawn points, add them to list
        {
            spawnPoints.Add(child.gameObject);
        }

        RandomMiscDelay(0f, miscMaxSpawnDelay);
        RandomPlanetDelay(0f, planetMaxSpawnDelay);
    }

    private void Update()
    {
        if (!GameManager.instance.gameOver)
        {
            // misc spawning
            if (timeSinceLastMisc >= currentMiscDelay) // if enough time has passed, spawn misc
            {
                Debug.Log("spawning misc object now");
                SpawnMisc();

                timeSinceLastMisc = 0f; // reset this as we have now spawned
                RandomMiscDelay(miscMinSpawnDelay, miscMaxSpawnDelay); // pick a random delay for next spawn
            }
            else // add time since last spawned
            {
                timeSinceLastMisc += Time.deltaTime;
            }

            // planet spawning
            if (timeSinceLastPlanet >= currentPlanetDelay) // enough time has passed, spawn planet
            {
                Debug.Log("Spawning planet now");
                SpawnPlanet();

                timeSinceLastPlanet = 0f;
                RandomPlanetDelay(planetMinSpawnDelay, planetMaxSpawnDelay);
            }
            else // still tracking time until hit delay amount
            {
                timeSinceLastPlanet += Time.deltaTime;
            }
        }
    }

    void RandomSpawn()  // grabs rand spawn pt
    {
        currentSpawn = spawnPoints[Random.Range(0, spawnPoints.Count)]; // gets a random spawn point
    }

    Vector3 LaunchDir() // returns a direction left of the current object
    {
        return currentSpawn.transform.TransformDirection(Vector3.left);
    }

    void SpawnPlanet()
    {
        RandomSpawn();

        if (planetSprites.Count > 0)
        {
            currentSprite = planetSprites[Random.Range(0, planetSprites.Count)]; // gets a random sprite from sprite list
            currentPlanet = Instantiate(planetPrefab, currentSpawn.transform.position, Quaternion.identity); // spawn the planet prefab
            currentPlanet.transform.SetParent(parentBackground.transform);
            currentPlanet.GetComponent<SpriteRenderer>().sprite = currentSprite; // change the planet sprite to the random one from above
            currentPlanet.transform.localScale = Vector3.one; // fixes weird random scaling
            currentPlanet.GetComponent<Planet>().travelDir = LaunchDir();
        }
        else Debug.Log("There are no planet sprites assigned to the misc spawner. Need to add.");
    }

    void SpawnMisc() // spawns a misc object
    {
        RandomSpawn();

        if (miscObjects.Count > 0) // if there are misc objects to spawn
        {
            currentMiscObj = Instantiate(miscObjects[Random.Range(0, miscObjects.Count)], currentSpawn.transform.position, Quaternion.identity); // spawns a rand misc obj
            currentMiscObj.transform.SetParent(parentBackground.transform);
            currentMiscObj.transform.localScale = Vector3.one;
        }
        else Debug.Log("There are no misc objects assigned to the misc spawner. Need to add.");
    }

    void RandomMiscDelay(float min, float max) // grabs a random duration to wait for next misc spawn
    {
        currentMiscDelay = Random.Range(min, max);
    }

    void RandomPlanetDelay(float min, float max) // grabs a random duration to wait for next planet spawn
    {
        currentPlanetDelay = Random.Range(min, max);
    }
}
