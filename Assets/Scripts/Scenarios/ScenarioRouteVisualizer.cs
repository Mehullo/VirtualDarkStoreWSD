using System.Collections.Generic;
using UnityEngine;

namespace VirtualDarkStore.Scenarios
{
    /// <summary>
    /// In-world route visualisation: draws the recorded route of a run as a LineRenderer just above the floor.
    /// </summary>
    public class ScenarioRouteVisualizer : MonoBehaviour
    {
        [SerializeField] private float lineHeight = 0.08f;
        [SerializeField] private float lineWidth = 0.12f;

        private readonly List<LineRenderer> lines = new List<LineRenderer>();

        public LineRenderer ShowRoute(ScenarioRunResult run, Color color)
        {
            if (run == null || run.routePoints.Count < 2) return null;
            var go = new GameObject($"Route_{run.scenario.scenarioId}");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = color;
            lr.startWidth = lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.positionCount = run.routePoints.Count;
            for (int i = 0; i < run.routePoints.Count; i++)
            {
                var p = run.routePoints[i];
                lr.SetPosition(i, new Vector3(p.x, lineHeight, p.z));
            }
            lines.Add(lr);
            return lr;
        }

        public void Clear()
        {
            foreach (var l in lines) if (l != null) Destroy(l.gameObject);
            lines.Clear();
        }
    }
}
