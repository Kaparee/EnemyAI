using UnityEngine;

public class Turret : WeaponBase
{
    public Transform turretHead;
    public float rotationSpeed = 2f;
    public Transform target;

    [Header("Ograniczenia Kątowe")]
    public float minYaw = -90f;
    public float maxYaw = 90f;

    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileDamage = 13f;

    [Header("Celność i Balans")]
    [SerializeField] private float accuracySpread = 3f;

    private Vector3? aimPointOverride;
    private Rigidbody parentRb;

    void Start()
    {
        parentRb = GetComponentInParent<Rigidbody>();
        if (turretHead == null) turretHead = transform;
        if (firePoint == null) firePoint = turretHead;

        if (!CompareTag("Player") && (transform.parent == null || !transform.parent.CompareTag("Player")))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                ShipStats pStats = player.GetComponent<ShipStats>();
                if (pStats != null) projectileDamage = pStats.GetMaxHP() / 6f;

                HeavyKineticLauncher pLauncher = player.GetComponentInChildren<HeavyKineticLauncher>();
                if (pLauncher != null && pLauncher.Cooldown > 0f)
                {
                    fireRate = (1f / pLauncher.Cooldown) * 0.85f;
                }
            }
        }
    }
    public void SetAimPoint(Vector3 worldPoint)
    {
        aimPointOverride = worldPoint;
    }

    public float GetProjectileSpeed()
    {
        if (projectilePrefab == null) return 40f;
        var kinetic = projectilePrefab.GetComponent<HeavyKineticProjectile>();
        if (kinetic != null) return kinetic.GetInitialSpeed();
        var basic = projectilePrefab.GetComponent<Projectile>();
        if (basic != null) return basic.muzzleVelocity;
        return 40f;
    }

    private void Update()
    {
        AimAtTargetManually();

        if (HasValidTarget())
        {
            if (Time.time >= nextFireTime)
            {
                Fire();
                nextFireTime = Time.time + (1f / fireRate) + Random.Range(0f, 0.15f);
            }
        }
        else
        {
            nextFireTime = Time.time + 0.5f; 
        }
    }

    private bool HasValidTarget()
    {
        return target != null || aimPointOverride.HasValue;
    }

    private void AimAtTargetManually()
    {
        Vector3 aimPoint = aimPointOverride ?? (target != null ? target.position : transform.position + transform.forward);
        if (turretHead == null) return;

        Vector3 localTargetPos = transform.InverseTransformPoint(aimPoint);
        float targetYaw = Mathf.Atan2(localTargetPos.x, localTargetPos.z) * Mathf.Rad2Deg;
        targetYaw = Mathf.Clamp(targetYaw, minYaw, maxYaw);

        Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f);
        turretHead.localRotation = Quaternion.RotateTowards(
            turretHead.localRotation,
            targetRotation,
            rotationSpeed * 50f * Time.deltaTime);
    }

    public override void Fire()
    {
        if (projectilePrefab == null || firePoint == null) return;

        Vector3 aimPoint = aimPointOverride ?? (target != null ? target.position : firePoint.position + firePoint.forward * 100f);
        Vector3 shootDirection = (aimPoint - firePoint.position).normalized;

        Vector3 randomSpread = Random.insideUnitSphere * Mathf.Tan(accuracySpread * Mathf.Deg2Rad);
        shootDirection = (shootDirection + randomSpread).normalized;

        GameObject projGo = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(shootDirection));

        Vector3 shooterVelocity = parentRb != null ? parentRb.linearVelocity : Vector3.zero;
        var kinetic = projGo.GetComponent<HeavyKineticProjectile>();
        if (kinetic != null)
        {
            kinetic.Launch(
                shootDirection,
                shooterVelocity,
                parentRb != null ? parentRb.transform : transform,
                projectileDamage);
        }
        else
        {
            Rigidbody projRb = projGo.GetComponent<Rigidbody>();
            if (projRb != null)
                projRb.linearVelocity = shooterVelocity + shootDirection * GetProjectileSpeed();
        }

        Collider projCollider = projGo.GetComponent<Collider>();
        if (projCollider != null && parentRb != null)
        {
            foreach (Collider c in parentRb.GetComponentsInParent<Collider>())
                Physics.IgnoreCollision(projCollider, c);
        }
    }
}
