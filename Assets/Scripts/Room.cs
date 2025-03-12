using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Room class, handles spawning enemies, populating with lootables and decorations
public class Room : MonoBehaviour
{
    private Animation _anim;

    [SerializeField]
    private GameObject ExitPrefab;
    [SerializeField]
    private GameObject[] ChestPrefabs;
    [SerializeField]
    private float[] SpawnOffset;
    [SerializeField]
    private GameObject[] Walls;
    [SerializeField]
    private Transform[] Doors;
    [SerializeField]
    private GameObject[] Blockers;

    [SerializeField]
    private Vector2Int LootCount;
    [SerializeField]
    private int MaxAttemptsPerSpawn = 10;
    [SerializeField]
    private LayerMask ObstacleLayer;
    [SerializeField]
    private Vector2 RoomSize;
    [SerializeField]
    private GameObject[] LootablePrefabs;

    [SerializeField]
    private GameObject[] ShopLayouts;
    [SerializeField]
    private GameObject[] BossLayouts;

    [SerializeField, Header("Props")]
    private PropPrefab[] PropPrefabs;
    [SerializeField, Header("Obstacles")]
    private ObstaclePrefab[] RockFormations;
    [SerializeField, Header("Enemies")]
    private float MinEnemyPlayerDistance = 5f;
    [SerializeField]
    private EnemyTable[] EnemyTables;

    [HideInInspector]
    public RoomType Type;
    [HideInInspector]
    public bool[] Neighbors;

    [HideInInspector]
    public bool HasCleared = false;
    [HideInInspector]
    public List<Enemy> LiveEnemies;
    [SerializeField]
    private float EntranceBlockDistance = 1.5f;

    [SerializeField]
    private SpriteRenderer[] Gradients;
    [SerializeField]
    private Color[] ItemHighlightColors;

    private Vector2Int CurrentPosition;

    [System.Serializable]
    private struct EnemyTable
    {
        public float Weight;
        public int MaxSpawnCount;

        public EnemyPrefab[] EnemyPrefabs;
    }

    [System.Serializable]
    private struct EnemyPrefab
    {
        public int MaxSpawnCount;
        public float Weight;
        public float Radius;

        public GameObject Prefab;
    }

    [System.Serializable]
    private struct ObstaclePrefab
    {
        public Direction[] NotAllowedExits;
        public RoomType[] NotAllowedRooms;

        public float SpawnPercentage;

        public GameObject Prefab;
    }

    [System.Serializable]
    private struct PropPrefab
    {
        public RoomType[] AllowedRooms;

        public int MaxSpawnCount;
        public float SpawnChance;

        public bool RandomRotation;
        public bool RandomColor;
        public bool RandomSize;

        public Vector2 DefaultSize;

        public Vector4 SizeRanges;
        public Gradient RandomColors;

        public GameObject Prefab;
    }

    public void SetNeighbors(bool[] neighbors)
    {
        Neighbors = neighbors;

        for (int i = 0; i < 4; i++)
        {
            Walls[i].SetActive(!neighbors[i]);
            Blockers[i].SetActive(neighbors[i]);
        }
    }

    public void OnEnter(Direction exit)
    {
        if (!HasCleared)
        {
            _anim = GetComponent<Animation>();

            if (Type == RoomType.ENEMY)
            {
                SpawnEnemies();

                Lock();
            }
            else if (Type == RoomType.BOSS)
            {
                GameUI.Instance.SetBossBarActive(true);
                SpawnBoss();
                Lock();
            }
        }
    }

