using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Big big ugly messy class with tons of functions
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public delegate void PlayerDamageEvent(int newHealth);
    public static event PlayerDamageEvent OnPlayerDamage;

    public delegate void PlayerDeathEvent();
    public static event PlayerDeathEvent OnPlayerDie;

    public delegate void PlayerStatEvent(PlayerStat stat, int delta);
    public static event PlayerStatEvent OnStatChanged;

    public delegate void LockInEvent(bool evil);
    public static event LockInEvent OnPlayerLocked;

    [SerializeField]
    private float PlayerFireRate = 0.25f;

    [SerializeField]
    private PlayerStats DefaultStats;
    [SerializeField]
    private PlayerStats DefaultEasyStats;

    [SerializeField]
    public PlayerStats CurrentStats;

    public List<Item> CollectedItems;
    public HashSet<ItemEnum> CollectedItemTypes;
    public List<ShotModifier> CurrentModifiers;

    public float Beads = 0;
    public float MaxBeads = 10;

    public int PlayerHealth = 3;
    public int PlayerMaxHealth = 3;

    public int Evil = 0;
    public ProjectileType ShotType;

    public Color[] TintColors;

    [SerializeField]
    private ItemPool[] ItemPools;
    [HideInInspector]
    public float GameSpeed = 1f;
    [HideInInspector]
    public List<Bullet> ActiveBullets;

    private float BloodDamageBonus = 0;
    private bool DoubleDamage = false;

    private int MaxHealthPickups = 0;
    private float CelestialDamageBonus = 0;

    [HideInInspector]
    public bool LockedIn = false;

    public List<Enemy> Bosses;
    public Enemy FinalBossPrefab;

    [SerializeField]
    private Dialogue NeutralIntroDialogue;
    [SerializeField]
    private Dialogue EvilIntroDialogue;
    [SerializeField]
    private Dialogue GoodIntroDialogue;

    [SerializeField]
    private Dialogue NeutralEndDialogue;
    [SerializeField]
    private Dialogue EvilEndDialogue;
    [SerializeField]
    private Dialogue GoodEndDialogue;

    [SerializeField]
    private GameObject GoodEndCutsceneObject;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ActiveBullets = new List<Bullet>();
        CurrentModifiers = new List<ShotModifier>();

        // Set player stats
        CurrentStats = GameMaster.Singleton.CurrentDifficulty == 0 ? DefaultStats : DefaultEasyStats;

        if (GameMaster.Singleton.CurrentDifficulty == 0)
        {
            PlayerHealth = 3;
            PlayerMaxHealth = 3;
        }
        else
        {
            PlayerHealth = 6;
            PlayerMaxHealth = 6;
        }

        GameUI.Instance.UpdateHealthDisplay(PlayerHealth);

        Item[] items = Resources.LoadAll<Item>("Items").ToArray();
        foreach (var item in items)
        {
            ItemPools[(int)item.Pool].ItemsInPool.Add(item);
        }

        CollectedItemTypes = new();
        if (CollectedItems == null)
        {
            CollectedItems = new();
        }
        else
        {
            var copy = new List<Item>(CollectedItems);
            CollectedItems = new();
            foreach (var item in copy)
                OnItemPicked(item);
        }
    }

    // Soft prayer item behaviour
    public void OnFloorLoaded()
    {
        if (CollectedItemTypes.Contains(ItemEnum.SOFT_PRAYER))
        {
            PlayerHealth = Mathf.Min(PlayerHealth + 1, PlayerMaxHealth);
            AudioManager.Instance.PlaySFX(SFX.HEAL, PlayType.LIMITED_MULTI);
            GameUI.Instance.UpdateHealthDisplay(PlayerHealth);
            UpdateCelestialDamageBonus();
        }
    }

    public void SetGameSpeed(float speed)
    {
        GameSpeed = speed;
        Time.timeScale = speed;
        AudioManager.Instance.SetMusicPitch(speed);
    }

    public void ResetGameSpeed()
    {
        Time.timeScale = GameSpeed;
    }

    // This is a dedicated function so damage related item logic can go here
    public void DamagePlayer()
    {
        if (CollectedItemTypes.Contains(ItemEnum.DEMONS_GREED) && Beads > 0) Beads--;

        AudioManager.Instance.PlaySFX(SFX.PLAYER_HIT, PlayType.SINGLE);

        PlayerHealth--;
        if (DoubleDamage) PlayerHealth--;

        OnPlayerDamage?.Invoke(PlayerHealth);
        GameUI.Instance.UpdateHealthDisplay(PlayerHealth);

        if (PlayerHealth <= 0)
        {
            OnPlayerDie?.Invoke();
            GameMaster.Singleton.LoadMenu();
        }
        UpdateCelestialDamageBonus();

        GameMaster.Singleton.Stats.HitCount++;
    }

    // This is a dedicated function so healing related item logic can go here
    public void HealPlayer()
    {
        if (BloodDamageBonus > 0)
        {
            var prev = BloodDamageBonus;
            BloodDamageBonus -= 0.25f;
            BloodDamageBonus = Mathf.Max(0, BloodDamageBonus);

            CurrentStats.ChangeStat(PlayerStat.DAMAGE, BloodDamageBonus - prev);
            OnStatChanged?.Invoke(PlayerStat.DAMAGE, -1);
        }

        if (CollectedItemTypes.Contains(ItemEnum.HEAVENS_GIFT) && PlayerHealth == PlayerMaxHealth)
        {
            MaxHealthPickups++;
            if (MaxHealthPickups == 5)
            {
                MaxHealthPickups = 0;
                PlayerMaxHealth++;
                PlayerHealth++;
                GameUI.Instance.UpdateHealthDisplay(PlayerHealth);
                AudioManager.Instance.PlaySFX(SFX.HEALTH_UP, PlayType.LIMITED_MULTI);
            }
        }

        PlayerHealth = Mathf.Min(PlayerHealth + 1, PlayerMaxHealth);
        GameUI.Instance.UpdateHealthDisplay(PlayerHealth);

        AudioManager.Instance.PlaySFX(SFX.HEAL, PlayType.LIMITED_MULTI);
        UpdateCelestialDamageBonus();
    }

    // Gives player a bead, if they have 10 give random stat up and reset counter
    public void AddBead()
    {
        Beads++;

        while (Beads >= 10)
        {
            Beads -= 10;
            PlayerStat[] options = new PlayerStat[] {
                PlayerStat.SPEED,
                PlayerStat.DAMAGE,
                PlayerStat.FIRE_RATE,
                PlayerStat.FIRE_SPEED,
                PlayerStat.RANGE,
                PlayerStat.LUCK
            };

            var randomStat = options[Random.Range(0, options.Length)];
            int changed = CurrentStats.ChangeStat(randomStat, 0.25f);
            if (changed > 0)
                OnStatChanged?.Invoke(randomStat, 1);
            AudioManager.Instance.PlaySFX(SFX.HEALTH_UP, PlayType.LIMITED_MULTI);
        }
    }

    // Picks a random item based on luck stat, scale weight and pool
    public Item GetRandomItem(ItemPoolType pool)
    {
        float luckModifier = GetLuck / 10f;
        if (!LockedIn)
        {
            if (pool == ItemPoolType.GOOD) luckModifier -= Evil / 10f;
            else if (pool == ItemPoolType.EVIL) luckModifier += Evil / 10f;

            if (Mathf.Abs(Evil) <= 2 && pool == ItemPoolType.NEUTRAL) luckModifier += (5 - 2 * Mathf.Abs(Evil)) / 10f;
            if (Mathf.Abs(Evil) <= 2 && pool != ItemPoolType.NEUTRAL) luckModifier += (3 - Mathf.Abs(Evil)) / 20f;
        }

        System.Random random = new System.Random();
        var itemPool = ItemPools[(int)pool];

        // Define base weights for each quality level
        Dictionary<int, float> baseWeights = new Dictionary<int, float>
        {
            { 0, 4 },  // Most common
            { 1, 2 },  // Half as common as quality 1
            { 2, 1 }   // Half as common as quality 2
        };

        // Modify weights based on luck
        Dictionary<int, float> modifiedWeights = baseWeights.ToDictionary(kvp => kvp.Key, kvp =>
        {
            float weight = kvp.Value * (1 + luckModifier * (kvp.Key / 2f)); // Higher quality affected more by luck
            return Mathf.Max(weight, 0.1f); // Prevent negative or zero weights
        });

        // Create a weighted list of items
        List<(Item item, float weight)> weightedItems = itemPool.ItemsInPool.Select(item =>
            (item, modifiedWeights[item.Quality])).ToList();

        // Get the total weight
        float totalWeight = weightedItems.Sum(x => x.weight);

        // Pick a random weighted item
        float roll = (float)random.NextDouble() * totalWeight;
        float sum = 0;

        foreach (var (item, weight) in weightedItems)
        {
            sum += weight;
            if (roll <= sum)
            {
                // Remove item from pool
                itemPool.ItemsInPool.Remove(item);
                ItemPools[(int)pool] = itemPool;

                return item;
            }
        }

        Debug.LogWarning("Using fallback item!!");

        // Fallback
        return itemPool.Fallback;
    }

    // Terrible code to pick the correct items depending on chest type
    public void PickItemSequence(ChestType chest)
    {
        PlayerMovement.LockControls = true;

        Item[] options = chest switch
        {
            ChestType.ITEM_DOUBLE => (!IsEvilLocked && !IsGoodLocked) ? new Item[] { GetRandomItem(ItemPoolType.EVIL), GetRandomItem(ItemPoolType.GOOD) } :
            (IsEvilLocked ? new Item[] { GetRandomItem(ItemPoolType.EVIL) } : new Item[] { GetRandomItem(ItemPoolType.GOOD) }),
            ChestType.ITEM_NEUTRAL => new Item[] { GetRandomItem(ItemPoolType.NEUTRAL) },
            _ => throw new System.NotImplementedException("What")
        };

        GameUI.Instance.PresentOptions(options);
    }

    // Code which runs when the player has picked an item, all item logic goes here :sob:
    public void OnItemPicked(Item item)
    {
        CollectedItems.Add(item);
        CollectedItemTypes.Add(item.ItemType);
        PlayerMovement.LockControls = false;

        foreach (var stat in item.StatModifiers)
        {
            int changed;
            if (stat.MultiplyMode) changed = CurrentStats.ApplyMultiplier(stat.Stat, stat.Value);
            else changed = CurrentStats.ChangeStat(stat.Stat, stat.Value);

            if (changed != 0 && stat.Stat != PlayerStat.POISON && stat.Stat != PlayerStat.FEAR) OnStatChanged?.Invoke(stat.Stat, changed);
        }

        int prevHealth = PlayerMaxHealth;
        PlayerMaxHealth += item.HealthModifier;
        PlayerMaxHealth = Mathf.Min(PlayerMaxHealth, 12);
        if (PlayerMaxHealth != prevHealth && item.ItemType != ItemEnum.LEFTOVER_OFFERINGS)
        {
            if (PlayerMaxHealth > prevHealth)
                PlayerHealth += PlayerMaxHealth - prevHealth;
            else
                PlayerHealth = Mathf.Min(PlayerHealth, PlayerMaxHealth);
        }
        GameUI.Instance.UpdateHealthDisplay(PlayerHealth);

        // Item special behaviours
        if (item.ItemType == ItemEnum.BLOOD_CONTRACT)
        {
            BloodDamageBonus = 1.25f;
            CurrentStats.ChangeStat(PlayerStat.DAMAGE, 2);
            OnStatChanged?.Invoke(PlayerStat.DAMAGE, 1);
        }
        else if (item.ItemType == ItemEnum.DEVIL_DEAL)
            DoubleDamage = true;

        if (item.Pool == ItemPoolType.EVIL) Evil += 1 + item.Quality;
        else if (item.Pool == ItemPoolType.GOOD) Evil -= 1 + item.Quality;

        if (item.OrbitalPrefab != null) PlayerOrbitals.Instance.SpawnOrbital(item.OrbitalPrefab);

        if (item.Projectile != ProjectileType.RED_CARD) ShotType = item.Projectile;

        if (item.Modifiers != null) CurrentModifiers.AddRange(item.Modifiers);

        GameUI.Instance.UpdateScale();
        UpdateCelestialDamageBonus();

        if (!LockedIn && Mathf.Abs(Evil) >= 5)
        {
            OnPlayerLocked?.Invoke(Evil > 0);
            AudioManager.Instance.PlaySFX(SFX.SCALE_TIP, PlayType.UNRESTRICTED);
            LockedIn = true;
        }
    }

    void UpdateCelestialDamageBonus()
    {
        if (!CollectedItemTypes.Contains(ItemEnum.CELESTIAL_PACT)) return;

        var prev = CelestialDamageBonus;
        CelestialDamageBonus = PlayerHealth * 0.1f;

        if (prev != CelestialDamageBonus)
        {
            CurrentStats.ChangeStat(PlayerStat.DAMAGE, CelestialDamageBonus - prev);
            OnStatChanged?.Invoke(PlayerStat.DAMAGE, prev > CelestialDamageBonus ? -1 : 1);
        }
    }

    public float GetEffectDuration(StatusEffect effect)
    {
        return effect switch {
            StatusEffect.FEAR => 3,
            StatusEffect.POISON => 3,
            _ => 999
        };
    }

    public void AddBullet(Bullet bullet)
    {
        ActiveBullets.Add(bullet);
    }

    public void RemoveBullet(Bullet bullet)
    {
        ActiveBullets.Remove(bullet);
    }

    // Gets the color for being affected by a set of status effects
    public Color GetTintColor(StatusEffect[] statusEffects)
    {
        Color color = Color.white;

        if (statusEffects == null) return color;

        foreach (var effect in statusEffects)
        {
            color += TintColors[(int)effect];
        }
        color /= statusEffects.Length + 1;

        return color;
    }

    // Use a potion
    public void ConsumePotion(ConsumableData data)
    {
        if (data.HealthOnly)
        {
            PlayerMaxHealth += data.HealthModifier;
            GameUI.Instance.UpdateHealthDisplay(PlayerHealth);

            UpdateCelestialDamageBonus();
        }
        else
        {
            int changed = CurrentStats.ChangeStat(data.Stat, data.Modifier);
            if (changed != 0) OnStatChanged?.Invoke(data.Stat, changed);
        }

        GameUI.Instance.ShowAnnouncement(data.GetDisplayName(false));
    }

    // Pick a random boss for the floor
    public Enemy GetRandomBoss()
    {
        var index = Random.Range(0, Bosses.Count);
        var boss = Bosses[index];
        Bosses.RemoveAt(index);
        return boss;
    }

    public void EndingCutscene()
    {
        StartCoroutine(BossEndingCutscene());
    }

    // Hard coded ending scene
    IEnumerator BossEndingCutscene()
    {
        AudioManager.Instance.FadeOutMusic(2f);
        yield return new WaitForSeconds(1f);
        PlayerMovement.LockControls = true;

        PlayerMovement.Instance.ForceDirection(FinalBoss.Instance.transform.position);

        yield return new WaitForSeconds(2f);

        if (!LockedIn)
        {
            FinalBoss.Instance.StandUp();
            yield return new WaitForSeconds(2f);
        }

        if (!LockedIn) GameUI.Instance.ShowDialogue(NeutralEndDialogue);
        else if (Evil > 0) GameUI.Instance.ShowDialogue(EvilEndDialogue);
        else GameUI.Instance.ShowDialogue(GoodEndDialogue);

        yield return new WaitUntil(() => !GameUI.Instance.InDialogue);

        if (LockedIn && Evil < 0)
        {
            AudioManager.Instance.PlaySFX(SFX.BEAM, PlayType.UNRESTRICTED);
            Instantiate(GoodEndCutsceneObject, FinalBoss.Instance.transform.position, Quaternion.identity);

            GameMaster.Singleton.LoadEndScreen(true);
        }
        else
        {
            GameMaster.Singleton.LoadEndScreen(false);
        }
    }

    public void FinalBossIntro()
    {
        StartCoroutine(BossIntroCutscene());
    }

    // Hard coded final boss intro scene
    IEnumerator BossIntroCutscene()
    {
        AudioManager.Instance.FadeOutMusic(0.2f);
        PlayerMovement.LockControls = true;

        yield return new WaitForSeconds(0.33f);

        if (!LockedIn) GameUI.Instance.ShowDialogue(NeutralIntroDialogue);
        else if (Evil > 0) GameUI.Instance.ShowDialogue(EvilIntroDialogue);
        else GameUI.Instance.ShowDialogue(GoodIntroDialogue);
        AudioManager.Instance.PlayMusic(Music.DIALOGUE_CREEPY);

        PlayerMovement.Instance.ForceDirection(RoomManager.Instance.GetCurrentRoom.transform.position);

        yield return new WaitUntil(() => !GameUI.Instance.InDialogue);

        AudioManager.Instance.FadeOutMusic();

        yield return new WaitForSeconds(1f);

        PlayerAnimation.Instance.SetTransformed();

        yield return new WaitForSeconds(1f);
        if (LockedIn) yield return new WaitForSeconds(1f);

        PlayerMovement.LockControls = false;
        AudioManager.Instance.PlayMusic(Music.FINAL_BOSS);
        PlayerMovement.Instance.StopForceDirection();

        FinalBoss.Instance.OnCutsceneEnd();
    }

    public bool IsEvilLocked => LockedIn && Evil > 0;
    public bool IsGoodLocked => LockedIn && Evil < 0;

    public float GetSpeed => CurrentStats.Speed;
    public float GetFireRate => PlayerFireRate - CurrentStats.FireRate / 30f;
    public float GetBulletSpeed => CurrentStats.BaseBulletSpeed;
    public float GetBulletLifetime => CurrentStats.BaseBulletRange / GetBulletSpeed;
    public float GetDamage => CurrentStats.Damage;
    public int GetLuck => CurrentStats.Luck;
    public float GetPoisonChance => CurrentStats.PoisonChance * (1 + CurrentStats.BaseLuck / 24f);
    public float GetFearChance => CurrentStats.FearChance * (1 + CurrentStats.BaseLuck / 24f);
}

