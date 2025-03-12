using UnityEngine;

// Simplistic bullet class, used on all bullets in the game :D
public class Bullet : MonoBehaviour
{
    [SerializeField]
    protected LayerMask DeathMask;
    [SerializeField]
    public float Speed;
    [SerializeField]
    public float Acceleration;
    [SerializeField]
    public float RotationSpeed;

    protected float Rotation;

    public void OnSpawn(Vector2 direction) => OnSpawn(VectorExtensions.Vec2ToAngle(direction));

    public void OnSpawn(float angle)
    {
        Rotation = angle;
        // Register bullet
        GameManager.Instance.AddBullet(this);
    }

    protected void FixedUpdate()
    {
        Speed += Acceleration * Time.fixedDeltaTime;
        Rotation += Time.fixedDeltaTime * RotationSpeed;
        transform.Translate((VectorExtensions.AngleToVec2(Rotation) * Speed) * Time.fixedDeltaTime);
    }

    protected void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & DeathMask) != 0)
        {
            Remove();
        }
    }

    public void Remove()
    {
        // Unregister bullet
        GameManager.Instance.RemoveBullet(this);
        Destroy(gameObject);
    }
}
