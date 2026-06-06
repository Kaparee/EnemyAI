using UnityEngine;

public static class AILog
{
    private const string Tag = "<color=#88CCFF>[System AI]</color>";

    public static void AStar(string message)
    {
        Debug.Log($"{Tag} <color=#FFD966>[Nawigacja A*]</color> {message}");
    }
    public static void Minimax(string message)
    {
        Debug.Log($"{Tag} <color=#FF8866>[Decyzje Minimax]</color> {message}");
    }
    public static void FSM(string message)
    {
        Debug.Log($"{Tag} <color=#AAFFAA>[Stan FSM]</color> {message}");
    }
    public static void Radar(string message)
    {
        Debug.Log($"{Tag} <color=#FFAAFF>[Radar / Sensory]</color> {message}");
    }
    public static void Reactive(string message)
    {
        Debug.Log($"{Tag} <color=#FFFF88>[Unikanie Kolizji]</color> {message}");
    }
    public static void Leading(string message)
    {
        Debug.Log($"{Tag} <color=#88FFFF>[Przewidywanie Celu]</color> {message}");
    }
    public static void Patrol(string message)
    {
        Debug.Log($"{Tag} <color=#CCCCCC>[Patrolowanie]</color> {message}");
    }
    public static void Theory(string subsystem, string message)
    {
        Debug.Log($"{Tag} <color=#FFFFFF>[{subsystem}]</color> {message}");
    }
}
