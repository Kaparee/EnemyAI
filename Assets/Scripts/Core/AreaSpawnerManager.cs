using UnityEngine;
using System.Collections.Generic;

public class AreaSpawnerManager : MonoBehaviour
{
    [Header("Resources")]
    public ResourceDatabase database;
    
    [Header("Area settings")]
    public Vector3 areaSize = new Vector3(300f, 40f, 80f);

    public Vector3 worldSpawnSize = new Vector3(500f, 0f, 500f);

    [Header("Objects")]
    public GameObject[] prefabs;
    private GameObject player;

    private List<GameObject> areas = new List<GameObject>();

    public int currentSectorStage;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }


    public void InitialSpawn(SectorData data) {
        if (!data.hasAsteroidGroup) return;

        foreach (BeltSavedData belt in data.belts) {
            // Najpierw sprawdzamy, czy wszytskie asteroidy w pasie są wykopane
            int emptyCount = 0;
            foreach (var ast in belt.asteroids) {
                if (ast.loot.Count == 0) emptyCount++;
            }

            // Jeśli tak, to kładziemy na niego lache
            if (emptyCount == belt.asteroids.Count) continue;

            // Stwórz Cube obszaru
            GameObject areaObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            areaObj.transform.position = transform.position + belt.beltCenter;
            areaObj.transform.localScale = areaSize;
            areaObj.transform.SetParent(this.transform);
            SetupMaterial(areaObj);

            foreach (AsteroidSavedData astData in belt.asteroids) {
                if (astData.loot.Count == 0) continue;

                Vector3 worldPos = (transform.position + belt.beltCenter) + astData.localPos;
                GameObject obj = Instantiate(prefabs[Random.Range(0, prefabs.Length)], worldPos, Quaternion.identity, this.transform);

                InteractableObject io = obj.GetComponent<InteractableObject>();
                if (io == null) {
                    io = obj.AddComponent<InteractableObject>();
                }

                io.manager = this;
                io.parentArea = areaObj;
                io.lootTable = astData.loot;
                io.myBelt = belt;
                io.myData = astData;
                

                Asteroid asteroidVisual = obj.GetComponent<Asteroid>();
                if (asteroidVisual != null) {
                    asteroidVisual.materials = astData.loot;
                }
            }
            areas.Add(areaObj);
        }
    }
    public void OnObjectInteracted(GameObject currentArea, BeltSavedData beltData)
    {
        // Sprawdzamy ile pasów jest pustych
        int emptyCount = 0;
        int totalAsteroids = beltData.asteroids.Count;

        foreach (var ast in beltData.asteroids) {
            if (ast.loot.Count == 0) emptyCount++;
        }


        // Jak wykopane asteroidy w pasie >80% to tworzy nowy pas na mapie
        float minedPercentage = (float)emptyCount / totalAsteroids;

        Debug.Log($"Pas: {beltData.beltCenter} | Wydobyto: {emptyCount}/{totalAsteroids} ({minedPercentage * 100}%)");
        if (minedPercentage >= 0.80f && !beltData.respawnTriggered) {
            Debug.Log("DEBUG: WARUNEK 80% SPEŁNIONY!");
            if (ChunkManager.Instance != null) {
                ChunkManager.Instance.TrySpawnNewBeltGlobal();
            }
            beltData.respawnTriggered = true; // Blokada, żeby respiło tylko raz
        }

        // Jak wykopiemy całe to go usuwa na ament
        if (emptyCount == totalAsteroids) {
            Debug.Log("Pas całkowicie wyczyszczony. Usuwam strefę.");
            if (areas.Contains(currentArea)) areas.Remove(currentArea);
            if (currentArea != null) Destroy(currentArea);
        }
    }

    public void SetupMaterial(GameObject area) {
        // To nam wywala żeby laser ze statku nie łapał cube tylko zawsze asteroide
        area.layer = 2;

        // Musi to być, bo nie da sie wleciec w strefe
        BoxCollider col = area.GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.enabled = true;

        MeshRenderer mr = area.GetComponent<MeshRenderer>();
        if (mr != null) {
            mr.enabled = false;
        }

        MeshFilter mf = area.GetComponent<MeshFilter>();
        if (mf != null) {
            Destroy(mf);
        }
    }
}
