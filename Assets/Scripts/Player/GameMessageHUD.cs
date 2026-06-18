using UnityEngine;
using TMPro;
using System.Collections;

// Odbiera zdarzenia tekstowe (np. 'Zniszczono wroga') i animuje stosowne powiadomienia w widocznym miejscu na ekranie.
public class GameMessageHUD : MonoBehaviour
{
    private TextMeshProUGUI messageText;
    private Coroutine hideCoroutine;

    // Subskrybuje nasluchiwacz zdarzen pod powiadomienia o destrukcji wrogow wysylane przez globalny menedzer wydarzen.
    private void Awake()
    {
        EventBus.OnEnemyDeath += HandleEnemyDeath;
    }

    // Uruchamia konstrukcje warstwy wyswietlania napisow bezposrednio po dodaniu instancji obiektu do sceny.
    private void Start()
    {
        BuildUi();
    }

    // Wyrejestrowuje podlaczona funkcje obslugujaca zdarzenia destrukcji jednostek, zabezpieczajac aplikacje przed martwymi referencjami.
    private void OnDestroy()
    {
        EventBus.OnEnemyDeath -= HandleEnemyDeath;
    }

    // Odbiera sygnal o zniszczeniu konkretnego przeciwnika i inicjalizuje renderowanie zadanego komunikatu o zwyciestwie na zdefiniowany czas.
    private void HandleEnemyDeath(EnemyAI enemy)
    {
        ShowMessage("ZWYCIĘSTWO!", new Color(1f, 0.8f, 0.2f), 3f);
    }

    // Cofa aktywne animacje znikania, przypisuje nowy komunikat do elementu graficznego i wznawia operacje opoznionego ukrywania.
    private void ShowMessage(string text, Color color, float duration)
    {
        if (messageText == null) return;
        
        messageText.text = text;
        messageText.color = color;
        messageText.gameObject.SetActive(true);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideMessageAfter(duration));
    }

    // Wstrzymuje asynchronicznie uplyw czasu, po czym deaktywuje obiekt wiadomosci, zwalniajac czesc interfejsu uzytkownika na ekranie.
    private IEnumerator HideMessageAfter(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    // Dynamicznie stawia i parametruje calkowita podstawe UI przeznaczona do wyswietlania szerokich ogloszen nad centrum ekranu.
    private void BuildUi()
    {
        if (SharedUIManager.Instance == null || SharedUIManager.Instance.MainCanvas == null)
            return;

        var textGo = new GameObject("GameMessageText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(SharedUIManager.Instance.MainCanvas.transform, false);

        var rect = textGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.75f);
        rect.anchorMax = new Vector2(0.5f, 0.75f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1000f, 200f);

        messageText = textGo.GetComponent<TextMeshProUGUI>();
        messageText.fontSize = 90f;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.fontStyle = FontStyles.Bold;
        messageText.color = Color.white;
        
        messageText.gameObject.SetActive(false);
    }
}
