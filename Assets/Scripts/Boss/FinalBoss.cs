using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Class for final boss marisa
public class FinalBoss : Enemy
{
    public static FinalBoss Instance;

    private Rigidbody2D _rb2d;

    [SerializeField]
    private Sprite[] FacingDirections;
    [SerializeField]
    private Sprite[] FacingPostDirections;
    [SerializeField]
    private Sprite FallenSprite;
    [SerializeField]
    private GameObject Broom;

    [SerializeField]
    private Vector2 WaitTimeRange;
    [SerializeField]
    private float MaxSpeed;
    [SerializeField]
    private float Acceleration;
    [SerializeField]
    private float ScalingHealthFactor = 100;

    [SerializeField]
    private int Attack1SpawnCount = 30;
    [SerializeField]
    private int Attack1RepeatCount = 8;
    [SerializeField]
    private float Attack1FireRate = 0.6f;

    [SerializeField]
    private int Attack2SpawnCount = 16;
    [SerializeField]
    private float Attack2FireRate = 1f;

    [SerializeField]
    private float Attack3FireRate = 1f;

    [SerializeField]
    private int Attack4SpawnCount = 16;
    [SerializeField]
    private float Attack4AccelerationMultipler = 2.5f;
    [SerializeField]
    private int Attack4RepeatCount = 8;

    [SerializeField]
    private float FinalAttackFireRate = 1f;

    [SerializeField]
    private GameObject HeartPrefab;
    [SerializeField]
    private Vector2 RoomSize = new Vector2(5f, 7f);

    [SerializeField]
    private Color WarningColor;
    [SerializeField]
    private Bullet[] BulletPrefabs;

    private Vector2 Velocity;
    private Vector2 TargetPosition;

    private Direction CurrentDirection;
    private bool LookAtPlayer = false;

    private bool HasStarted = false;
    private bool HasTarget;

    private float HeartSpawnChance = 0.1f;
    private float StartAcceleration;
    private float StartSpeed;
    private float FinalAttackStartFireRate;

    private int LastAttack = -1;
    private int SecondLastAttack = -1;
    private List<int> PossibleAttacks = new List<int> { 0, 1, 2, 3 };

    private bool IsOver = false;
    private bool IsLaying = false;

    void Awake()
    {
        Instance = this;
    }

    protected override void Start()
    {
        _rb2d = GetComponent<Rigidbody2D>();

        // Shitty boss armor calculation
        StartHealth += ScalingHealthFactor * GameManager.Instance.GetDamage * 1/(GameManager.Instance.GetFireRate);

        GameUI.Instance.SetBossBarValue(1f);
        base.Start();

        // Tell game manager to play the intro cutscene
        GameManager.Instance.FinalBossIntro();

        LookAtPlayer = true;
        TargetPosition = transform.position;
        StartAcceleration = Acceleration;
        StartSpeed = MaxSpeed;
        FinalAttackStartFireRate = FinalAttackFireRate;
    }

    IEnumerator ShowWarning(float duration = 1f)
    {
        OverrideColor = true;
        var tintColor = _sprites[0].color;
        var interpolator = new Interpolator<Color>(duration, tintColor, (tintColor + WarningColor) / 2);
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
    }

    void ChooseAttack()
    {
        if (Random.Range(0, 1f) < HeartSpawnChance)
        {
            Instantiate(HeartPrefab, transform.position, Quaternion.identity);
            HeartSpawnChance = 0f;
        }
        else HeartSpawnChance += 0.1f;

        // Choose attack while avoiding the last two attacks
        if (Health > StartHealth * 0.25f)
        {
            int attack = GetNonRepeatingAttack();

            // Update attack history
            SecondLastAttack = LastAttack;
            LastAttack = attack;

            if (attack == 0)
                StartCoroutine(Attack01());
            else if (attack == 1)
                StartCoroutine(Attack02());
            else if (attack == 2)
                StartCoroutine(Attack03());
            else if (attack == 3)
                StartCoroutine(Attack04());
        }
        else
            StartCoroutine(FinalAttack());
    }

    // Prevents the same attack from being spammed
    int GetNonRepeatingAttack()
    {
        List<int> availableAttacks = new List<int>(PossibleAttacks);
        availableAttacks.Remove(LastAttack);
        if (availableAttacks.Count > 1)
            availableAttacks.Remove(SecondLastAttack);

        return availableAttacks[Random.Range(0, availableAttacks.Count)];
    }

