using UnityEngine;

// Skrypt obsługujący zachowanie, fizykę i zadawane obrażenia ciężkich pocisków kinetycznych po wystrzeleniu.
public class HeavyKineticProjectile : MonoBehaviour
{
    [Header("FIZYKA")]
    [SerializeField] private float initialSpeed = 40f;
    [SerializeField] private float dragInSpace = 0.05f;
    [SerializeField] private float selfDestructTimeout = 6f;
    [SerializeField] private float damage = 120f;

    private Rigidbody rb;
    private float timer;
    private bool hitTarget;
    private Transform ownerRoot;
    private float activeDamage;

    // Inicjalizuje parametry fizyczne oraz wizualne pocisku przy powstaniu obiektu
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = dragInSpace;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.mass = 80f;

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.center = Vector3.zero;
            capsule.radius = 0.25f;
            capsule.height = 1.2f;
            capsule.direction = 2;
        }

        activeDamage = damage;

        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.material = new Material(Shader.Find("Sprites/Default"));
            
            trail.startColor = new Color(1f, 0.6f, 0.1f, 1.0f);
            trail.endColor = new Color(1f, 0.2f, 0f, 0.0f);
            
            trail.startWidth = 0.3f;
            trail.endWidth = 0.05f;
            trail.time = 0.4f; 
        }
    }

    // Zwraca zdefiniowana predkosc poczatkowa wlasnego lotu
    public float GetInitialSpeed() => initialSpeed;

    // Zapisuje informacje o strzelajacym i dodaje sily inicjujace ruch pocisku
    public void Launch(Vector3 direction, Vector3 shooterVelocity = default, Transform owner = null, float damageOverride = -1f)
    {
        ownerRoot = owner != null ? owner.root : null;
        activeDamage = damageOverride > 0f ? damageOverride : damage;
        rb.linearVelocity = shooterVelocity;
        rb.AddForce(direction.normalized * initialSpeed, ForceMode.VelocityChange);
    }

    // Przeliczanie fizyki. Tutaj aplikowane są wszystkie siły, żeby uniknąć jitteringu.

    // Przelicza fizyke obrotu wzgledem wektora predkosci oraz niszczy obiekt po czasie
    void FixedUpdate()
    {
        rb.linearDamping = dragInSpace;
        timer += Time.fixedDeltaTime;

        if (rb.linearVelocity.sqrMagnitude > 0.1f)
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);

        if (timer >= selfDestructTimeout && !hitTarget)
            Destroy(gameObject);
    }

    // Oblicza sily zderzenia i zadaje obrazenia przy kontakcie z cialem fizycznym
    void OnCollisionEnter(Collision col)
    {
        if (ownerRoot != null && col.transform.root == ownerRoot)
            return;

        hitTarget = true;

        ShipStats stats = col.collider.GetComponentInParent<ShipStats>();
        if (stats != null && col.contactCount > 0)
        {
            stats.TakeZonedDamage(activeDamage, col.GetContact(0).normal);
            Destroy(gameObject);
            return;
        }

        if (col.rigidbody != null && col.contactCount > 0)
            col.rigidbody.AddForceAtPosition(rb.linearVelocity * 100f, col.GetContact(0).point, ForceMode.Impulse);

        Destroy(gameObject);
    }
}
