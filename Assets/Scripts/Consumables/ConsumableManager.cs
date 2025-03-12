using UnityEngine;

// Class which handles generating the potion pool and which potion you're holding
public class ConsumableManager : MonoBehaviour
{
    public static ConsumableManager Instance;

    public ConsumableData Holding;

    [SerializeField]
    private Consumable ConsumablePrefab;
    [SerializeField]
    private int ConsumablePoolSize;
    [SerializeField]
    private Gradient PotionColors;

    private ConsumableData[] Consumables;

    void Awake()
    {
        Instance = this;
        PopulatePool();
    }

    void Update()
    {
        // If player can, let them use their potion
        if (!PlayerMovement.LockControls && CustomInput.UsePress && Holding != null)
        {
            AudioManager.Instance.PlaySFX(SFX.POTION_USE, PlayType.LIMITED_MULTI);
            GameManager.Instance.ConsumePotion(Holding);

            Holding.Discovered = true;
            Holding = null;
            GameUI.Instance.UpdatePotion(null);
        }
    }

    public void PickUp(ConsumableData data)
    {
        // If we're already holding a potion, drop it to pick up the new one
        if (Holding != null) DropCurrent();

        AudioManager.Instance.PlaySFX(SFX.POTION_PICK_UP, PlayType.LIMITED_MULTI);

        Holding = data;
        GameUI.Instance.UpdatePotion(data);
    }

    public void DropCurrent()
    {
        var potion = Instantiate(ConsumablePrefab, PlayerMovement.Instance.transform.position, Quaternion.identity);
        potion.SetConsumable(Holding);
        Holding = null;
        GameUI.Instance.UpdatePotion(null);
    }

    public ConsumableData GetRandomConsumable()
    {
        return Consumables[Random.Range(0, ConsumablePoolSize)];
    }

    // Creates a set of random potions for the run
    void PopulatePool()
    {
        Consumables = new ConsumableData[ConsumablePoolSize];
        for (int i = 0; i < ConsumablePoolSize; i++)
        {
            ConsumableData data = new ConsumableData();
            data.PotionColor = PotionColors.Evaluate(Random.Range(0, 1f));
            data.Discovered = false;
            data.SpriteIndex = Random.Range(0, 3);

            if (Random.Range(0, 1f) < 0.1f)
            {
                data.HealthModifier = Random.Range(0f, 1f) < 0.4f ? -1 : 1;
                data.HealthOnly = true;
            }
            else
            {
                data.Stat = new PlayerStat[] {
                    PlayerStat.SPEED,
                    PlayerStat.DAMAGE,
                    PlayerStat.FIRE_RATE,
                    PlayerStat.FIRE_SPEED,
                    PlayerStat.RANGE,
                    PlayerStat.LUCK
                }[Random.Range(0, 6)];
                data.Modifier = Random.Range(0.1f, 0.3f) * (Random.Range(0f, 1f) < 0.4f ? -1 : 1);

                if (data.Stat == PlayerStat.LUCK) data.Modifier = Mathf.CeilToInt(Mathf.Abs(data.Modifier)) * Mathf.Sign(data.Modifier);
                if (data.Stat == PlayerStat.DAMAGE) data.Modifier /= 2;
                if (data.Stat == PlayerStat.SPEED || data.Stat == PlayerStat.RANGE) data.Modifier *= 2;
            }

            Consumables[i] = data;
        }
    }
}

public class ConsumableData
{
    public bool Discovered;

    public int SpriteIndex;
    public Color PotionColor;
    public PlayerStat Stat;
    public float Modifier;

    public bool HealthOnly;
    public int HealthModifier;

    public string GetDisplayName(bool obscured = true)
    {
        if (obscured && !Discovered) return "???";
        else if (HealthOnly)
        {
            return "HP " + (HealthModifier > 0 ? "UP" : "DOWN");
        }
        else
        {
            string prefix = Stat switch
            {
                PlayerStat.DAMAGE => "DAMAGE",
                PlayerStat.FIRE_RATE => "FIRE",
                PlayerStat.LUCK => "LUCK",
                PlayerStat.RANGE => "RANGE",
                PlayerStat.SPEED => "SPEED",
                PlayerStat.FIRE_SPEED => "FIRE SPD",
                _ => ""
            };

            return prefix + (Modifier > 0 ? " UP" : " DOWN");
        }
    }
}

