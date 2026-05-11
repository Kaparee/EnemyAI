using UnityEngine;

public class HeavyKineticLauncher : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float cooldown = 2.5f;

    private float lastShot = -99f;

    public void TryFire()
    {
        if (Time.time - lastShot < cooldown) return;
        lastShot = Time.time;

        var go = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
        go.GetComponent<HeavyKineticProjectile>().Launch(muzzle.forward);
    }
}