// Contains player stats and multipliers
[System.Serializable]
public struct PlayerStats
{
    public float BaseSpeed;
    public float BaseDamage;
    public int BaseLuck;
    public float BaseFireRate;
    public float BaseBulletSpeed;
    public float BaseBulletRange;

    public float DamageMultiplier;
    public float FireRateMultipler;

    public float PoisonChance;
    public float FearChance;

    public float Speed => BaseSpeed;
    public float Damage => BaseDamage * DamageMultiplier;
    public int Luck => BaseLuck;
    public float FireRate => BaseFireRate * FireRateMultipler;

    public int ChangeStat(PlayerStat stat, float amount)
    {
        switch (stat)
        {
            case PlayerStat.SPEED:
                {
                    var prev = BaseSpeed;
                    BaseSpeed += amount;
                    BaseSpeed = Mathf.Clamp(BaseSpeed, 1f, 14);
                    return Mathf.Approximately(BaseSpeed, prev) ? 0 : (BaseSpeed > prev ? 1 : -1);
                }
            case PlayerStat.DAMAGE:
                {
                    var prev = BaseDamage;
                    BaseDamage += amount;
                    BaseDamage = Mathf.Max(BaseDamage, 0.05f);
                    return BaseDamage > prev ? 1 : (BaseDamage < prev ? -1 : 0);
                }
            case PlayerStat.FIRE_RATE:
                {
                    var prev = BaseFireRate;
                    BaseFireRate += amount;
                    BaseFireRate = Mathf.Clamp(BaseFireRate, 0.1f, 10);
                    return Mathf.Approximately(BaseFireRate, prev) ? 0 : (BaseFireRate > prev ? 1 : -1);
                }
            case PlayerStat.LUCK:
                {
                    var prev = BaseLuck;
                    BaseLuck += Mathf.CeilToInt(amount);
                    return BaseLuck > prev ? 1 : (BaseLuck < prev ? -1 : 0);
                }
            case PlayerStat.FIRE_SPEED:
                {
                    var prev = BaseBulletSpeed;
                    BaseBulletSpeed += amount;
                    BaseBulletSpeed = Mathf.Clamp(BaseBulletSpeed, 1, 30);
                    return Mathf.Approximately(BaseBulletSpeed, prev) ? 0 : (BaseBulletSpeed > prev ? 1 : -1);
                }
            case PlayerStat.RANGE:
                {
                    var prev = BaseBulletRange;
                    BaseBulletRange += amount;
                    BaseBulletRange = Mathf.Max(BaseBulletRange, 0.1f);
                    return BaseBulletRange > prev ? 1 : (BaseBulletRange < prev ? -1 : 0);
                }
            case PlayerStat.POISON:
                {
                    PoisonChance += amount;
                    return 1;
                }
            case PlayerStat.FEAR:
                {
                    FearChance += amount;
                    return 1;
                }
        }
        return 0;
    }

