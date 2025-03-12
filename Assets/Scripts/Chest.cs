using UnityEngine;
using System.Collections.Generic;

// Code for the chest object seen in both shops and item rooms
public class Chest : MonoBehaviour
{
    private SpriteRenderer _sprite;

    [SerializeField]
    private ChestType ChestType;
    [SerializeField]
    private Sprite[] ChestSprites;

    // Used for shop chests only
    [SerializeField]
    private bool IsShopItem;
    [SerializeField]
    private GameObject PriceTag;
    [SerializeField]
    private int Price;

    private bool HasOpened = false;

    void Start()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _sprite.sprite = ChestSprites[(int)ChestType * 2];

        // Garbage way to despawn neutral item chests after the scale has been tipped
        if (GameManager.Instance.LockedIn && ChestType == ChestType.ITEM_NEUTRAL) Destroy(gameObject);
    }

    void Update()
    {
        if (GameManager.Instance.LockedIn && ChestType == ChestType.ITEM_NEUTRAL) Destroy(gameObject);
        _sprite.sortingOrder = transform.position.y + 0.6f > PlayerMovement.Instance.transform.position.y ? 10 : 14; 
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasOpened && collision.collider.CompareTag("Player"))
        {
            // Purchasing logic
            if (IsShopItem)
            {
                if (GameManager.Instance.Beads >= Price)
                {
                    PriceTag.SetActive(false);
                    AudioManager.Instance.PlaySFX(SFX.PURCHASE, PlayType.LIMITED_MULTI);
                    GameManager.Instance.Beads -= Price;
                }
                else return;
            }

            // Give player an item if opening was successful
            GameManager.Instance.PickItemSequence(ChestType);
            _sprite.sprite = ChestSprites[(int)ChestType * 2 + 1];
            AudioManager.Instance.PlaySFX(SFX.CHEST_OPEN, PlayType.UNRESTRICTED);
            HasOpened = true;
        }
    }
}

public enum ChestType
{
    ITEM_DOUBLE = 0,
    ITEM_NEUTRAL = 1
}
