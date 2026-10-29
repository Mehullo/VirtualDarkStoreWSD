using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualDarkStore.Scenarios
{
    /// <summary>
    /// Simple, serializable description of one simulated fulfilment scenario.
    /// A scenario fixes the order (SKU list), the sequence in which the virtual picker visits the
    /// order lines, the starting position, and the fixed elemental parameters of the virtual picker.
    /// It contains no optimisation or prediction logic: the user defines the sequence explicitly.
    /// </summary>
    [Serializable]
    public class ScenarioDefinition
    {
        public string scenarioId = "SCN-A";
        public string scenarioName = "Scenario A";
        public string description = string.Empty;
        public string orderId = "ORD-SCN-0001";

        /// <summary>SKU IDs in the exact order the picker visits/picks them (this is also the order line sequence).</summary>
        public List<string> skuSequence = new List<string>();

        /// <summary>Picker start position (metres, world space). Picker stands here during the initial wait.</summary>
        public Vector3 startPosition = new Vector3(0f, 0f, -8.5f);

        /// <summary>Constant virtual walking speed (m/s).</summary>
        public float walkSpeed = 1.4f;

        /// <summary>Stationary time before the order is dispatched (s). Must be &gt; 0 (classifier treats 0 as 'unset').</summary>
        public float initialWaitSeconds = 1.0f;

        /// <summary>Stationary shelf-scanning time at every pick location (s).</summary>
        public float searchSeconds = 2.0f;

        /// <summary>Stationary time at the packing bench before packing is finalised (s).</summary>
        public float packingSeconds = 2.0f;

        /// <summary>Simulation time step (s) used to advance the classifier.</summary>
        public float timeStep = 0.1f;

        public bool Validate(out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(scenarioId)) { error = "scenarioId is empty."; return false; }
            if (string.IsNullOrWhiteSpace(orderId)) { error = "orderId is empty."; return false; }
            if (skuSequence == null || skuSequence.Count == 0) { error = "skuSequence must contain at least one SKU."; return false; }
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sku in skuSequence)
            {
                if (string.IsNullOrWhiteSpace(sku)) { error = "skuSequence contains an empty SKU id."; return false; }
                if (!seen.Add(sku.Trim())) { error = $"skuSequence contains duplicate SKU '{sku}'."; return false; }
            }
            if (walkSpeed <= 0.2f) { error = "walkSpeed must be greater than 0.2 m/s (above classifier stationary threshold)."; return false; }
            if (initialWaitSeconds <= 0f) { error = "initialWaitSeconds must be > 0."; return false; }
            if (searchSeconds < 0f || packingSeconds < 0f) { error = "searchSeconds/packingSeconds cannot be negative."; return false; }
            if (timeStep <= 0.001f || timeStep > 1f) { error = "timeStep must be within (0.001, 1] s."; return false; }
            return true;
        }

        public ScenarioDefinition Clone()
        {
            var clone = (ScenarioDefinition)MemberwiseClone();
            clone.skuSequence = new List<string>(skuSequence);
            return clone;
        }
    }
}
