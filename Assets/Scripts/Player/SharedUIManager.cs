using UnityEngine;
using UnityEngine.UI;

// Menedżer zapobiegający nakładaniu się wielu paneli UI (np. jednocześnie odpalonego ekwipunku i statystyk).
public class SharedUIManager : MonoBehaviour
{
    public static SharedUIManager Instance { get; private set; }

    public Canvas MainCanvas { get; private set; }

    // Egzekwuje wzorzec projektowy Singleton zwalniajac zdublowane instancje i wywolujac instalacje bazowego plotna ekranu.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildMainCanvas();
    }

    // Powoluje do zycia glowne plotno gry i naklada na nie automatyczne skalowanie zaleznie od fizycznej rozdzielczosci okna renderera.
    private void BuildMainCanvas()
    {
        var canvasGo = new GameObject("SharedMainCanvas");
        canvasGo.transform.SetParent(transform, false);

        MainCanvas = canvasGo.AddComponent<Canvas>();
        MainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        MainCanvas.sortingOrder = 50;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasGo.AddComponent<GraphicRaycaster>();
    }
}
