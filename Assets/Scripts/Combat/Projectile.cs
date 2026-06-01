using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public float muzzleVelocity = 100f;
    public float baseDamage = 50f;

    private void Start()
    {
        GetComponent<Rigidbody>().linearVelocity = transform.forward * muzzleVelocity;
        Destroy(gameObject, 5f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 vel = collision.relativeVelocity;
        Vector3 norm = collision.contacts[0].normal;

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

        Debug.Log($"Współczynnik kąta: {impactFactor}. Obrażenia: {finalDamage}");
        Destroy(gameObject);
    }
}