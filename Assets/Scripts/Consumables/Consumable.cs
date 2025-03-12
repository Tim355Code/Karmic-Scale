using UnityEngine;

// Potions item class
public class Consumable : MonoBehaviour
{
    private bool CanBePickedUp;
    private ConsumableData Data;

    public bool IsShopItem = false;

    [SerializeField]
    private SpriteRenderer _bottle;
    [SerializeField]
    private SpriteRenderer _liquid;

    [SerializeField]
    private Sprite[] BottleSprites;
    [SerializeField]
    private Sprite[] LiquidSprites;

    void Start()
    {
        CanBePickedUp = false;
        Invoke(nameof(ResetPickup), 0.2f);

        if (Data == null)
            SetConsumable(ConsumableManager.Instance.GetRandomConsumable());
    }

    // Set color and bottle based on what potion it is
    public void SetConsumable(ConsumableData data)
    {
        Data = data;

        int index = Data.SpriteIndex;
        _bottle.sprite = BottleSprites[index];
        _liquid.sprite = LiquidSprites[index];

        _liquid.color = Data.PotionColor;
    }

    void ResetPickup()
    {
        CanBePickedUp = true;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!CanBePickedUp) return;

        if (collision.CompareTag("Player"))
        {
            // If this potion is in a shop don't let the player get it unless they have enough beads
            if (IsShopItem)
            {
                if (GameManager.Instance.Beads >= 3)
                {
                    AudioManager.Instance.PlaySFX(SFX.PURCHASE, PlayType.LIMITED_MULTI);
                    GameManager.Instance.Beads -= 3;
                }
                else return;
            }

            ConsumableManager.Instance.PickUp(Data);
            Destroy(gameObject);
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        // Bad code making sure you dont get stuck picking things up constantly
        if (collision.CompareTag("Player"))
        {
            CanBePickedUp = true;
        }
    }
}
