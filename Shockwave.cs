using UnityEngine;
using System.Collections.Generic;

public class Shockwave : MonoBehaviour
{
    [SerializeField] List<ParticleSystem> shockwaveParticles = new List<ParticleSystem>();
    bool adjusted = false;
    float startSpeed;
    [SerializeField] float slowAmt;
    [SerializeField] float slowMoDelay = 0f; // need to let a certain amt of time pass before being able to slow shockwave down (let it grow to be atleast as big as titan)
    [SerializeField] float returnSpd = 0f; // how fast particles return to normal speed
    float lifeTime = 0f;

    private void Start()
    {
        Destroy(gameObject, 6f);
        startSpeed = shockwaveParticles[0].main.simulationSpeed;
    }

    private void Update()
    {
        lifeTime += Time.deltaTime;

        if (lifeTime >= slowMoDelay)
        {
            if (SlowMoController.SlowMoCheck())
            {
                // slow here
                var main = shockwaveParticles[0].main;
                var main2 = shockwaveParticles[1].main;

                if (!adjusted)
                {
                    main.simulationSpeed /= slowAmt;
                    main2.simulationSpeed /= slowAmt;

                    adjusted = true;
                }

                if (main.simulationSpeed <= startSpeed)
                {
                    main.simulationSpeed += returnSpd * Time.deltaTime;
                    main2.simulationSpeed += returnSpd * Time.deltaTime;
                }
            }
            else
            {
                // reg speed here
                if (adjusted)
                {
                    var main = shockwaveParticles[0].main;
                    main.simulationSpeed = startSpeed;

                    var main2 = shockwaveParticles[1].main;
                    main2.simulationSpeed = startSpeed;

                    adjusted = false;
                }
            }
        }
    }
}
