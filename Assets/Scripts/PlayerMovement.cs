using UnityEngine;

// Hnadles player movement, as well as setting their current walking and facing directions
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;
    public static bool LockControls;

    private Rigidbody2D _rb2d;
    private PlayerShooting _shooting;

    [Header("References")]
    [SerializeField]
    private GameObject BulletPrefab;

    [Header("Settings")]
    [SerializeField]
    private float Acceleration;

    private Vector2 CurrentSpeed;

    public Direction FacingDirection { private set; get; }
    public Direction WalkingDirection { private set; get; }

    public bool ForcingDirection;
    public Direction ForcedDirection;

    void Start()
    {
        Instance = this;
        _rb2d = GetComponent<Rigidbody2D>();
        _shooting = GetComponent<PlayerShooting>();
    }

    void OnEnable()
    {
        GameManager.OnPlayerDie += OnPlayerDeath;
        RoomManager.OnRoomSwitch += OnSwitchRoom;
    }

    void OnDisable()
    {
        GameManager.OnPlayerDie -= OnPlayerDeath;
        RoomManager.OnRoomSwitch -= OnSwitchRoom;
    }

    void FixedUpdate()
    {
        var speed = GameManager.Instance.GetSpeed;
        var targetSpeed = speed * CustomInput.MoveInput.normalized;

        if (LockControls) targetSpeed = Vector2.zero;

        CurrentSpeed = Vector2.MoveTowards(CurrentSpeed, targetSpeed, Time.fixedDeltaTime * Acceleration);
        _rb2d.velocity = CurrentSpeed;

        UpdateDirections();
    }

    void OnSwitchRoom(Vector2Int oldPosition, Vector2Int newPosition)
    {
        var delta = newPosition - oldPosition;
        var direction = RoomManager.Instance.VectorToEnum[delta];

        var room = FloorManager.Instance.GetRoomAtPosition(newPosition);

        transform.position = room.GetSpawnPosition(direction);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("LeftDoor"))
            RoomManager.Instance.SwitchRoom(Direction.Left);
        else if (collision.CompareTag("TopDoor"))
            RoomManager.Instance.SwitchRoom(Direction.Up);
        else if (collision.CompareTag("RightDoor"))
            RoomManager.Instance.SwitchRoom(Direction.Right);
        else if (collision.CompareTag("BottomDoor"))
            RoomManager.Instance.SwitchRoom(Direction.Down);
    }

    void UpdateDirections()
    {
        // Set walking direction
        if (CurrentSpeed.magnitude < 0.01f)
        {
            WalkingDirection = Direction.Down;
        }
        else if (Mathf.Abs(CurrentSpeed.x) > Mathf.Abs(CurrentSpeed.y))
        {
            if (Mathf.Sign(CurrentSpeed.x) < 0)
                WalkingDirection = Direction.Left;
            else
                WalkingDirection = Direction.Right;
        }
        else
        {
            if (Mathf.Sign(CurrentSpeed.y) < 0)
                WalkingDirection = Direction.Down;
            else
                WalkingDirection = Direction.Up;
        }

        // Set shooting direction
        var shootingInput = CustomInput.FireInput;
        if (LockControls) shootingInput = Vector2.zero;

        if (Mathf.Abs(shootingInput.x) > 0.001f && Mathf.Abs(shootingInput.x) > Mathf.Abs(shootingInput.y))
        {
            if (Mathf.Sign(shootingInput.x) < 0)
                FacingDirection = Direction.Left;
            else
                FacingDirection = Direction.Right;
        }
        else if (Mathf.Abs(shootingInput.y) > 0.001f)
        {
            if (Mathf.Sign(shootingInput.y) < 0)
                FacingDirection = Direction.Down;
            else
                FacingDirection = Direction.Up;
        }
        else
            FacingDirection = WalkingDirection;

        if (ForcingDirection)
        {
            WalkingDirection = ForcedDirection;
            FacingDirection = ForcedDirection;
        }
    }

    void OnPlayerDeath()
    {
        LockControls = true;
    }

    // Used in final boss cutscene to make reimu look at the boss
    public void ForceDirection(Direction direction)
    {
        ForcingDirection = true;
        ForcedDirection = direction;
    }

    // Same thing but you pass a positino to look at
    public void ForceDirection(Vector2 position)
    {
        ForcingDirection = true;
        ForcedDirection = GetFacingDirection(position);
    }

    public void StopForceDirection()
    {
        ForcingDirection = false;
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

    private bool IsWalking => CurrentSpeed.sqrMagnitude > 0.001f;
}
