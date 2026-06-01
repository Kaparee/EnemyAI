using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Profiling;

public class TacticalBrain : MonoBehaviour
{
    [Header("Minimax Settings")]
    [SerializeField] private int searchDepth = 2;
    [SerializeField] private float maneuverDistance = 40f;
    [SerializeField] private float playerManeuverDistance = 30f;

    private int alphaBetaCutCount;

    public Vector3 CalculateLeadingPosition(Transform target, Vector3 shooterPos, float projectileSpeed)
    {
        if (target == null || projectileSpeed <= 0.01f) return shooterPos;

        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        Vector3 targetVelocity = targetRb != null ? targetRb.linearVelocity : Vector3.zero;

        float distance = Vector3.Distance(shooterPos, target.position);
        float timeToHit = distance / projectileSpeed;
        Vector3 leadPos = target.position + targetVelocity * timeToHit;

        for (int i = 0; i < 2; i++)
        {
            timeToHit = Vector3.Distance(shooterPos, leadPos) / projectileSpeed;
            leadPos = target.position + targetVelocity * timeToHit;
        }

        AILog.Leading(
            $"Gracz się rusza — celuję z wyprzedzeniem (~{timeToHit:F1}s do trafienia).");

        return leadPos;
    }

    public IEnumerator GetOptimalCombatPositionCoroutine(Transform target, Vector3 currentPos, Vector3 currentForward, System.Action<Vector3> callback)
    {
        if (target == null)
        {
            callback?.Invoke(currentPos);
            yield break;
        }

        alphaBetaCutCount = 0;
        Profiler.BeginSample("TacticalBrain.Minimax");

        List<Vector3> candidates = GetCandidatePositions(currentPos, currentForward, maneuverDistance);
        AILog.Minimax(
            $"Minimax: sprawdzam {candidates.Count} moich ruchów na głębokość {searchDepth} (gracz też się rusza w drzewie).");
        Vector3 bestManeuver = currentPos;
        float bestVal = float.NegativeInfinity;

        foreach (Vector3 cand in candidates)
        {
            Vector3 heading = (cand - currentPos).sqrMagnitude > 0.01f
                ? (cand - currentPos).normalized
                : currentForward;

            float val = MinimaxRecursive(cand, heading, target.position, searchDepth, float.NegativeInfinity, float.PositiveInfinity, false);
            if (val > bestVal)
            {
                bestVal = val;
                bestManeuver = cand;
            }

            yield return null;
        }

        Profiler.EndSample();
        AILog.Minimax(
            $"Wybrałem pozycję {bestManeuver}, ocena={bestVal:F0}. " +
            $"Odrzuciłem {alphaBetaCutCount} gorszych gałęzi (alpha-beta).");
        callback?.Invoke(bestManeuver);
    }

    private List<Vector3> GetCandidatePositions(Vector3 origin, Vector3 forward, float distance)
    {
        Vector3 f = forward.sqrMagnitude > 0.01f ? forward.normalized : transform.forward;
        Vector3 r = Vector3.Cross(Vector3.up, f).normalized;
        if (r.sqrMagnitude < 0.01f) r = transform.right;

        return new List<Vector3>
        {
            origin + f * distance,
            origin - f * distance,
            origin + r * distance,
            origin - r * distance,
            origin + Vector3.up * distance * 0.5f,
            origin - Vector3.up * distance * 0.5f
        };
    }

    private List<Vector3> GetPlayerCandidatePositions(Vector3 playerPos, Vector3 aiPos)
    {
        Vector3 awayFromAi = (playerPos - aiPos).sqrMagnitude > 0.01f
            ? (playerPos - aiPos).normalized
            : Vector3.forward;

        Vector3 lateral = Vector3.Cross(Vector3.up, awayFromAi).normalized;

        return new List<Vector3>
        {
            playerPos + lateral * playerManeuverDistance,
            playerPos - lateral * playerManeuverDistance,
            playerPos + awayFromAi * playerManeuverDistance,
            playerPos - awayFromAi * playerManeuverDistance,
            playerPos + Vector3.up * playerManeuverDistance * 0.5f,
            playerPos - Vector3.up * playerManeuverDistance * 0.5f
        };
    }

    private float MinimaxRecursive(Vector3 aiPos, Vector3 aiForward, Vector3 playerPos, int depth, float alpha, float beta, bool isMaximizing)
    {
        if (depth == 0)
            return EvaluatePosition(aiPos, aiForward, playerPos);

        if (isMaximizing)
        {
            float maxEval = float.NegativeInfinity;
            foreach (Vector3 cand in GetCandidatePositions(aiPos, aiForward, maneuverDistance))
            {
                Vector3 heading = (cand - aiPos).normalized;
                float eval = MinimaxRecursive(cand, heading, playerPos, depth - 1, alpha, beta, false);
                maxEval = Mathf.Max(maxEval, eval);
                alpha = Mathf.Max(alpha, eval);
                if (beta <= alpha)
                {
                    alphaBetaCutCount++;
                    break;
                }
            }
            return maxEval;
        }

        float minEval = float.PositiveInfinity;
        foreach (Vector3 playerMove in GetPlayerCandidatePositions(playerPos, aiPos))
        {
            float eval = MinimaxRecursive(aiPos, aiForward, playerMove, depth - 1, alpha, beta, true);
            minEval = Mathf.Min(minEval, eval);
            beta = Mathf.Min(beta, eval);
            if (beta <= alpha)
            {
                alphaBetaCutCount++;
                break;
            }
        }
        return minEval;
    }

    private float EvaluatePosition(Vector3 aiPos, Vector3 aiForward, Vector3 targetPos)
    {
        float score = 0f;
        float dist = Vector3.Distance(aiPos, targetPos);

        if (dist > 80f && dist < 120f) score += 50f;
        else score -= Mathf.Abs(dist - 100f) * 0.5f;

        Vector3 forward = aiForward.sqrMagnitude > 0.01f ? aiForward.normalized : transform.forward;
        Vector3 toTarget = (targetPos - aiPos).normalized;
        float broadsideDot = Mathf.Abs(Vector3.Dot(forward, toTarget));
        score += (1f - broadsideDot) * 40f;

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float sideExposure = Mathf.Abs(Vector3.Dot(right, toTarget));
        score += sideExposure * 25f;

        if (ObstacleRegistry.Instance != null && ObstacleRegistry.Instance.IsPositionBlocked(aiPos, 5f))
            score -= 100f;

        return score;
    }
}