    // Red center attack
    IEnumerator Attack01()
    {
        yield return ShowWarning(0.5f);
        LookAtPlayer = true;

        SetTarget(Center);
        yield return new WaitUntil(() => !HasTarget);
        Velocity = Vector2.zero;

        var startAngle = 0f;

        for (int i = 0; i < Attack1RepeatCount; i++)
        {
            Aimer aimer = new Aimer(new AimSettings { AimAngle = startAngle, SpreadAngle = 360f }, Attack1SpawnCount);
            for (int j = 0; j < Attack1SpawnCount; j++)
            {
                var angle = aimer.NextAngle();

                var bullet = Instantiate(BulletPrefabs[i % 2], transform.position, Quaternion.identity);
                bullet.GetComponent<Bullet>().OnSpawn(angle);
            }
            startAngle += 360f / (2 * Attack1SpawnCount);

            AudioManager.Instance.PlaySFX(SFX.FIRE_1, PlayType.UNRESTRICTED);
            yield return new WaitForSeconds(Attack1FireRate);
        }

        OnAttackEnd();
    }

    // Purple stars attack
    IEnumerator Attack02()
    {
        yield return ShowWarning(1f);
        LookAtPlayer = false;

        var routine = StartCoroutine(Attack02ShootCycle());

        for (int i = 0; i < 2; i++)
        {
            SetTarget(GetCorner(0));
            yield return new WaitUntil(() => !HasTarget);

            SetTarget(GetCorner(1));
            yield return new WaitUntil(() => !HasTarget);

            SetTarget(GetCorner(2));
            yield return new WaitUntil(() => !HasTarget);

            SetTarget(GetCorner(3));
            yield return new WaitUntil(() => !HasTarget);

            SetTarget(GetCorner(0));
            yield return new WaitUntil(() => !HasTarget);
        }

        StopCoroutine(routine);
        OnAttackEnd();
    }
    IEnumerator Attack02ShootCycle()
    {
        while (true)
        {
            Aimer aimer = new Aimer(new AimSettings { AimAngle = Random.Range(0, 360f), SpreadAngle = 360f }, Attack2SpawnCount);
            for (int j = 0; j < Attack2SpawnCount; j++)
            {
                var angle = aimer.NextAngle();

                var bullet = Instantiate(BulletPrefabs[2], transform.position, Quaternion.identity);
                if (j % 2 == 0) bullet.RotationSpeed *= -1;
                bullet.GetComponent<Bullet>().OnSpawn(angle);
            }

            AudioManager.Instance.PlaySFX(SFX.FIRE_1, PlayType.UNRESTRICTED);

            yield return new WaitForSeconds(Attack2FireRate);
        }
    }

    // Green star attack
    IEnumerator Attack03()
    {
        yield return ShowWarning(1f);
        LookAtPlayer = false;

        Vector2 startPos;
        Vector2 endPos;

        for (int i = 0; i < 2; i++)
        {
            while (true)
            {
                int startIndex = Random.Range(0, 4);
                int endIndex = startIndex switch
                {
                    0 => 3,
                    1 => 2,
                    2 => 1,
                    _ => 0
                };
                startPos = GetCorner(startIndex);
                endPos = GetCorner(endIndex);

                if (((Vector2)transform.position - startPos).magnitude > 1f) break;
            }

            SetTarget(startPos);
            yield return new WaitUntil(() => !HasTarget);

            var coroutine = StartCoroutine(Attack03ShootCycle());
            SetTarget(endPos);
            yield return new WaitUntil(() => !HasTarget);

            SetTarget(startPos);
            yield return new WaitUntil(() => !HasTarget);

            StopCoroutine(coroutine);
        }

        OnAttackEnd();
    }

    IEnumerator Attack03ShootCycle()
    {
        yield return new WaitForSeconds(Attack3FireRate);
        while (true)
        {
            var angle = VectorExtensions.Vec2ToAngle(Velocity);

            var bullet = Instantiate(BulletPrefabs[3], transform.position, Quaternion.identity);
            bullet.GetComponent<Bullet>().OnSpawn(angle + 90f);

            bullet = Instantiate(BulletPrefabs[3], transform.position, Quaternion.identity);
            bullet.GetComponent<Bullet>().OnSpawn(angle - 90f);

            AudioManager.Instance.PlaySFX(SFX.FIRE_0, PlayType.LIMITED_MULTI);
            yield return new WaitForSeconds(Attack3FireRate);
        }
    }

