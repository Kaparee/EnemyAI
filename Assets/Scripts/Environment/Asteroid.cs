using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Obsługuje logikę pojedynczej asteroidy – nadaje wytrzymałość, fizykę i przyjmuje na siebie obrażenia z pocisków.
public class Asteroid : MonoBehaviour
{
    public List<ResourceStack> materials = new List<ResourceStack>();

    // Oblicza wazona liste temperatur na podstawie ilosci poszczegolnych materialow w strukturze asteroidy.
    private List<float> GetWeightedTemps()
    {
        float sum = (float)materials.Sum(m => m.amount);
        if (sum == 0) return new List<float> { 0f };

        return materials
            .Select(m => (float)m.definition.optimalTemp * (1f + (float)m.amount / sum))
            .ToList();
    }

    // Wylicza prog tolerancji termicznej, agregujac i skalujac minimalna oraz srednia temperature zebranych materialow.
    public float ToleranceTemperature()
    {
        var temps = GetWeightedTemps();
        return Mathf.Ceil(temps.Min() * 0.8f + temps.Average() * 0.2f);
    }

    // Wyznacza maksymalna mozliwa temperature asteroidy poprzez analizie maksymalnych i srednich temperatur materialow.
    public float CalculateTemperature()
    {
        var temps = GetWeightedTemps();
        return Mathf.Ceil(temps.Max() * 0.8f + temps.Average() * 0.2f);
    }

    // Inicjalizuje poczatkowy stan obiektu natychmiast po jego uruchomieniu w scenie.
    void Start()
    {
    }
}
