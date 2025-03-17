using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Bastract class used on all bosses and enemies, defines some basic taking damage logic and being afflicted with status effects
public abstract class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField]
    protected float StartHealth;
    [SerializeField]
    private SFX DeathSFX;
    [SerializeField]
    private SFX HurtSFX;
    [SerializeField]
    private ParticleSystem HitParticles;
    [SerializeField]
    private GameObject DeathParticles;

    protected float Health;
    [HideInInspector]
    public Room OwnerRoom;
    [SerializeField]
    protected SpriteRenderer[] _sprites;

    protected bool OverrideColor = false;

    protected Dictionary<StatusEffect, float> CurrentEffects;

    protected virtual void Start()
    {
        CurrentEffects = new Dictionary<StatusEffect, float>();
        StartHealth += (GameMaster.Singleton.CurrentFloor / 3f);
        if (GameManager.Instance.Evil < 0)
            StartHealth *= (1 + 0.05f * -GameManager.Instance.Evil);

        Health = StartHealth;
    }

    public virtual void OnHit(float damage, StatusEffect[] effects = null)
    {
        if (effects != null)
        {
            foreach (var effect in effects)
            {
                AddEffect(effect);
            }
        }    

        HitParticles?.Play();
        AudioManager.Instance.PlaySFX(HurtSFX, PlayType.LIMITED_MULTI);
        Health -= damage;
        if (Health <= 0) OnDeath();
    }

    protected virtual void FixedUpdate()
    {
        if (!OverrideColor)
        {
            var tintColor = CurrentEffects.Count > 0 ? GameManager.Instance.GetTintColor(CurrentEffects.Keys.ToArray()) : Color.white;
            foreach (var sprite in _sprites)
                sprite.color = tintColor;
        }

        foreach (var effect in CurrentEffects.Keys.ToList())
        {
            CurrentEffects[effect] -= Time.fixedDeltaTime;
            if (CurrentEffects[effect] <= 0) CurrentEffects.Remove(effect);
        }

        if (CurrentEffects.ContainsKey(StatusEffect.POISON))
        {
            DealPoisonDamage();
        }
        if (CurrentEffects.ContainsKey(StatusEffect.WITHER))
        {
            if (Health <= StartHealth * 0.4f)
            {
                CurrentEffects.Remove(StatusEffect.WITHER);
                return;
            }
            DealPoisonDamage();
        }
    }

    public void AddEffect(StatusEffect effect)
    {
        if (!CurrentEffects.ContainsKey(effect))
            CurrentEffects[effect] = GameManager.Instance.GetEffectDuration(effect);
        else
            CurrentEffects[effect] += GameManager.Instance.GetEffectDuration(effect);
    }

    protected void DealPoisonDamage()
    {
        Health -= Time.fixedDeltaTime * 0.16f * GameManager.Instance.GetDamage / GameManager.Instance.GetFireRate;
        if (Health <= 0) OnDeath();
    }

    protected virtual void OnDeath()
    {
        if (DeathParticles != null) Instantiate(DeathParticles, transform.position, Quaternion.identity);
        RoomManager.Instance.GetCurrentRoom.NotifyDeath(this);
        AudioManager.Instance.PlaySFX(DeathSFX, PlayType.UNRESTRICTED);
    }
}
