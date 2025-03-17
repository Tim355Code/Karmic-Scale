using UnityEngine;

// Extremely basic wrapper class to work with the new input system
public static class CustomInput
{
    private static CustomMap Map;

    public static Vector2 m_MoveInput;
    public static Vector2 m_FireInput;
    public static bool UsePress => Map.Default.Use.WasPerformedThisFrame();
    public static bool HoldingFocus => Map.Default.Focus.IsPressed();

    public static bool InvertY = false;

    public static Vector2 MoveInput { get => Vector2.Scale(m_MoveInput, new Vector2(1, InvertY ? -1 : 1)); }
    public static Vector2 FireInput { get => Vector2.Scale(m_FireInput, new Vector2(1, InvertY ? -1 : 1)); }

    public static void Initialize()
    {
        Map = new CustomMap();
        Map.Enable();

        Map.Default.MovementHorizontal.performed += ctx => m_MoveInput.x = ctx.ReadValue<float>();
        Map.Default.MovementHorizontal.canceled += _ => m_MoveInput.x = 0;
        Map.Default.MovementVertical.performed += ctx => m_MoveInput.y = ctx.ReadValue<float>();
        Map.Default.MovementVertical.canceled += _ => m_MoveInput.y = 0;

        Map.Default.FireHorizontal.performed += ctx => m_FireInput.x = ctx.ReadValue<float>();
        Map.Default.FireHorizontal.canceled += _ => m_FireInput.x = 0;
        Map.Default.FireVertical.performed += ctx => m_FireInput.y = ctx.ReadValue<float>();
        Map.Default.FireVertical.canceled += _ => m_FireInput.y = 0;
    }
}
