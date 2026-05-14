using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Idle, Patrol, Chase, Combat, Flee }

    [Header("Stan AI")]
    public EnemyState currentState = EnemyState.Idle;

    [Header("Parametry Fizyczne")]
    public float mass = 40000f;
    public float mainThrust = 600000f;
    public float rotationSpeed = 2.5f;
    public float linearDrag = 0.5f;
    public float angularDrag = 1.2f;

    [Header("Logika AI")]
    public Transform playerTarget;
    public float detectionRange = 500f;
    public float combatRange = 150f;
    public float fleeHealthThreshold = 0.2f; // 20% HP
    public float stopDistance = 50f;

    [Header("Statystyki")]
    // TODO: Zastąpić ujednoliconym systemem pancerza i statystyk od Kuby
    public float maxHealth = 500f;
    public float currentHealth = 500f;

    [Header("Patrol Settings")]
    // TODO: Zintegrować z Patrol Waypoint Manager od Kuby 
    public List<Vector3> patrolWaypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    public float waypointThreshold = 20f;

    private Rigidbody rb;
    private HeavyKineticLauncher launcher;
    private ObstacleAvoidance avoidance;
    private TacticalBrain tacticalBrain;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        launcher = GetComponent<HeavyKineticLauncher>();
        avoidance = GetComponent<ObstacleAvoidance>();
        if (avoidance == null) avoidance = gameObject.AddComponent<ObstacleAvoidance>();
        
        tacticalBrain = GetComponent<TacticalBrain>();
        if (tacticalBrain == null) tacticalBrain = gameObject.AddComponent<TacticalBrain>();

        rb.mass = mass;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;
        rb.useGravity = false;

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }

        // Jeśli brak waypointów, stwórz jakieś wokół pozycji startowej
        if (patrolWaypoints.Count == 0)
        {
            for (int i = 0; i < 7; i++)
            {
                patrolWaypoints.Add(transform.position + new Vector3(Random.Range(-200, 200), Random.Range(-50, 50), Random.Range(-200, 200)));
            }
        }
    }

    void FixedUpdate()
    {
        UpdateState();
        ExecuteState();
    }

    private void UpdateState()
    {
        if (playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        float healthPercent = currentHealth / maxHealth;

        // Priorytet 1: Ucieczka
        if (healthPercent < fleeHealthThreshold)
        {
            currentState = EnemyState.Flee;
            return;
        }

        // Priorytet 2: Walka/Pościg
        if (distance < combatRange)
        {
            currentState = EnemyState.Combat;
        }
        else if (distance < detectionRange)
        {
            currentState = EnemyState.Chase;
        }
        else
        {
            currentState = EnemyState.Patrol;
        }
    }

    private void ExecuteState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                // Nic nie rób
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
        // TODO: Połączyć z Manualnym A* dla poruszania się po gridzie
        if (patrolWaypoints.Count == 0) return;

        Vector3 target = patrolWaypoints[currentWaypointIndex];
        float dist = Vector3.Distance(transform.position, target);

        if (dist < waypointThreshold)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Count;
        }

        MoveTowards(target);
    }

    private void ChaseLogic()
    {
        if (playerTarget == null) return;
        MoveTowards(playerTarget.position);
    }

    private void CombatLogic()
    {
        if (playerTarget == null) return;

        // Wybór optymalnego manewru
        Vector3 tacticalPosition = tacticalBrain.GetOptimalCombatPosition(playerTarget, transform.position);
        MoveTowards(tacticalPosition);

        // Celowanie z wyprzedzeniem
        Vector3 leadPosition = tacticalBrain.CalculateLeadingPosition(playerTarget, transform.position, 40f); // 40f to aktualna prędkość pocisku
        FaceTarget(leadPosition);

        if (launcher != null)
        {
            float angle = Vector3.Angle(transform.forward, leadPosition - transform.position);
            if (angle < 10f)
            {
                launcher.TryFire();
            }
        }
    }

    private void FleeLogic()
    {
        if (playerTarget == null) return;

        // Uciekaj w przeciwnym kierunku niż gracz
        Vector3 fleeDirection = (transform.position - playerTarget.position).normalized;
        Vector3 fleeTarget = transform.position + fleeDirection * 100f;
        MoveTowards(fleeTarget);
    }

    private void MoveTowards(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        
        // Uniki reaktywne
        if (avoidance != null)
        {
            direction = avoidance.GetModifiedDirection(direction);
        }

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);

        float distance = Vector3.Distance(transform.position, targetPos);
        if (distance > stopDistance)
        {
            rb.AddRelativeForce(Vector3.forward * mainThrust);
        }
    }

    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed * 2f);
    }

    public void TakeDamage(float amount)
    {
        // TODO: Dodać kalkulację pancerza strefowego od Kuby
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            // Eksplozja itp.
            Destroy(gameObject);
        }
    }
}
