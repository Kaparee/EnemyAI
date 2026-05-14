using UnityEngine;

public class Sector : MonoBehaviour {
    private SectorData data;

    public void Setup(SectorData newData, float size) {
        this.data = newData;

        AreaSpawnerManager spawner = GetComponent<AreaSpawnerManager>();
        if (spawner != null) {
            spawner.currentSectorStage = data.sectorStage;
            spawner.InitialSpawn(data);
        }

    }
}
