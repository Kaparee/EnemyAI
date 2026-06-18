using UnityEngine;

// Klasa pomocnicza do szczegółowego logowania działań AI.
// Bardzo przydatna przy debugowaniu drzew zachowań i ścieżek A* bez zaśmiecania standardowej konsoli.
public static class AILog
{
    private const string Tag = "<color=#88CCFF>[System AI]</color>";

    // Rejestruje w konsoli wiadomosc dotyczaca procesu znajdowania sciezki algorytmem A*.
    public static void AStar(string message)
    {
        Debug.Log($"{Tag} <color=#FFD966>[Nawigacja A*]</color> {message}");
    }
    // Loguje informacje powiazane z drzewem decyzyjnym oraz ocena stanow w algorytmie Minimax.
    public static void Minimax(string message)
    {
        Debug.Log($"{Tag} <color=#FF8866>[Decyzje Minimax]</color> {message}");
    }
    // Zapisuje zdarzenia i zmiany stanow w maszynie stanow skonczonych (FSM) sztucznej inteligencji.
    public static void FSM(string message)
    {
        Debug.Log($"{Tag} <color=#AAFFAA>[Stan FSM]</color> {message}");
    }
    // Formatuje i wypisuje logi z dzialania systemow sensorycznych i radarowych sprawdzajacych otoczenie.
    public static void Radar(string message)
    {
        Debug.Log($"{Tag} <color=#FFAAFF>[Radar / Sensory]</color> {message}");
    }
    // Raportuje akcje podejmowane w trybie reaktywnym, na przyklad podczas naglego unikania kolizji z przeszkodami.
    public static void Reactive(string message)
    {
        Debug.Log($"{Tag} <color=#FFFF88>[Unikanie Kolizji]</color> {message}");
    }
    // Zwraca w konsoli parametry zwiazane z celowaniem z wyprzedzeniem i przewidywaniem wektora ruchu przeciwnika.
    public static void Leading(string message)
    {
        Debug.Log($"{Tag} <color=#88FFFF>[Przewidywanie Celu]</color> {message}");
    }
    // Wyswietla informacje o statusie jednostki przebywajacej w trybie rutynowego patrolowania sektora.
    public static void Patrol(string message)
    {
        Debug.Log($"{Tag} <color=#CCCCCC>[Patrolowanie]</color> {message}");
    }
    // Uniwersalna metoda logujaca niestandardowe komunikaty dla wybranego podsystemu logiki sztucznej inteligencji.
    public static void Theory(string subsystem, string message)
    {
        Debug.Log($"{Tag} <color=#FFFFFF>[{subsystem}]</color> {message}");
    }
}
