using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Obsługa ekranu wyświetlanego w momencie zniszczenia statku gracza (widok Game Over, powrót do bazy).
public class DeathScreenUI : MonoBehaviour
{
    private GameObject deathMenuRoot;

    // Rejestruje lokalny nasluchiwacz na zdarzenie zniszczenia statku gracza w globalnej szynie zdarzen.
    private void Awake()
    {
        EventBus.OnPlayerDeath += ShowDeathScreen;
    }

    // Rozpoczyna proces budowy struktury interfejsu uzytkownika dla ekranu koncowego.
    private void Start()
    {
        BuildUi();
    }

    // Wyrejestrowuje nasluchiwacz ze zdarzenia zniszczenia statku w celu zapobiegania bledom wyciekow pamieci.
    private void OnDestroy()
    {
        EventBus.OnPlayerDeath -= ShowDeathScreen;
    }

    // Generuje hierarchie obiektow UI ekranu smierci w pamieci, ustawiajac ich kotwice, rozmiary oraz parametry tekstu.
    private void BuildUi()
    {
        if (SharedUIManager.Instance == null || SharedUIManager.Instance.MainCanvas == null) return;
        Transform canvasTransform = SharedUIManager.Instance.MainCanvas.transform;

        deathMenuRoot = new GameObject("DeathMenuRoot", typeof(RectTransform), typeof(Image));
        deathMenuRoot.transform.SetParent(canvasTransform, false);
        var rootRect = deathMenuRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;
        deathMenuRoot.GetComponent<Image>().color = new Color(0.3f, 0f, 0f, 0.85f);

        var textGo = new GameObject("DeathText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(deathMenuRoot.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 100f);
        textRect.sizeDelta = new Vector2(800f, 200f);
        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = "ZOSTAŁEŚ ZNISZCZONY";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.red;
        tmp.fontSize = 90f;
        tmp.fontStyle = FontStyles.Bold;

        CreateButton("Restart", deathMenuRoot.transform, new Vector2(0f, -50f), OnRestartButtonClicked);

        deathMenuRoot.SetActive(false);
    }

    // Tworzy interaktywny przycisk interfejsu uzytkownika, konfiguruje jego komponenty i przypisuje funkcje zwrotna.
    private void CreateButton(string label, Transform parent, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        var btnGo = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        var rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(250f, 70f);

        btnGo.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
        var btn = btnGo.GetComponent<Button>();
        btn.onClick.AddListener(action);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(btnGo.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontSize = 28f;
    }

    // Aktywuje widocznosc panelu koncowego gry oraz odblokowuje widocznosc i swobode kursora myszy.
    private void ShowDeathScreen()
    {
        if (deathMenuRoot != null) deathMenuRoot.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Ukrywa panel smierci, ponownie blokuje kursor myszy oraz wywoluje sekwencje odrodzenia lub restartu calej sceny.
    public void OnRestartButtonClicked()
    {
        if (deathMenuRoot != null) deathMenuRoot.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RespawnAtBase();
            Debug.Log("Przycisk Restart kliknięty - powrót do bazy.");
        }
        else
        {
            Restart restart = FindFirstObjectByType<Restart>();
            if (restart != null) restart.RestartMethod();
        }
    }
}