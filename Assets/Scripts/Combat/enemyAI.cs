using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
// Główny moduł sterujący zachowaniem przeciwników w oparciu o maszynę stanów (FSM).
// Oddzieliłem logikę decyzyjną od czystego poruszania się, żeby łatwiej było dodawać nowe stany
// takie jak patrolowanie czy ucieczka. Korzysta z TacticalBrain do oceny sytuacji.
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Idle, Patrol, Chase, Combat, Flee }

    [Header("Stan AI")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Parametry Fizyczne")]
    public float mass = 40000f;
    public float mainThrust = 1000000f; 
    public float rotationSpeed = 1.2f; // Znacznie wolniejszy, naturalny obrót
    public float linearDrag = 0.1f;
    public float maxFlightSpeed = 70f;
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

    [Header("Uzbrojenie główne")]
    public Turret mainTurret;

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
    
    private float dodgeCooldownTimer = 0f;
    private Vector3 currentDodgeVector = Vector3.zero;

    // Przeszukuje strukture obiektu by przypisac kluczowe skrypty logiki i fizyki do pol wewnetrznych
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

        Turret[] allTurrets = GetComponentsInChildren<Turret>();
        if (allTurrets.Length > 0)
        {
            mainTurret = allTurrets[0];
            for (int i = 1; i < allTurrets.Length; i++)
            {
                allTurrets[i].gameObject.SetActive(false);
                Destroy(allTurrets[i].gameObject);
            }
        }

        radar.BindTurrets(mainTurret != null ? new Turret[] { mainTurret } : new Turret[0]);

        if (stats != null && stats.GetMaxHP() > 0f && stats.CurrentHP <= 0f)
            stats.SetHP(stats.GetMaxHP());
    }

    // Konfiguruje wlasciwosci fizyczne oraz statystyki bazujac na danych pobranych z obiektu gracza
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

        if (playerTarget != null)
        {
            ShipStats pStats = playerTarget.GetComponent<ShipStats>();
            ShipController pCtrl = playerTarget.GetComponent<ShipController>();

            if (pStats != null && stats != null)
            {
                stats.SetMaxHP(pStats.GetMaxHP() * 0.85f);
                stats.SetHP(stats.GetMaxHP());
                
                mainThrust = pStats.MaxMainThrust * 0.85f;
                stats.ManeuverForce = pStats.ManeuverForce * 0.85f;
                stats.RollForce = pStats.RollForce * 0.85f;
            }

            if (pCtrl != null)
            {
                maxFlightSpeed = pCtrl.maxOverallSpeed * 0.85f;
            }
        }

        if (patrolWaypoints.Count == 0)
            AssignPatrolWaypoints();

        if (currentState == EnemyState.Idle)
            currentState = EnemyState.Patrol;

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterEnemy(this);

        AILog.FSM($"Start. Stan: {currentState}. Punktów patrolowych: {patrolWaypoints.Count}.");
    }

    // Podlacza funkcje reakcji na wykrycie gracza do globalnego systemu zdarzen
    void OnEnable()
    {
        EventBus.OnPlayerDetected += OnPlayerDetected;
    }

    // Odlacza funkcje reakcji na gracza od systemu zdarzen przed usunieciem obiektu
    void OnDisable()
    {
        EventBus.OnPlayerDetected -= OnPlayerDetected;
    }

    // Pobiera od globalnego menedzera liste przygotowanych punktow lub generuje tymczasowe jesli brakuje
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

    // Tworzy awaryjna liste punktow patrolowych w obrebie sektora uzywajac wbudowanego generatora sfery
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

    // Rejestruje namierzonego gracza jako aktualny cel i wymusza wejscie statku w tryb walki
    private void OnPlayerDetected(Transform player)
    {
        playerTarget = player;
        if (currentState == EnemyState.Idle || currentState == EnemyState.Patrol || currentState == EnemyState.Chase)
        {
            AILog.FSM("Widzę gracza — przechodzę z patrolowania na walkę.");
            currentState = EnemyState.Combat;
        }
    }

    // Przeliczanie fizyki. Tutaj aplikowane są wszystkie siły, żeby uniknąć jitteringu.

    // Wykonuje cykliczne i stabilne przeliczanie fizyki ruchu oraz zadan wynikajacych z maszyny stanow
    void FixedUpdate()
    {
        UpdateState();
        HandleEvasion();
        ExecuteState();
        ClampFlightSpeed();
        EnforceSectorBounds();
    }

    // Ogranicza aktualna predkosc lotu do bezpiecznego maksimum by zapobiec zjawisku przenikania obiektow
    private void ClampFlightSpeed()
    {
        if (maxFlightSpeed <= 0f) return;
        if (rb.linearVelocity.sqrMagnitude > maxFlightSpeed * maxFlightSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxFlightSpeed;
    }

    // Wykrywa nadlatujace pociski i wykonuje losowe uniki ratunkowe oparte na weryfikacji kierunku wektora
    private void HandleEvasion()
    {
        if (dodgeCooldownTimer > 0f)
            dodgeCooldownTimer -= Time.fixedDeltaTime;

        if (dodgeCooldownTimer > 1.5f) return;

        Collider[] nearby = Physics.OverlapSphere(transform.position, 80f);
        foreach (var col in nearby)
        {
            HeavyKineticProjectile proj = col.GetComponent<HeavyKineticProjectile>();
            if (proj != null)
            {
                Rigidbody projRb = proj.GetComponent<Rigidbody>();
                if (projRb != null && projRb.linearVelocity.sqrMagnitude > 100f)
                {
                    Vector3 toMe = transform.position - projRb.position;
                    if (Vector3.Dot(projRb.linearVelocity.normalized, toMe.normalized) > 0.97f)
                    {
                        if (Random.value < 0.3f) 
                        {
                            PerformDodge(projRb.linearVelocity);
                        }
                        else
                        {
                            dodgeCooldownTimer = 1.0f; 
                        }
                        return;
                    }
                }
            }
        }
    }

    // Oblicza optymalny kierunek ratunkowy po czym aplikuje natychmiastowa sile odrzutu korygujac tor lotu
    private void PerformDodge(Vector3 threatVelocity)
    {
        Vector3 dodgeDir = Vector3.Cross(Vector3.up, threatVelocity).normalized;
        if (Random.value > 0.5f) dodgeDir = -dodgeDir;
        if (dodgeDir.sqrMagnitude < 0.01f) dodgeDir = transform.right;

        rb.AddForce(dodgeDir * mainThrust * 1.2f, ForceMode.Impulse);

        currentDodgeVector = dodgeDir * 60f;
        dodgeCooldownTimer = 3.0f;
    }

    // Weryfikuje ilosc punktow zdrowia i dynamicznie decyduje o przejsciu pomiedzy trybami walki a ucieczki
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

    // Rejestruje w systemie debugowania unikalne zdarzenia przejsc pomiedzy roznymi stanami decyzyjnymi
    private void LogStateChange()
    {
        if (currentState == lastLoggedState) return;
        lastLoggedState = currentState;
        AILog.FSM($"Zmiana stanu: {currentState}");
    }

    // Uruchamia odpowiedni fragment logiki decyzyjnej bazujac na wylosowanym trybie postepowania
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

    // Odpytuje system znajdowania drogi i przemieszcza statek pomiedzy zaplanowanymi punktami sektora
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

    // Zleca bezposredni poscig w strone ostatnio zapamietanej lokalizacji uciekajacego przeciwnika
    private void ChaseLogic()
    {
        if (playerTarget == null) return;
        MoveTowards(playerTarget.position, false, patrolStopDistance);
    }

    // Konsultuje sie z systemem taktycznym i realizuje skomplikowane manewry zjednoczone ze strzelaniem
    private void CombatLogic()
    {
        if (playerTarget == null)
        {
            currentState = EnemyState.Patrol;
            return;
        }

        ShipStats targetStats = playerTarget.GetComponent<ShipStats>();
        if (targetStats != null && targetStats.IsDestroyed)
        {
            AILog.FSM("Gracz zniszczony. Wracam do patrolowania.");
            playerTarget = null;
            currentState = EnemyState.Patrol;
            return;
        }

        // UWAGA DLA PROJEKTU: Używamy tutaj Twojego własnego algorytmu MINIMAX
        // Minimax (z odcięciem alpha-beta) przewiduje ruchy gracza i wyznacza optymalną pozycję.
        if (!isCalculatingTactics && Time.time >= tacticalRecalcTimer)
        {
            isCalculatingTactics = true;
            AILog.Minimax("Walka: liczę najlepszą pozycję (Minimax + odcięcie gałęzi).");
            StartCoroutine(tacticalBrain.GetOptimalCombatPositionCoroutine(
                playerTarget, transform.position, transform.forward, pos =>
                {
                    currentTacticalPosition = pos;
                    isCalculatingTactics = false;
                    tacticalRecalcTimer = Time.time + 1.2f; // Co 1.2s nowa kalkulacja
                }));
        }

        Vector3 moveDirection;
        if (currentTacticalPosition != Vector3.zero)
        {
            // Sekret płynnego lotu: Minimax wskazuje nam najlepszy punkt w kosmosie, 
            // ale my traktujemy go jako wektor KIERUNKOWY, żeby się nie zatrzymywać!
            moveDirection = (currentTacticalPosition - transform.position).normalized;
        }
        else
        {
            // Zabezpieczenie, jeśli Minimax jeszcze nie policzył pierwszego ruchu
            Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
            moveDirection = Vector3.Cross(Vector3.up, directionToPlayer).normalized;
            if (moveDirection.sqrMagnitude < 0.01f) moveDirection = transform.right;
        }

        currentDodgeVector = Vector3.Lerp(currentDodgeVector, Vector3.zero, Time.fixedDeltaTime * 2f);

        // Statek bez przerwy odpala silniki w kierunku, który doradził Minimax!
        MoveTowards(transform.position + moveDirection * 150f + currentDodgeVector, false, 0f);

        float projSpeed = GetAverageProjectileSpeed();
        Vector3 leadPosition = tacticalBrain.CalculateLeadingPosition(playerTarget, transform.position, projSpeed);
        UpdateTurretLeading(leadPosition);
    }

    // Wyznacza odlegly punkt w wektorze ucieczkowym i aktywuje pelen ciag silnika do oddalenia sie
    private void FleeLogic()
    {
        Vector3 fleeDirection = playerTarget != null
            ? (transform.position - playerTarget.position).normalized
            : -transform.forward;

        MoveTowards(transform.position + fleeDirection * 150f, false, patrolStopDistance);
    }

    // Kontroluje pozycje obiektu zeby wymusic usuniecie jednostek wylatujacych poza aktywne obszary
    private void EnforceSectorBounds()
    {
        if (ChunkManager.Instance == null) return;
        if (ChunkManager.Instance.IsInsideSector(transform.position, null, 5f)) return;

        DespawnOutOfSector();
    }

    // Bezpiecznie niszczy obiekt przekazujac informacje o smierci do pozostalych komponentow gry
    public void DespawnOutOfSector()
    {
        AILog.Theory("Sektor", "Wyleciałem poza mapę sektora — usuwam się, spawner postawi nowego.");
        EventBus.TriggerOnEnemyDeath(this);
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterEnemy(this);
        Destroy(gameObject);
    }

    // Moduluje pozycje i ciag glownego silnika uwzgledniajac detekcje przeszkod i stabilizacje rotacji
    private void MoveTowards(Vector3 targetPos, bool broadsideToTarget, float haltDistance)
    {
        Vector3 direction = (targetPos - transform.position).normalized;

        if (avoidance != null)
            direction = avoidance.GetModifiedDirection(direction);

        Quaternion targetLookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetLookRotation, Time.fixedDeltaTime * rotationSpeed);

        // Nakładamy limity przechyłów (pitch i roll) identyczne jak u gracza, 
        // żeby nie robił fikołków ani beczek w czasie lotu.
        Vector3 euler = transform.localEulerAngles;
        float pitch = euler.x;
        if (pitch > 180f) pitch -= 360f;
        float roll = euler.z;
        if (roll > 180f) roll -= 360f;

        pitch = Mathf.Clamp(pitch, -40f, 40f);
        roll = Mathf.Clamp(roll, -30f, 30f);

        transform.localEulerAngles = new Vector3(pitch, euler.y, roll);

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

    // Sprawdza mozliwosci przypisanej broni bacznie szukajac parametru predkosci by ulatwic celowanie
    private float GetAverageProjectileSpeed()
    {
        if (mainTurret == null) return 40f;
        return mainTurret.GetProjectileSpeed();
    }

    // Przekazuje najnowsza pozycje wyprzedzajaca bezposrednio do systemu pozycjonowania glownej lufy
    private void UpdateTurretLeading(Vector3 leadPosition)
    {
        if (mainTurret != null)
            mainTurret.SetAimPoint(leadPosition);
    }

    // Zmienia wbudowane statystyki i dynamike lotu tak by odpowiadaly wybranemu wariantowi statku
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

    // Wytwarza efekt wizualny rozpadu statku i informuje swiat o zakonczeniu swojego funkcjonowania
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

    // Wyrejestrowuje aktywna jednostke z menedzera chroniac system przed niepotrzebnymi bledami pamieci
    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterEnemy(this);
    }
}
