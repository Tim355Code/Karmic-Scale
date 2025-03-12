using UnityEngine;

public class MarisaOrbital : Orbital
{
    private float Scale = 1f;

    // Grow in scale and contact damage if you let it touch beads
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bead"))
        {
            Scale += 0.05f;
            Scale = Mathf.Min(Scale, 1.4f);

            GetComponent<DamageSource>().Damage += 0.05f;
            transform.localScale = new Vector3(Scale, Scale, Scale);

            AudioManager.Instance.PlaySFX(SFX.BEAD, PlayType.LIMITED_MULTI);
            Destroy(collision.gameObject);
        }
    }
}
