using UnityEngine;
using UnityEngine.UI;

// Wizualny celownik pomagający przewidzieć trajektorię lotu pocisków względem wybranego kąta obrotu statku.
public class PlayerAimHud : MonoBehaviour
{
    [SerializeField] private HeavyKineticLauncher launcher;
    [SerializeField] private float previewDistance = 350f;

    private RectTransform trajectoryDot;
    private Canvas canvas;

    // Wyszukuje komponent wyposazenia broni glownej i natychmiast zleca utworzenie wezlow celownika na plotnie interfejsu.
    void Awake()
    {
        if (launcher == null)
            launcher = GetComponent<HeavyKineticLauncher>();

        BuildUi();
    }

    // Rzutuje obliczony punkt przeciecia pociskow ze srodowiskiem na wspolrzedne plaskiego ekranu kamery glownej.
    void LateUpdate()
    {
        if (launcher == null || Camera.main == null || SharedUIManager.Instance == null || SharedUIManager.Instance.MainCanvas == null)
            return;

        Transform owner = launcher.transform;
        Vector3 muzzlePos = launcher.MuzzlePosition;
        if (!PlayerWeaponAim.TryGetAimPoint(muzzlePos, owner, previewDistance, out Vector3 aimPoint, out _))
        {
            trajectoryDot.gameObject.SetActive(false);
            return;
        }

        Vector3 screen = Camera.main.WorldToScreenPoint(aimPoint);
        if (screen.z <= 0f)
        {
            trajectoryDot.gameObject.SetActive(false);
            return;
        }

        trajectoryDot.position = screen;
        trajectoryDot.gameObject.SetActive(true);
    }

    // Proceduralnie instaluje na glownym plotnie celownik krzyzowy wraz ze wskaźnikiem trajektorii, parametryzujac ich grafike.
    private void BuildUi()
    {
        if (SharedUIManager.Instance == null || SharedUIManager.Instance.MainCanvas == null)
            return;

        var root = CreateRect("ReticleRoot", SharedUIManager.Instance.MainCanvas.transform);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = Vector2.zero;

        CreateCrossBar(root, new Vector2(14f, 2f));
        CreateCrossBar(root, new Vector2(2f, 14f));

        trajectoryDot = CreateDot(root, new Vector2(10f, 10f), new Color(1f, 0.75f, 0.2f, 0.95f));
        trajectoryDot.anchoredPosition = Vector2.zero;
    }

    // Konstruuje podstawowy niewidoczny komponent prostokata na podstawie wezla transformacji o przypisanej domyslnie nazwie.
    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    // Wykorzystuje funkcje pomocnicza do utworzenia polprzezroczystego paska tworzacego ramie glownego krzyza celowniczego.
    private static void CreateCrossBar(RectTransform parent, Vector2 size)
    {
        var bar = CreateDot(parent, size, new Color(1f, 1f, 1f, 0.9f));
        bar.anchoredPosition = Vector2.zero;
    }

    // Instancjuje graficzna reprezentacje wezla punktowego na interfejsie i blokuje dla niego detekcje promieni kursora myszy.
    private static RectTransform CreateDot(RectTransform parent, Vector2 size, Color color)
    {
        var go = new GameObject("Dot", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return rect;
    }
}
