using UnityEngine;

public static class EnemyRuntimeFactory
{
    public static GameObject Create(Vector3 position, Quaternion rotation, GameObject projectilePrefab, Mesh hullMesh = null)
    {
        GameObject root = new GameObject("EnemyWroga");
        root.transform.SetPositionAndRotation(position, rotation);
        root.tag = "Enemy";

        if (hullMesh != null)
        {
            MeshFilter mf = root.AddComponent<MeshFilter>();
            mf.sharedMesh = hullMesh;
            MeshRenderer mr = root.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
        else
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(4f, 1.5f, 8f);
            Object.Destroy(visual.GetComponent<BoxCollider>());
        }

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 40000f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 1.2f;

        MeshCollider mc = root.AddComponent<MeshCollider>();
        mc.convex = true;
        if (hullMesh != null)
        {
            MeshFilter mf = root.GetComponent<MeshFilter>();
            mc.sharedMesh = mf != null ? mf.sharedMesh : hullMesh;
        }

        ShipStats stats = root.AddComponent<ShipStats>();
        stats.SetMaxHP(240f);
        stats.SetHP(240f);

        EnemyAI ai = root.AddComponent<EnemyAI>();
        root.AddComponent<TacticalBrain>();
        root.AddComponent<ObstacleAvoidance>();
        root.AddComponent<CustomRadarSystem>();

        Turret main = CreateTurret(root.transform, "MainTurret", new Vector3(0f, 0.5f, 1f), projectilePrefab, -90f, 90f);
        ai.mainTurret = main;

        return root;
    }

    private static Turret CreateTurret(Transform parent, string name, Vector3 localPos, GameObject projectilePrefab, float minYaw, float maxYaw)
    {
        GameObject turretGo = new GameObject(name);
        turretGo.transform.SetParent(parent, false);
        turretGo.transform.localPosition = localPos;

        GameObject head = new GameObject("Head");
        head.transform.SetParent(turretGo.transform, false);

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(head.transform, false);
        firePoint.transform.localPosition = new Vector3(0f, 0f, 1.5f);

        Turret turret = turretGo.AddComponent<Turret>();
        turret.turretHead = head.transform;
        turret.firePoint = firePoint.transform;
        turret.projectilePrefab = projectilePrefab;
        turret.minYaw = minYaw;
        turret.maxYaw = maxYaw;
        turret.fireRate = 0.5f;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.GetComponent<ShipStats>() != null)
            turret.projectileDamage = player.GetComponent<ShipStats>().GetMaxHP() / 6.0f;
        else
            turret.projectileDamage = 17f;

        return turret;
    }
}
