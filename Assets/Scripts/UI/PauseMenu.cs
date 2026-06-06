using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    public static bool isPaused;
    
    private GameObject pauseMenuRoot;

    private void Start()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        if (SharedUIManager.Instance == null || SharedUIManager.Instance.MainCanvas == null) return;
        Transform canvasTransform = SharedUIManager.Instance.MainCanvas.transform;

        pauseMenuRoot = new GameObject("PauseMenuRoot", typeof(RectTransform), typeof(Image));
        pauseMenuRoot.transform.SetParent(canvasTransform, false);
        var rootRect = pauseMenuRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;
        pauseMenuRoot.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);

        CreateButton("Wznów", pauseMenuRoot.transform, new Vector2(0f, 60f), ResumeGame);
        CreateButton("Restart", pauseMenuRoot.transform, new Vector2(0f, -20f), RestartGame);
        CreateButton("Wyjście z gry", pauseMenuRoot.transform, new Vector2(0f, -100f), QuitGame);

        pauseMenuRoot.SetActive(false);
    }

    private void CreateButton(string label, Transform parent, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        var btnGo = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        var rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(250f, 60f);

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
        tmp.fontSize = 24f;
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        Restart restart = FindFirstObjectByType<Restart>();
        if (restart != null) restart.RestartMethod();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}