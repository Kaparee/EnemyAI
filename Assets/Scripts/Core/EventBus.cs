using System;
using UnityEngine;

public static class EventBus
{
    public static Action<Transform> OnPlayerDetected;
    public static Action<EnemyAI> OnEnemyDeath;
}
