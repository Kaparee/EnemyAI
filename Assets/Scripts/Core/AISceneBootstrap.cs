using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AISceneBootstrap : MonoBehaviour
{
    [Header("Sektor")]
    [SerializeField] private Vector3 sectorCenter = Vector3.zero;
    [SerializeField] private float sectorRadius = 300f;

    [Header("Wróg")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private AIArchetype[] defaultArchetypes;

    [Header("Opcje")]
    [SerializeField] private bool removeLegacySceneEnemy = true;

    private void Awake()
    {
        EnsurePlayerSetup();
        EnsureObstacleRegistry();
        EnsurePatrolWaypointManager();
        EnsureSectorSpawner();
        CleanupLegacyEnemy();
    }

    private void EnsurePlayerSetup()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (AILayers.Player >= 0)
            AILayers.SetLayerRecursively(player, AILayers.Player);

        if (player.GetComponent<PlayerMarker>() == null)
            player.AddComponent<PlayerMarker>();
    }

    private void EnsureObstacleRegistry()
    {
        if (FindFirstObjectByType<ObstacleRegistry>() == null)
        {
            var go = new GameObject("ObstacleRegistry");
            go.AddComponent<ObstacleRegistry>();
        }
    }

    private void EnsurePatrolWaypointManager()
    {
        if (FindFirstObjectByType<PatrolWaypointManager>() != null) return;

        var go = new GameObject("PatrolWaypointManager");
        go.transform.position = sectorCenter;
        var pwm = go.AddComponent<PatrolWaypointManager>();
        pwm.sectorRadius = sectorRadius;
        pwm.sectorCenter = sectorCenter;
    }

    private void EnsureSectorSpawner()
    {
        CustomSectorSpawner spawner = FindFirstObjectByType<CustomSectorSpawner>();
        if (spawner == null)
        {
            var go = new GameObject("CustomSectorSpawner");
            go.transform.position = sectorCenter;
            spawner = go.AddComponent<CustomSectorSpawner>();
        }

        if (spawner.enemyPrefab == null)
            spawner.enemyPrefab = ResolveEnemyPrefab();

        if (spawner.availableArchetypes == null || spawner.availableArchetypes.Length == 0)
            spawner.availableArchetypes = ResolveArchetypes();
    }

    private void CleanupLegacyEnemy()
    {
        if (!removeLegacySceneEnemy) return;

        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (EnemyAI enemy in enemies)
        {
            if (enemy.GetComponent<AISpawnedMarker>() == null)
                Destroy(enemy.gameObject);
        }
    }

    private GameObject ResolveEnemyPrefab()
    {
        if (enemyPrefab != null) return enemyPrefab;
        enemyPrefab = Resources.Load<GameObject>("AI/EnemyWroga");
        return enemyPrefab;
    }

    private AIArchetype[] ResolveArchetypes()
    {
        var list = new System.Collections.Generic.List<AIArchetype>();

        if (defaultArchetypes != null)
        {
            foreach (AIArchetype a in defaultArchetypes)
            {
                if (a != null && !list.Contains(a))
                    list.Add(a);
            }
        }

        if (list.Count == 0)
        {
            foreach (string name in new[] { "ArchetypeStandard", "ArchetypeHeavy", "ArchetypeScout" })
            {
                AIArchetype a = Resources.Load<AIArchetype>("AI/" + name);
                if (a != null) list.Add(a);
            }
        }

        return list.Count > 0 ? list.ToArray() : null;
    }
}

public class AISpawnedMarker : MonoBehaviour { }
