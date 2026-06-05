using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapHUD : MonoBehaviour
{
    [SerializeField] private float minimapRange = 300f;
    [SerializeField] private float asteroidRefreshInterval = 1f;

    private const float MinimapSize = 200f;
    private const float DotLimitPadding = 10f;

    private readonly List<RectTransform> enemyDots = new List<RectTransform>();
    private readonly List<RectTransform> asteroidDots = new List<RectTransform>();

    private Canvas canvas;
    private RectTransform minimapRoot;
    private RectTransform playerDot;
    private Sprite circleSprite;
    private Asteroid[] cachedAsteroids = new Asteroid[0];
    private float nextAsteroidRefresh;

    private void Awake()
    {
        circleSprite = CreateCircleSprite(128);
        BuildUi();
    }

    private void LateUpdate()
    {
        RefreshAsteroidCache();
        UpdatePlayerDot();
        UpdateEnemyDots();
        UpdateAsteroidDots();
    }

    private void OnDestroy()
    {
        if (canvas != null)
            Destroy(canvas.gameObject);
    }

    private void RefreshAsteroidCache()
    {
        if (Time.time < nextAsteroidRefresh)
            return;

        cachedAsteroids = FindObjectsByType<Asteroid>(FindObjectsSortMode.None);
        nextAsteroidRefresh = Time.time + asteroidRefreshInterval;
    }

    private void UpdatePlayerDot()
    {
        if (playerDot == null)
            return;

        playerDot.anchoredPosition = Vector2.zero;
        playerDot.localRotation = Quaternion.identity;
    }

    private void UpdateEnemyDots()
    {
        int index = 0;

        if (GameManager.Instance != null && GameManager.Instance.activeEnemies != null)
        {
            foreach (EnemyAI enemy in GameManager.Instance.activeEnemies)
            {
                if (enemy == null)
                    continue;

                RectTransform dot = GetDot(enemyDots, index, new Color(1f, 0.1f, 0.08f, 0.95f), 10f);
                dot.anchoredPosition = WorldToMinimapPosition(enemy.transform.position);
                dot.gameObject.SetActive(true);
                index++;
            }
        }

        DisableUnusedDots(enemyDots, index);
    }

    private void UpdateAsteroidDots()
    {
        int index = 0;

        foreach (Asteroid asteroid in cachedAsteroids)
        {
            if (asteroid == null)
                continue;

            Vector2 position = WorldToMinimapPosition(asteroid.transform.position);
            RectTransform dot = GetDot(asteroidDots, index, new Color(0.72f, 0.72f, 0.72f, 0.8f), 6f);
            dot.anchoredPosition = position;
            dot.gameObject.SetActive(true);
            index++;
        }

        DisableUnusedDots(asteroidDots, index);
    }

    private Vector2 WorldToMinimapPosition(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        Vector2 relative = new Vector2(local.x, local.z) / Mathf.Max(1f, minimapRange);
        relative = Vector2.ClampMagnitude(relative, 1f);

        float radius = (MinimapSize * 0.5f) - DotLimitPadding;
        return relative * radius;
    }

    private RectTransform GetDot(List<RectTransform> dots, int index, Color color, float size)
    {
        while (dots.Count <= index)
            dots.Add(CreateDot("Dot", minimapRoot, color, size));

        RectTransform dot = dots[index];
        dot.sizeDelta = new Vector2(size, size);

        Image image = dot.GetComponent<Image>();
        if (image != null)
            image.color = color;

        return dot;
    }

    private static void DisableUnusedDots(List<RectTransform> dots, int usedCount)
    {
        for (int i = usedCount; i < dots.Count; i++)
            dots[i].gameObject.SetActive(false);
    }

    private void BuildUi()
    {
        var canvasGo = new GameObject("MinimapHUD");
        canvasGo.transform.SetParent(transform, false);

        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasGo.AddComponent<GraphicRaycaster>();

        var rootGo = new GameObject("MinimapRoot", typeof(RectTransform), typeof(Image), typeof(Mask));
        rootGo.transform.SetParent(canvas.transform, false);

        minimapRoot = rootGo.GetComponent<RectTransform>();
        minimapRoot.anchorMin = new Vector2(1f, 0f);
        minimapRoot.anchorMax = new Vector2(1f, 0f);
        minimapRoot.pivot = new Vector2(1f, 0f);
        minimapRoot.anchoredPosition = new Vector2(-40f, 40f);
        minimapRoot.sizeDelta = new Vector2(MinimapSize, MinimapSize);

        Image background = rootGo.GetComponent<Image>();
        background.sprite = circleSprite;
        background.color = new Color(0f, 0f, 0f, 0.55f);
        background.raycastTarget = false;

        Mask mask = rootGo.GetComponent<Mask>();
        mask.showMaskGraphic = true;

        playerDot = CreateDot("PlayerDot", minimapRoot, new Color(0.15f, 1f, 0.25f, 1f), 14f);
    }

    private RectTransform CreateDot(string name, Transform parent, Color color, float size)
    {
        var dotGo = new GameObject(name, typeof(RectTransform), typeof(Image));
        dotGo.transform.SetParent(parent, false);

        var rect = dotGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);

        Image image = dotGo.GetComponent<Image>();
        image.sprite = circleSprite;
        image.color = color;
        image.raycastTarget = false;

        return rect;
    }

    private static Sprite CreateCircleSprite(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Bilinear;

        float radius = size * 0.5f;
        Vector2 center = new Vector2(radius, radius);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color solid = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? solid : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }
}
