using UnityEngine;

namespace VirtualDarkStore.Environment
{
    public enum ShelfSide
    {
        Left,
        Right
    }

    /// <summary>
    /// Component representing an industrial warehouse shelving rack or bay.
    /// Tracks aisle association, side, bay index, tier dimensions, and ensures solid collision.
    /// </summary>
    public class ShelfUnit : MonoBehaviour
    {
        [Header("Shelf Identity")]
        [SerializeField] private int aisleNumber = 1;
        [SerializeField] private ShelfSide side = ShelfSide.Left;
        [SerializeField] private int bayIndex = 1;
        [SerializeField] private int tierCount = 3;

        [Header("Dimensions (Meters)")]
        [SerializeField] private float rackDepth = 0.6f;
        [SerializeField] private float rackLength = 3.0f;
        [SerializeField] private float rackHeight = 2.2f;

        public int AisleNumber => aisleNumber;
        public ShelfSide Side => side;
        public int BayIndex => bayIndex;
        public int TierCount => tierCount;
        public float RackDepth => rackDepth;
        public float RackLength => rackLength;
        public float RackHeight => rackHeight;

        public void Initialize(int aisle, ShelfSide shelfSide, int bay, int tiers, float depth, float length, float height)
        {
            aisleNumber = aisle;
            side = shelfSide;
            bayIndex = bay;
            tierCount = tiers;
            rackDepth = depth;
            rackLength = length;
            rackHeight = height;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + Vector3.up * (rackHeight * 0.5f), new Vector3(rackDepth, rackHeight, rackLength));
        }
    }
}
