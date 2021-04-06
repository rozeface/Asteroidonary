using UnityEngine;

public class BombTitan : MonoBehaviour // tracks whether all the chunks have been destroyed
{
    private void Update()
    {
        if (transform.childCount == 0) // all chunks destroyed
        {
            Destroy(gameObject);
        }
    }
}
