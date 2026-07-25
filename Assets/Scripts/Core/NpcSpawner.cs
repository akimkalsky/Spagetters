using System.Collections.Generic;
using UnityEngine;

public class NpcSpawner : MonoBehaviour
{
    public GameObject npcPrefab;
    public int count = 6;
    public float minRadius = 10f;
    public float maxRadius = 32f;
    public float minSeparation = 6f;
    public int duelDistance = 3;
    public int seed = 0;

    void Start()
    {
        Debug.Log("Random NPCs: " + GameManager.Instance.randomNpcSpawns);
        if (GameManager.Instance != null &&
    !GameManager.Instance.randomNpcSpawns)
        {
            return;
        }

        if (npcPrefab == null)
        {
            return;
        }

        var rng = seed != 0 ? new System.Random(seed) : new System.Random();
        var placed = new List<Vector3>();
        int guard = count * 30;

        while (placed.Count < count && guard-- > 0)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(minRadius, maxRadius, (float)rng.NextDouble());
            var pos = transform.position + new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);

            if (TooClose(placed, pos))
            {
                continue;
            }

            pos.y = GroundHeight(pos, transform.position.y);
            placed.Add(pos);

            float yaw = (float)rng.NextDouble() * 360f;
            var npc = Instantiate(npcPrefab, pos, Quaternion.Euler(0f, yaw, 0f));
            npc.name = $"DuelNpc {placed.Count}";

            var rends = npc.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                var bounds = rends[0].bounds;
                for (int r = 1; r < rends.Length; r++)
                {
                    bounds.Encapsulate(rends[r].bounds);
                }
                float below = pos.y - bounds.min.y;
                if (below > 0f)
                {
                    npc.transform.position += Vector3.up * below;
                }
            }

            var goon = npc.GetComponentInChildren<Goon>(true);
            if (goon != null)
            {
                goon.duelDistance = duelDistance;
                if (RivalRoster.Count > 0)
                {
                    goon.rivalIndex = (placed.Count - 1) % RivalRoster.Count;
                }
            }
        }
    }

    bool TooClose(List<Vector3> placed, Vector3 pos)
    {
        foreach (var p in placed)
        {
            if ((p - pos).sqrMagnitude < minSeparation * minSeparation)
            {
                return true;
            }
        }
        return false;
    }

    static float GroundHeight(Vector3 pos, float fallback)
    {
        var origin = pos + Vector3.up * 60f;
        if (Physics.Raycast(origin, Vector3.down, out var hit, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }
        return fallback;
    }
}
