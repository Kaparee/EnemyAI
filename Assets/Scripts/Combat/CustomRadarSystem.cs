using UnityEngine;

public class CustomRadarSystem : MonoBehaviour
{
    public float detectionRadius = 500f;
    public Transform detectedPlayer;
    public Turret[] turretsToControl;

    private float scanInterval = 0.5f;
    private float nextScanTime;

    private void Update()
    {
        if (Time.time >= nextScanTime)
        {
            ScanForPlayerManually();
            nextScanTime = Time.time + scanInterval;
        }
    }

    private void ScanForPlayerManually()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, LayerMask.GetMask("Player"));
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = hit.transform;
            }
        }

        if (closest != null && detectedPlayer != closest)
        {
            detectedPlayer = closest;
            EventBus.OnPlayerDetected?.Invoke(detectedPlayer);
        }
        else if (closest == null)
        {
            detectedPlayer = null;
        }

        foreach (Turret turret in turretsToControl)
        {
            turret.target = detectedPlayer;
        }
    }
}