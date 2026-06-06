using UnityEngine;

public class Restart : MonoBehaviour
{
    private ShipStats shipStats;

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