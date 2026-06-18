using UnityEngine;

// Definicje warstw (layers) używanych przez AI do raycastów.
// Optymalizuje fizykę, ograniczając sprawdzanie kolizji tylko do odpowiednich masek (np. omijanie własnych jednostek).
public static class AILayers
{
    public const string PlayerLayerName = "Player";
    public const string AsteroidLayerName = "Asteroid";

    public static int Player => LayerMask.NameToLayer(PlayerLayerName);
    public static int Asteroid => LayerMask.NameToLayer(AsteroidLayerName);

    public static LayerMask PlayerMask
    {
        get
        {
            int layer = Player;
            return layer >= 0 ? (1 << layer) : 0;
        }
    }

    public static LayerMask AsteroidMask
    {
        get
        {
            int layer = Asteroid;
            return layer >= 0 ? (1 << layer) : 0;
        }
    }

    // Rekursywnie ustawia warstwe dla wybranego obiektu oraz wszystkich jego dzieci w hierarchii transformacji.
    public static void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null || layer < 0) return;
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
