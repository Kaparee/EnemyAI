using UnityEngine;
using TMPro;

using System.Collections.Generic;
public enum GameState
{
    Exploration,
    Mining,
    Fighting,
    Menu,
    Console,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameState currentState = GameState.Exploration;

    [Header("UI Notifications")]
    public TextMeshProUGUI notificationText;
    public TextMeshProUGUI infoText;

    [Header("Death System")]
    [SerializeField] private GameObject deathScreenCanvas;
    [SerializeField] private Transform baseSpawnPoint;
    [SerializeField] private GameObject player;
    [SerializeField] private ShipStats shipStats;

    public List<Transform> allRepairStationsPosition = new List<Transform>();

    [Header("Enemy Registry")]
    public List<EnemyAI> activeEnemies = new List<EnemyAI>();

    public void RegisterEnemy(EnemyAI enemy)
    {
        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(EnemyAI enemy)
    {
        if (activeEnemies.Contains(enemy))
            activeEnemies.Remove(enemy);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // przetrwa zmianę sceny
            //Debug.Log("<color=cyan>GameManager został zainicjalizowany jako Singleton</color>");
        }
        else
        {
            Debug.LogWarning("Drugi GameManager został zniszczony (duplikat)");
            Destroy(gameObject);
            return;
        }

        // Próba automatycznego znalezienia gracza jeśli nie jest przypisany
        if (player == null) {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p;
        }

        if (player != null && shipStats == null) {
            shipStats = player.GetComponent<ShipStats>();
        }
    }

    private void Start()
    {
        if (deathScreenCanvas != null)
        {
            deathScreenCanvas.SetActive(false);
        }
    }

    public void ShowMiningNotification(string message, Color color)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.color = color;
            notificationText.gameObject.SetActive(true);

            // Wyłączamy napis po 3 sekundach
            CancelInvoke("HideNotification"); 
            Invoke("HideNotification", 8f);
        }
    }

    public void ShowSectorInfo(string message, Color color)
    {
        if (infoText != null)
        {
            infoText.text = message;
            infoText.color = color;
        }
    }

    private void HideNotification()
    {
        if (notificationText != null)
            notificationText.gameObject.SetActive(false);
    }

    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log($"<color=yellow>GameState zmieniony na: <b>{newState}</b></color>");

        if (newState == GameState.Exploration || newState == GameState.Fighting || newState == GameState.Mining)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void TriggerGameOver()
    {
        ChangeState(GameState.GameOver);

        if (deathScreenCanvas != null)
            deathScreenCanvas.SetActive(true);

        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void RespawnAtBase()
    {
        Time.timeScale = 1f;

        // Jeśli wciąż null, spróbuj znaleźć
        if (player == null) {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        if (player != null && shipStats == null) {
            shipStats = player.GetComponent<ShipStats>();
        }

        if (shipStats != null) {
            shipStats.Heal(shipStats.GetMaxHP());
        }

        if (player != null)
        {
            Vector3 targetPosition = Vector3.zero;
            bool stationFound = false;

            if (allRepairStationsPosition.Count > 0)
            {
                float minDistance = Mathf.Infinity;
                Transform nearestStation = null;

                foreach (Transform t in allRepairStationsPosition)
                {
                    float distance = Vector3.Distance(player.transform.position, t.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestStation = t;
                    }
                }

                if (nearestStation != null)
                {
                    targetPosition = nearestStation.position;
                    stationFound = true;
                }
            }

            if (!stationFound && baseSpawnPoint != null)
            {
                targetPosition = baseSpawnPoint.position;
            }

            player.transform.position = targetPosition;
            player.transform.rotation = Quaternion.identity;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (deathScreenCanvas != null)
            deathScreenCanvas.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        ChangeState(GameState.Exploration);
    }
}