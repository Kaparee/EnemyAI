using UnityEngine;

public class HeavyKineticProjectile : MonoBehaviour
{
    [Header("FIZYKA")]
    [SerializeField] private float initialSpeed = 40f;
    [SerializeField] private float dragInSpace = 0.05f;
    [SerializeField] private float selfDestructTimeout = 6f;
    [SerializeField] private float damage = 120f;

    private Rigidbody rb;
    private float timer = 0f;
    private bool hitEnemy = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = dragInSpace;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.mass = 80f;
    }

    public void Launch(Vector3 direction)
    {
        rb.AddForce(direction.normalized * initialSpeed, ForceMode.VelocityChange);
    }

    void FixedUpdate()
    {
        rb.linearDamping = dragInSpace;

        timer += Time.fixedDeltaTime;
        if (timer >= selfDestructTimeout && !hitEnemy)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision col)
    {
        //if (col.gameObject.CompareTag("Enemy"))
        //{
        //    hitEnemy = true;
        //    col.gameObject.GetComponent<EnemyHealth>()?.TakeDamage(damage);
        //}
        //Destroy(gameObject);
    }
}