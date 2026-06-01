using UnityEngine;

/// <summary>Proste logi do konsoli — widać co AI robi krok po kroku.</summary>
public static class AILog
{
    private const string Tag = "<color=#88CCFF>[Wróg AI]</color>";

    public static void AStar(string message) =>
        Debug.Log($"{Tag} <color=#FFD966>[A*]</color> {message}");

    public static void Minimax(string message) =>
        Debug.Log($"{Tag} <color=#FF8866>[Minimax]</color> {message}");

    public static void FSM(string message) =>
        Debug.Log($"{Tag} <color=#AAFFAA>[Stan]</color> {message}");

    public static void Radar(string message) =>
        Debug.Log($"{Tag} <color=#FFAAFF>[Radar]</color> {message}");

    public static void Reactive(string message) =>
        Debug.Log($"{Tag} <color=#FFFF88>[Unikanie]</color> {message}");

    public static void Leading(string message) =>
        Debug.Log($"{Tag} <color=#88FFFF>[Celowanie]</color> {message}");

    public static void Patrol(string message) =>
        Debug.Log($"{Tag} <color=#CCCCCC>[Patrol]</color> {message}");

    public static void Theory(string subsystem, string message) =>
        Debug.Log($"{Tag} <color=#FFFFFF>[{subsystem}]</color> {message}");
}
