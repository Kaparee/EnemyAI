using UnityEngine;
using UnityEngine.SceneManagement;
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

// Główny menedżer stanu gry (Singleton).
// Zarządza cyklem życia aplikacji, przepływem scen i globalnymi zdarzeniami w systemie.
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

    // Dodaje nowo utworzonego lub aktywnego przeciwnika do globalnego rejestru sledzonych jednostek wroga.
    public void RegisterEnemy(EnemyAI enemy)
    {
        if (enemy != null && !activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    // Usuwa zniszczonego lub wylaczonego przeciwnika z listy aktywnych zagrozen w aktualnej instancji menedzera.
    public void UnregisterEnemy(EnemyAI enemy)
    {
        activeEnemies.Remove(enemy);
    }

    // Odswieza struktury danych menedzera po zaladowaniu nowej sceny, pozbywajac sie referencji do nieistniejacych juz obiektow.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        activeEnemies.RemoveAll(e => e == null);
    }

    // Oczyszcza globalne delegaty i zdarzenia w momencie niszczenia instancji menedzera gry, zeby zapobiec wyciekom pamieci.
    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Wymusza wzorzec Singletona podczas ladowania pierwszego skryptu i zachowuje go miedzy scenami, niszczac ewentualne duplikaty.
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Debug.LogWarning("Wykryto dodatkową instancję GameManager - usuwanie duplikatu.");
            Destroy(gameObject);
            return;
        }

        if (player == null) {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p;
        }

        if (player != null && shipStats == null) {
            shipStats = player.GetComponent<ShipStats>();
        }
    }

    // Dezaktywuje domyslnie wyswietlane ekrany smierci na starcie biezacej sesji eksploracyjnej.
    private void Start()
    {
        if (deathScreenCanvas != null)
        {
            deathScreenCanvas.SetActive(false);
        }
    }

    // Wyswietla na interfejsie gracza krotkotrwale powiadomienie tekstowe dotyczace zebranych surowcow lub statusu wydobycia.
    public void ShowMiningNotification(string message, Color color)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.color = color;
            notificationText.gameObject.SetActive(true);

            CancelInvoke("HideNotification"); 
            Invoke("HideNotification", 8f);
        }
    }

    // Aktualizuje stale pole informacyjne UI danymi o aktualnie odwiedzanym sektorze wszechswiata.
    public void ShowSectorInfo(string message, Color color)
    {
        if (infoText != null)
        {
            infoText.text = message;
            infoText.color = color;
        }
    }

    // Ukrywa tekstowe powiadomienie z interfejsu uzytkownika po wygasnieciu jego okreslonego czasu wyswietlania.
    private void HideNotification()
    {
        if (notificationText != null)
            notificationText.gameObject.SetActive(false);
    }

    // Modyfikuje globalny stan aplikacji i na jego podstawie konfiguruje widocznosc kursora myszy dla gracza.
    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log($"<color=yellow>[GameManager] Zmiana stanu gry na: <b>{newState}</b></color>");

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

    // Przerwa biezaca petle rozgrywki, zatrzymuje czas i wlacza wyswietlanie ostatecznego panelu z informacja o porazce.
    public void TriggerGameOver()
    {
        ChangeState(GameState.GameOver);

        if (deathScreenCanvas != null)
            deathScreenCanvas.SetActive(true);

        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Przenosi gracza w bezpieczne miejsce po utracie calego zdrowia, przywracajac mu pelnie wytrzymalosci i wznawiajac uplyw czasu.
    public void RespawnAtBase()
    {
        Time.timeScale = 1f;

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