    private Vector2 GetCorner(int index)
    {
        if (index == 0) return Center + RoomSize;
        else if (index == 1) return Center + Vector2.Scale(RoomSize, new Vector2(-1, 1));
        else if (index == 2) return Center + Vector2.Scale(RoomSize, new Vector2(1, -1));
        else return Center - RoomSize;
    }

    // Player chase attack
    IEnumerator Attack04()
    {
        Acceleration *= Attack4AccelerationMultipler;
        yield return ShowWarning(0.3f);
        yield return ShowWarning(0.3f);
        yield return ShowWarning(0.3f);
        yield return ShowWarning(0.3f);

        for (int i = 0; i < Attack4RepeatCount; i++)
        {
            LookAtPlayer = false;
            SetTarget(PlayerMovement.Instance.transform.position);
            yield return new WaitUntil(() => !HasTarget);

            Aimer aimer = new Aimer(new AimSettings { AimAngle = Random.Range(0, 360f), SpreadAngle = 360f }, Attack4SpawnCount);
            for (int j = 0; j < Attack4SpawnCount; j++)
            {
                var angle = aimer.NextAngle();

                var bullet = Instantiate(BulletPrefabs[4], transform.position, Quaternion.identity);
                bullet.GetComponent<Bullet>().OnSpawn(angle);
            }

            AudioManager.Instance.PlaySFX(SFX.FIRE_1, PlayType.LIMITED_MULTI);
        }

        OnAttackEnd();
    }

    // Final attack once HP is low, flies around like DVD logo shooting yellow srats
    IEnumerator FinalAttack()
    {
        _rb2d.position = new Vector2(
            Mathf.Clamp(_rb2d.position.x, Center.x - RoomSize.x, Center.x + RoomSize.x),
            Mathf.Clamp(_rb2d.position.y, Center.y - RoomSize.y, Center.y + RoomSize.y)
        );

        Vector2 moveDirection = new Vector2(Random.value > 0.5f ? 1 : -1, Random.value > 0.5f ? 1 : -1);
        StartCoroutine(FinalAttackShootCycle());
        LookAtPlayer = false;
        Acceleration = 999f;

        while (true)
        {
            // Set acceleration to scale
            float normalizedHealth = Health / (StartHealth * 0.25f);
            MaxSpeed = Mathf.Lerp(StartSpeed * 1.1f, StartSpeed * 0.75f, normalizedHealth);
            FinalAttackFireRate = Mathf.Lerp(0.75f * FinalAttackStartFireRate, FinalAttackStartFireRate, normalizedHealth);

            // Find the next collision point based on current direction
            Vector2 target = GetNextWallHit(transform.position, moveDirection);

            // Move to the target position
            SetTarget(target);
            yield return new WaitUntil(() => !HasTarget);

            // Reflect direction when hitting a wall
            moveDirection = ReflectDirection(transform.position, moveDirection);
        }
    }
    IEnumerator FinalAttackShootCycle()
    {
        yield return new WaitForSeconds(FinalAttackFireRate);
        while (true)
        {
            var angle = VectorExtensions.Vec2ToAngle(Velocity);

            var bullet = Instantiate(BulletPrefabs[5], transform.position, Quaternion.identity);
            bullet.GetComponent<Bullet>().OnSpawn(angle + 90f);

            bullet = Instantiate(BulletPrefabs[5], transform.position, Quaternion.identity);
            bullet.GetComponent<Bullet>().OnSpawn(angle - 90f);

            AudioManager.Instance.PlaySFX(SFX.FIRE_0, PlayType.LIMITED_MULTI);
            yield return new WaitForSeconds(FinalAttackFireRate);
        }
    }

