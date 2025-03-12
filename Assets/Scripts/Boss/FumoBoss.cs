using UnityEngine;
using System.Collections;

// Cobbled together class containing all the fumo boss variations
public class FumoBoss : Enemy
{
    // Range of attacks that can be used
    [SerializeField]
    private Vector2Int Attacks;
    [SerializeField]
    private Bullet[] BulletPrefabs;

    // Incredible variable naming I know
    [SerializeField]
    private float Attack1Duration = 5f;
    [SerializeField]
    private float Attack1FireRate;
    [SerializeField]
    private int Attack1BulletCount;
    [SerializeField]
    private int Attack1Bullet;

    [SerializeField]
    private float Attack2Duration = 7f;
    [SerializeField]
    private float Attack2TargetDuration = 1f;
    [SerializeField]
    private int Attack2Bullets = 17;
    [SerializeField]
    private float Attack2FireRate = 0.1f;

    [SerializeField]
    private float Attack3FireRate;
    [SerializeField]
    private float Attack3DeltaAngle;

    [SerializeField]
    private int Attack4RepeatCount = 8;
    [SerializeField]
    private int Attack4Bullets = 18;
    [SerializeField]
    private float Attack4FireRate = 0.2f;

    [SerializeField]
    private int Attack5RepeatCount = 8;
    [SerializeField]
    private int Attack5Bullets = 18;
    [SerializeField]
    private float Attack5FireRate = 0.2f;

    [SerializeField]
    private int Attack6RepeatCount = 8;
    [SerializeField]
    private Vector2 Attack6Speed;
    [SerializeField]
    private Vector2 Attack6FireRate;

    private bool SaidFumoLastTime = false;

    protected override void Start()
    {
        StartHealth += GameMaster.Singleton.CurrentFloor * 30;

        GameUI.Instance.SetBossBarValue(1f);
        AudioManager.Instance.PlayMusic(Music.BOSS);
        base.Start();

        Invoke(nameof(PickAttack), 2f);
    }

    protected override void FixedUpdate()
    {
        // Make the player appear behind the boss when they walk above it
        foreach (var sprite in _sprites)
        {
            sprite.sortingOrder = transform.position.y > PlayerMovement.Instance.transform.position.y ? 11 : 20;
        }
        // Update boss HP bar
        GameUI.Instance.SetBossBarValue(Mathf.Max(0, Health / StartHealth));

        base.FixedUpdate();
    }

    // Pick an attack at random, with a chance to do nothing and say "Fumo"
    void PickAttack()
    {
        int attack = Random.Range(Attacks.x, Attacks.y);

        if (!SaidFumoLastTime && Random.Range(0, 1f) < 0.2f)
        {
            SaidFumoLastTime = true;
            StartCoroutine(Attack00());
        }
        else
        {
            SaidFumoLastTime = false;
            if (attack == 1)
                StartCoroutine(Attack01());
            else if (attack == 2)
                StartCoroutine(Attack02());
            else if (attack == 3)
                StartCoroutine(Attack03());
            else if (attack == 4)
                StartCoroutine(Attack04());
            else if (attack == 5)
                StartCoroutine(Attack05());
            else if (attack == 6)
                StartCoroutine(Attack06());
        }
    }

    IEnumerator Attack00()
    {
        AudioManager.Instance.PlaySFX(SFX.FUMO, PlayType.UNRESTRICTED);
        yield return new WaitForSeconds(Random.Range(1, 3f));
        Invoke(nameof(PickAttack), 0.5f);
    }

    IEnumerator Attack01()
    {
        var endTime = Time.fixedTime + Attack1Duration;

        while (endTime > Time.fixedTime)
        {
            var aimer = new Aimer(new AimSettings { AimAngle = Random.Range(0, 360f), SpreadAngle = 360f }, Attack1BulletCount);

            for (int i = 0; i < Attack1BulletCount; i++)
            {
                var angle = aimer.NextAngle();

                var bullet = Instantiate(BulletPrefabs[Attack1Bullet], transform.position, Quaternion.identity);
                bullet.GetComponent<Bullet>().OnSpawn(angle);
                AudioManager.Instance.PlaySFX(SFX.FIRE_1, PlayType.LIMITED_MULTI);
            }

            yield return new WaitForSeconds(Attack1FireRate);
        }

        Invoke(nameof(PickAttack), 0.5f);
    }

