using UnityEngine;

public class AsteroidExplosion : MonoBehaviour
{
    public float explosionForce = 10f;
    public float explosionRadius = 5f;
    public float destroyTime = 5f;

    void Start()
    {
        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in bodies)
        {
            Vector3 dir = Random.onUnitSphere;
            rb.AddForce(dir * Random.Range(4f, 10f), ForceMode.Impulse);
        }

        Destroy(gameObject, destroyTime);
    }
}