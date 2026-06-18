using UnityEngine;

// Zarządza efektami cząsteczkowymi i generowaniem mniejszych odłamków skał po destrukcji dużej asteroidy.
public class AsteroidExplosion : MonoBehaviour
{
    public float explosionForce = 10f;
    public float explosionRadius = 5f;
    public float destroyTime = 5f;

    // Aplikuje losowa sile i kierunek do wszystkich elementow fizycznych odlamkow oraz ustawia czas calkowitego zniszczenia obiektu.
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