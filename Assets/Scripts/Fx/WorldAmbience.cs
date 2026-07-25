using System.Collections.Generic;
using UnityEngine;

public class WorldAmbience : MonoBehaviour
{
    public int tumbleweeds = 6;
    public float speed = 3.5f;
    public float area = 40f;

    readonly List<Transform> weeds = new();
    Vector3 wind;

    void Start()
    {
        var rng = new System.Random();
        float a = (float)rng.NextDouble() * Mathf.PI * 2f;
        wind = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));

        for (int i = 0; i < tumbleweeds; i++)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            w.name = "Tumbleweed";
            Destroy(w.GetComponent<Collider>());
            w.transform.SetParent(transform);
            w.transform.localScale = Vector3.one * (0.6f + (float)rng.NextDouble() * 0.5f);
            w.AddComponent<ColorTint>().color = new Color(0.55f, 0.42f, 0.2f);
            w.transform.position = RandomPos(rng);
            weeds.Add(w.transform);
        }
    }

    Vector3 RandomPos(System.Random rng)
    {
        float x = ((float)rng.NextDouble() - 0.5f) * 2f * area;
        float z = ((float)rng.NextDouble() - 0.5f) * 2f * area;
        var p = transform.position + new Vector3(x, 0f, z);
        p.y = Ground(p) + 0.6f;
        return p;
    }

    static float Ground(Vector3 p)
    {
        if (Physics.Raycast(p + Vector3.up * 60f, Vector3.down, out var hit, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }
        return p.y;
    }

    void Update()
    {
        if (GameFlow.Instance != null && GameFlow.Instance.State != GameState.Explore)
        {
            return;
        }
        var roll = Vector3.Cross(Vector3.up, wind);
        foreach (var w in weeds)
        {
            w.position += wind * (speed * Time.deltaTime);
            w.Rotate(roll * (speed * 100f * Time.deltaTime), Space.World);

            if ((w.position - transform.position).magnitude > area * 1.5f)
            {
                var np = transform.position - wind * area + roll * ((Random.value - 0.5f) * area * 2f);
                np.y = Ground(np) + 0.6f;
                w.position = np;
            }
        }
    }
}
