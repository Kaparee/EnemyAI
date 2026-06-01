using System.Collections;
using UnityEngine;

public class CustomSectorSpawner : MonoBehaviour
{
    public AIArchetype[] availableArchetypes;
    public GameObject enemyPrefab;

    [Header("Pojedynczy wróg")]
    public int maxActiveEnemies = 1;
    public float respawnDelay = 25f;
    public float minSpawnDistanceFromPlayer = 80f;

    private float nextSpawnTime;

    private void OnEnable()
    {
        EventBus.OnEnemyDeath += OnEnemyDeath;
    }

    private void OnDisable()
    {
        EventBus.OnEnemyDeath -= OnEnemyDeath;
    }

    private void Start()
    {
        nextSpawnTime = 0f;
        StartCoroutine(SpawnWhenSectorReady());
    }

    private IEnumerator SpawnWhenSectorReady()
    {
        yield return null;

        PatrolWaypointManager pwm = FindFirstObjectByType<PatrolWaypointManager>();
        if (pwm != null) pwm.RefreshFromChunkManager();

        TrySpawnEnemy();
    }

    private void Update()
    {
        if (Time.time < nextSpawnTime) return;
        TrySpawnEnemy();
    }

    private void OnEnemyDeath(EnemyAI _)
    {
        nextSpawnTime = Time.time + respawnDelay;
    }

    private void TrySpawnEnemy()
    {
        if (CountActiveEnemies() >= maxActiveEnemies) return;

        ManualSpawnInsideSector();
        nextSpawnTime = float.MaxValue;
    }

    private int CountActiveEnemies()
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.activeEnemies.Count;

        return FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Length;
    }

    private void ManualSpawnInsideSector()
    {
        GameObject prefab = ResolveEnemyPrefab();
        if (prefab == null) return;

        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        Vector3 playerPos = player != null ? player.position : Vector3.zero;

        Vector3 center = ChunkManager.Instance != null
            ? ChunkManager.Instance.GetSectorWorldCenter()
            : playerPos;

        float half = ChunkManager.Instance != null
            ? ChunkManager.Instance.GetSectorHalfExtent() * 0.85f
            : 170f;

        Vector3 spawnPosition = FindSpawnPosition(center, half, playerPos);
        Vector3 lookTarget = player != null ? playerPos : center;

        AIArchetype chosen = availableArchetypes != null && availableArchetypes.Length > 0
            ? availableArchetypes[Random.Range(0, availableArchetypes.Length)]
            : null;

        SpawnEnemyAt(prefab, spawnPosition, lookTarget, chosen != null ? new[] { chosen } : availableArchetypes);
    }

    private Vector3 FindSpawnPosition(Vector3 center, float half, Vector3 playerPos)
    {
        Vector3 away = center - playerPos;
        away.y = 0f;
        if (away.sqrMagnitude < 1f)
            away = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        away.Normalize();

        for (int attempt = 0; attempt < 16; attempt++)
        {
            float dist = Random.Range(half * 0.55f, half * 0.9f);
            Vector3 candidate = center + away * dist;
            candidate += new Vector3(Random.Range(-30f, 30f), 0f, Random.Range(-30f, 30f));

            if (ChunkManager.Instance != null)
                candidate = ChunkManager.Instance.ClampToSector(candidate, null, 10f);

            candidate.y = playerPos.y;

            if (Vector3.Distance(candidate, playerPos) >= minSpawnDistanceFromPlayer)
                return candidate;

            away = Quaternion.Euler(0f, Random.Range(30f, 90f), 0f) * away;
        }

        Vector3 fallback = ChunkManager.Instance != null
            ? ChunkManager.Instance.ClampToSector(center + away * half * 0.7f, null, 10f)
            : center + away * half * 0.7f;
        fallback.y = playerPos.y;
        return fallback;
    }

    private GameObject ResolveEnemyPrefab()
    {
        if (enemyPrefab != null) return enemyPrefab;
        return Resources.Load<GameObject>("AI/EnemyWroga");
    }

    public static GameObject SpawnEnemyAt(GameObject prefab, Vector3 spawnPosition, Vector3 lookTarget, AIArchetype[] archetypes)
    {
        if (prefab == null) return null;

        GameObject newEnemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
        newEnemy.AddComponent<AISpawnedMarker>();

        Vector3 lookDir = lookTarget - spawnPosition;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f)
            newEnemy.transform.rotation = Quaternion.LookRotation(lookDir.normalized);

        AIArchetype chosen = archetypes != null && archetypes.Length > 0
            ? archetypes[Random.Range(0, archetypes.Length)]
            : null;

        EnemyAI aiComp = newEnemy.GetComponent<EnemyAI>();
        if (aiComp != null)
        {
            aiComp.ApplyArchetype(chosen);
            aiComp.AssignPatrolWaypoints();
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterEnemy(aiComp);
        }

        return newEnemy;
    }
}
