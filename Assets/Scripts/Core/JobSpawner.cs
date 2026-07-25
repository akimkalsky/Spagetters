using System.Collections.Generic;
using UnityEngine;

public class JobSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct Job
    {
        public string key;
        public string title;
    }

    public Job[] jobs;
    public float minRadius = 12f;
    public float maxRadius = 34f;
    public float minSeparation = 8f;
    public int seed = 0;

    void Start()
    {
        if (GameSettings.StoryMode || jobs == null || jobs.Length == 0)
        {
            return;
        }

        var rng = seed != 0 ? new System.Random(seed) : new System.Random();
        var placed = new List<Vector3>();

        foreach (var j in jobs)
        {
            Vector3 pos = transform.position;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float rad = Mathf.Lerp(minRadius, maxRadius, (float)rng.NextDouble());
                var candidate = transform.position + new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);

                bool ok = true;
                foreach (var p in placed)
                {
                    if ((p - candidate).sqrMagnitude < minSeparation * minSeparation)
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                {
                    pos = candidate;
                    break;
                }
            }

            pos.y = GroundHeight(pos, transform.position.y);
            placed.Add(pos);
            Spawn(j, pos);
        }
    }

    void Spawn(Job j, Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "JobStation_" + j.key;
        go.transform.localScale = new Vector3(1.3f, 1.6f, 1.3f);
        go.transform.position = new Vector3(pos.x, pos.y + 0.8f, pos.z);
        go.AddComponent<ColorTint>().color = new Color(0.45f, 0.5f, 0.85f);

        var js = go.AddComponent<JobStation>();
        js.minigameKey = j.key;
        js.title = j.title;
        js.interactDistance = 4;
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
