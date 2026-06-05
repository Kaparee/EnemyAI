using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private float riseDistance = 65f;

    private static DamagePopup instance;
    private Canvas canvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        var go = new GameObject("DamagePopupManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DamagePopup>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildCanvas();
    }

    private void OnEnable()
    {
        ShipStats.OnDamageDealt += ShowDamage;
    }

    private void OnDisable()
    {
        ShipStats.OnDamageDealt -= ShowDamage;
    }

    private void ShowDamage(Vector3 worldPosition, float damage, bool damageToPlayer)
    {
        if (canvas == null)
            BuildCanvas();

        if (Camera.main == null || canvas == null)
            return;

        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition + Vector3.up * 2f);
        if (screenPosition.z <= 0f)
            return;

        StartCoroutine(AnimatePopup(screenPosition, damage, damageToPlayer));
    }

    private IEnumerator AnimatePopup(Vector3 startPosition, float damage, bool damageToPlayer)
    {
        var textGo = new GameObject("DamagePopupText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(canvas.transform, false);

        var rect = textGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.position = startPosition;
        rect.sizeDelta = new Vector2(140f, 45f);

        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = $"-{Mathf.CeilToInt(damage)}";
        text.fontSize = 34f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = damageToPlayer ? new Color(1f, 0.15f, 0.1f, 1f) : new Color(1f, 0.82f, 0.15f, 1f);
        text.raycastTarget = false;

        Color baseColor = text.color;
        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);
            rect.position = startPosition + Vector3.up * (riseDistance * t);
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t);
            yield return null;
        }

        Destroy(textGo);
    }

    private void BuildCanvas()
    {
        var canvasGo = new GameObject("DamagePopupCanvas");
        canvasGo.transform.SetParent(transform, false);

        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasGo.AddComponent<GraphicRaycaster>();
    }
}
