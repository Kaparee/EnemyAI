using UnityEngine;
using FMOD.Studio;
using FMODUnity;

public class DamageCollision : MonoBehaviour
{
    [Header("--- KALIBRACJA ZDERZEŃ ---")]
    private float damageThreshold = 5000f;
    public float damageMultiplier = 0.005f;

    [Header("--- EFEKTY ---")]
    public ParticleSystem impactParticles;
    [SerializeField] private EventReference hitSfx;

    private ShipStats shipStats;


    private void Start()
    {
        shipStats = GetComponent<ShipStats>();
        if (shipStats == null)
            shipStats = GetComponentInParent<ShipStats>();

        if (shipStats == null)
            Debug.LogError("Brak ShipStats na statku lub jego rodzicu!");
    }

    private void OnCollisionEnter(Collision collision)
    {
        RuntimeManager.PlayOneShot(hitSfx, collision.collider.transform.position);

        float impactForce = collision.impulse.magnitude;
        if (impactForce < damageThreshold) return;

        float damage = (impactForce - damageThreshold) * damageMultiplier;
        damage = Mathf.Max(0f, damage);

        // <<< INTEGRACJA Z SHIPSTATS >>>
        if (shipStats != null)
        {
            Debug.Log("Damage z DamageCollision.cs" + damage);
            shipStats.TakeDamage(damage);
        }

        // Debug + efekty
        Debug.Log($"<color=red>KOLIZJA!</color> {collision.gameObject.name} | Siła: {impactForce:F0} | Obrażenia: {damage:F1}");

        if (impactParticles != null)
        {
            impactParticles.transform.position = collision.contacts[0].point;
            impactParticles.Play();
        }

        if (shipStats.IsDestroyed)
        {
            Debug.Log("<color=red>STATEK ZNISZCZONY!</color>");

            GameManager.Instance.TriggerGameOver();
        }
    }
}