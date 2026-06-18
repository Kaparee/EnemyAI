using UnityEngine;

// System radaru skanującego otoczenie. Zbiera informacje o jednostkach w zasięgu.
// Zoptymalizowałem to przez rzadsze odpytywanie fizyki (Physics.OverlapSphere), żeby odciążyć CPU.
public class CustomRadarSystem : MonoBehaviour
{
    public float detectionRadius = 500f;
    public Transform detectedPlayer;
    public Turret[] turretsToControl;

    private TacticalBrain tacticalBrain;
    private float scanInterval = 0.4f;
    private float nextScanTime;

    // Pobiera referencje do komponentu TacticalBrain podczas inicjalizacji skryptu
    void Awake()
    {
        tacticalBrain = GetComponent<TacticalBrain>();
    }

    // Zapisuje przekazana tablice wiezyczek do wewnetrznej zmiennej sterujacej
    public void BindTurrets(Turret[] turrets)
    {
        turretsToControl = turrets;
    }

    // Główna pętla logiczna klatki. Staram się tu minimalizować ciężkie obliczenia.

    // Wykonuje cykliczne skanowanie otoczenia bazujac na ustalonym interwale czasowym
    private void Update()
    {
        if (Time.time < nextScanTime) return;
        ScanForPlayer();
        nextScanTime = Time.time + scanInterval;
    }

    // Szuka najblizszego gracza w zasiegu i aktualizuje stan systemu oraz cele wiezyczek
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

    // Przeszukuje sferycznie otoczenie i zwraca transformacje najblizszego obiektu gracza
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

    // Weryfikuje czy dany collider nalezy do gracza sprawdzajac tagi oraz warstwy
    private static bool IsPlayerCollider(Collider col)
    {
        if (col.CompareTag("Player")) return true;
        int playerLayer = AILayers.Player;
        return playerLayer >= 0 && col.gameObject.layer == playerLayer;
    }

    // Oblicza pozycje wyprzedzajaca dla celu i weryfikuje katy celowania wiezyczek
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
