using System;
using UnityEngine;

namespace VirtualDarkStore.WorkStudy
{
    /// <summary>
    /// Represents an immutable work-study measurement event.
    /// Strictly contains only physically measurable telemetry; excludes uninstrumented physiological or cognitive claims.
    /// </summary>
    [Serializable]
    public class WorkEvent
    {
        [SerializeField] private string id;
        [SerializeField] private ActivityType activity;
        [SerializeField] private float startTime;
        [SerializeField] private float endTime;
        [SerializeField] private float duration;
        [SerializeField] private Vector3 position;
        [SerializeField] private string locationName;
        [SerializeField] private string relatedSku;
        [SerializeField] private string relatedOrder;
        [SerializeField] private float distance;

        public string Id => id;
        public ActivityType Activity => activity;
        public float StartTime => startTime;
        public float EndTime => endTime;
        public float Duration => duration;
        public Vector3 Position => position;
        public string LocationName => locationName;
        public string RelatedSku => relatedSku;
        public string RelatedOrder => relatedOrder;
        public float Distance => distance;

        public WorkEvent(
            string id,
            ActivityType activity,
            float startTime,
            float endTime,
            Vector3 position,
            string locationName = "",
            string relatedSku = "",
            string relatedOrder = "",
            float distance = 0f)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Event ID cannot be null or empty.", nameof(id));
            }

            if (startTime < 0f)
            {
                throw new ArgumentException($"StartTime ({startTime}) cannot be negative.", nameof(startTime));
            }

            if (endTime < startTime)
            {
                throw new ArgumentException($"EndTime ({endTime}) cannot be less than StartTime ({startTime}).", nameof(endTime));
            }

            if (distance < 0f)
            {
                throw new ArgumentException($"Distance ({distance}) cannot be negative.", nameof(distance));
            }

            this.id = id;
            this.activity = activity;
            this.startTime = startTime;
            this.endTime = endTime;
            this.duration = endTime - startTime;
            this.position = position;
            this.locationName = locationName ?? string.Empty;
            this.relatedSku = relatedSku ?? string.Empty;
            this.relatedOrder = relatedOrder ?? string.Empty;
            this.distance = distance;
        }

        public override string ToString()
        {
            return $"[{Id}] {Activity} | T: {StartTime:F2}s - {EndTime:F2}s ({Duration:F2}s) | Dist: {Distance:F2}m | Order: {RelatedOrder} | SKU: {RelatedSku} | Loc: {LocationName} | Pos: ({Position.x:F1}, {Position.y:F1}, {Position.z:F1})";
        }
    }
}