    // Used to show if you're next to a boss room, item or shop room
    public void SetGradients()
    {
        for (int i = 0; i < 4; i++)
        {
            if (Neighbors[i])
            {
                var direction = Vector2Int.zero;
                if (i == 0) direction = Vector2Int.left;
                else if (i == 1) direction = Vector2Int.up;
                else if (i == 2) direction = Vector2Int.right;
                else if (i == 3) direction = Vector2Int.down;

                var neighborType = FloorManager.Instance.GetRoomAtPosition(CurrentPosition + direction).Type;
                if (neighborType != RoomType.ENEMY && neighborType != RoomType.FREE)
                {
                    Gradients[i].color = ItemHighlightColors[(int)neighborType];
                    Gradients[i].gameObject.SetActive(true);
                }
            }
        }
    }

    public void Populate(Vector2Int position, RoomType room)
    {
        CurrentPosition = position;
        Type = room;

        if (room == RoomType.FREE || room == RoomType.SHOP || room == RoomType.ITEM) HasCleared = true;

        if (room == RoomType.ENEMY || room == RoomType.FREE)
        {
            // Place obstacles
            List<ObstaclePrefab> options = new List<ObstaclePrefab>(RockFormations.Where(prefab => !prefab.NotAllowedRooms.Contains(Type)).Where(
                prefab =>
                {
                    foreach (var direction in prefab.NotAllowedExits)
                    {
                        if (direction == Direction.Left && Neighbors[0]) return false;
                        if (direction == Direction.Up && Neighbors[1]) return false;
                        if (direction == Direction.Right && Neighbors[2]) return false;
                        if (direction == Direction.Down && Neighbors[3]) return false;
                    }

                    return true;
                }
            ));

            var chosen = options[Random.Range(0, options.Count)];
            if (chosen.Prefab != null) Instantiate(chosen.Prefab, transform.position, Quaternion.identity, transform);

            // Spawn loot
            SpawnLoot();
            // Spawn props
            SpawnProps();
        }
        else if (room == RoomType.ITEM)
        {
            Instantiate(ChestPrefabs[0], transform.position, Quaternion.identity, transform);

            // Spawn loot
            SpawnLoot();
            SpawnLoot();
            // Spawn props
            SpawnProps();
            SpawnProps();
        }
        else if (room == RoomType.BOSS)
        {
            SpawnProps();
            Instantiate(BossLayouts[Random.Range(0, BossLayouts.Length)], transform.position, Quaternion.identity, transform);
        }
        else if (room == RoomType.SHOP)
        {
            SpawnProps();
            Instantiate(ShopLayouts[Random.Range(0, ShopLayouts.Length)], transform.position, Quaternion.identity, transform);
        }
    }

    public void SpawnExit()
    {
        Instantiate(ExitPrefab, transform.position, Quaternion.identity, transform);
    }

    public Vector2 GetSpawnPosition(Direction exit)
    {
        return exit switch
        {
            Direction.Right => Doors[0].transform.position + new Vector3(SpawnOffset[0], 0, 0),
            Direction.Down => Doors[1].transform.position + new Vector3(0, -SpawnOffset[2], 0),
            Direction.Left => Doors[2].transform.position + new Vector3(-SpawnOffset[0], 0, 0),
            Direction.Up => Doors[3].transform.position + new Vector3(0, SpawnOffset[1], 0),
            _ => transform.position
        };
    }

    // Yeah idk good luck figuring this one out
    void SpawnProps()
    {
        var propPrefabs = PropPrefabs.Where(prop => prop.AllowedRooms.Contains(Type));
        foreach (var prop in propPrefabs)
        {
            int spawned = 0;
            while (spawned < prop.MaxSpawnCount && Random.Range(0, 1f) <= prop.SpawnChance)
            {
                Vector2 size = prop.RandomSize ? new Vector2(Random.Range(prop.SizeRanges.x, prop.SizeRanges.y),
                    Random.Range(prop.SizeRanges.z, prop.SizeRanges.w)) : prop.DefaultSize;
                Vector2 position = new Vector2(Random.Range(-RoomSize.x / 2f + size.x/2, RoomSize.x / 2f - size.x / 2),
                    Random.Range(-RoomSize.y / 2f + size.y / 2, RoomSize.y / 2f - size.y / 2));

                bool rotate = prop.RandomRotation && Random.Range(0, 2) == 1;
                if (rotate) size = new Vector2(size.y, size.x);

                var spawnedProp = Instantiate(prop.Prefab, position + (Vector2)transform.position, rotate ? Quaternion.Euler(0, 0, 90) : Quaternion.identity);
                var sprite = spawnedProp.GetComponent<SpriteRenderer>();

                if (prop.RandomSize) sprite.size = size;
                if (prop.RandomColor) sprite.color = prop.RandomColors.Evaluate(Random.Range(0f, 1f));

                spawned++;
            }
        }
    }

