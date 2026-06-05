using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatHUD : MonoBehaviour
{
    private const float BarWidth = 320f;
    private const float BarHeight = 28f;

    private ShipStats playerStats;
    private Canvas canvas;
    private Image playerHealthFill;
    private Image enemyHealthFill;
    private TextMeshProUGUI playerHPText;
    private TextMeshProUGUI enemyHPText;
    private GameObject enemyHealthRoot;

    private void Awake()
    {
        playerStats = GetComponent<ShipStats>();
        BuildUi();
    }

    private void LateUpdate()
    {
        UpdatePlayerHealth();
        UpdateEnemyHealth();
    }

    private void OnDestroy()
    {
        if (canvas != null)
            Destroy(canvas.gameObject);
    }

    private void UpdatePlayerHealth()
    {
        if (playerStats == null)
            playerStats = GetComponent<ShipStats>();

        SetHealthBar(playerStats, playerHealthFill, playerHPText);
    }

    private void UpdateEnemyHealth()
    {
        ShipStats enemyStats = FindNearestEnemyStats();
        bool hasEnemy = enemyStats != null && enemyStats.GetMaxHP() > 0f;

        if (enemyHealthRoot != null)
            enemyHealthRoot.SetActive(hasEnemy);

        if (hasEnemy)
            SetHealthBar(enemyStats, enemyHealthFill, enemyHPText);
    }

    private ShipStats FindNearestEnemyStats()
    {
        if (GameManager.Instance == null || GameManager.Instance.activeEnemies == null)
            return null;

        float bestDistanceSqr = float.MaxValue;
        ShipStats bestStats = null;

        foreach (EnemyAI enemy in GameManager.Instance.activeEnemies)
        {
            if (enemy == null)
                continue;

            ShipStats stats = enemy.GetComponent<ShipStats>();
            if (stats == null || stats.IsDestroyed)
                continue;

            float distanceSqr = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                bestStats = stats;
            }
        }

        return bestStats;
    }

    private static void SetHealthBar(ShipStats stats, Image fill, TextMeshProUGUI text)
    {
        if (fill == null || text == null)
            return;

        if (stats == null || stats.GetMaxHP() <= 0f)
        {
            fill.fillAmount = 0f;
            text.text = "0 / 0";
            return;
        }

        float maxHp = stats.GetMaxHP();
        float currentHp = Mathf.Clamp(stats.CurrentHP, 0f, maxHp);
        float hpPercent = currentHp / maxHp;

        fill.fillAmount = hpPercent;
        fill.color = GetHealthColor(hpPercent);
        text.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
    }

    private static Color GetHealthColor(float hpPercent)
    {
        if (hpPercent > 0.5f)
            return Color.Lerp(Color.yellow, Color.green, (hpPercent - 0.5f) * 2f);

        return Color.Lerp(Color.red, Color.yellow, hpPercent * 2f);
    }

    private void BuildUi()
    {
        var canvasGo = new GameObject("CombatHUD");
        canvasGo.transform.SetParent(transform, false);

        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasGo.AddComponent<GraphicRaycaster>();

        CreateHealthBar(
            "PlayerHealth",
            canvas.transform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(40f, 40f),
            "PLAYER HP",
            out playerHealthFill,
            out playerHPText,
            out _);

        CreateHealthBar(
            "EnemyHealth",
            canvas.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-40f, -40f),
            "ENEMY HP",
            out enemyHealthFill,
            out enemyHPText,
            out enemyHealthRoot);
    }

    private static void CreateHealthBar(
        string name,
        Transform parent,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 anchoredPosition,
        string label,
        out Image fillImage,
        out TextMeshProUGUI valueText,
        out GameObject rootGo)
    {
        rootGo = new GameObject(name, typeof(RectTransform), typeof(Image));
        rootGo.transform.SetParent(parent, false);

        var root = rootGo.GetComponent<RectTransform>();
        root.anchorMin = anchor;
        root.anchorMax = anchor;
        root.pivot = pivot;
        root.anchoredPosition = anchoredPosition;
        root.sizeDelta = new Vector2(BarWidth, BarHeight);

        var background = rootGo.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.65f);
        background.raycastTarget = false;

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(rootGo.transform, false);

        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        fillImage = fillGo.GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.color = Color.green;
        fillImage.raycastTarget = false;

        valueText = CreateText("Value", rootGo.transform, Vector2.zero, Vector2.one, Vector2.zero, 18f);
        valueText.text = "0 / 0";

        TextMeshProUGUI labelText = CreateText(
            "Label",
            rootGo.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 20f),
            16f);
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.rectTransform.sizeDelta = new Vector2(0f, 20f);
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        float fontSize)
    {
        var textGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);

        var rect = textGo.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = anchoredPosition;

        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }
}
