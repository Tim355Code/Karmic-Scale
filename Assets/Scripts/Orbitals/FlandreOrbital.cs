using UnityEngine;

public class FlandreOrbital : Orbital
{
    [SerializeField]
    private GameObject ExplosionPrefab;

    [SerializeField]
    private Vector2 WaitDurationRange;
    [SerializeField]
    private Vector2 RadiusRange;
    [SerializeField]
    private Vector2 SpeedRange;
    [SerializeField]
    private float RadiusChangeSpeed;
    [SerializeField]
    private LayerMask ExplosionMask;
    [SerializeField]
    private float RespawnChance = 0.33f;

    private float TargetRadius;
    private float SwitchTime;

    private bool IsActive = true;

    // Random change radius of rotation, speed and direction
    protected override void Update()
    {
        if (!IsActive)
            return;

        if (SwitchTime <= Time.time)
        {
            SwitchTime = Time.time + Random.Range(WaitDurationRange.x, WaitDurationRange.y);
            TargetRadius = Random.Range(RadiusRange.x, RadiusRange.y);
            RotationSpeed = Random.Range(SpeedRange.x, SpeedRange.y) * (Random.Range(0, 2) == 0 ? 1 : -1);
        }

        Radius = Mathf.MoveTowards(Radius, TargetRadius, Time.deltaTime * TargetRadius);

        base.Update();
    }

    void Start()
    {
        RoomManager.OnRoomSwitch += OnRoomSwitch;
    }

    void OnDestroy()
    {
        RoomManager.OnRoomSwitch -= OnRoomSwitch;
    }

    // Chance to respawn on entering uncleared rooms (or starting room due to that never being "cleared")
    void OnRoomSwitch(Vector2Int prev, Vector2Int newPos)
    {
        if (!IsActive && !FloorManager.Instance.GetRoomAtPosition(newPos).HasCleared && Random.Range(0, 1f) < RespawnChance)
        {
            gameObject.SetActive(true);
            IsActive = true;
        }
    }

    // Explodes when colliding with an enemy
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsActive) return;

        if (((1 << collision.gameObject.layer) & ExplosionMask) != 0)
        {
            AudioManager.Instance.PlaySFX(SFX.EXPLOSION, PlayType.LIMITED_MULTI);
            Instantiate(ExplosionPrefab, transform.position, Quaternion.identity);

            IsActive = false;
            gameObject.SetActive(false);
        }
    }
}
