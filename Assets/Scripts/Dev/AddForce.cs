using UnityEngine;

public class AddForce : MonoBehaviour
{
#if CC_DEBUG

    private Rigidbody2D rb = default;
    public Vector2 force = Vector2.zero;
    public bool apply = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Debug.Log("Started and got rb " + rb.GetInstanceID());
    }

    void FixedUpdate()
    {
        if (apply) {
            apply = false;
            rb.AddForce(force, ForceMode2D.Impulse);
            Debug.Log("Applying force of " + force + " to rb " + rb.GetInstanceID());
        }
    }

#endif
}
