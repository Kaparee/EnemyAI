using UnityEngine;

public class Sector : MonoBehaviour {
    private SectorData data;

    [SerializeField] private GameObject shopPrefab;
    [SerializeField] private GameObject repairStationPrefab;

    public void Setup(SectorData newData, float size) {
        this.data = newData;

        AreaSpawnerManager spawner = GetComponent<AreaSpawnerManager>();
        if (spawner != null) {
            spawner.currentSectorStage = data.sectorStage;
            spawner.InitialSpawn(data);
        }

        if (newData.haveShop)
        {
            GameObject shop = Instantiate(shopPrefab, transform);
            shop.transform.localPosition = data.shopLocalPos;
        }

        if (newData.haveRepairStation)
        {
            GameObject repairStation = Instantiate(repairStationPrefab, transform);
            repairStation.transform.localPosition = data.repairStationLocalPos;
        }
    }
}
