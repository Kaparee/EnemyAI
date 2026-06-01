using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class ManualAStar
{
    private struct GridKey : IEquatable<GridKey>
    {
        public int x, y, z;

        public static GridKey From(Vector3 pos, float step)
        {
            return new GridKey
            {
                x = Mathf.RoundToInt(pos.x / step),
                y = Mathf.RoundToInt(pos.y / step),
                z = Mathf.RoundToInt(pos.z / step)
            };
        }

        public bool Equals(GridKey other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object obj) => obj is GridKey other && Equals(other);
        public override int GetHashCode() => (x * 73856093) ^ (y * 19349663) ^ (z * 83492791);
    }

    private class Node
    {
        public Vector3 position;
        public GridKey key;
        public Node parent;
        public float gCost;
        public float hCost;
        public float FCost => gCost + hCost;
    }

    public static IEnumerator FindPathCoroutine(Vector3 start, Vector3 target, float nodeRadius, LayerMask obstacleMask, Action<List<Vector3>> callback)
    {
        float step = nodeRadius * 2f;
        List<Node> openList = new List<Node>();
        HashSet<GridKey> closedSet = new HashSet<GridKey>();
        Dictionary<GridKey, Node> nodeLookup = new Dictionary<GridKey, Node>();

        Vector3 snappedStart = SnapToGrid(start, step);
        Vector3 snappedTarget = SnapToGrid(target, step);

        AILog.AStar(
            $"Start: szukam drogi z {snappedStart} do {snappedTarget} (krok siatki {step:F0}).");

        Node startNode = new Node
        {
            position = snappedStart,
            key = GridKey.From(snappedStart, step),
            gCost = 0,
            hCost = Vector3.Distance(snappedStart, snappedTarget)
        };
        openList.Add(startNode);
        nodeLookup[startNode.key] = startNode;

        int maxIterations = 400;
        int currentIteration = 0;

        while (openList.Count > 0 && currentIteration < maxIterations)
        {
            currentIteration++;

            if (currentIteration % 40 == 0)
                yield return null;

            Node currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].FCost < currentNode.FCost ||
                    (Mathf.Approximately(openList[i].FCost, currentNode.FCost) && openList[i].hCost < currentNode.hCost))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedSet.Add(currentNode.key);

            if (Vector3.Distance(currentNode.position, snappedTarget) <= step)
            {
                List<Vector3> path = RetracePath(startNode, currentNode);
                AILog.AStar(
                    $"Znalazłem trasę: {path.Count} punktów, sprawdziłem {currentIteration} komórek siatki.");
                callback?.Invoke(path);
                yield break;
            }

            foreach (Vector3 neighborPos in GetNeighbors(currentNode.position, step))
            {
                GridKey neighborKey = GridKey.From(neighborPos, step);
                if (closedSet.Contains(neighborKey)) continue;

                if (IsBlocked(neighborPos, nodeRadius, obstacleMask))
                    continue;

                float newGCost = currentNode.gCost + Vector3.Distance(currentNode.position, neighborPos);

                if (!nodeLookup.TryGetValue(neighborKey, out Node neighborNode))
                {
                    neighborNode = new Node { position = neighborPos, key = neighborKey };
                    nodeLookup[neighborKey] = neighborNode;
                    openList.Add(neighborNode);
                }
                else if (newGCost >= neighborNode.gCost)
                {
                    continue;
                }

                neighborNode.gCost = newGCost;
                neighborNode.hCost = Vector3.Distance(neighborPos, snappedTarget);
                neighborNode.parent = currentNode;
            }
        }

        AILog.AStar(
            $"Koniec — brak drogi po {currentIteration} krokach (za dużo asteroid albo limit).");
        callback?.Invoke(new List<Vector3>());
    }

    private static bool IsBlocked(Vector3 pos, float checkRadius, LayerMask obstacleMask)
    {
        if (ObstacleRegistry.Instance != null && ObstacleRegistry.Instance.IsPositionBlocked(pos, checkRadius * 0.9f))
            return true;

        if (obstacleMask.value != 0)
            return Physics.CheckSphere(pos, checkRadius * 0.85f, obstacleMask, QueryTriggerInteraction.Ignore);

        return false;
    }

    private static Vector3 SnapToGrid(Vector3 pos, float step)
    {
        return new Vector3(
            Mathf.Round(pos.x / step) * step,
            Mathf.Round(pos.y / step) * step,
            Mathf.Round(pos.z / step) * step
        );
    }

    private static List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }

    private static List<Vector3> GetNeighbors(Vector3 pos, float step)
    {
        return new List<Vector3>
        {
            pos + new Vector3(step, 0, 0),
            pos + new Vector3(-step, 0, 0),
            pos + new Vector3(0, step, 0),
            pos + new Vector3(0, -step, 0),
            pos + new Vector3(0, 0, step),
            pos + new Vector3(0, 0, -step)
        };
    }
}
