using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Generates the random floors
public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance;

    public delegate void FloorGeneratedEvent(Vector2Int size);
    public static event FloorGeneratedEvent OnFloorGenerated;

    // Used for testing
    [ContextMenuItem("Run generator", "GenerateWithDefault")]
    [SerializeField]
    private GeneratorSettings DefaultSettings;
    [SerializeField, Range(0, 1)]
    private float LoopCreationChance;

    [SerializeField]
    private GeneratorSettings[] FloorSettings;

    [SerializeField]
    private Vector2 RoomDimensions;

    [SerializeField]
    private Room BaseRoom;
    [SerializeField]
    private Room StartRoom;

    private Dictionary<Vector2Int, Room> SpawnedRooms;

    void Awake()
    {
        Instance = this;
    }

    public void GenerateWithDefault()
    {
        GenerateDungeon(DefaultSettings);
    }

    // Each floor has predefined settings, so they get bigger the further along you go in the game
    public void GenerateFloor(int floor) => GenerateDungeon(FloorSettings[floor]);

    public void GenerateDungeon(GeneratorSettings settings)
    {
        if (SpawnedRooms != null)
        {
            foreach (var room in SpawnedRooms.Values)
                Destroy(room.gameObject);
        }

        SpawnedRooms = new Dictionary<Vector2Int, Room>();

        Vector2Int startPos = Vector2Int.zero;
        List<Vector2Int> roomPositions = new List<Vector2Int> { startPos };
        SpawnedRooms[startPos] = Instantiate(StartRoom, Vector3.zero, Quaternion.identity); // Start room
        SpawnedRooms[startPos].Type = RoomType.FREE;

        var roomCount = Random.Range(settings.RoomCountRange.x, settings.RoomCountRange.y);

        for (int i = 1; i < roomCount; i++)
        {
            // Find the position that has the least neighbors, encouraging branching
            Vector2Int newRoomPos = GetBestRoomPositionToExpand(roomPositions);

            // Ensure the new room doesn't exceed max distance in x or y direction
            if (Mathf.Abs(newRoomPos.x) <= settings.DungeonSize.x && Mathf.Abs(newRoomPos.y) <= settings.DungeonSize.y)
            {
                if (!SpawnedRooms.ContainsKey(newRoomPos))
                {
                    Room roomPrefab = BaseRoom;
                    SpawnedRooms[newRoomPos] = Instantiate(roomPrefab, new Vector3(newRoomPos.x * RoomDimensions.x, newRoomPos.y * RoomDimensions.y, 0), Quaternion.identity);
                    roomPositions.Add(newRoomPos);
                }
            }
        }

        // Set room neighbords
        foreach (var position in roomPositions)
        {
            Room room = SpawnedRooms[position];
            List<Vector2Int> neighborPositions = GetRoomsAround(position);

            bool[] neighbors = new bool[4];

            for (int i = 0; i < 4; i++) neighbors[i] = SpawnedRooms.ContainsKey(neighborPositions[i]);

            room.SetNeighbors(neighbors);
        }

        roomPositions.Remove(Vector2Int.zero);

        float furthestDistance = 0;
        Vector2Int farthestRoom = Vector2Int.zero;

        foreach (var position in roomPositions)
        {
            if (SpawnedRooms[position].Neighbors.Count(neighbor => neighbor) <= 1)
            {
                var distance = position.sqrMagnitude;
                if (distance > furthestDistance)
                {
                    furthestDistance = distance;
                    farthestRoom = position;
                }
            }
        }
        if (farthestRoom == Vector2Int.zero)
        {
            foreach(var position in roomPositions)
            {
                var distance = position.sqrMagnitude;
                if (distance > furthestDistance)
                {
                    furthestDistance = distance;
                    farthestRoom = position;
                }
            }
        }

        roomPositions.Remove(farthestRoom);
        SpawnedRooms[farthestRoom].Populate(farthestRoom, RoomType.BOSS);

        // Place random item room
        var possibleItemRooms = roomPositions
            .Where(position => SpawnedRooms[position].Neighbors.Count(neighbor => neighbor) <= 1)
            .ToList();
        if (possibleItemRooms.Count == 0)
            possibleItemRooms = roomPositions;

        var randomIndex = Random.Range(0, possibleItemRooms.Count);

        SpawnedRooms[possibleItemRooms[randomIndex]].Populate(possibleItemRooms[randomIndex], RoomType.ITEM);
        roomPositions.Remove(possibleItemRooms[randomIndex]);

        // Place random shop room
        possibleItemRooms = roomPositions
            .Where(position => SpawnedRooms[position].Neighbors.Count(neighbor => neighbor) <= 1)
            .ToList();
        if (possibleItemRooms.Count == 0)
            possibleItemRooms = roomPositions;

        randomIndex = Random.Range(0, possibleItemRooms.Count);

        SpawnedRooms[possibleItemRooms[randomIndex]].Populate(possibleItemRooms[randomIndex], RoomType.SHOP);
        roomPositions.Remove(possibleItemRooms[randomIndex]);

        int enemyRooms = Mathf.FloorToInt(roomPositions.Count * 0.85f);

        Shuffle(roomPositions);

        // Set room roles
        foreach (var position in roomPositions)
        {
            RoomType type = RoomType.FREE;
            Room room = SpawnedRooms[position];

            if (enemyRooms > 0)
            {
                type = RoomType.ENEMY;
                enemyRooms--;
            }

            room.Populate(position, type);
        }

        foreach (var room in SpawnedRooms.Values) room.SetGradients();

        Vector2Int size = new Vector2Int(0, 0);
        foreach (var position in SpawnedRooms.Keys)
        {
            if (Mathf.Abs(position.x) > size.x) size.x = Mathf.Abs(position.x);
            if (Mathf.Abs(position.y) > size.y) size.y = Mathf.Abs(position.y);
        }
        size += new Vector2Int(1, 1);

        OnFloorGenerated?.Invoke(size * 2);
    }

    public Room GetRoomAtPosition(Vector2Int position)
    {
        if (SpawnedRooms.ContainsKey(position))
            return SpawnedRooms[position];
        else
            return null;
    }

    List<Vector2Int> GetRoomsAround(Vector2Int position)
    {
        List<Vector2Int> possiblePositions = new List<Vector2Int>();

        possiblePositions.Add(position + Vector2Int.left);
        possiblePositions.Add(position + Vector2Int.up);
        possiblePositions.Add(position + Vector2Int.right);
        possiblePositions.Add(position + Vector2Int.down);

        return possiblePositions;
    }

    // Wow no way back when I bothered to make good comments
    Vector2Int GetBestRoomPositionToExpand(List<Vector2Int> roomPositions)
    {
        List<Vector2Int> possiblePositions = new List<Vector2Int>();

        foreach (var position in roomPositions)
        {
            // Add all possible positions from the current room
            possiblePositions.AddRange(GetRoomsAround(position));
        }

        // Filter out positions that are already occupied
        possiblePositions = possiblePositions.Where(pos => !SpawnedRooms.ContainsKey(pos)).ToList();

        // Optionally shuffle the list to make the selection more random
        possiblePositions = possiblePositions.OrderBy(x => Random.value).ToList();

        // Introduce a chance to create a loop by allowing positions with more neighbors
        bool shouldCreateLoop = Random.Range(0f, 1f) < LoopCreationChance;

        if (shouldCreateLoop)
        {
            // Pick a position that has more neighbors (to create a loop)
            Vector2Int loopPosition = possiblePositions.OrderByDescending(pos => GetNeighborCount(pos)).First();
            return loopPosition;
        }
        else
        {
            // Pick a position with fewer neighbors to ensure branching
            Vector2Int branchPosition = possiblePositions.OrderBy(pos => GetNeighborCount(pos)).First();
            return branchPosition;
        }
    }

    int GetNeighborCount(Vector2Int position)
    {
        int count = 0;

        if (SpawnedRooms.ContainsKey(position + Vector2Int.up)) count++;
        if (SpawnedRooms.ContainsKey(position + Vector2Int.down)) count++;
        if (SpawnedRooms.ContainsKey(position + Vector2Int.left)) count++;
        if (SpawnedRooms.ContainsKey(position + Vector2Int.right)) count++;

        return count;
    }

    void Shuffle(IList<Vector2Int> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            var value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}

[System.Serializable]
public struct GeneratorSettings
{
    public Vector2Int RoomCountRange;
    public Vector2Int DungeonSize;
}