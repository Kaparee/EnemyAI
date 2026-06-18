using UnityEngine;
using System.Collections.Generic;

// Oznacza precyzyjne współrzędne statku gracza, udostępniając jednostkom AI informację o ich głównym celu.
public class PlayerMarker : MonoBehaviour
{
    public static List<PlayerMarker> AllPlayers = new List<PlayerMarker>();

    // Dodaje znacznik obecnego gracza do globalnej listy aktywnych obiektow
    private void OnEnable() => AllPlayers.Add(this);
    // Usuwa znacznik gracza z globalnej listy po zniszczeniu lub wylaczeniu
    private void OnDisable() => AllPlayers.Remove(this);
}