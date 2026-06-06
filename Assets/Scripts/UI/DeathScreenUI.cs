using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeathScreenUI : MonoBehaviour
{
    private GameObject deathMenuRoot;

    private void Awake()
    {
        EventBus.OnPlayerDeath += ShowDeathScreen;
    }

    private void Start()
    {
        BuildUi();
    }

    private void OnDestroy()
    {
        EventBus.OnPlayerDeath -= ShowDeathScreen;
    }

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

    private void ShowDeathScreen()
    {
        if (deathMenuRoot != null) deathMenuRoot.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

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