using UnityEngine;

// Switched fire color depending on level
public class Torch : MonoBehaviour
{
    [SerializeField]
    private GameObject[] RedFireObjects;
    [SerializeField]
    private GameObject[] BlueFireObjects;

    void OnEnable()
    {
        foreach (var redFireObject in RedFireObjects)
        {
            redFireObject.SetActive(GameMaster.Singleton.CurrentFloor < 3);
        }
        foreach (var blueFireObject in BlueFireObjects)
        {
            blueFireObject.SetActive(GameMaster.Singleton.CurrentFloor >= 3);
        }
    }
}
