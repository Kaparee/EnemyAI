using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
// Klasa nadrzędna dla wszelkiego rodzaju amunicji. Definiuje zachowanie fizyczne wektorów prędkości oraz logikę kolizji.
public class Projectile : MonoBehaviour
{
    public float muzzleVelocity = 100f;
    public float baseDamage = 50f;

    // Nadaje poczatkowa predkosc pociskowi i ustawia jego automatyczne zniszczenie po czasie
    private void Start()
    {
        GetComponent<Rigidbody>().linearVelocity = transform.forward * muzzleVelocity;
        Destroy(gameObject, 5f);
    }

    // Rozpatruje kat trafienia i oblicza ostateczne obrazenia przekazywane do statystyk statku
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount == 0)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 vel = collision.relativeVelocity;
        Vector3 norm = collision.GetContact(0).normal;

        float magnitude = Mathf.Sqrt((vel.x * vel.x) + (vel.y * vel.y) + (vel.z * vel.z));
        float nx = 0, ny = 0, nz = 0;

        if (magnitude > 0.0001f)
        {
            nx = vel.x / magnitude;
            ny = vel.y / magnitude;
            nz = vel.z / magnitude;
        }

        float dotProduct = (nx * norm.x) + (ny * norm.y) + (nz * norm.z);
        float impactFactor = dotProduct < 0 ? -dotProduct : dotProduct;
        float finalDamage = baseDamage * impactFactor;

        ShipStats stats = collision.collider.GetComponentInParent<ShipStats>();
        if (stats != null && collision.contactCount > 0)
        {
            stats.TakeZonedDamage(finalDamage, collision.GetContact(0).normal);
        }

        Destroy(gameObject);
    }
}