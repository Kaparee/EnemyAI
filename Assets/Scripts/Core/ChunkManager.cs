using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[System.Serializable]
// System zarządzania fragmentami świata (chunks).
// Optymalizuje pamięć i rendering poprzez ładowanie/rozładowywanie sektorów w miarę przemieszczania się gracza.
public class AsteroidSavedData {
    public Vector3 localPos;
}

[System.Serializable]
public class BeltSavedData {
    public Vector3 beltCenter;
    public List<AsteroidSavedData> asteroids = new List<AsteroidSavedData>();
    public bool respawnTriggered = false;
}

public class SectorData {
    public Vector2Int gridPosition;
    public int sectorStage;
    public bool hasAsteroidGroup;
    public bool wasSpawned = false;
    public List<BeltSavedData> belts = new List<BeltSavedData>();
}

public class ChunkManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI sectorInfo;
    [SerializeField] private MapDisplay mapDisplay;

    public static ChunkManager Instance;

    // Przypisuje instancje singletona, umozliwiajac globalny dostep do zarzadcy sektorow.
    void Awake() {
        Instance = this;
    }

    public int mapCols { get; private set; } = 1;
    public int mapRows { get; private set; } = 1;
    [SerializeField] private float sectorSize = 900f;
    public float SectorSize => sectorSize;
    [SerializeField] private Transform player;
    public Transform Player => player;
    public Vector2Int CurrentPlayerSector => currentPlayerSector;

    // Oblicza i zwraca srodek podanego lub aktualnego sektora w globalnej przestrzeni swiata.
    public Vector3 GetSectorWorldCenter(Vector2Int? grid = null)
    {
        Vector2Int g = grid ?? currentPlayerSector;
        if (g.x < 0) g = Vector2Int.zero;
        return new Vector3(g.x * sectorSize + sectorSize * 0.5f, 0f, g.y * sectorSize + sectorSize * 0.5f);
    }

    // Zwraca polowe wielkosci boku sektora, co jest przydatne do obliczen promienia dzialania algorytmow i granic.
    public float GetSectorHalfExtent() => sectorSize * 0.5f;

    // Sprawdza, czy podana globalna pozycja znajduje sie wewnatrz zdefiniowanego sektora z uwzglednieniem dodatkowego marginesu.
    public bool IsInsideSector(Vector3 worldPos, Vector2Int? grid = null, float margin = 0f)
    {
        Vector3 center = GetSectorWorldCenter(grid);
        float half = GetSectorHalfExtent() - margin;
        return Mathf.Abs(worldPos.x - center.x) <= half
            && Mathf.Abs(worldPos.z - center.z) <= half;
    }

    // Ogranicza wskazana pozycje na scenie do granic wyznaczonego sektora, nie pozwalajac obiektom na wyjscie poza jego zasieg.
    public Vector3 ClampToSector(Vector3 worldPos, Vector2Int? grid = null, float margin = 5f)
    {
        Vector3 center = GetSectorWorldCenter(grid);
        float half = GetSectorHalfExtent() - margin;
        worldPos.x = Mathf.Clamp(worldPos.x, center.x - half, center.x + half);
        worldPos.z = Mathf.Clamp(worldPos.z, center.z - half, center.z + half);
        return worldPos;
    }

    // Wyszukuje gracza i po raz pierwszy oblicza jego przynaleznosc do sektora gridu, ladujac odpowiedni widok.
    private void InitPlayerSector()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player == null) return;

        int playerXPosition = Mathf.FloorToInt(player.position.x / sectorSize);
        int playerZPosition = Mathf.FloorToInt(player.position.z / sectorSize);
        currentPlayerSector = new Vector2Int(playerXPosition, playerZPosition);
        RefreshSectorView(currentPlayerSector);
    }
    [SerializeField] private GameObject sector;
    private GameObject currentSectorObject = null;

    public Dictionary<Vector2Int, SectorData> allSectorData { get; private set; } = new Dictionary<Vector2Int, SectorData>();

    private Vector2Int currentPlayerSector = new Vector2Int(-1, -1);

    public static int globalGroupCount = 0;
    public int maxGlobalGroups = 20;

    // Przygotowuje dane startowe dla calej siatki mapy, przydzielajac odpowiednie poziomy trudnosci i generujac glowne struktury.
    private void GenerateWorldData() {

        List<Vector2Int> allCoords = new List<Vector2Int>();

        for (int x = 0; x < mapCols; x++) {
            for (int y = 0; y < mapRows; y++) {
                Vector2Int pos = new Vector2Int(x, y);
                allCoords.Add(pos);

                SectorData newData = new SectorData();
                int stage = Mathf.Max(x, y);
                newData.gridPosition = pos;
                newData.sectorStage = Mathf.Clamp(stage == 0 ? 0 : stage - 1, 0, 4);
                newData.hasAsteroidGroup = false;
                newData.belts = new List<BeltSavedData>();

                allSectorData.Add(pos, newData);
            }
        }

        allCoords.Remove(Vector2Int.zero);
        SectorData startSector = allSectorData[Vector2Int.zero];
        startSector.hasAsteroidGroup = true;
        PopulateSectorWithBelts(startSector);

        int groupsToSpawn = 29;
        for (int i = 0; i < groupsToSpawn; i++) {
            if (allCoords.Count == 0) break;

            int randomIndex = Random.Range(0, allCoords.Count);
            Vector2Int picked = allCoords[randomIndex];
            allCoords.RemoveAt(randomIndex);

            SectorData sd = allSectorData[picked];
            sd.hasAsteroidGroup = true;
            PopulateSectorWithBelts(sd);
        }

        if (mapDisplay != null) {
            mapDisplay.GenerateMapUI();
        }
    }

    // Losuje trojwymiarowe koordynaty wewnatrz przestrzeni ograniczonej rozmiarami aktualnie wczytanego sektora.
    public Vector3 GenerateRandomCords()
    {
        float limit = (sectorSize / 2f);
        return new Vector3(
            Random.Range(-limit, limit),
            Random.Range(-limit, limit),
            Random.Range(-limit, limit)
        );
    }

    // Odswieza renderowanie sektora, niszczac stary i instancjonujac nowy obiekt wizualizujacy, bazujac na obliczonych koordynatach.
    private void RefreshSectorView(Vector2Int sectorCooRD) {
        if (currentSectorObject != null) {
            Destroy(currentSectorObject);
        }
        if (sector == null)
        {
            Debug.LogError("<color=red>Brak przypisanego prefaba sektora w ChunkManager!</color>");
            return;
        }

        Vector3 sectorSpawnPos = new Vector3((sectorCooRD.x * sectorSize) + (sectorSize / 2f), 0, (sectorCooRD.y * sectorSize) + (sectorSize / 2f));
        currentSectorObject = Instantiate(sector, sectorSpawnPos, Quaternion.identity);

        if (allSectorData.ContainsKey(sectorCooRD)) {
            SectorData dataFromMemory = allSectorData[sectorCooRD];

            Sector sectorScript = currentSectorObject.GetComponent<Sector>();
            if (sectorScript != null) {
                sectorScript.Setup(dataFromMemory, sectorSize);
            }
            string x = ((char)('A' + sectorCooRD.y)).ToString();
            int y = sectorCooRD.x + 1;
            if (sectorInfo != null) {
                sectorInfo.SetText("Aktualny Sektor: " + x + y);
            }
        }
    }

    // Wypelnia dane sektora parametrami o losowych pasach asteroid, przypisujac im pozycje i rozmiary do pozniejszego instancjonowania.
    private void PopulateSectorWithBelts(SectorData targetSector) {
        float halfSector = sectorSize / 2f;
        float safeLimit = halfSector - 100f;

        int beltCount = Random.Range(3, 9);
        for (int b = 0; b < beltCount; b++) {
            BeltSavedData belt = new BeltSavedData();

            belt.beltCenter = new Vector3(
                Random.Range(-safeLimit, safeLimit),
                Random.Range(-safeLimit, safeLimit),
                Random.Range(-safeLimit, safeLimit)
            );

            int astCount = Random.Range(5, 11);
            for (int a = 0; a < astCount; a++) {
                AsteroidSavedData ast = new AsteroidSavedData();
                // Rozrzucamy asteroidy znacznie szerzej na dużym obszarze
                ast.localPos = new Vector3(Random.Range(-120f, 120f), Random.Range(-30f, 30f), Random.Range(-120f, 120f));
                belt.asteroids.Add(ast);
            }
            targetSector.belts.Add(belt);
        }
    }

    // Uruchamia sekwencje generowania swiata oraz konfiguracji sektora poczatkowego gracza tu po uruchomieniu obiektu.
    void Start()
    {
        GenerateWorldData();
        InitPlayerSector();
    }

    // Główna pętla logiczna klatki. Staram się tu minimalizować ciężkie obliczenia.

    // Wykonuje sie co klatke, aktualizujac ograniczenia pozycji gracza i weryfikujac jego przejscia pomiedzy sektorami.
    void Update()
    {
        if (player == null) {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else return;
        }

        float maxX = mapCols * sectorSize;
        float maxZ = mapRows * sectorSize;

        Vector3 limitedPos = player.position;
        limitedPos.x = Mathf.Clamp(limitedPos.x, 0, maxX - 1);
        limitedPos.z = Mathf.Clamp(limitedPos.z, 0, maxZ - 1);
        limitedPos.y = Mathf.Clamp(limitedPos.y, -sectorSize / 2f, sectorSize / 2f);
        player.position = limitedPos;

        int playerXPosition = Mathf.FloorToInt(player.position.x / sectorSize);
        int playerZPosition = Mathf.FloorToInt(player.position.z / sectorSize);

        Vector2Int currentPos = new Vector2Int(playerXPosition, playerZPosition);

        if (currentPos != currentPlayerSector) {
            currentPlayerSector = currentPos;
            RefreshSectorView(currentPos);
        }
    }

    // Probuje wygenerowac nowy pas asteroid globalnie w losowym sektorze (poza obecnym), o ile istnieja odpowiednie miejsca.
    public void TrySpawnNewBeltGlobal() {
        if (allSectorData.Count == 0) return;

        List<SectorData> availableSectors = new List<SectorData>();
        foreach (var sector in allSectorData) {
            if (sector.Key != currentPlayerSector && sector.Value.belts.Count < 9) {
                availableSectors.Add(sector.Value);
            }
        }

        if (availableSectors.Count > 0) {
            SectorData targetSector = availableSectors[Random.Range(0, availableSectors.Count)];
            targetSector.hasAsteroidGroup = true;
            PopulateSectorWithBelts(targetSector);
        }
    }
}
