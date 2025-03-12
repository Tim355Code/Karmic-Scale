using UnityEngine;

// Basic class attached to bead game objects, when touched by player play a SFX and despawn
public class Bead : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            AudioManager.Instance.PlaySFX(SFX.BEAD, PlayType.LIMITED_MULTI);

            GameManager.Instance.AddBead();
            Destroy(gameObject);
        }
    }
}