    public int ApplyMultiplier(PlayerStat stat, float multiplier)
    {
        switch (stat)
        {
            case PlayerStat.DAMAGE:
                {
                    var prev = DamageMultiplier;
                    DamageMultiplier *= multiplier;
                    return Mathf.Approximately(prev, DamageMultiplier) ? 0 : (DamageMultiplier > prev ? 1 : -1);
                }
            case PlayerStat.FIRE_RATE:
                {
                    var prev = FireRateMultipler;
                    FireRateMultipler *= multiplier;
                    return Mathf.Approximately(prev, FireRateMultipler) ? 0 : (FireRateMultipler > prev ? 1 : -1);
                }
        }
        return 0;
    }
}

public enum ShotModifier
{
    HOMING,
    PIERCING,
    SPECTRAL,
    RANDOM_SIZE,
    SINE_MOVEMENT,
    OSCILLATE_SCALE
}

public enum ProjectileType
{
    RED_CARD = 0,
    NEEDLE = 1,
}

[System.Serializable]
public struct ItemPool
{
    public ItemPoolType Type;
    [HideInInspector]
    public List<Item> ItemsInPool;

    public Item Fallback;
}

public enum ItemPoolType
{
    EVIL = 0,
    GOOD = 1,
    NEUTRAL = 2
}

public enum PlayerStat
{
    SPEED = 0,
    DAMAGE = 1,
    FIRE_RATE = 2,
    FIRE_SPEED = 3,
    RANGE = 4,
    LUCK = 5,
    POISON,
    FEAR
}

public enum StatusEffect
{
    POISON = 0,
    FEAR = 1,
    WITHER = 2
}
