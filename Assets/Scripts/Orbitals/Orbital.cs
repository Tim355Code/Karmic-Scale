using UnityEngine;

// Base orbital class to make things more convinient
public class Orbital : MonoBehaviour
{
    private SpriteRenderer _sprite;

    public float RotationSpeed;
    public float Radius;

    public float Rotation = 0;

    protected virtual void OnEnable()
    {
        _sprite = GetComponent<SpriteRenderer>();
    }

    protected virtual void Update()
    {
        _sprite.sortingOrder = transform.localPosition.y > 0f ? 10 : 20;
    }
}
