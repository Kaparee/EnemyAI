using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Idle, Patrol, Chase, Combat, Flee }

    [Header("Stan AI")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Parametry Fizyczne")]
    public float mass = 40000f;
    public float mainThrust = 280000f;
    public float rotationSpeed = 1.8f;
    public float linearDrag = 1f;
    public float maxFlightSpeed = 35f;
    public float angularDrag = 1.2f;
    public AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float antiDriftFactor = 2f;
    private float currentAccelerationTime = 0f;

    [Header("Logika AI")]
    public Transform playerTarget;
    public float detectionRange = 500f;
    public float combatRange = 150f;
    public float fleeHealthThreshold = 0.25f;
    public float stopDistance = 50f;
    public float patrolStopDistance = 12f;

    [Header("Uzbrojenie burtowe")]
    public Turret[] sideTurrets;

    private ShipStats stats;
    private Rigidbody rb;
    private ObstacleAvoidance avoidance;
    private TacticalBrain tacticalBrain;
    private CustomRadarSystem radar;

    [Header("Patrol Settings")]
    public List<Vector3> patrolWaypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    public float waypointThreshold = 20f;

    private List<Vector3> currentAStarPath = new List<Vector3>();
    private int currentPathIndex = 0;
    private float pathRecalculationTimer = 0f;
    private bool isCalculatingPath = false;
    private bool isCalculatingTactics = false;
    private Vector3 currentTacticalPosition = Vector3.zero;
    private float tacticalRecalcTimer = 0f;
    private EnemyState lastLoggedState = EnemyState.Idle;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        stats = GetComponent<ShipStats>();
        avoidance = GetComponent<ObstacleAvoidance>();
        tacticalBrain = GetComponent<TacticalBrain>();
        radar = GetComponent<CustomRadarSystem>();

        if (accelerationCurve == null || accelerationCurve.length == 0)
            accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        if (avoidance == null) avoidance = gameObject.AddComponent<ObstacleAvoidance>();
        if (tacticalBrain == null) tacticalBrain = gameObject.AddComponent<TacticalBrain>();
        if (radar == null) radar = gameObject.AddComponent<CustomRadarSystem>();

        if (sideTurrets == null || sideTurrets.Length == 0)
            sideTurrets = GetComponentsInChildren<Turret>();

        radar.BindTurrets(sideTurrets);

        if (stats != null && stats.GetMaxHP() > 0f && stats.CurrentHP <= 0f)
            stats.SetHP(stats.GetMaxHP());
    }

    void Start()
    {
        rb.mass = mass;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;
        rb.useGravity = false;

        if (stats == null) stats = gameObject.AddComponent<ShipStats>();
        if (stats.GetMaxHP() > 0f && stats.CurrentHP <= 0f)
            stats.SetHP(stats.GetMaxHP());

        rb.isKinematic = false;
        rb.WakeUp();

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }

        if (patrolWaypoints.Count == 0)
            AssignPatrolWaypoints();

        if (currentState == EnemyState.Idle)
            currentState = EnemyState.Patrol;

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterEnemy(this);

        AILog.FSM($"Start. Stan: {currentState}. Punktów patrolowych: {patrolWaypoints.Count}.");
    }

    void OnEnable()
    {
        EventBus.OnPlayerDetected += OnPlayerDetected;
    }

    void OnDisable()
    {
        EventBus.OnPlayerDetected -= OnPlayerDetected;
    }

    public void AssignPatrolWaypoints()
    {
        PatrolWaypointManager pwm = Object.FindFirstObjectByType<PatrolWaypointManager>();
        if (pwm != null)
        {
            pwm.RefreshFromChunkManager();
            patrolWaypoints = pwm.GetWaypoints();
        }
        else
        {
            GenerateFallbackWaypoints();
        }

        currentWaypointIndex = 0;
        currentAStarPath.Clear();
        currentPathIndex = 0;

        AILog.Patrol($"Dostałem {patrolWaypoints.Count} punktów do oblotu sektora.");
    }

    private void GenerateFallbackWaypoints()
    {
        Vector3 center = ChunkManager.Instance != null
            ? ChunkManager.Instance.GetSectorWorldCenter()
            : transform.position;

        float radius = ChunkManager.Instance != null
            ? ChunkManager.Instance.GetSectorHalfExtent() * 0.88f
            : 120f;
        float halfH = ChunkManager.Instance != null
            ? ChunkManager.Instance.GetSectorHalfExtent() * 0.42f
            : 60f;
        float minSep = radius * 0.38f;

        patrolWaypoints.Clear();
        int attempts = 0;
        while (patrolWaypoints.Count < 7 && attempts < 200)
        {
            attempts++;
            Vector3 c = center + new Vector3(
                Random.Range(-radius, radius),
                Random.Range(-halfH, halfH),
                Random.Range(-radius, radius));

            bool farEnough = true;
            foreach (Vector3 w in patrolWaypoints)
            {
                if (Vector3.Distance(c, w) < minSep) { farEnough = false; break; }
            }
            if (farEnough) patrolWaypoints.Add(c);
        }
    }

    private void OnPlayerDetected(Transform player)
    {
        playerTarget = player;
        if (currentState == EnemyState.Idle || currentState == EnemyState.Patrol || currentState == EnemyState.Chase)
        {
            AILog.FSM("Widzę gracza — przechodzę z patrolowania na walkę.");
            currentState = EnemyState.Combat;
        }
    }

    void FixedUpdate()
    {
        UpdateState();
        ExecuteState();
        ClampFlightSpeed();
        EnforceSectorBounds();
    }

    private void ClampFlightSpeed()
    {
        if (maxFlightSpeed <= 0f) return;
        if (rb.linearVelocity.sqrMagnitude > maxFlightSpeed * maxFlightSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxFlightSpeed;
    }

    private void UpdateState()
    {
        if (stats == null) return;

        float maxHp = stats.GetMaxHP();
        if (maxHp <= 0f) return;

        float healthPercent = stats.CurrentHP / maxHp;
        if (healthPercent < fleeHealthThreshold)
        {
            if (currentState != EnemyState.Flee)
                AILog.FSM($"Mało HP ({healthPercent * 100f:F0}%) — uciekam.");
            currentState = EnemyState.Flee;
            return;
        }

        if (currentState == EnemyState.Flee && healthPercent >= fleeHealthThreshold + 0.05f)
        {
            EnemyState next = playerTarget != null ? EnemyState.Combat : EnemyState.Patrol;
            AILog.FSM($"HP wróciło — idę z powrotem w tryb {next}.");
            currentState = next;
        }

        LogStateChange();
    }

    private void LogStateChange()
    {
        if (currentState == lastLoggedState) return;
        lastLoggedState = currentState;
        AILog.FSM($"Zmiana stanu: {currentState}");
    }

    private void ExecuteState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                break;
            case EnemyState.Patrol:
                PatrolLogic();
                break;
            case EnemyState.Chase:
                ChaseLogic();
                break;
            case EnemyState.Combat:
                CombatLogic();
                break;
            case EnemyState.Flee:
                FleeLogic();
                break;
        }
    }

    private void PatrolLogic()
    {
        if (patrolWaypoints.Count == 0) return;

        Vector3 target = patrolWaypoints[currentWaypointIndex];

        if (!isCalculatingPath && (currentAStarPath.Count == 0 || Time.time > pathRecalculationTimer))
        {
            isCalculatingPath = true;
            LayerMask mask = AILayers.AsteroidMask;
            if (mask.value == 0) mask = ~AILayers.PlayerMask;

            AILog.AStar(
                $"Licze trasę do punktu {currentWaypointIndex + 1}/{patrolWaypoints.Count} " +
                $"(mój A*, nie NavMesh). Cel: {target}");

            StartCoroutine(ManualAStar.FindPathCoroutine(transform.position, target, 30f, mask, path =>
            {
                currentAStarPath = path ?? new List<Vector3>();
                currentPathIndex = 0;
                pathRecalculationTimer = Time.time + 3f;
                isCalculatingPath = false;
                if (currentAStarPath.Count == 0)
                    AILog.AStar("Nie znalazłem drogi — lecę na prosto do punktu.");
            }));
        }

        Vector3 moveTarget = target;
        if (currentAStarPath.Count > 0 && currentPathIndex < currentAStarPath.Count)
            moveTarget = currentAStarPath[currentPathIndex];

        MoveTowards(moveTarget, false, patrolStopDistance);

        if (currentAStarPath.Count > 0 && currentPathIndex < currentAStarPath.Count)
        {
            if (Vector3.Distance(transform.position, currentAStarPath[currentPathIndex]) < waypointThreshold)
                currentPathIndex++;
        }

        if (Vector3.Distance(transform.position, target) < waypointThreshold * 2f)
        {
            AILog.Patrol($"Jestem na punkcie {currentWaypointIndex + 1} — lecę do następnego.");
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Count;
            currentAStarPath.Clear();
        }
    }

    private void ChaseLogic()
    {
        if (playerTarget == null) return;
        MoveTowards(playerTarget.position, false, patrolStopDistance);
    }

    private void CombatLogic()
    {
        if (playerTarget == null)
        {
            currentState = EnemyState.Patrol;
            return;
        }

        if (!isCalculatingTactics && Time.time >= tacticalRecalcTimer)
        {
            isCalculatingTactics = true;
            AILog.Minimax("Walka: liczę najlepszą pozycję (Minimax + odcięcie gałęzi).");
            StartCoroutine(tacticalBrain.GetOptimalCombatPositionCoroutine(
                playerTarget, transform.position, transform.forward, pos =>
                {
                    currentTacticalPosition = pos;
                    isCalculatingTactics = false;
                    tacticalRecalcTimer = Time.time + 1.5f;
                }));
        }

        if (currentTacticalPosition != Vector3.zero)
            MoveTowards(currentTacticalPosition, true, stopDistance);
        else if (playerTarget != null)
            MoveTowards(playerTarget.position, true, stopDistance);

        float projSpeed = GetAverageProjectileSpeed();
        Vector3 leadPosition = tacticalBrain.CalculateLeadingPosition(playerTarget, transform.position, projSpeed);
        UpdateTurretLeading(leadPosition);
    }

    private void FleeLogic()
    {
        Vector3 fleeDirection = playerTarget != null
            ? (transform.position - playerTarget.position).normalized
            : -transform.forward;

        MoveTowards(transform.position + fleeDirection * 150f, false, patrolStopDistance);
    }

    private void EnforceSectorBounds()
    {
        if (ChunkManager.Instance == null) return;
        if (ChunkManager.Instance.IsInsideSector(transform.position, null, 5f)) return;

        DespawnOutOfSector();
    }

    public void DespawnOutOfSector()
    {
        AILog.Theory("Sektor", "Wyleciałem poza mapę sektora — usuwam się, spawner postawi nowego.");
        EventBus.TriggerOnEnemyDeath(this);
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterEnemy(this);
        Destroy(gameObject);
    }

    private void MoveTowards(Vector3 targetPos, bool broadsideToTarget, float haltDistance)
    {
        Vector3 direction = (targetPos - transform.position).normalized;

        if (avoidance != null)
            direction = avoidance.GetModifiedDirection(direction);

        if (broadsideToTarget && playerTarget != null)
        {
            Vector3 toPlayer = (playerTarget.position - transform.position).normalized;
            Vector3 broadsideForward = Vector3.Cross(Vector3.up, toPlayer).normalized;
            if (broadsideForward.sqrMagnitude < 0.01f)
                broadsideForward = transform.forward;

            direction = Vector3.Slerp(direction, broadsideForward, 0.65f).normalized;
        }

        Quaternion targetLookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetLookRotation, Time.fixedDeltaTime * rotationSpeed);

        float distance = Vector3.Distance(transform.position, targetPos);
        if (distance > haltDistance)
        {
            currentAccelerationTime += Time.fixedDeltaTime;
            float thrustMultiplier = accelerationCurve.Evaluate(Mathf.Clamp01(currentAccelerationTime / 2f));
            rb.AddRelativeForce(Vector3.forward * mainThrust * thrustMultiplier);
        }
        else
        {
            currentAccelerationTime = 0f;
        }

        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        rb.AddRelativeForce(
            new Vector3(-localVelocity.x * antiDriftFactor, -localVelocity.y * antiDriftFactor, 0f),
            ForceMode.Acceleration);
    }

    private float GetAverageProjectileSpeed()
    {
        if (sideTurrets == null || sideTurrets.Length == 0) return 40f;

        float sum = 0f;
        int count = 0;
        foreach (Turret t in sideTurrets)
        {
            if (t == null) continue;
            sum += t.GetProjectileSpeed();
            count++;
        }
        return count > 0 ? sum / count : 40f;
    }

    private void UpdateTurretLeading(Vector3 leadPosition)
    {
        if (sideTurrets == null) return;
        foreach (Turret turret in sideTurrets)
        {
            if (turret != null)
                turret.SetAimPoint(leadPosition);
        }
    }

    public void ApplyArchetype(AIArchetype archetype)
    {
        if (archetype == null) return;

        if (stats == null) stats = GetComponent<ShipStats>();
        if (stats != null && stats.CurrentHP <= 0f && stats.GetMaxHP() > 0f)
            stats.SetHP(stats.GetMaxHP());

        rb.isKinematic = false;
        if (stats != null)
        {
            stats.SetMaxHP(archetype.maxHealth);
            stats.SetHP(archetype.maxHealth);
        }

        mainThrust = 280000f * Mathf.Max(0.5f, archetype.speed);
        rotationSpeed = 1.8f * Mathf.Max(0.5f, archetype.speed);
        maxFlightSpeed = 35f * Mathf.Max(0.5f, archetype.speed);
    }

    public void Die()
    {
        EventBus.TriggerOnEnemyDeath(this);
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterEnemy(this);

        for (int i = 0; i < 5; i++)
        {
            GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.transform.position = transform.position + Random.insideUnitSphere * 2f;
            debris.transform.localScale = Vector3.one * Random.Range(0.5f, 2f);
            Rigidbody drb = debris.AddComponent<Rigidbody>();
            drb.useGravity = false;
            drb.AddExplosionForce(500f, transform.position, 10f);
            Destroy(debris, 5f);
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterEnemy(this);
    }
}
