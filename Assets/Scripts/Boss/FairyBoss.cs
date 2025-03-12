using UnityEngine;
using System.Collections;

// Cobbled together class containing all the variations of the fairy boss
public class FairyBoss : Enemy
{
    private Rigidbody2D _rb2d;

    [SerializeField]
    private float MaxHorizontalSpeed;
    [SerializeField]
    private float HorizontalAcceleration;

    [SerializeField]
    private int Variant;

    [SerializeField]
    private Vector3 SpawnOffset;
    [SerializeField]
    private float VerticalAmplitude;
    [SerializeField]
    private float WaveT;
    [SerializeField]
    private float MaxX;
    [SerializeField]
    private float EnemySpawnRate = 4f;
    [SerializeField]
    private float EnemyCap = 6;
    [SerializeField]
    private Enemy EnemyPrefab;
    [SerializeField]
    private Bullet BulletPrefab;
    [SerializeField]
    private Bullet BulletPrefab2;
    [SerializeField]
    private float FireRate = 0.2f;
    [SerializeField]
    private Color WarningColor;
    [SerializeField]
    private float WarningDuration = 1f;
    [SerializeField]
    private float DeltaAngle;

    private float CurrentSpeed;
    private bool MovingLeft;
    private float SpawnTime;

    protected override void Start()
    {
        StartHealth += GameMaster.Singleton.CurrentFloor * 20;
        _rb2d = GetComponent<Rigidbody2D>();

        CurrentSpeed = 0;
        MovingLeft = Random.Range(0, 2) == 1;
        transform.position += SpawnOffset;
        SpawnTime = Time.time;

        GameUI.Instance.SetBossBarValue(1f);
        AudioManager.Instance.PlayMusic(Music.BOSS);
        base.Start();

        StartCoroutine(EnemySpawner());
    }

    // Messy way of getting the sine wave movement and acceleration
    protected override void FixedUpdate()
    {
        if (transform.localPosition.x > MaxX && !MovingLeft) MovingLeft = true;
        if (transform.localPosition.x < -MaxX && MovingLeft) MovingLeft = false;

        CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, MovingLeft ? -MaxHorizontalSpeed : MaxHorizontalSpeed, Time.fixedDeltaTime * HorizontalAcceleration);

        _rb2d.position = new Vector2(_rb2d.position.x, OwnerRoom.transform.position.y + VerticalAmplitude * Mathf.Sin((Time.time - SpawnTime) * WaveT));
        _rb2d.velocity = new Vector2(CurrentSpeed, 0);

        GameUI.Instance.SetBossBarValue(Mathf.Max(0, Health / StartHealth));

        base.FixedUpdate();
    }

    protected override void OnDeath()
    {
        var bullets = FindObjectsOfType<Bullet>();
        foreach (var bullet in bullets) bullet.Remove();

        GameUI.Instance.SetBossBarValue(0);
        AudioManager.Instance.FadeOutMusic(0.2f);
        base.OnDeath();
        Destroy(gameObject);
    }

    // Despite the name this also contains all the attacks, beatifully hard coded :D
    IEnumerator EnemySpawner()
    {
        while (true)
        {
            yield return new WaitForSeconds(EnemySpawnRate);

            OverrideColor = true;
            var tintColor = _sprites[0].color;
            var interpolator = new Interpolator<Color>(WarningDuration, tintColor, (tintColor + WarningColor) / 2);
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

            if (Variant == 0)
            {
                if (Random.Range(0, 2) == 1)
                {
                    var bulletToShoot = Random.Range(5, 10);
                    for (int i = 0; i < bulletToShoot; i++)
                    {
                        AudioManager.Instance.PlaySFX(SFX.FIRE_0, PlayType.LIMITED_MULTI);
                        var bullet = Instantiate(BulletPrefab, transform.position, Quaternion.identity);
                        bullet.OnSpawn(PlayerMovement.Instance.transform.position - transform.position);
                        yield return new WaitForSeconds(FireRate);
                    }
                }
                else
                {
                    Aimer aimer = new Aimer(new AimSettings { AimAngle = 0, SpreadAngle = 360f }, 18);
                    for (int i = 0; i < 18; i++)
                    {
                        var angle = aimer.NextAngle();

                        var bullet = Instantiate(BulletPrefab, transform.position, Quaternion.identity);
                        bullet.GetComponent<Bullet>().OnSpawn(VectorExtensions.Vec2ToAngle((Vector2)PlayerMovement.Instance.transform.position - _rb2d.position) + angle);
                    }

                    AudioManager.Instance.PlaySFX(SFX.FIRE_0, PlayType.UNRESTRICTED);
                }
            }
            else if (Variant == 1)
            {
                var angle = 0f;
                for (int i = 0; i < 64; i++)
                {
                    var bullet = Instantiate(BulletPrefab, transform.position, Quaternion.identity);
                    bullet.GetComponent<Bullet>().OnSpawn(angle);

                    angle += DeltaAngle;
                    AudioManager.Instance.PlaySFX(SFX.FIRE_0, PlayType.UNRESTRICTED);
                    yield return new WaitForSeconds(FireRate);
                }

                AudioManager.Instance.PlaySFX(SFX.FIRE_0, PlayType.UNRESTRICTED);
            }
            else
            {
                int j = Random.Range(0, 2);
                Aimer aimer = new Aimer(new AimSettings { AimAngle = 0, SpreadAngle = 360f }, 18);
                for (int i = 0; i < 18; i++)
                {
                    var angle = aimer.NextAngle();

                    var bullet = Instantiate(j == 1 ? BulletPrefab : BulletPrefab2, transform.position, Quaternion.identity);
                    bullet.GetComponent<Bullet>().OnSpawn(VectorExtensions.Vec2ToAngle((Vector2)PlayerMovement.Instance.transform.position - _rb2d.position) + angle);
                }

                AudioManager.Instance.PlaySFX(SFX.FIRE_0, PlayType.UNRESTRICTED);
            }

            if (OwnerRoom.LiveEnemies.Count - 1 < EnemyCap && Random.Range(0, 1f) < 0.5f + (EnemyCap + 1f - OwnerRoom.LiveEnemies.Count) / (2f * EnemyCap))
            {
                AudioManager.Instance.PlaySFX(SFX.FIRE_1, PlayType.LIMITED_MULTI);
                var enemy = Instantiate(EnemyPrefab, transform.position, Quaternion.identity);
                OwnerRoom.LiveEnemies.Add(enemy);
            }
        }
    }
}
