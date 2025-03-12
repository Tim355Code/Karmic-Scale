using UnityEngine;

// Makes stuff switch layers to appear behind or in front of player
public class SpriteSorter : MonoBehaviour
{
    private SpriteRenderer _sprite;

    public float Offset;

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        _sprite.sortingOrder = transform.position.y + Offset > PlayerMovement.Instance.transform.position.y ? -4 : 13;
    }
}
