using UnityEngine;

public class HeavyKineticLauncher : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float cooldown = 2.5f;
    [SerializeField] private float playerProjectileDamage = 30f;

    private float lastShot = -99f;
    private Rigidbody parentRb;

    public Vector3 MuzzlePosition => muzzle != null ? muzzle.position : transform.position;
    public float Cooldown => cooldown;

    void Start()
    {
        parentRb = GetComponentInParent<Rigidbody>();
        if (muzzle == null)
            muzzle = transform.Find("WeaponMuzzle");
    }

    public float GetProjectileSpeed()
    {
        if (projectilePrefab != null)
        {
            var proj = projectilePrefab.GetComponent<HeavyKineticProjectile>();
            if (proj != null) return proj.GetInitialSpeed();
        }
        return 40f;
    }

    public void TryFire()
    {
        if (Time.time - lastShot < cooldown) return;

        Transform ownerTransform = parentRb != null ? parentRb.transform : transform;
        ShipStats stats = ownerTransform.GetComponent<ShipStats>();
        if (stats != null && stats.IsDestroyed) return;

        Vector3 spawnPos = MuzzlePosition;
        Vector3 shootDirection = muzzle != null ? muzzle.forward : transform.forward;

        if (ownerTransform.CompareTag("Player"))
        {
            shootDirection = PlayerWeaponAim.GetDirection(spawnPos, ownerTransform);
            
            float angleDown = Vector3.Angle(shootDirection, -ownerTransform.up);
            if (angleDown < 45f)
                return;
        }

        lastShot = Time.time;

        Vector3 shooterVelocity = parentRb != null ? parentRb.linearVelocity : Vector3.zero;

        var go = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(shootDirection));
        go.GetComponent<HeavyKineticProjectile>().Launch(
            shootDirection,
            shooterVelocity,
            ownerTransform,
            playerProjectileDamage);

        Collider projCollider = go.GetComponent<Collider>();
        if (projCollider == null) return;

        foreach (Collider c in ownerTransform.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(projCollider, c);
    }
}
