using UnityEngine;

// Ogólny szablon dla systemów uzbrojenia, obsługujący m.in. chłodzenie (cooldown) po wystrzale i zarządzenie amunicją.
public abstract class WeaponBase : MonoBehaviour
{
    public float fireRate = 1f;
    protected float nextFireTime = 0f;
    // Wymusza implementacje logiki oddania strzalu w klasach dziedziczacych
    public abstract void Fire();
}