    void SpawnLoot()
    {
        int propCount = Random.Range(LootCount.x, LootCount.y + 1);

        for (int i = 0; i < propCount; i++)
        {
            GameObject propPrefab = LootablePrefabs[Random.Range(0, LootablePrefabs.Length)];
            float freeSpaceRadius = 0.5f;

            Vector3 spawnPoint;
            bool placed = false;

            for (int attempt = 0; attempt < MaxAttemptsPerSpawn; attempt++)
            {
                spawnPoint = GetRandomPoint();

                foreach (var blocker in Blockers)
                {
                    if ((blocker.transform.position - spawnPoint).magnitude < EntranceBlockDistance)
                        continue;
                }

                if (Physics2D.OverlapCircleAll(spawnPoint, freeSpaceRadius, ObstacleLayer).Length == 0)
                {
                    Instantiate(propPrefab, spawnPoint, Quaternion.identity);
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                Debug.LogWarning($"Failed to place {propPrefab.name} after {MaxAttemptsPerSpawn} attempts.");
            }
        }
    }

    private void SpawnEnemies()
    {
        LiveEnemies = new List<Enemy>();
        var chosenTables = SelectEnemyTables(EnemyTables, 1);
        foreach (var table in chosenTables)
        {
            SpawnFromTable(table);
        }
    }

    void SpawnBoss()
    {
        LiveEnemies = new List<Enemy>();
        Enemy bossToSpawn = GameMaster.Singleton.CurrentFloor == 4 ? GameManager.Instance.FinalBossPrefab : GameManager.Instance.GetRandomBoss();

        var boss = Instantiate(bossToSpawn, transform.position, Quaternion.identity, transform);
        LiveEnemies.Add(boss);
        boss.OwnerRoom = this;
    }

    // The worst set of functions to spawn enemies EVER
    private List<EnemyTable> SelectEnemyTables(EnemyTable[] tables, int count)
    {
        float totalWeight = tables.Sum(t => t.Weight);
        List<EnemyTable> chosenTables = new List<EnemyTable>();

        for (int i = 0; i < count; i++)
        {
            float weight = Random.Range(0, totalWeight);
            foreach (var table in tables.OrderBy(_ => Random.value)) // Shuffle
            {
                weight -= table.Weight;
                if (weight <= 0)
                {
                    chosenTables.Add(table);
                    break;
                }
            }
        }
        return chosenTables;
    }

    private void SpawnFromTable(EnemyTable table)
    {
        int spawnCount = Random.Range(Mathf.Max((table.MaxSpawnCount + (GameMaster.Singleton.CurrentFloor/2)) / 2, 1), table.MaxSpawnCount + (GameMaster.Singleton.CurrentFloor/2));
        float totalWeight = table.EnemyPrefabs.Sum(e => e.Weight);  

        while (spawnCount > 0)
        {
            var enemy = SelectEnemy(table.EnemyPrefabs, totalWeight);
            spawnCount -= SpawnEnemyInstances(enemy, spawnCount);
        }
    }

    private EnemyPrefab SelectEnemy(EnemyPrefab[] enemies, float totalWeight)
    {
        float weight = Random.Range(0, totalWeight);
        foreach (var enemy in enemies.OrderBy(_ => Random.value)) // Shuffle
        {
            weight -= enemy.Weight;
            if (weight <= 0) return enemy;
        }
        return enemies[0]; // Fallback
    }

    private int SpawnEnemyInstances(EnemyPrefab enemy, int maxSpawnCount)
    {
        int enemySpawnCount = Random.Range(Mathf.Max(enemy.MaxSpawnCount / 2, 1), enemy.MaxSpawnCount);
        int spawned = 0;

        for (int i = 0; i < enemySpawnCount && spawned < maxSpawnCount; i++)
        {
            if (TrySpawnEnemy(enemy)) spawned++;
        }

        return spawned;
    }

    private bool TrySpawnEnemy(EnemyPrefab enemy)
    {
        for (int i = 0; i < MaxAttemptsPerSpawn; i++)
        {
            var spawnPoint = GetRandomPoint();
            if (IsValidSpawnPoint(spawnPoint, enemy.Radius, ObstacleLayer))
            {
                var spawnedEnemy = Instantiate(enemy.Prefab, spawnPoint, Quaternion.identity, transform);
                spawnedEnemy.GetComponent<Enemy>().OwnerRoom = this;
                LiveEnemies.Add(spawnedEnemy.GetComponent<Enemy>());
                return true;
            }
        }
        return false;
    }

    private bool IsValidSpawnPoint(Vector2 point, float radius, int layerMask)
    {
        return (point - (Vector2)PlayerMovement.Instance.transform.position).magnitude >= MinEnemyPlayerDistance &&
               !Physics2D.OverlapCircle(point, radius, layerMask);
    }

    Vector3 GetRandomPoint()
    {
        return new Vector3(
            transform.position.x + Random.Range(-RoomSize.x / 2, RoomSize.x / 2),
            transform.position.y + Random.Range(-RoomSize.y / 2, RoomSize.y / 2),
            0);
    }

    // When an enemy dies register it, if all enemies died unlock room
    public void NotifyDeath(Enemy enemy)
    {
        LiveEnemies.Remove(enemy);
        if (LiveEnemies.Count == 0)
        {
            Unlock();
            if (Type == RoomType.BOSS)
            {
                GameUI.Instance.SetBossBarActive(false);
                if (GameMaster.Singleton.CurrentFloor < 4)
                {
                    AudioManager.Instance.PlayMusic(Music.BOSS_OVER);
                    SpawnExit();
                    if (!GameManager.Instance.LockedIn) Instantiate(ChestPrefabs[1], transform.position - new Vector3(0, 2, 3), Quaternion.identity, transform);
                }
                else
                {
                    AudioManager.Instance.FadeOutMusic();
                    GameManager.Instance.EndingCutscene();
                }
            }
        }
        // Wow some item logic made it in here :D
        else if (GameManager.Instance.CollectedItemTypes.Contains(ItemEnum.SANGUINE_FEAST))
        {
            float closestDistance = Mathf.Infinity;
            Enemy closestEnemy = null;

            foreach (var liveEnemy in LiveEnemies)
            {
                var distance = (liveEnemy.transform.position - enemy.transform.position).sqrMagnitude;
                if (closestDistance >= distance)
                {
                    closestDistance = distance;
                    closestEnemy = liveEnemy;
                }
            }

            closestEnemy.AddEffect(StatusEffect.WITHER);
        }
    }

    void Lock()
    {
        _anim.Play("anim_room_lock");
        AudioManager.Instance.PlaySFX(SFX.GATE_CLOSE, PlayType.SINGLE);
    }

    void Unlock()
    {
        HasCleared = true;

        _anim.Play("anim_room_unlock");
        AudioManager.Instance.PlaySFX(SFX.GATE_OPEN, PlayType.SINGLE);
    }
}

public enum RoomType
{
    ITEM = 0,
    BOSS = 1,
    SHOP = 2,
    ENEMY,
    FREE
}

static class RandomExtensions
{
    public static void Shuffle<T>(T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            int k = Random.Range(0, n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }
}