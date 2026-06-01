using UnityEngine;

public static class PlayerWeaponAim
{
    public static Vector3 GetDirection(Vector3 muzzlePos, Transform owner)
    {
        TryGetAimPoint(muzzlePos, owner, 5000f, out _, out Vector3 direction);
        return direction;
    }

    public static bool TryGetAimPoint(Vector3 muzzlePos, Transform owner, float maxDistance, out Vector3 aimPoint, out Vector3 direction)
    {
        aimPoint = muzzlePos + Vector3.forward * maxDistance;
        direction = Vector3.forward;

        if (Camera.main == null)
            return false;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        direction = ray.direction;
        aimPoint = ray.GetPoint(maxDistance);

        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (IsOwnCollider(hit.collider, owner))
                continue;

            aimPoint = hit.point;
            direction = (aimPoint - muzzlePos).normalized;
            break;
        }

        if (Vector3.Dot(direction, ray.direction) < 0.05f)
            direction = ray.direction;

        return true;
    }

    private static bool IsOwnCollider(Collider col, Transform owner)
    {
        return col.transform == owner || col.transform.IsChildOf(owner);
    }
}
