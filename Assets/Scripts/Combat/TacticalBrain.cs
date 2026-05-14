using UnityEngine;
using System.Collections.Generic;

public class TacticalBrain : MonoBehaviour
{
    [Header("Minimax Settings")]
    [SerializeField] private int searchDepth = 2;
    [SerializeField] private float maneuverDistance = 40f;

    /// <summary>
    /// Obliczanie wyprzedzenia strzału (Leading).
    /// </summary>
    public Vector3 CalculateLeadingPosition(Transform target, Vector3 shooterPos, float projectileSpeed)
    {
        // TODO: Zintegrować z prędkością wylotową z klasy bazowej broni Kamila
        if (target == null) return Vector3.zero;

        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        Vector3 targetVelocity = (targetRb != null) ? targetRb.linearVelocity : Vector3.zero;

        float distance = Vector3.Distance(shooterPos, target.position);
        float timeToHit = distance / projectileSpeed;

        // Proste prowadzenie: P_lead = P_target + V_target * t
        Vector3 leadPos = target.position + (targetVelocity * timeToHit);

        // Opcjonalnie: iteracja dla większej precyzji
        float refinedTimeToHit = Vector3.Distance(shooterPos, leadPos) / projectileSpeed;
        leadPos = target.position + (targetVelocity * refinedTimeToHit);

        return leadPos;
    }

    /// <summary>
    /// Wybór optymalnej pozycji walki (Manewr Burtowego) przy użyciu Minimaxa z Alpha-Beta pruning.
    /// </summary>
    public Vector3 GetOptimalCombatPosition(Transform target, Vector3 currentPos)
    {
        if (target == null) return currentPos;

        List<Vector3> candidates = GetCandidatePositions(currentPos);
        Vector3 bestManeuver = currentPos;
        float bestVal = float.NegativeInfinity;

        foreach (Vector3 cand in candidates)
        {
            // Start Minimax with depth 2
            float val = MinimaxRecursive(cand, target.position, searchDepth, float.NegativeInfinity, float.PositiveInfinity, false);
            if (val > bestVal)
            {
                bestVal = val;
                bestManeuver = cand;
            }
        }

        return bestManeuver;
    }

    private List<Vector3> GetCandidatePositions(Vector3 origin)
    {
        return new List<Vector3>
        {
            origin + transform.forward * maneuverDistance,
            origin - transform.forward * maneuverDistance,
            origin + transform.right * maneuverDistance,
            origin - transform.right * maneuverDistance,
            origin + transform.up * maneuverDistance,
            origin - transform.up * maneuverDistance
        };
    }

    private float MinimaxRecursive(Vector3 pos, Vector3 targetPos, int depth, float alpha, float beta, bool isMaximizing)
    {
        if (depth == 0) return EvaluatePosition(pos, targetPos);

        if (isMaximizing)
        {
            float maxEval = float.NegativeInfinity;
            foreach (Vector3 cand in GetCandidatePositions(pos))
            {
                float eval = MinimaxRecursive(cand, targetPos, depth - 1, alpha, beta, false);
                maxEval = Mathf.Max(maxEval, eval);
                alpha = Mathf.Max(alpha, eval);
                if (beta <= alpha) break;
            }
            return maxEval;
        }
        else
        {
            float minEval = float.PositiveInfinity;
            // TODO: Wykorzystać dane z sensoryki/radaru Kamila zamiast bezpośredniej symulacji
            // Symulacja odpowiedzi gracza (gracz stara się nas mieć przed nosem)
            // Uproszczony ruch gracza w stronę AI
            Vector3 simulatedPlayerPos = targetPos + (pos - targetPos).normalized * 10f;
            
            float eval = EvaluatePosition(pos, simulatedPlayerPos);
            minEval = Mathf.Min(minEval, eval);
            beta = Mathf.Min(beta, eval);
            
            return minEval;
        }
    }

    private float EvaluatePosition(Vector3 pos, Vector3 targetPos)
    {
        float score = 0;
        float dist = Vector3.Distance(pos, targetPos);

        // 1. Dystans optymalny (80-120 jednostek)
        if (dist > 80f && dist < 120f) score += 50f;
        else score -= Mathf.Abs(dist - 100f) * 0.5f;

        // 2. Kąt burtowy (chcemy być prostopadle do gracza)
        Vector3 toTarget = (targetPos - pos).normalized;
        float dot = Vector3.Dot(transform.forward, toTarget);
        score += (1f - Mathf.Abs(dot)) * 30f;

        // 3. Sprawdzenie kolizji
        if (Physics.CheckSphere(pos, 5f)) score -= 100f;

        return score;
    }
}
