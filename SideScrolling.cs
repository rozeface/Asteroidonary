using UnityEngine;

public class SideScrolling : MonoBehaviour
{
    // can be placed on an object that we want to scroll across the screen and REPEAT scrolling
    private Rigidbody2D rb;
    public float scrollSpeed; // how fast we want this object to scroll
    float startScrollSpeed;
    bool adjusted = false;

    private BoxCollider2D objCollider; // using for size reference
    private float horizontalLength;

    public bool shouldScroll = false; // whether this object should currently be scrolling

    void Start()
    {
        rb = this.GetComponent<Rigidbody2D>();

        objCollider = this.GetComponent<BoxCollider2D>();
        horizontalLength = this.objCollider.size.x;

        shouldScroll = true;

        startScrollSpeed = scrollSpeed;
    }

    private void Update()
    {
        if (this.transform.localPosition.x < -horizontalLength) // positive horiz if moving right, negative if moving left 
            //(must use localposition to get accurate info as it is a child obj)
        {
            RepositionBG();
        }

        if (SlowMoController.SlowMoCheck())
        {
            if (!adjusted)
            {
                scrollSpeed /= GameManager.regularSlowMo;
                adjusted = true;
            }

            if (scrollSpeed >= startScrollSpeed)
            {
                scrollSpeed -= PlayerMovement.instance.speedIncrement / 6f * Time.deltaTime;
            }
        }
        else
        {
            if (adjusted)
            {
                scrollSpeed = startScrollSpeed;
                adjusted = false;
            }
        }
    }

    private void FixedUpdate()
    {
        if (shouldScroll)
        {
            rb.velocity = new Vector2(scrollSpeed, 0f);
        }
        else rb.velocity = Vector2.zero;
    }

    void RepositionBG()
    {
        Vector2 offset = new Vector2(horizontalLength * 2f, 0);
        this.transform.position = (Vector2)this.transform.position + offset;
    }
}
