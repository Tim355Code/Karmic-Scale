using UnityEngine;

// Super simple class for the heart pick ups
public class HeartPickup : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GameManager.Instance.HealPlayer();
            Destroy(gameObject);
        }
    }
}
