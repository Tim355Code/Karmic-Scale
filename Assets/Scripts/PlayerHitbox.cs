using UnityEngine;

// For layering annoyance reasons, this sits on the hitbox object and detects bullet collisions
public class PlayerHitbox : MonoBehaviour
{
    [SerializeField]
    private PlayerHealth _health;

    [SerializeField]
    private LayerMask HurtLayer;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ouch") && ((1 << collision.gameObject.layer) & HurtLayer) != 0)
        {
            _health.Damage();
        }
    }
}
