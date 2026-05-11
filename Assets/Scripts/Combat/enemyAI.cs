using UnityEngine;

public class enemyAI : MonoBehaviour
{
    public enum EnemyState { Idle, Chase, Fight }

    [Header("Stan AI")]
    public EnemyState currentState = EnemyState.Idle;

    [Header("Parametry Fizyczne (bazowane na Excelu)")]
    public float mass = 40000f;          // kg
    public float mainThrust = 600000f;   // N (Siła ciągu)
    public float rotationSpeed = 2.5f;   // Prędkość obrotu
    public float linearDrag = 0.5f;      // Opór liniowy
    public float angularDrag = 1.2f;     // Opór obrotu

    [Header("Logika AI")]
    public Transform playerTarget;
    public float detectionRange = 500f;  // Duży zasięg w kosmosie
    public float attackRange = 100f;     // Dystans otwarcia ognia
    public float stopDistance = 30f;     // Dystans hamowania

    private Rigidbody rb;
    private float nextAttackTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.mass = mass;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;
        rb.useGravity = false;

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }
    }

    void FixedUpdate()
    {
        if (playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // Maszyna Stanów
        switch (currentState)
        {
            case EnemyState.Idle:
                // Przejście do stanu Chase jeśli gracz wejdzie w zasięg
                if (distance < detectionRange)
                {
                    currentState = EnemyState.Chase;
                }
                break;

            case EnemyState.Chase:
                FaceTarget();
                MoveToTarget(distance);

                // Przejścia ze stanu Chase
                if (distance < attackRange)
                {
                    currentState = EnemyState.Fight;
                }
                else if (distance >= detectionRange)
                {
                    // Gracz uciekł z zasięgu wracamy do patrolowania
                    currentState = EnemyState.Idle;
                }
                break;

            case EnemyState.Fight:
                FaceTarget(); // W trakcie walki też patrzymy na gracza
                MoveToTarget(distance); // utrzymywać optymalny dystans/manewrować
                TryAttack();

                // Przejścia ze stanu Fight
                if (distance >= attackRange)
                {
                    // Gracz odleciał za daleko na strzał wracamy do pościgu
                    currentState = EnemyState.Chase;
                }
                break;
        }
    }

    void FaceTarget()
    {
        Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);
    }

    void MoveToTarget(float distance)
    {
        float angleToPlayer = Vector3.Angle(transform.forward, playerTarget.position - transform.position);

        if (distance > stopDistance && angleToPlayer < 20f)
        {
            rb.AddRelativeForce(Vector3.forward * mainThrust);
        }
        else if (distance <= stopDistance)
        {
            rb.AddRelativeForce(Vector3.back * (mainThrust * 0.5f));
        }
    }

    void TryAttack()
    {
        if (Time.time > nextAttackTime)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, attackRange))
            {
                if (hit.transform == playerTarget)
                {
                    Debug.Log("Pew Pew! Strzał laserem w gracza.");
                    nextAttackTime = Time.time + 1.0f;
                }
            }
        }
    }
}