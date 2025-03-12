using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handles how the player looks
public class PlayerAnimation : MonoBehaviour
{
    public static PlayerAnimation Instance;

    private PlayerMovement _movement;
    private PlayerHealth _health;
    private Animator _anim;

    [Header("References")]
    [SerializeField]
    private Sprite[] FacingSprites;
    [SerializeField]
    private Sprite[] EvilFacingSprites;
    [SerializeField]
    private Sprite[] GoodFacingSprites;
    [SerializeField]
    private SpriteRenderer _head;
    [SerializeField]
    private Color FlickerColor;
    [SerializeField]
    private float FlickerRate = 0.07f;
    [SerializeField]
    private SpriteRenderer[] SpritesToFlicker;
    [SerializeField]
    private GameObject HurtParticles;
    [SerializeField]
    private GameObject Halo;
    [SerializeField]
    private GameObject EvilHalo;

    [SerializeField]
    private GameObject EvilTransformationParticles;
    [SerializeField]
    private RuntimeAnimatorController EvilAnimator;
    [SerializeField]
    private GameObject GoodTransformationParticles;
    [SerializeField]
    private RuntimeAnimatorController GoodAnimator;

    [SerializeField]
    private bool TransformedMode;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _movement = GetComponent<PlayerMovement>();
        _anim = GetComponent<Animator>();
        _health = GetComponent<PlayerHealth>();
    }

    void OnEnable()
    {
        GameManager.OnPlayerDie += OnPlayerDeath;
        GameManager.OnPlayerDamage += OnPlayerHurt;
        GameManager.OnPlayerLocked += OnLockedIn;
    }

    void OnDisable()
    {
        GameManager.OnPlayerDie -= OnPlayerDeath;
        GameManager.OnPlayerDamage -= OnPlayerHurt;
        GameManager.OnPlayerLocked -= OnLockedIn;
    }

    // Set head and body direction independantly
    void Update()
    {
        _head.sprite = !TransformedMode ? FacingSprites[(int)_movement.FacingDirection] :
            (GameManager.Instance.Evil > 0 ? EvilFacingSprites[(int)_movement.FacingDirection] : GoodFacingSprites[(int)_movement.FacingDirection]);

        _head.sortingOrder = _movement.FacingDirection == Direction.Down ? 11 : 13;
        _head.flipX = _movement.FacingDirection == Direction.Right;

        _anim.SetFloat("Direction", (float)_movement.WalkingDirection);
    }

    // Changes reimu to look evil or good for the final boss
    public void SetTransformed()
    {
        if (GameManager.Instance.LockedIn)
        {
            if (GameManager.Instance.Evil > 0)
            {
                _anim.runtimeAnimatorController = EvilAnimator;
                AudioManager.Instance.PlaySFX(SFX.FLAME, PlayType.UNRESTRICTED);
                Instantiate(EvilTransformationParticles, transform.position, Quaternion.identity);
            }
            else
            {
                _anim.runtimeAnimatorController = GoodAnimator;
                AudioManager.Instance.PlaySFX(SFX.CHOIR, PlayType.UNRESTRICTED);
                Instantiate(GoodTransformationParticles, transform.position, Quaternion.identity);
            }
            TransformedMode = true;
        }
    }

    void OnLockedIn(bool evil)
    {
        if (!evil)
            Halo.SetActive(true);
        else
            EvilHalo.SetActive(true);
    }

    void OnPlayerHurt(int newHealth)
    {
        Instantiate(HurtParticles, transform.position, Quaternion.identity);
        if (newHealth > 0)
            StartCoroutine(PlayerFlickerCycle());
    }

    // Invicibility frames
    IEnumerator PlayerFlickerCycle()
    {
        var endTime = Time.time + _health.InvulnerabilityDuration;
        bool state = false;

        while (Time.time < endTime)
        {
            state = !state;
            foreach (var sprite in SpritesToFlicker)
            {
                sprite.color = state ? FlickerColor : Color.white;
            }

            yield return new WaitForSeconds(FlickerRate);
        }

        foreach (var sprite in SpritesToFlicker)
        {
            sprite.color = Color.white;
        }
    }

    void OnPlayerDeath()
    {
        _anim.SetTrigger("Death");
    }
}
