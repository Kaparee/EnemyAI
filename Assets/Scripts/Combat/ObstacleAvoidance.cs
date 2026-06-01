using UnityEngine;

public class ObstacleAvoidance : MonoBehaviour
{
    [Header("Avoidance Settings")]
    [SerializeField] private float detectionDistance = 40f;
    [SerializeField] private float avoidanceForce = 1.5f;
    [SerializeField] private LayerMask obstacleLayer;

    private float lastLogTime;

    private void Awake()
    {
        if (obstacleLayer.value == 0)
        {
            obstacleLayer = AILayers.AsteroidMask;
            if (obstacleLayer.value == 0)
                obstacleLayer = ~AILayers.PlayerMask;
        }
    }

    public Vector3 GetModifiedDirection(Vector3 desiredDirection)
    {
        Vector3 forward = transform.forward;
        Vector3 left = (transform.forward - transform.right * 0.5f).normalized;
        Vector3 right = (transform.forward + transform.right * 0.5f).normalized;

        bool hitCenter = Physics.Raycast(transform.position, forward, out RaycastHit centerHit, detectionDistance, obstacleLayer);
        bool hitLeft = Physics.Raycast(transform.position, left, out RaycastHit leftHit, detectionDistance, obstacleLayer);
        bool hitRight = Physics.Raycast(transform.position, right, out RaycastHit rightHit, detectionDistance, obstacleLayer);

        if (!hitCenter && !hitLeft && !hitRight)
            return desiredDirection;

        if (Time.time - lastLogTime > 2f)
        {
            lastLogTime = Time.time;
            AILog.Reactive("Coś przedemną — skręcam na bok (3 promienie wykrywające).");
        }

        Vector3 avoidanceVector = Vector3.zero;

        if (hitCenter)
            avoidanceVector += centerHit.normal * avoidanceForce;

        if (hitLeft)
            avoidanceVector += transform.right * avoidanceForce;

        if (hitRight)
            avoidanceVector -= transform.right * avoidanceForce;

        return (desiredDirection + avoidanceVector).normalized;
    }
}
