using UnityEngine;

// The item SO's, contains stat modifiers, description, quality, pool it belongs to and prefabs for any orbitals they may spawn
[CreateAssetMenu(fileName = "New Item", menuName = "ScriptableObjects/Item", order = 1)]
public class Item : ScriptableObject
{
    public ItemEnum ItemType;
    [Range(0, 3)]
    public int Quality = 1;

    public ItemPoolType Pool;

    public string Name;
    [TextArea]
    public string Description;
    public string Announcement;
    public Sprite CardSprite;
    public Vector2 CardSpriteSize;

    public StatChange[] StatModifiers;
    public int HealthModifier;
    public ProjectileType Projectile;
    public ShotModifier[] Modifiers;

    public Orbital OrbitalPrefab;
    // Unused as there are none
    public GameObject FamiliarPrefab;
}

[System.Serializable]
public struct StatChange
{
    public PlayerStat Stat;
    public bool MultiplyMode;

    public float Value;
}

public enum ItemEnum
{
    RICE_BALL,
    LUNARIAN_TEA,
    SILVER_LEAF,
    ROTTEN_MEAT,
    DEMONS_GREED,
    DEMONS_BRAND,
    CRIMSON_SHACKLES,
    REIMU_FUMO,
    SAKUYA_FUMO,
    MARISA_FUMO,
    BLOOD_CONTRACT,
    DEVIL_DEAL,
    SANGUINE_FEAST,
    HEAVENS_GIFT,
    FLANDRE_FUMO,
    CELESTIAL_PACT,
    NIGHTMARE_SIGIL,
    SPIRIT_NEEDLES,
    CELESTIAL_RATIONS,
    PAPER_FAN,
    RUSTY_NAIL,
    LEFTOVER_OFFERINGS,
    SOFT_PRAYER,
    MUSHROOM_STEW,
    BALANCE_CHARM,
    MYSTERIOUS_LIQUID,
    STONE_SHELL,
    EIENTEI_HERBAL_MEDICINE,
    SHARPENED_SIGHT,
    NETHERLAND_LANTERN,
    PRISMRIVER_CRESCENDO,
    STATIC_CHARGE
}
