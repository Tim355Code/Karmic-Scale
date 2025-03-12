using UnityEngine;

// Basic class to keep track of when player should and shouldn't be able to take damage
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    private bool Invincible;

    public float InvulnerabilityDuration = 1f;

    void Awake()
    {
        Instance = this;
    }

    public void Damage()
    {
        if (Invincible) return;

        GameManager.Instance.DamagePlayer();
        Invincible = true;

        if (GameManager.Instance.PlayerHealth > 0) Invoke(nameof(ResetInvulnerability), InvulnerabilityDuration);
    }

    void ResetInvulnerability()
    {
        Invincible = false;
    }
}
