using UnityEngine;

// Wykrywa i przelicza trafienia obiektów, odejmując im punkty zdrowia przy kolizjach lub postrzałach.
public class DamageCollision : MonoBehaviour
{
    [SerializeField] private float damageMultiplier = 1.0f;

    // Aplikuje obrazenia obiektowi bazujac na sile zderzenia fizycznego miedzy cialami
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > 5f)
        {
            float damage = collision.relativeVelocity.magnitude * damageMultiplier;
            
            ShipStats stats = GetComponent<ShipStats>();
            if (stats != null)
            {
                stats.TakeDamage(damage);
            }

            if (collision.gameObject.CompareTag("Player"))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameOver();
                }
                else
                {
                    Debug.LogWarning("DamageCollision: Brak GameManager'a w scenie!");
                }
            }
        }
    }
}
