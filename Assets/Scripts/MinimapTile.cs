using UnityEngine;
using UnityEngine.UI;

// Used in the UI minimap, sets the explored status and special room icon
public class MinimapTile : MonoBehaviour
{
    [SerializeField]
    private Sprite[] MapSprites;
    [SerializeField]
    private Sprite[] RoomMarkerSprites;
    [SerializeField]
    private GameObject PlayerIcon;

    [SerializeField]
    private Image _marker;
    [SerializeField]
    private Image _image;

    public void SetRoomType(RoomType room)
    {
        if (room == RoomType.ITEM) _marker.sprite = RoomMarkerSprites[0];
        else if (room == RoomType.BOSS) _marker.sprite = RoomMarkerSprites[1];
        else if (room == RoomType.SHOP) _marker.sprite = RoomMarkerSprites[2];
        else _marker.enabled = false;
    }

    public void SetPlayerPresence(bool presence)
    {
        PlayerIcon.SetActive(presence);
    }

    public void SetVisited()
    {
        _image.sprite = MapSprites[1];
    }
}
