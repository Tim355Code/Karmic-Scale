using UnityEngine;

public class SakuyaOrbital : Orbital
{
    [SerializeField]
    private float ProcDistance = 4f;
    [SerializeField]
    private float SlowestSpeed = 0.7f;

    // Slow the game down based on proximity to bullets
    void FixedUpdate()
    {
        var minDistance = Mathf.Infinity;

        foreach (var bullet in GameManager.Instance.ActiveBullets)
        {
            minDistance = Mathf.Min(minDistance, (transform.position - bullet.transform.position).magnitude);
        }

        if (minDistance < ProcDistance)
        {
            GameManager.Instance.SetGameSpeed(1 - SlowestSpeed * (ProcDistance - minDistance) / ProcDistance);
        }
        else
            GameManager.Instance.SetGameSpeed(1f);
    }
}
