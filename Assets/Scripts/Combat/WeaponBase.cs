using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    public float fireRate = 1f;
    protected float nextFireTime = 0f;
    public abstract void Fire();
}