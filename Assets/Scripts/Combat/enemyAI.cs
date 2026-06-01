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
    public AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float antiDriftFactor = 2f;
    private float currentAccelerationTime = 0f;

    [Header("Logika AI")]
    public Transform playerTarget;
    public float detectionRange = 500f;
    public float combatRange = 150f;
    public float fleeHealthThreshold = 0.2f; // 20% HP
    public float stopDistance = 50f;

    [Header("Statystyki")]
    public float maxHealth = 500f;
    public float currentHealth = 500f;

    [Header("Patrol Settings")]
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
        
        if (avoidance != null)
        {
            direction = avoidance.GetModifiedDirection(direction);
        }

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);

        float distance = Vector3.Distance(transform.position, targetPos);
        if (distance > stopDistance)
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
        float driftX = localVelocity.x;
        float driftY = localVelocity.y;
        rb.AddRelativeForce(new Vector3(-driftX * antiDriftFactor, -driftY * antiDriftFactor, 0f), ForceMode.Acceleration);
    }

    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed * 2f);
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            EventBus.OnEnemyDeath?.Invoke(this);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UnregisterEnemy(this);
            }
            
            for (int i = 0; i < 5; i++)
            {
                GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debris.transform.position = transform.position + Random.insideUnitSphere * 2f;
                debris.transform.localScale = Vector3.one * Random.Range(0.5f, 2f);
                Rigidbody drb = debris.AddComponent<Rigidbody>();
                drb.AddExplosionForce(500f, transform.position, 10f);
                Destroy(debris, 5f);
            }
            
            Destroy(gameObject);
        }
    }
}
