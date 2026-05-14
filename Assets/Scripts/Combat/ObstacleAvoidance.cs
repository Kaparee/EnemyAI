using UnityEngine;

public class ObstacleAvoidance : MonoBehaviour
{
    [Header("Avoidance Settings")]
    [SerializeField] private float detectionDistance = 30f;
    [SerializeField] private float avoidanceForce = 1.5f;
    [SerializeField] private LayerMask obstacleLayer;

    private void Start()
    {
        // Jeśli nie ustawiono warstwy, użyj Default (0) ale odrzuć Player (jeśli ma tag)
        if (obstacleLayer == 0)
        {
            obstacleLayer = ~LayerMask.GetMask("Ignore Raycast", "Player");
        }
    }

    /// <summary>
    /// Modyfikacja kierunku lotu na podstawie 3 raycastów nosowych.
    /// </summary>
    public Vector3 GetModifiedDirection(Vector3 desiredDirection)
    {
        // TODO: Zintegrować z Manualnym A* od Kuby dla korekty ścieżki na gridzie
        Vector3 finalDirection = desiredDirection;
        RaycastHit hit;

        // 3 Raycasty nosowe
        Vector3 forward = transform.forward;
        Vector3 left = (transform.forward - transform.right * 0.5f).normalized;
        Vector3 right = (transform.forward + transform.right * 0.5f).normalized;

        bool hitCenter = Physics.Raycast(transform.position, forward, out hit, detectionDistance, obstacleLayer);
        bool hitLeft = Physics.Raycast(transform.position, left, detectionDistance, obstacleLayer);
        bool hitRight = Physics.Raycast(transform.position, right, detectionDistance, obstacleLayer);

        if (hitCenter || hitLeft || hitRight)
        {
            Vector3 avoidanceVector = Vector3.zero;

            if (hitCenter)
            {
                // Odbij się od normalnej przeszkody
                avoidanceVector += hit.normal * avoidanceForce;
                Debug.DrawRay(transform.position, forward * detectionDistance, Color.red);
            }
            
            if (hitLeft)
            {
                avoidanceVector += transform.right * avoidanceForce;
                Debug.DrawRay(transform.position, left * detectionDistance, Color.yellow);
            }

            if (hitRight)
            {
                avoidanceVector -= transform.right * avoidanceForce;
                Debug.DrawRay(transform.position, right * detectionDistance, Color.yellow);
            }

            finalDirection = (desiredDirection + avoidanceVector).normalized;
        }

        return finalDirection;
    }
}
