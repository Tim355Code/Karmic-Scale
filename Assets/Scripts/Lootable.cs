using UnityEngine;

// Used for buckets and pots
public class Lootable : MonoBehaviour, IDamageable
{
    [SerializeField]
    private LootTable DestroyTable;
    [SerializeField]
    private float Health;
    [SerializeField]
    private GameObject DeathParticles;
    [SerializeField]
    private SFX DestroySound;

    private bool IsDead = false;

    public void OnHit(float damage, StatusEffect[] effects = null)
    {
        Health -= damage;

        if (Health <= 0 && !IsDead)
        {
            IsDead = true;
            OnDeath();
        }
    }

    // Give a random reward when destroyed
    void OnDeath()
    {
        AudioManager.Instance.PlaySFX(DestroySound, PlayType.LIMITED_MULTI, 0.5f);
        if (DeathParticles != null) Instantiate(DeathParticles, transform.position, Quaternion.identity);

        var loot = DestroyTable.Pick();
        if (loot != null)
        {
            Instantiate(loot.Value.Prefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}

[System.Serializable]
public struct LootObject
{
    public bool IsConsumable;

    public int Chance;
    public GameObject Prefab;
}

[System.Serializable]
public class LootTable
{
    public LootObject[] LootObjects;

    public LootObject? Pick()
    {
        float randomValue = Random.Range(0f, 100f);
        float cumulative = 0f;

        foreach (var loot in LootObjects)
        {
            cumulative += loot.Chance;
            if (loot.IsConsumable && GameManager.Instance.CollectedItemTypes.Contains(ItemEnum.CELESTIAL_RATIONS)) cumulative += loot.Chance;

            if (cumulative >= randomValue) return loot;
        }

        return null;
    }
}
