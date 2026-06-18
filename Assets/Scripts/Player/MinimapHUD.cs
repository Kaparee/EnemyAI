using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Wizualna warstwa UI łącząca się z systemem MapDisplay w celu wyświetlania czytelnego radaru dla gracza.
public class MinimapHUD : MonoBehaviour
{
    [SerializeField] private float minimapRange = 120f;
    [SerializeField] private float asteroidRefreshInterval = 1f;

    private const float MinimapSize = 200f;
    private const float DotLimitPadding = 10f;

    private readonly List<RectTransform> enemyDots = new List<RectTransform>();
    private readonly List<RectTransform> asteroidDots = new List<RectTransform>();

    private RectTransform minimapRoot;
    private RectTransform playerDot;
    private static Sprite cachedCircleSprite;
    private Sprite circleSprite;
    private Asteroid[] cachedAsteroids = new Asteroid[0];
    private float nextAsteroidRefresh;

    // Generuje globalna okragla teksture bazowa dla minimapy i inicjuje budowanie wezlow GUI zaraz po ladowaniu skryptu.
    private void Awake()
    {
        if (cachedCircleSprite == null)
            cachedCircleSprite = CreateCircleSprite(128);
        circleSprite = cachedCircleSprite;
        BuildUi();
    }

    // Zbiera aktualne referencje do cial niebieskich, a nastepnie odswieza pozycje wszystkich wezlow znacznikow na biezacej klatce radaru.
    private void LateUpdate()
    {
        RefreshAsteroidCache();
        UpdatePlayerDot();
        UpdateEnemyDots();
        UpdateAsteroidDots();
    }

    // Pusta metoda sprzatajaca pozostawiona jako uchwyt na potencjalne zwalnianie referencji do wezlow minimalizujace wycieki pamieci.
    private void OnDestroy()
    {
    }

    // Zapobiega ciaglemu analizowaniu wielkiej ilosci skal w kazdej klatce odpytujac pule srodowiska co scisle wyznaczony dystans w czasie.
    private void RefreshAsteroidCache()
    {
        if (Time.time < nextAsteroidRefresh)
            return;

        cachedAsteroids = FindObjectsByType<Asteroid>(FindObjectsSortMode.None);
        nextAsteroidRefresh = Time.time + asteroidRefreshInterval;
    }

    // Sztywnie utrzymuje centrowana pozycje oraz resetuje obrot reprezentacji statku gracza w sercu radaru.
    private void UpdatePlayerDot()
    {
        if (playerDot == null)
            return;

        playerDot.anchoredPosition = Vector2.zero;
        playerDot.localRotation = Quaternion.identity;
    }

    // Odczytuje transformacje zarejestrowanych jednostek wrogow, przeklada je na uklad 2D i aktywuje ich znaczniki wizualne w obrebie minimapy.
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

    // Rzutuje relatywne pozycje zbuforowanych obiektow skalnych na wspolrzedne lokalne kontrolki UI z odpowiednim formatowaniem.
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

    // Przeksztalca dystans wektorowy w przestrzeni 3D na lokalny plaski uklad rzutowania biorac pod uwage przeskalowanie i zawezenie graniczne radaru.
    private Vector2 WorldToMinimapPosition(Vector3 worldPosition)
    {
        Vector3 diff = worldPosition - transform.position;
        diff.y = 0f; 

        Quaternion yawRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        Vector3 localFlat = Quaternion.Inverse(yawRotation) * diff;

        Vector2 relative = new Vector2(localFlat.x, localFlat.z) / Mathf.Max(1f, minimapRange);
        relative = Vector2.ClampMagnitude(relative, 1f);

        float radius = (MinimapSize * 0.5f) - DotLimitPadding;
        return relative * radius;
    }

    // Alokuje nowe obiekty kropek radaru tylko wtedy, gdy limit wyczerpano, oszczedzajac obciazenie zwiazane z masowym klonowaniem obiektow podczas operacji.
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

    // Wylacza flagi widocznosci we wszystkich nadliczbowych i przestarzalych obiektach elementow radaru pozostalych w puli listy.
    private static void DisableUnusedDots(List<RectTransform> dots, int usedCount)
    {
        for (int i = usedCount; i < dots.Count; i++)
            dots[i].gameObject.SetActive(false);
    }

    // Uklada i doczepia odpowiednie kontrolki obrazow na plotnie wyposazajac je rowniez w mechanike maskowania obrazu kolowego.
    private void BuildUi()
    {
        if (SharedUIManager.Instance == null || SharedUIManager.Instance.MainCanvas == null)
            return;

        var rootGo = new GameObject("MinimapRoot", typeof(RectTransform), typeof(Image), typeof(Mask));
        rootGo.transform.SetParent(SharedUIManager.Instance.MainCanvas.transform, false);

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

    // Konstruuje pojedyncza kropke graficzna przypisujac do niej parametry zakotwiczenia, kafelkowania obrazu oraz precyzyjne atrybuty kolorystyczne.
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

    // Interpoluje proceduralna teksture 2D badajac dystans od centrum siatki pikseli, aby uformowac i zapisac wygladzone kolo alfa.
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