    IEnumerator Attack02()
    {
        var endTime = Time.fixedTime + Attack2Duration;

        while (endTime > Time.fixedTime)
        {
            var aimer = new Aimer(new AimSettings { AimAngle = Random.Range(0, 360f), SpreadAngle = 360f }, Attack2Bullets);
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < Attack2Bullets; i++)
                {
                    var angle = aimer.NextAngle();

                    var bullet = Instantiate(BulletPrefabs[0], transform.position, Quaternion.identity);
                    bullet.GetComponent<Bullet>().OnSpawn(angle);
                }
                yield return new WaitForSeconds(Attack1FireRate);
                AudioManager.Instance.PlaySFX(SFX.FIRE_1, PlayType.LIMITED_MULTI);
            }

            var endTime2 = Time.fixedTime + Attack2TargetDuration;
            while (endTime2 > Time.fixedTime)
            {
                var bullet = Instantiate(BulletPrefabs[1], transform.position, Quaternion.identity);
                bullet.GetComponent<Bullet>().OnSpawn(PlayerMovement.Instance.transform.position - transform.position);

                yield return new WaitForSeconds(Attack1FireRate);
                AudioManager.Instance.PlaySFX(SFX.FIRE_0, PlayType.LIMITED_MULTI);
            }
        }

        Invoke(nameof(PickAttack), 0.5f);
    }

    IEnumerator Attack03()
    {
        var angle = Random.Range(0, 360f);
        for (int i = 0; i < 64; i++)
        {
            var bullet = Instantiate(BulletPrefabs[3], transform.position, Quaternion.identity);
            bullet.GetComponent<Bullet>().OnSpawn(angle);
            var bullet2 = Instantiate(BulletPrefabs[3], transform.position, Quaternion.identity);
            bullet2.GetComponent<Bullet>().OnSpawn(-angle);

            angle += Attack3DeltaAngle;
            AudioManager.Instance.PlaySFX(SFX.FIRE_0, PlayType.UNRESTRICTED);
            yield return new WaitForSeconds(Attack3FireRate);
        }

        Invoke(nameof(PickAttack), 0.5f);
    }

    IEnumerator Attack04()
    {
        var startAngle = 0f;
        for (int i = 0; i < Attack4RepeatCount; i++)
        {
            Aimer aimer = new Aimer(new AimSettings { AimAngle = startAngle, SpreadAngle = 360f }, Attack4Bullets);
            for (int j = 0; j < Attack4Bullets; j++)
            {
                var angle = aimer.NextAngle();

                var bullet = Instantiate(BulletPrefabs[4], transform.position, Quaternion.identity);
                bullet.GetComponent<Bullet>().OnSpawn(angle);
            }
            startAngle += 360f / (2 * Attack4Bullets);

            AudioManager.Instance.PlaySFX(SFX.FIRE_0, PlayType.UNRESTRICTED);
            yield return new WaitForSeconds(Attack4FireRate);
        }
        Invoke(nameof(PickAttack), 0.5f);
    }

    IEnumerator Attack05()
    {
        for (int i = 0; i < Attack5RepeatCount; i++)
        {
            Aimer aimer = new Aimer(new AimSettings { AimAngle = 0, SpreadAngle = 360f }, Attack5Bullets);
            for (int j = 0; j < Attack5Bullets; j++)
            {
                var angle = aimer.NextAngle();
                var bullet = Instantiate(BulletPrefabs[5], transform.position, Quaternion.identity);
                if (i % 2 == 0) bullet.RotationSpeed *= -1;
                bullet.GetComponent<Bullet>().OnSpawn(angle);
            }

            AudioManager.Instance.PlaySFX(SFX.FIRE_1, PlayType.UNRESTRICTED);
            yield return new WaitForSeconds(Attack5FireRate);
        }
        Invoke(nameof(PickAttack), 1.75f);
    }

    IEnumerator Attack06()
    {
        for (int i = 0; i < Attack6RepeatCount; i++)
        {
            var bullet = Instantiate(BulletPrefabs[6], transform.position, Quaternion.identity);
            bullet.Speed = Attack6Speed.x + (Attack6Speed.y - Attack6Speed.x) * (i / (float)(Attack6RepeatCount - 1));
            bullet.GetComponent<Bullet>().OnSpawn(VectorExtensions.Vec2ToAngle((Vector2)PlayerMovement.Instance.transform.position - (Vector2)transform.position));

            AudioManager.Instance.PlaySFX(SFX.FIRE_0, PlayType.UNRESTRICTED);

            yield return new WaitForSeconds(Mathf.Lerp(Attack6FireRate.x, Attack6FireRate.y, i / (float)(Attack6RepeatCount - 1)));
        }
        Invoke(nameof(PickAttack), 0.5f);
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
}
