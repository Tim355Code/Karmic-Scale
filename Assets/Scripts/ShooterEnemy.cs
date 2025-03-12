using System.Collections;
using UnityEngine;

// Class used for all the shooting enemies
public class ShooterEnemy : Enemy
{
    private Rigidbody2D _rb2d;

    [SerializeField]
    private GameObject BulletPrefab;
    [SerializeField]
    private int FireCount;
    [SerializeField]
    private float FireRatePerCluster;
    [SerializeField]
    private float FireRate;
    [SerializeField]
    private float Speed;
    [SerializeField]
    private float Acceleration;
    [SerializeField]
    private bool RandomizeInitialDelay;
    [SerializeField]
    private SFX FireSFX;
    [SerializeField]
    private Color FireWarningColor;
    [SerializeField]
    private float WarningDuration = 0.2f;
    [SerializeField]
    private float TargetDistance;

    [SerializeField]
    private AimSettings ShootSettings;

    private bool FirstCycle;

    protected override void Start()
    {
        if (GameManager.Instance.Evil > 0)
            FireRate -= 0.13f * GameManager.Instance.Evil;

        FirstCycle = true;
        _rb2d = GetComponent<Rigidbody2D>();
        base.Start();

        StartCoroutine(FireCycle());
    }

    protected override void FixedUpdate()
    {
        var direction = ((Vector2)PlayerMovement.Instance.transform.position - _rb2d.position).normalized;

        if (CurrentEffects.ContainsKey(StatusEffect.FEAR)) direction = -direction;
        else if (((Vector2)PlayerMovement.Instance.transform.position - _rb2d.position).magnitude < TargetDistance) direction = Vector2.zero;

        _rb2d.velocity = Vector2.MoveTowards(_rb2d.velocity, direction * Speed, Acceleration * Time.fixedDeltaTime);

        base.FixedUpdate();
    }

    // Shoots, waits a bit, repeat
    IEnumerator FireCycle()
    {
        while (true)
        {
            if (FirstCycle && RandomizeInitialDelay)
            {
                FirstCycle = false;
                yield return WaitForFixedTime(Random.Range(0, FireRate));
            }
            else
                yield return WaitForFixedTime(FireRate);

            // Warning
            OverrideColor = true;
            var tintColor = _sprites[0].color;
            var interpolator = new Interpolator<Color>(WarningDuration, tintColor, (tintColor + FireWarningColor) / 2);
            while (!interpolator.HasFinished)
            {
                var color = interpolator.Update(Time.deltaTime);

                foreach (var sprite in _sprites)
                {
                    sprite.color = color;
                }
                yield return new WaitForEndOfFrame();
            }
            OverrideColor = false;

            Aimer aimer = new Aimer(ShootSettings, FireCount);
            for (int i = 0; i < FireCount; i++)
            {
                var angle = aimer.NextAngle();

                var bullet = Instantiate(BulletPrefab, transform.position, Quaternion.identity);
                bullet.GetComponent<Bullet>().OnSpawn(VectorExtensions.Vec2ToAngle((Vector2)PlayerMovement.Instance.transform.position - _rb2d.position) + angle);

                AudioManager.Instance.PlaySFX(FireSFX, PlayType.UNRESTRICTED);

                yield return WaitForFixedTime(FireRatePerCluster);
            }
        }
    }

    IEnumerator WaitForFixedTime(float duration)
    {
        var endTime = Time.fixedTime + duration;
        while (endTime > Time.fixedTime) yield return new WaitForFixedUpdate();
    }

    protected override void OnDeath()
    {
        StopAllCoroutines();
        base.OnDeath();
        Destroy(gameObject);
    }
}
