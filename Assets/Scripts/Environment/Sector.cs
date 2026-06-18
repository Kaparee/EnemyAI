using UnityEngine;

// Konfiguruje parametry mniejszego wycinka przestrzeni (poziom trudności, ilość wrogów) dla wewnętrznych spawnerów.
public class Sector : MonoBehaviour {
    private SectorData data;

    // Przypisuje nowe dane konfiguracyjne do sektora oraz inicjalizuje menedzer spawnowania dla odpowiedniej fazy.
    public void Setup(SectorData newData, float size) {
        this.data = newData;

        AreaSpawnerManager spawner = GetComponent<AreaSpawnerManager>();
        if (spawner != null) {
            spawner.currentSectorStage = data.sectorStage;
            spawner.InitialSpawn(data);
        }

    }
}
