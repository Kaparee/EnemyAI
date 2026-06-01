using UnityEngine;

public class CustomRadarSystem : MonoBehaviour
{
    public float detectionRadius = 500f;
    public Transform detectedPlayer;
    public Turret[] turretsToControl;

    private TacticalBrain tacticalBrain;
    private float scanInterval = 0.4f;
    private float nextScanTime;

    void Awake()
    {
        tacticalBrain = GetComponent<TacticalBrain>();
    }

    public void BindTurrets(Turret[] turrets)
    {
        turretsToControl = turrets;
    }

    private void Update()
    {
        if (Time.time < nextScanTime) return;
        ScanForPlayer();
        nextScanTime = Time.time + scanInterval;
    }

    private void ScanForPlayer()
    {
        Transform closest = FindClosestPlayer();
        bool hadTarget = detectedPlayer != null;
        detectedPlayer = closest;

        if (closest != null)
        {
            if (!hadTarget)
            {
                AILog.Radar($"Gracz w zasięgu radaru ({detectionRadius:F0}m) — włączam walkę.");
                EventBus.TriggerOnPlayerDetected(closest);
                if (GameManager.Instance != null)
                    GameManager.Instance.ChangeState(GameState.Fighting);
            }

            UpdateTurrets(closest);
        }
        else if (turretsToControl != null)
        {
            foreach (Turret turret in turretsToControl)
            {
                if (turret != null)
                    turret.target = null;
            }
        }
    }

    private Transform FindClosestPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (!IsPlayerCollider(hit)) continue;

            Transform root = hit.transform.root;
            float dist = Vector3.Distance(transform.position, root.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = root;
            }
        }

        return closest;
    }

    private static bool IsPlayerCollider(Collider col)
    {
        if (col.CompareTag("Player")) return true;
        int playerLayer = AILayers.Player;
        return playerLayer >= 0 && col.gameObject.layer == playerLayer;
    }

    private void UpdateTurrets(Transform target)
    {
        if (turretsToControl == null) return;

        float projSpeed = 40f;
        if (turretsToControl.Length > 0 && turretsToControl[0] != null)
            projSpeed = turretsToControl[0].GetProjectileSpeed();

        Vector3 leadPos = target.position;
        if (tacticalBrain != null)
            leadPos = tacticalBrain.CalculateLeadingPosition(target, transform.position, projSpeed);

        foreach (Turret turret in turretsToControl)
        {
            if (turret == null) continue;
            turret.target = target;
            turret.SetAimPoint(leadPos);
        }
    }
}
