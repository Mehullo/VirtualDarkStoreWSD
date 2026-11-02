using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualDarkStore.Routing
{
    /// <summary>
    /// Reusable square travel-cost matrix for dark store multi-stop pick routing.
    /// Encapsulates node positions, descriptive labels, and traversable path costs.
    /// Strictly validates dimensions, zero diagonal, and explicit unreachable handling.
    /// </summary>
    [Serializable]
    public class TravelCostMatrix
    {
        [SerializeField] private int nodeCount;
        [SerializeField] private List<Vector3> nodePositions = new List<Vector3>();
        [SerializeField] private List<string> nodeLabels = new List<string>();
        private float[,] costs;

        public int NodeCount => nodeCount;
        public IReadOnlyList<Vector3> NodePositions => nodePositions;
        public IReadOnlyList<string> NodeLabels => nodeLabels;

        public TravelCostMatrix(int count, IList<Vector3> positions, IList<string> labels, float[,] costMatrix)
        {
            if (count < 2) throw new ArgumentOutOfRangeException(nameof(count), "Cost matrix must contain at least 2 nodes (start and end).");
            if (positions == null || positions.Count != count) throw new ArgumentException("Positions count must match nodeCount.", nameof(positions));
            if (costMatrix == null || costMatrix.GetLength(0) != count || costMatrix.GetLength(1) != count)
            {
                throw new ArgumentException("Cost matrix dimensions must be square and match nodeCount.", nameof(costMatrix));
            }

            nodeCount = count;
            nodePositions = new List<Vector3>(positions);
            nodeLabels = labels != null ? new List<string>(labels) : new List<string>();
            while (nodeLabels.Count < nodeCount)
            {
                nodeLabels.Add($"Node-{nodeLabels.Count}");
            }

            costs = new float[count, count];
            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    costs[i, j] = costMatrix[i, j];
                }
            }
        }

        public float GetCost(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= nodeCount) throw new ArgumentOutOfRangeException(nameof(fromIndex));
            if (toIndex < 0 || toIndex >= nodeCount) throw new ArgumentOutOfRangeException(nameof(toIndex));
            if (costs == null) return float.PositiveInfinity;
            return costs[fromIndex, toIndex];
        }

        public bool IsReachable(int fromIndex, int toIndex)
        {
            float c = GetCost(fromIndex, toIndex);
            return !float.IsPositiveInfinity(c) && !float.IsNaN(c) && c >= 0f;
        }

        /// <summary>
        /// Validates mathematical consistency of the travel-cost matrix:
        /// 1. Node count >= 2.
        /// 2. Matrix is square with matching dimensions.
        /// 3. Diagonal entries are strictly 0.0f (cost from node to itself is zero).
        /// 4. Non-diagonal entries are non-negative or positive infinity.
        /// </summary>
        public bool Validate(out string errorReason)
        {
            errorReason = null;

            if (nodeCount < 2)
            {
                errorReason = $"Invalid node count: {nodeCount}. Minimum 2 nodes required.";
                return false;
            }

            if (costs == null || costs.GetLength(0) != nodeCount || costs.GetLength(1) != nodeCount)
            {
                errorReason = $"Matrix dimension mismatch: expected [{nodeCount}x{nodeCount}].";
                return false;
            }

            for (int i = 0; i < nodeCount; i++)
            {
                if (Mathf.Abs(costs[i, i]) > 0.0001f)
                {
                    errorReason = $"Non-zero diagonal at node {i} ('{nodeLabels[i]}'): cost={costs[i, i]}.";
                    return false;
                }

                for (int j = 0; j < nodeCount; j++)
                {
                    float c = costs[i, j];
                    if (float.IsNaN(c) || c < 0f)
                    {
                        errorReason = $"Invalid cost entry at [{i},{j}]: {c}.";
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Factory method to build a travel cost matrix from start position, stop positions, and end position.
        /// Node 0 = Start (Order Desk).
        /// Nodes 1..K = Stop positions (SKU bins).
        /// Node K+1 = End (Packing Bench).
        /// </summary>
        public static TravelCostMatrix Build(
            Vector3 startPos,
            IReadOnlyList<Vector3> stopPositions,
            Vector3 endPos,
            IReadOnlyList<string> stopLabels = null,
            INavPathDistanceCalculator distanceCalc = null)
        {
            if (stopPositions == null) stopPositions = Array.Empty<Vector3>();
            int k = stopPositions.Count;
            int totalNodes = k + 2;

            var positions = new List<Vector3>(totalNodes);
            var labels = new List<string>(totalNodes);

            // Node 0: Start
            positions.Add(startPos);
            labels.Add("Start Desk");

            // Nodes 1..K: Stops
            for (int i = 0; i < k; i++)
            {
                positions.Add(stopPositions[i]);
                string lbl = (stopLabels != null && i < stopLabels.Count) ? stopLabels[i] : $"Stop-{i + 1}";
                labels.Add(lbl);
            }

            // Node K+1: End
            positions.Add(endPos);
            labels.Add("Packing Bench");

            if (distanceCalc == null)
            {
                distanceCalc = new NavMeshPathDistanceCalculator(fallbackToEuclideanIfNoNavMesh: true);
            }

            float[,] matrix = new float[totalNodes, totalNodes];

            for (int i = 0; i < totalNodes; i++)
            {
                for (int j = 0; j < totalNodes; j++)
                {
                    if (i == j)
                    {
                        matrix[i, j] = 0f;
                    }
                    else
                    {
                        if (distanceCalc.TryCalculateDistance(positions[i], positions[j], out float dist))
                        {
                            matrix[i, j] = dist;
                        }
                        else
                        {
                            matrix[i, j] = float.PositiveInfinity;
                        }
                    }
                }
            }

            return new TravelCostMatrix(totalNodes, positions, labels, matrix);
        }
    }
}
