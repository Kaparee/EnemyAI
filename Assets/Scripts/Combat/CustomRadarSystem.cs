using UnityEngine;

public class CustomRadarSystem : MonoBehaviour
{
    public float detectionRadius = 500f;
    public Transform detectedPlayer;
    public Turret[] turretsToControl;

    private void Update()
    {
        ScanForPlayerManually();

        foreach (Turret turret in turretsToControl)
        {
            turret.target = detectedPlayer;
        }
    }

    private void ScanForPlayerManually()
    {
        detectedPlayer = null;
        float detectionRadiusSqr = detectionRadius * detectionRadius;
        float closestDistanceSqr = float.MaxValue;

        foreach (PlayerMarker player in PlayerMarker.AllPlayers)
        {
            float dx = player.transform.position.x - transform.position.x;
            float dy = player.transform.position.y - transform.position.y;
            float dz = player.transform.position.z - transform.position.z;

            float distanceSqr = (dx * dx) + (dy * dy) + (dz * dz);

            if (distanceSqr <= detectionRadiusSqr && distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                detectedPlayer = player.transform;
            }
        }
    }
}