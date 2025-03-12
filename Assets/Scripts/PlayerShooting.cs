using System.Collections.Generic;
using UnityEngine;

// Handles spawning player projectiles
public class PlayerShooting : MonoBehaviour
{
    private PlayerMovement _player;
    private Rigidbody2D _rb2d;

    private Vector2 PreviousPosition;
    private float NextFireTime = 0f;

    [SerializeField]
    private GameObject[] BulletPrefabs;

    private void Start()
    {
        _player = GetComponent<PlayerMovement>();
        _rb2d = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        RoomManager.OnRoomSwitch += OnRoomSwitch;
    }

    void OnDisable()
    {
        RoomManager.OnRoomSwitch -= OnRoomSwitch;
    }

    void FixedUpdate()
    {
        var fireInput = CustomInput.FireInput;
        if (PlayerMovement.LockControls) fireInput = Vector2.zero;

        // Shooting
        if (fireInput.magnitude > 0.01f)
        {
            while (NextFireTime <= Time.fixedTime)
            {
                // Use the right variant depending on which items have been collected
                var projectile = Instantiate(BulletPrefabs[(int)GameManager.Instance.ShotType], transform.position, Quaternion.identity);
                var playerProj = projectile.GetComponent<PlayerProjectile>();

                projectile.GetComponent<SpriteRenderer>().sortingOrder = _player.FacingDirection == Direction.Up ? 10 : 15;

                var actualVelocity = (_rb2d.position - PreviousPosition) / Time.fixedDeltaTime;

                float relativeSpeed = _player.FacingDirection switch
                {
                    Direction.Up => actualVelocity.y,
                    Direction.Down => -actualVelocity.y,
                    Direction.Left => -actualVelocity.x,
                    Direction.Right => actualVelocity.x,
                    _ => 0
                };

                AudioManager.Instance.PlaySFX(SFX.SHOOT, PlayType.LIMITED_MULTI, 0.33f);

                // Apply effects if neccessary
                var effects = new List<StatusEffect>();

                if (Random.Range(0, 1f) < GameManager.Instance.GetPoisonChance) effects.Add(StatusEffect.POISON);
                if (Random.Range(0, 1f) < GameManager.Instance.GetFearChance) effects.Add(StatusEffect.FEAR);

                playerProj.OnSpawn(_player.FacingDirection, Time.fixedTime - NextFireTime, relativeSpeed, effects.ToArray());

                NextFireTime += GameManager.Instance.GetFireRate;
            }
        }
        else
            NextFireTime = Mathf.Max(Time.fixedTime, NextFireTime);

        PreviousPosition = _rb2d.position;
    }

    void OnRoomSwitch(Vector2Int prevRoom, Vector2Int newRoom)
    {
        PreviousPosition = transform.position;
        _rb2d.position = transform.position;
    }
}
