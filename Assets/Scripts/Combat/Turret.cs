using UnityEngine;

public class Turret : WeaponBase
{
    public Transform turretHead;
    public float rotationSpeed = 2f;
    public Transform target;

    public GameObject projectilePrefab;
    public Transform firePoint;

    private void Update()
    {
        AimAtTargetManually();

        if (target != null && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + (1f / fireRate);
        }
    }

    private void AimAtTargetManually()
    {
        if (target == null) return;

        float dx = target.position.x - turretHead.position.x;
        float dz = target.position.z - turretHead.position.z;

        float targetYaw = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
        float currentYaw = turretHead.eulerAngles.y;
        float deltaAngle = targetYaw - currentYaw;

        while (deltaAngle > 180f) deltaAngle -= 360f;
        while (deltaAngle < -180f) deltaAngle += 360f;

        currentYaw += deltaAngle * rotationSpeed * Time.deltaTime;
        turretHead.eulerAngles = new Vector3(0f, currentYaw, 0f);
    }

    public override void Fire()
    {
        if (projectilePrefab != null && firePoint != null)
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
    }
}