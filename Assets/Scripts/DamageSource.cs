using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Simple object which will damage enemies (and potentially player), used on the explosion caused by flandre fumo
public class DamageSource : MonoBehaviour
{
    [SerializeField]
    private float DamageCooldown = 0.2f;
    [SerializeField]
    private bool HurtPlayer;
    [SerializeField]
    private LayerMask Mask;

    [SerializeField]
    public float Damage;

    private Dictionary<int, float> Hurt = new Dictionary<int, float>();

    void FixedUpdate()
    {
        foreach (var key in Hurt.Keys.ToList())
        {
            Hurt[key] -= Time.fixedDeltaTime;
            if (Hurt[key] <= 0) Hurt.Remove(key);
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (HurtPlayer && collision.CompareTag("Player")) PlayerHealth.Instance.Damage();

        var damageable = collision.GetComponent<IDamageable>();
        if (damageable == null || Hurt.ContainsKey(damageable.GetHashCode())) return;

        if (((1 << collision.gameObject.layer) & Mask) != 0)
        {
            damageable.OnHit(Damage);
            Hurt.Add(damageable.GetHashCode(), DamageCooldown);
        }
    }
}
