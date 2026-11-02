using System;
using UnityEngine;
using UnityEngine.AI;

namespace VirtualDarkStore.Routing
{
    /// <summary>
    /// Abstraction for calculating actual traversable travel distances between two points.
    /// Decouples path calculation from navigation implementation, enabling unit testing without baked NavMesh scenes.
    /// </summary>
    public interface INavPathDistanceCalculator
    {
        bool TryCalculateDistance(Vector3 from, Vector3 to, out float distance);
    }

    /// <summary>
    /// Navigation distance calculator based on Unity NavMesh.
    /// Accurately computes path length along corridor corners rather than misleading straight-line Euclidean distance.
    /// Handles unreachable positions explicitly.
    /// </summary>
    public class NavMeshPathDistanceCalculator : INavPathDistanceCalculator
    {
        private readonly bool fallbackToEuclideanIfNoNavMesh;

        public NavMeshPathDistanceCalculator(bool fallbackToEuclideanIfNoNavMesh = false)
        {
            this.fallbackToEuclideanIfNoNavMesh = fallbackToEuclideanIfNoNavMesh;
        }

        public bool TryCalculateDistance(Vector3 from, Vector3 to, out float distance)
        {
            distance = float.PositiveInfinity;

            NavMeshPath path = new NavMeshPath();
            bool pathCalculated = NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);

            if (pathCalculated && path.status == NavMeshPathStatus.PathComplete && path.corners.Length >= 2)
            {
                float totalDist = 0f;
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    totalDist += Vector3.Distance(path.corners[i], path.corners[i + 1]);
                }
                distance = totalDist;
                return true;
            }

            if (pathCalculated && path.status == NavMeshPathStatus.PathComplete && path.corners.Length <= 1)
            {
                // From and To are identical or negligible distance
                distance = 0f;
                return true;
            }

            // If no NavMesh is loaded (e.g. in EditMode unit tests without loaded scene)
            if (fallbackToEuclideanIfNoNavMesh)
            {
                distance = Vector3.Distance(from, to);
                return true;
            }

            // Truly unreachable path
            return false;
        }
    }

    /// <summary>
    /// Direct Manhattan / Euclidean distance calculator for deterministic unit testing of matrix math and TSP solvers.
    /// </summary>
    public class MockDistanceCalculator : INavPathDistanceCalculator
    {
        private readonly Func<Vector3, Vector3, float?> distanceFunc;

        public MockDistanceCalculator(Func<Vector3, Vector3, float?> distanceFunc = null)
        {
            this.distanceFunc = distanceFunc;
        }

        public bool TryCalculateDistance(Vector3 from, Vector3 to, out float distance)
        {
            if (distanceFunc != null)
            {
                float? res = distanceFunc(from, to);
                if (res.HasValue)
                {
                    distance = res.Value;
                    return true;
                }
                distance = float.PositiveInfinity;
                return false;
            }

            // Default: 2D Manhattan distance simulating warehouse grid corridors
            distance = Mathf.Abs(from.x - to.x) + Mathf.Abs(from.z - to.z);
            return true;
        }
    }
}
