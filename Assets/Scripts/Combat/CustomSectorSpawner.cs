using System.Collections;
using UnityEngine;

// Odpowiada za dynamiczne generowanie wrogów i obiektów w sektorach.
// Wykorzystałem tutaj pooling obiektów lub kontrolowaną instancjację, by zachować płynność w trakcie lotu.
public class CustomSectorSpawner : MonoBehaviour
{
    public AIArchetype[] availableArchetypes;
    public GameObject enemyPrefab;

    [Header("Pojedynczy wróg")]
    public int maxActiveEnemies = 1;
    public float respawnDelay = 25f;
    public float minSpawnDistanceFromPlayer = 80f;

    private float nextSpawnTime;

    // Rejestruje nasluchiwanie na zdarzenie smierci przeciwnika przy wlaczeniu komponentu
    private void OnEnable()
    {
        EventBus.OnEnemyDeath += OnEnemyDeath;
    }

    // Wyrejestrowuje nasluchiwanie na zdarzenie smierci przeciwnika przy wylaczeniu komponentu
    private void OnDisable()
    {
        EventBus.OnEnemyDeath -= OnEnemyDeath;
    }

    // Uruchamia korutyne przygotowujaca sektor po starcie gry
    private void Start()
    {
        nextSpawnTime = 0f;
        StartCoroutine(SpawnWhenSectorReady());
    }

    // Odczekuje jedna klatke i odswieza menedzera punktow kontrolnych przed proba spawnu
    private IEnumerator SpawnWhenSectorReady()
    {
        yield return null;

        PatrolWaypointManager pwm = FindFirstObjectByType<PatrolWaypointManager>();
        if (pwm != null) pwm.RefreshFromChunkManager();

        TrySpawnEnemy();
    }

    // Główna pętla logiczna klatki. Staram się tu minimalizować ciężkie obliczenia.

    // Sprawdza czas i uruchamia probe stworzenia nowego przeciwnika w dozwolonym momencie
    private void Update()
    {
        if (Time.time < nextSpawnTime) return;
        TrySpawnEnemy();
    }

    // Aktualizuje czas nastepnego mozliwego spawnu po zniszczeniu jednostki wroga
    private void OnEnemyDeath(EnemyAI _)
    {
        nextSpawnTime = Time.time + respawnDelay;
    }

    // Weryfikuje limit aktywnych jednostek przed wywolaniem procedury tworzenia wroga
    private void TrySpawnEnemy()
    {
        if (CountActiveEnemies() >= maxActiveEnemies) return;

        ManualSpawnInsideSector();
        nextSpawnTime = float.MaxValue;
    }

    // Zwraca aktualna liczbe przeciwnikow w scenie korzystajac z menedzera gry
    private int CountActiveEnemies()
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.activeEnemies.Count;

        return FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Length;
    }

    // Wylicza parametry wezla i tworzy nowy obiekt przeciwnika wewnatrz aktualnego sektora
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

    // Szuka optymalnej i bezpiecznej pozycji do odrodzenia z dala od aktualnej pozycji gracza
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

    // Pobiera prefabrykat wroga z przypisanej zmiennej lub laduje domyslny z folderu Resources
    private GameObject ResolveEnemyPrefab()
    {
        if (enemyPrefab != null) return enemyPrefab;
        return Resources.Load<GameObject>("AI/EnemyWroga");
    }

    // Instancjonuje obiekt przeciwnika i inicjalizuje jego parametry poczatkowe
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
