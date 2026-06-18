using System;
using UnityEngine;

// Magistrala zdarzeń (Event Bus). Rozsyła powiadomienia między skryptami (np. o zniszczeniu statku), pozbywając się twardych zależności.
public static class EventBus
{
    public static Action<Transform> OnPlayerDetected;
    public static Action<EnemyAI> OnEnemyDeath;
    public static Action OnPlayerDeath;

    // Wywoluje zdarzenie informujace systemy o wykryciu gracza przez przeciwnika lub inny system detekcji.
    public static void TriggerOnPlayerDetected(Transform player) => OnPlayerDetected?.Invoke(player);
    // Wysyla globalne powiadomienie o smierci jednostki przeciwnika do innych skryptow nasluchujacych.
    public static void TriggerOnEnemyDeath(EnemyAI enemy) => OnEnemyDeath?.Invoke(enemy);
    // Propaguje zdarzenie zakonczenia gry wywolane calkowitym zniszczeniem gracza.
    public static void TriggerOnPlayerDeath() => OnPlayerDeath?.Invoke();
}
