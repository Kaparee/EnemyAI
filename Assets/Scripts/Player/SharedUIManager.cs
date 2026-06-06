using UnityEngine;
using UnityEngine.UI;

public class SharedUIManager : MonoBehaviour
{
    public static SharedUIManager Instance { get; private set; }

    public Canvas MainCanvas { get; private set; }

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
