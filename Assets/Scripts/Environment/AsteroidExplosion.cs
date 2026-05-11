using UnityEngine;
using FMOD.Studio;
using FMODUnity;

public class AsteroidExplosion : MonoBehaviour
{
    public float explosionForce = 10f;
    public float explosionRadius = 5f;
    public float destroyTime = 5f;

    //sfx
    [SerializeField] private EventReference asteroidExplosionSfx;

    void Start()
    {
        //play sfx
        RuntimeManager.PlayOneShot(asteroidExplosionSfx, transform.position);


        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in bodies)
        {
            Vector3 dir = Random.onUnitSphere;

            rb.AddForce(dir * Random.Range(4f,10f), ForceMode.Impulse);
        }

        Destroy(gameObject, destroyTime);
    }
}