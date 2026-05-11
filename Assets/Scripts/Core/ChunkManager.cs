using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class AsteroidSavedData {
    public Vector3 localPos;
    public List<ResourceStack> loot = new List<ResourceStack>();
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
    public bool haveShop;
    public Vector3 shopLocalPos;
    public bool haveRepairStation;
    public Vector3 repairStationLocalPos;
}

public class ChunkManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI sectorInfo;
    [SerializeField] private MapDisplay mapDisplay;

    public static ChunkManager Instance;

    void Awake() {
        Instance = this;
    }

    public int mapCols { get; private set; } = 6;
    public int mapRows { get; private set; } = 6;
    // TO jest bardzo do zmiany, bo nie wiem ile daæ ¿eby by³o ok, narazie do testó 4x4x4 km starczy raczej
    [SerializeField] private float sectorSize  = 400f ;
    public float SectorSize => sectorSize;
    [SerializeField] private Transform player;
    public Transform Player => player;
    [SerializeField] private GameObject sector;
    [SerializeField] private ResourceDatabase resourceDB;
    private GameObject currentSectorObject = null;

    public Dictionary<Vector2Int, SectorData> allSectorData { get; private set; } = new Dictionary<Vector2Int, SectorData>();

    private Vector2Int currentPlayerSector = new Vector2Int(-1, -1);

    public static int globalGroupCount = 0;
    public int maxGlobalGroups = 20;

    // Generacja 6x6 mapy oraz predefiniowanie danych w asteroidach na ca³ej mapie
    private void GenerateWorldData() {
        List<Vector2Int> allCoords = new List<Vector2Int>();

        // Tworzenie pustych sektorów
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
                if ((x == 0 && y == 0) || (x == 3 && y == 3) || (x == 5 && y == 5))
                {
                    newData.haveShop = true;
                    newData.haveRepairStation = true;
                    float limit = (sectorSize / 2f);
                    Vector3 shopLocalPosition = GenerateRandomCords();
                    Vector3 repairStationLocalPosition = GenerateRandomCords();

                    if (Vector3.Distance(shopLocalPosition, repairStationLocalPosition) < 300)
                    {
                        repairStationLocalPosition = GenerateRandomCords();
                    }

                    newData.repairStationLocalPos = repairStationLocalPosition;
                    newData.shopLocalPos = shopLocalPosition;
                }
                else
                {
                    newData.haveShop = false;
                    newData.haveRepairStation = false;
                }

                allSectorData.Add(pos, newData);
            }
        }

        // Usuwamy sektor startowy z puli losowania i robimy tak, ¿eby ZAWSZE by³y tam asteroidy
        allCoords.Remove(Vector2Int.zero);
        SectorData startSector = allSectorData[Vector2Int.zero];
        startSector.hasAsteroidGroup = true;
        PopulateSectorWithBelts(startSector); // Odpalamy generator dla startówki


        // 29 bo usuwamy jeden sektor startowy z puli losowania, ale to chyba bêdziemy mieli do zmiany zobacyzmy ju¿ jak dodamy latanie i zbieranie, czy nie jest za du¿o
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

        // TEST DO KONSOLI
        //DebugMapStats();

        mapDisplay.GenerateMapUI();
    }

    public Vector3 GenerateRandomCords()
    {
        float limit = (sectorSize / 2f);
        Vector3 randomCords = new Vector3(
            Random.Range(-limit, limit),
            Random.Range(-limit, limit),
            Random.Range(-limit, limit)
        );

        return randomCords;
    }

    //public void DebugMapStats() {
    //    int totalSectorsWithAsteroids = 0;
    //    int totalBelts = 0;
    //    int totalAsteroids = 0;

    //    foreach (var data in allSectorData.Values) {
    //        if (data.hasAsteroidGroup) {
    //            totalSectorsWithAsteroids++;
    //            totalBelts += data.belts.Count;
    //            foreach (var belt in data.belts) {
    //                totalAsteroids += belt.asteroids.Count;
    //            }
    //        }
    //    }

    //    Debug.Log("RAPORT GENERACJI ŒWIATA");
    //    Debug.Log($"Sektory z asteroidami: {totalSectorsWithAsteroids} / 36");
    //    Debug.Log($"£¹czna liczba pasów: {totalBelts}");
    //    Debug.Log($"£¹czna liczba asteroid w pamiêci: {totalAsteroids}");
    //}

    private List<ResourceStack> PreGenerateLoot(int stage) {
        List<ResourceStack> generatedLoot = new List<ResourceStack>();
        int totalUnits = Random.Range(40, 121);
        int typesCount = Random.Range(3, 7);
        int amountPerType = totalUnits / typesCount;

        for (int i = 0; i < typesCount; i++) {
            int roll = Random.Range(0, 100);
            int targetStage = stage;
            if (roll < 20) targetStage--;
            else if (roll >= 90) targetStage++;

            ResourceDefinition res = resourceDB.GetRandomResource(Mathf.Clamp(targetStage, 0, 4));
            if (res != null) {
                generatedLoot.Add(new ResourceStack { definition = res, amount = amountPerType });
            }
        }
        return generatedLoot;
    }

    // Wyœwietlanie, aktualizacja i usuwanie aktywnego sektora
    private void RefreshSectorView(Vector2Int sectorCooRD) {
        if (currentSectorObject != null) {
            Destroy(currentSectorObject);
        }
        Vector3 sectorSpawnPos = new Vector3((sectorCooRD.x * sectorSize) + (sectorSize / 2f), 0, (sectorCooRD.y * sectorSize) + (sectorSize / 2f));
        currentSectorObject = Instantiate(sector, sectorSpawnPos, Quaternion.identity);

        if (allSectorData.ContainsKey(sectorCooRD)) {
            SectorData dataFromMemory = allSectorData[sectorCooRD];

            Sector sectorScript = currentSectorObject.GetComponent<Sector>();
            if (sectorScript != null) {
                sectorScript.Setup(dataFromMemory, sectorSize);
            }
            string x = ((char)('A' + sectorCooRD[1])).ToString();
            int y = sectorCooRD[0] + 1;
            sectorInfo.SetText("Aktualny Sektor: "+x+y);
        }
    }

    // Generacja asteroid w pasie
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
                ast.localPos = new Vector3(Random.Range(-19f, 19f), Random.Range(-19f, 19f), Random.Range(-19f, 19f));
                ast.loot = PreGenerateLoot(targetSector.sectorStage);
                belt.asteroids.Add(ast);
            }
            targetSector.belts.Add(belt);
        }
    }

    // Rozruch
    void Start()
    {
        GenerateWorldData();
        //Debug.Log("Wygenerowano bazê danych sektorów: " + allSectorData.Count);
    }

    void Update()
    {
        if (player == null) {
            return;
        }
        // Ograniczenie Clampem, ¿eby nie wylecieæ za sektory (bo wywala b³êdy)
        float maxX = mapCols * sectorSize;
        float maxZ = mapRows * sectorSize;

        Vector3 limitedPos = player.position;
        limitedPos.x = Mathf.Clamp(limitedPos.x, 0, maxX - 1);
        limitedPos.z = Mathf.Clamp(limitedPos.z, 0, maxZ - 1);

        // ¯eby nie wylecieæ poza Y Sektora
        limitedPos.y = Mathf.Clamp(limitedPos.y, -sectorSize / 2f, sectorSize / 2f);
        player.position = limitedPos;

        // Pobieranie sektora w którym znajduje sie gracz
        int playerXPosition = Mathf.FloorToInt(player.position.x / sectorSize);
        int playerZPosition = Mathf.FloorToInt(player.position.z / sectorSize);

        Vector2Int currentPos = new Vector2Int(playerXPosition, playerZPosition);

        // Zmiana informacji o sektorze
        if (currentPos != currentPlayerSector) {
            currentPlayerSector = currentPos;
            //Debug.Log("Wlecia³em do nowego sektora:" + currentPlayerSector);
            RefreshSectorView(currentPos);
        }
    }

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
            Debug.Log($"GDD Respawn: Nowa grupa asteroid zespawnowana w sektorze {targetSector.gridPosition}");
        }
    }

    public List<string> GetSectorStats(SectorData sector) {
        Dictionary<ResourceDefinition, int> allResources = new Dictionary<ResourceDefinition, int>();

        foreach (var belt in sector.belts) {
            foreach (var asteroid in belt.asteroids) {
                foreach (var resource in asteroid.loot) {
                    if (allResources.ContainsKey(resource.definition)) {
                        allResources[resource.definition] += resource.amount;
                    }
                    else {
                        allResources.Add(resource.definition, resource.amount);
                    }
                }
            }
        }

        float sum = allResources.Sum(s => s.Value);
        var top_three = allResources.OrderByDescending(s => s.Value).Take(3);

        List<string> array = new List<string>();
        if (sum != 0) {
            array.Add($"£¹cznie: {sum}");
            foreach (var top in top_three) {
                float result = (top.Value / sum) * 100;
                array.Add($"{top.Key.Name}: {result:F0}%");
            }
        }
        else {
            array.Add("Nic nie znajdujê siê w wybranych sektorze");
        }
        return array;
    }
}
