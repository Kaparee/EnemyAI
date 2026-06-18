using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ResourceDatabase", menuName = "Mining/ResourceDatabase")]
// Globalna baza danych przechowująca definicje i konfiguracje wszystkich typów surowców dostępnych w świecie gry.
public class ResourceDatabase : ScriptableObject
{

    [Header("Lista Surowców")]
    public List<ResourceDefinition> Resources = new List<ResourceDefinition>();

    // Losuje i zwraca typ surowca na podstawie ustalonych wag prawdopodobienstwa dla zadanego poziomu zaawansowania sektora.
    public ResourceDefinition GetRandomResource(int sectorStage)
    {

        float totalWeight = 0f;

        foreach (ResourceDefinition res in Resources) {
            totalWeight += res.weightsPerStage[sectorStage];
        }

        if (totalWeight <= 0) {
            return Resources[0];
        }

        float roll = Random.Range(0f, totalWeight);

        foreach (ResourceDefinition res in Resources) {
            if (roll <= res.weightsPerStage[sectorStage]) {
                return res;
            } else {
                roll -= res.weightsPerStage[sectorStage];
            }
        }
        return Resources[0];
    }
    // Zapisuje wszystkie konfiguracje i parametry zasobow z bazy danych do zewnetrznego pliku formacie JSON dla celow archiwizacji lub edycji.
    [ContextMenu("Eksportuj do JSON")]
    public void ExportToJSON() {
        string json = JsonUtility.ToJson(this, true);
        string sciezka = Application.dataPath + "/ResourcesExport.json";
        System.IO.File.WriteAllText(sciezka, json);
    }
}