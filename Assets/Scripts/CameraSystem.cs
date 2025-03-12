using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The only thing this does is pause the game and scroll the camera whenever you switch room
public class CameraSystem : MonoBehaviour
{
    public static CameraSystem Instance;

    [SerializeField]
    private float SwitchDuration = 0.5f;
    [SerializeField]
    private AnimationCurve TransitionCurve;

    private void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        RoomManager.OnRoomSwitch += OnRoomSwitch;
    }

    void OnDisable()
    {
        RoomManager.OnRoomSwitch -= OnRoomSwitch;
    }

    void OnRoomSwitch(Vector2Int oldPos, Vector2Int newPos)
    {
        StartCoroutine(SwitchToNewRoom(newPos));
    }

    IEnumerator SwitchToNewRoom(Vector2Int newPos)
    {
        Vector3 oldPosition = transform.position;
        Vector3 newPosition = FloorManager.Instance.GetRoomAtPosition(newPos).transform.position + new Vector3(0, 0, transform.position.z);

        // Stop time
        Time.timeScale = 0;

        // Interpolate position
        var interpolator = new Interpolator<float>(SwitchDuration, 0, 1);
        while (!interpolator.HasFinished)
        {
            transform.position = Vector3.Lerp(oldPosition, newPosition, TransitionCurve.Evaluate(interpolator.Update(Time.unscaledDeltaTime)));
            yield return null;
        }

        // Unfreeze time
        GameManager.Instance.ResetGameSpeed();
    }

    public void ResetPosition()
    {
        transform.position = new Vector3(0, 0, -10);
    }
}
