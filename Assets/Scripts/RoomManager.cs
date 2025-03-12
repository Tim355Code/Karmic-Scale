using UnityEngine;
using System.Collections.Generic;

// Stores current room and handles switching rooms
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    public delegate void RoomSwitchEvent(Vector2Int prevPos, Vector2Int newPos);
    public static event RoomSwitchEvent OnRoomSwitch;

    public Vector2Int CurrentRoom;

    [HideInInspector]
    public readonly Vector2Int[] EnumToVector = new Vector2Int[] {
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1)
    };

    public readonly Dictionary<Vector2Int, Direction> VectorToEnum = new Dictionary<Vector2Int, Direction>()
    {
        { new Vector2Int(-1, 0), Direction.Left },
        { new Vector2Int(0, 1), Direction.Up },
        { new Vector2Int(1, 0), Direction.Right },
        { new Vector2Int(0, -1), Direction.Down }
    };

    void Awake()
    {
        Instance = this;
    }

    public void SwitchRoom(Direction direction)
    {
        AudioManager.Instance.PlaySFX(SFX.WOOSH, PlayType.UNRESTRICTED);

        var oldRoom = CurrentRoom;
        CurrentRoom += EnumToVector[(int)direction];

        GetCurrentRoom.OnEnter(direction);
        OnRoomSwitch?.Invoke(oldRoom, CurrentRoom);

        if (FloorManager.Instance.GetRoomAtPosition(oldRoom).Type == RoomType.BOSS) AudioManager.Instance.PlayMusic(GameMaster.Singleton.CurrentFloor < 3 ? Music.FLOOR : Music.FLOOR_2);
        else if (FloorManager.Instance.GetRoomAtPosition(CurrentRoom).Type == RoomType.BOSS && FloorManager.Instance.GetRoomAtPosition(CurrentRoom).HasCleared) AudioManager.Instance.PlayMusic(Music.BOSS_OVER);
    }

    public Room GetCurrentRoom => FloorManager.Instance.GetRoomAtPosition(CurrentRoom);
}

public enum Direction
{
    Left = 0,
    Up = 1,
    Right = 2,
    Down = 3
}
