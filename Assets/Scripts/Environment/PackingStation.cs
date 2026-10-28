using UnityEngine;

namespace VirtualDarkStore.Environment
{
    /// <summary>
    /// Component representing the packing and staging station at the front of the dark store.
    /// Includes packing bench, order dispatch/drop zone, and worker interaction bounds.
    /// </summary>
    public class PackingStation : MonoBehaviour
    {
        [Header("Station Identity")]
        [SerializeField] private string stationId = "PACK-01";
        [SerializeField] private string stationName = "Main Packing Bench";

        [Header("References")]
        [SerializeField] private Transform packingBench;
        [SerializeField] private Transform toteDropZone;
        [SerializeField] private Transform stagingSpawnPoint;

        public string StationId => stationId;
        public string StationName => stationName;
        public Transform PackingBench => packingBench;
        public Transform ToteDropZone => toteDropZone;
        public Transform StagingSpawnPoint => stagingSpawnPoint;

        public void Initialize(string id, string name, Transform bench, Transform dropZone, Transform spawnPoint)
        {
            stationId = id;
            stationName = name;
            packingBench = bench;
            toteDropZone = dropZone;
            stagingSpawnPoint = spawnPoint;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, new Vector3(3f, 2f, 2f));
        }
    }
}
