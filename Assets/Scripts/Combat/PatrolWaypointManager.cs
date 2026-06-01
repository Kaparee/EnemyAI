using System.Collections.Generic;
using UnityEngine;

public class PatrolWaypointManager : MonoBehaviour
{
    public Vector3 sectorCenter = Vector3.zero;
    public float sectorRadius = 160f;
    public float sectorHalfHeight = 90f;
    public int waypointCount = 7;
    public float minWaypointSeparation = 85f;
    public LayerMask obstacleMask;

    private List<Vector3> waypoints = new List<Vector3>();

    void Start()
    {
        RefreshFromChunkManager();
    }

    public void RefreshFromChunkManager()
    {
        if (ChunkManager.Instance != null)
        {
            sectorCenter = ChunkManager.Instance.GetSectorWorldCenter();
            float half = ChunkManager.Instance.GetSectorHalfExtent();
            sectorRadius = half * 0.88f;
            sectorHalfHeight = half * 0.42f;
            minWaypointSeparation = half * 0.38f;
        }

        if (obstacleMask.value == 0)
            obstacleMask = AILayers.AsteroidMask;

        transform.position = sectorCenter;
        GenerateWaypoints();
    }

    public void GenerateWaypoints()
    {
        waypoints.Clear();
        int maxAttempts = waypointCount * 80;
        int attempts = 0;

        AILog.Patrol(
            $"Losuję {waypointCount} punktów w całym sektorze 3D " +
            $"(promień ~{sectorRadius:F0}, wysokość ±{sectorHalfHeight:F0}, min. odstęp {minWaypointSeparation:F0}).");

        while (waypoints.Count < waypointCount && attempts < maxAttempts)
        {
            attempts++;
            Vector3 candidate = sectorCenter + new Vector3(
                Random.Range(-sectorRadius, sectorRadius),
                Random.Range(-sectorHalfHeight, sectorHalfHeight),
                Random.Range(-sectorRadius, sectorRadius));

            if (ChunkManager.Instance != null && !ChunkManager.Instance.IsInsideSector(candidate, null, 15f))
                continue;

            if (!IsFarEnoughFromOthers(candidate))
                continue;

            if (IsBlocked(candidate))
                continue;

            waypoints.Add(candidate);
        }

        while (waypoints.Count < waypointCount)
        {
            Vector3 fallback = PickFallbackPoint(waypoints.Count);
            waypoints.Add(fallback);
        }

        AILog.Patrol($"Gotowe — {waypoints.Count} punktów rozrzuconych po sektorze (A* będzie między nimi latał).");
    }

    private Vector3 PickFallbackPoint(int index)
    {
        float golden = index * 2.399963f;
        float r = sectorRadius * (0.45f + (index % 3) * 0.15f);
        return sectorCenter + new Vector3(
            Mathf.Cos(golden) * r,
            Random.Range(-sectorHalfHeight, sectorHalfHeight),
            Mathf.Sin(golden) * r);
    }

    private bool IsFarEnoughFromOthers(Vector3 candidate)
    {
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (Vector3.Distance(candidate, waypoints[i]) < minWaypointSeparation)
                return false;
        }
        return true;
    }

    private bool IsBlocked(Vector3 pos)
    {
        if (obstacleMask.value != 0 && Physics.CheckSphere(pos, 20f, obstacleMask))
            return true;

        return ObstacleRegistry.Instance != null && ObstacleRegistry.Instance.IsPositionBlocked(pos, 20f);
    }

    public List<Vector3> GetWaypoints() => new List<Vector3>(waypoints);
}
