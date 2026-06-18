using UnityEngine;

// Steruje obrotem wieżyczek statku gracza, celując w punkt wskazywany przez kursor myszy na ekranie.
public static class PlayerWeaponAim
{
    // Oblicza wektor kierunkowy dla wiezyczki bazujac na wskazywanym celu z ignorowaniem odleglosci
    public static Vector3 GetDirection(Vector3 muzzlePos, Transform owner)
    {
        TryGetAimPoint(muzzlePos, owner, 5000f, out _, out Vector3 direction);
        return direction;
    }

    // Wykonuje rzut promienia przez kamere aby znalezc precyzyjny punkt trafienia w swiecie gry
    public static bool TryGetAimPoint(Vector3 muzzlePos, Transform owner, float maxDistance, out Vector3 aimPoint, out Vector3 direction)
    {
        if (Camera.main == null)
        {
            aimPoint = muzzlePos + owner.forward * maxDistance;
            direction = owner.forward;
            return false;
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        aimPoint = ray.GetPoint(maxDistance);

        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        
        bool hitSomething = false;

        foreach (RaycastHit hit in hits)
        {
            if (IsOwnCollider(hit.collider, owner))
                continue;

            aimPoint = hit.point;
            hitSomething = true;
            break;
        }

        direction = (aimPoint - muzzlePos).normalized;

        return hitSomething;
    }

    // Sprawdza czy promien uderzyl we wlasny statek aby uniknac strzelania w siebie
    private static bool IsOwnCollider(Collider col, Transform owner)
    {
        return col.transform == owner || col.transform.IsChildOf(owner);
    }
}
