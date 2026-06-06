using UnityEngine;
using TMPro;
using System.Collections;

public class GameMessageHUD : MonoBehaviour
{
    private TextMeshProUGUI messageText;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        EventBus.OnEnemyDeath += HandleEnemyDeath;
    }

    private void Start()
    {
        BuildUi();
    }

    private void OnDestroy()
    {
        EventBus.OnEnemyDeath -= HandleEnemyDeath;
    }

    private void HandleEnemyDeath(EnemyAI enemy)
    {
        ShowMessage("ZWYCIĘSTWO!", new Color(1f, 0.8f, 0.2f), 3f);
    }

    private void ShowMessage(string text, Color color, float duration)
    {
        if (messageText == null) return;
        
        messageText.text = text;
        messageText.color = color;
        messageText.gameObject.SetActive(true);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideMessageAfter(duration));
    }

    private IEnumerator HideMessageAfter(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

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
