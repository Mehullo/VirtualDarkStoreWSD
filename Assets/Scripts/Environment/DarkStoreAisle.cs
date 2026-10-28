using UnityEngine;

namespace VirtualDarkStore.Environment
{
    /// <summary>
    /// Component representing a dark-store picking aisle.
    /// Provides aisle numbering, aisle bounding dimensions, and entry/exit navigation waypoints.
    /// </summary>
    public class DarkStoreAisle : MonoBehaviour
    {
        [Header("Aisle Identity")]
        [SerializeField] private int aisleNumber = 1;
        [SerializeField] private string aisleLabel = "Aisle 01";

        [Header("Dimensions (Meters)")]
        [SerializeField] private float aisleWidth = 1.8f;
        [SerializeField] private float aisleLength = 12.0f;

        [Header("Waypoints")]
        [SerializeField] private Transform frontEntrance;
        [SerializeField] private Transform backExit;

        public int AisleNumber => aisleNumber;
        public string AisleLabel => aisleLabel;
        public float AisleWidth => aisleWidth;
        public float AisleLength => aisleLength;
        public Transform FrontEntrance => frontEntrance;
        public Transform BackExit => backExit;

        public void Initialize(int number, string label, float width, float length, Transform front, Transform back)
        {
            aisleNumber = number;
            aisleLabel = label;
            aisleWidth = width;
            aisleLength = length;
            frontEntrance = front;
            backExit = back;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, new Vector3(aisleWidth, 3f, aisleLength));
        }
    }
}
