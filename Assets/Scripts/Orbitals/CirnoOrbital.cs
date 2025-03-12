using UnityEngine;

public class CirnoOrbital : Orbital
{
    [SerializeField]
    private float SlowSpeed;

    private float DefaultSpeed;

    protected override void OnEnable()
    {
        DefaultSpeed = RotationSpeed;
        base.OnEnable();
    }

    // Slow down when the player is shooting
    protected override void Update()
    {
        RotationSpeed = CustomInput.FireInput.magnitude > 0 ? SlowSpeed : DefaultSpeed;
        base.Update();
    }
}