    // Garbich code to determines where the next collision with the walls will occur
    Vector2 GetNextWallHit(Vector2 startPosition, Vector2 direction)
    {
        float tX = float.MaxValue, tY = float.MaxValue;

        if (direction.x > 0)
            tX = (Center.x + RoomSize.x - startPosition.x) / direction.x;
        else if (direction.x < 0)
            tX = (Center.x - RoomSize.x - startPosition.x) / direction.x;

        Vector2 hitX = startPosition + direction * Mathf.Max(0, tX); // Ensure no negative movement

        if (direction.y > 0)
            tY = (Center.y + RoomSize.y - startPosition.y) / direction.y;
        else if (direction.y < 0)
            tY = (Center.y - RoomSize.y - startPosition.y) / direction.y;

        Vector2 hitY = startPosition + direction * Mathf.Max(0, tY);

        Vector2 target = tX < tY ? hitX : hitY;

        // Clamp to room bounds
        return new Vector2(
            Mathf.Clamp(target.x, Center.x - RoomSize.x, Center.x + RoomSize.x),
            Mathf.Clamp(target.y, Center.y - RoomSize.y, Center.y + RoomSize.y)
        );
    }
    Vector2 ReflectDirection(Vector2 position, Vector2 direction)
    {
        bool hitVerticalWall = position.x - 0.1f <= Center.x - RoomSize.x || position.x + 0.1f >= Center.x + RoomSize.x;
        bool hitHorizontalWall = position.y - 0.1f <= Center.y - RoomSize.y || position.y + 0.1f >= Center.y + RoomSize.y;

        if (hitVerticalWall) direction.x = Mathf.Sign(Center.x - position.x);  // Move inward
        if (hitHorizontalWall) direction.y = Mathf.Sign(Center.y - position.y);

        return direction;
    }

    protected override void FixedUpdate()
    {
        if (LookAtPlayer) CurrentDirection = GetFacingDirection(PlayerMovement.Instance.transform.position);
        else
        {
            if (Mathf.Abs(Velocity.x) * (1.01f)  > Mathf.Abs(Velocity.y))
            {
                if (Velocity.x > 0.5f) CurrentDirection = Direction.Right;
                else if (Velocity.x < -0.5f) CurrentDirection = Direction.Left;
            }
            else
            {
                if (Velocity.y > 0.5f) CurrentDirection = Direction.Up;
                else if (Velocity.y < -0.5f) CurrentDirection = Direction.Down;
            }
        }

        if (HasStarted)
        {
            if (HasTarget)
            {
                var direction = (TargetPosition - (Vector2)transform.position).normalized;
                Velocity = Vector2.MoveTowards(Velocity, direction * MaxSpeed, Time.fixedDeltaTime * Acceleration);
                _rb2d.MovePosition(_rb2d.position + Velocity * Time.fixedDeltaTime);

                if (AtDestination)
                {
                    _rb2d.position = TargetPosition;
                    Velocity = Vector2.zero;
                    HasTarget = false;
                }
            }
        }

        GameUI.Instance.SetBossBarValue(Mathf.Max(0, Health / StartHealth));

        if (!IsLaying)
        {
            _sprites[0].sprite = IsOver ? FacingPostDirections[(int)CurrentDirection] : FacingDirections[(int)CurrentDirection];
            _sprites[0].flipX = CurrentDirection == Direction.Right;
        }
        else
        {
            _sprites[0].sprite = FallenSprite;
            _sprites[0].flipX = false;
        }

        base.FixedUpdate();
    }

    bool AtDestination => (TargetPosition - (Vector2)transform.position).magnitude < 0.3f;

    void OnAttackEnd()
    {
        Acceleration = StartAcceleration;

        LookAtPlayer = true;
        Invoke(nameof(ChooseAttack), Random.Range(WaitTimeRange.x, WaitTimeRange.y));
    }

    public Direction GetFacingDirection(Vector2 position)
    {
        Vector2 difference = position - (Vector2)transform.position;

        if (Mathf.Abs(difference.y) > Mathf.Abs(difference.x))
        {
            return (difference.y > 0) ? Direction.Up : Direction.Down;
        }
        else
        {
            return (difference.x > 0) ? Direction.Right : Direction.Left;
        }
    }

    // Used in cutscene for neutral ending
    public void StandUp()
    {
        CurrentDirection = Direction.Down;
        IsLaying = false;
        Invoke("LookAtPlayerEnable", 2f);
    }

    void LookAtPlayerEnable()
    {
        LookAtPlayer = true;
    }

    // Don't destroy this object when defeated, instead play cutscene
    protected override void OnDeath()
    {
        if (IsOver) return;

        var bullets = FindObjectsOfType<Bullet>();
        foreach (var bullet in bullets) bullet.Remove();

        IsOver = true;
        StopAllCoroutines();
        HasTarget = false;
        base.OnDeath();

        IsLaying = true;
        // Turns on the broom that is on the floor
        Broom.SetActive(true);
    }

    public void OnCutsceneEnd()
    {
        HasStarted = true;
        OnAttackEnd();
    }

    void SetTarget(Vector2 target)
    {
        HasTarget = true;
        TargetPosition = target;
    }

    private Vector2 Center => OwnerRoom.transform.position;
}
