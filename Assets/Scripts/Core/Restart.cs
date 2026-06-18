using UnityEngine;

// Mechanizm przywracający stan gry do czystej karty, przeładowujący scenę bez ponownego ładowania zasobów.
public class Restart : MonoBehaviour
{
    private ShipStats shipStats;

    // Cofa parametry statku gracza do wartosci poczatkowych i teleportuje go z powrotem do punktu startowego na srodku sceny.
    public void RestartMethod()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            shipStats = player.GetComponent<ShipStats>();
            if (shipStats != null) shipStats.ResetData();

            player.transform.position = Vector3.zero;
            player.transform.rotation = Quaternion.identity;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}