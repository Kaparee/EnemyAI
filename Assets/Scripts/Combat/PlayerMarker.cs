using UnityEngine;
using System.Collections.Generic;

public class PlayerMarker : MonoBehaviour
{
    public static List<PlayerMarker> AllPlayers = new List<PlayerMarker>();

    private void OnEnable() => AllPlayers.Add(this);
    private void OnDisable() => AllPlayers.Remove(this);
}