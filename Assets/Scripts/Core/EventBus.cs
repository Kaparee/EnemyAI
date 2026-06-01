using System;
using UnityEngine;

public static class EventBus
{
    public static Action<Transform> OnPlayerDetected;
    public static Action<EnemyAI> OnEnemyDeath;

    public static void TriggerOnPlayerDetected(Transform player) => OnPlayerDetected?.Invoke(player);
    public static void TriggerOnEnemyDeath(EnemyAI enemy) => OnEnemyDeath?.Invoke(enemy);
}
