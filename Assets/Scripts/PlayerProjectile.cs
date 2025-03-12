using UnityEngine;
using System.Collections.Generic;

// The projectiles players shoot
public class PlayerProjectile : MonoBehaviour
{
    private Rigidbody2D _rb2d;
    private SpriteRenderer _sprite;

    [SerializeField]
    private LayerMask WallLayer;
    [SerializeField]
    private LayerMask SpectralBypass;
    [SerializeField]
    private GameObject Particles;
    [SerializeField]
    private float SineSpeed;
    [SerializeField]
    private float SineAmplitude;
    [SerializeField]
    private float ScaleSpeed;
    [SerializeField]
    private float ScaleAmplitude;

    private StatusEffect[] StatusEffects;

    private float Speed;

    private Direction CurrentDirection;
    private List<int> HitEnemies = new List<int>();

    private Vector2 StartPosition;
    private float StartScale;

    private bool FrameDelay;

    // Set proper flying direction and tint color based on status effects
    public void OnSpawn(Direction startDirection, float catchupTime, float relativeSpeed, StatusEffect[] effects)
    {
        StatusEffects = effects;
        _rb2d = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();

        Speed = GameManager.Instance.GetBulletSpeed + relativeSpeed / 2f;
        CurrentDirection = startDirection;
        switch (CurrentDirection)
        {
            case Direction.Left:
                {
                    transform.eulerAngles = new Vector3(0, 0, 180f);
                    break;
                }
            case Direction.Right:
                {
                    transform.eulerAngles = new Vector3(0, 0, 0);
                    break;
                }
            case Direction.Up:
                {
                    transform.eulerAngles = new Vector3(0, 0, 90);
                    break;
                }
            case Direction.Down:
                {
                    transform.eulerAngles = new Vector3(0, 0, 270);
                    break;
                }
        }

        transform.position += transform.right * Speed * catchupTime;
        Invoke("OnDeath", GameManager.Instance.GetBulletLifetime);

        _sprite.color = GameManager.Instance.GetTintColor(effects);
        StartPosition = transform.position;
        StartScale = GameManager.Instance.CurrentModifiers.Contains(ShotModifier.RANDOM_SIZE) ? Random.Range(0.4f, 1.1f) : 1;
        transform.localScale = new Vector3(StartScale, StartScale, 1);
    }

    // Spectral + piercing logic
    void OnTriggerEnter2D(Collider2D collision)
    {
        var damageable = collision.GetComponent<IDamageable>();
        if (damageable != null && !HitEnemies.Contains(collision.GetInstanceID()))
        {
            damageable.OnHit(GameManager.Instance.GetDamage, StatusEffects);
            if (GameManager.Instance.CurrentModifiers.Contains(ShotModifier.PIERCING))
            {
                HitEnemies.Add(collision.GetInstanceID());
                return;
            }
        }

        if (GameManager.Instance.CurrentModifiers.Contains(ShotModifier.SPECTRAL) && ((1 << collision.gameObject.layer) & SpectralBypass) != 0) return;

        if (((1 << collision.gameObject.layer) & WallLayer) != 0)
        {
            if (CurrentDirection == Direction.Left && !collision.CompareTag("LeftWall")) return;
            if (CurrentDirection == Direction.Right && !collision.CompareTag("RightWall")) return;
            if (CurrentDirection == Direction.Up && !collision.CompareTag("TopWall")) return;
            if (CurrentDirection == Direction.Down && !collision.CompareTag("BottomWall")) return;
        }

        OnDeath();
    }

    // Oscillation effect
    void FixedUpdate()
    {
        if (GameManager.Instance.CurrentModifiers.Contains(ShotModifier.OSCILLATE_SCALE))
        {
            var scale = StartScale + Mathf.Sin((Time.fixedTime +
                ((CurrentDirection == Direction.Up || CurrentDirection == Direction.Down) ? transform.position.y : transform.position.x)) * ScaleSpeed) * ScaleAmplitude * StartScale;
            transform.localScale = new Vector3(scale, scale, 1);
        }
        Move(Time.fixedDeltaTime);
    }

    // Sine movement effect, as well as moving in a direction
    void Move(float deltaTime)
    {
        var t = Time.fixedTime + ((CurrentDirection == Direction.Up || CurrentDirection == Direction.Down) ? transform.position.y : transform.position.x);
        Vector2 sineOffset = StartPosition + (GameManager.Instance.CurrentModifiers.Contains(ShotModifier.SINE_MOVEMENT) ? transform.up * Mathf.Sin(t * SineSpeed) * SineAmplitude : Vector2.zero);
        if (CurrentDirection == Direction.Up || CurrentDirection == Direction.Down)
            sineOffset.y = _rb2d.position.y;
        else
            sineOffset.x = _rb2d.position.x;

        _rb2d.MovePosition(sineOffset + (Vector2)transform.right * Speed * deltaTime);
    }

    void OnDeath()
    {
        Instantiate(Particles, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
