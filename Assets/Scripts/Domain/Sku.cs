using System;
using UnityEngine;

namespace VirtualDarkStore.Domain
{
    /// <summary>
    /// Pure domain entity representing a Stock Keeping Unit (SKU) in the dark store.
    /// Completely decoupled from visual GameObjects and rendering.
    /// </summary>
    [Serializable]
    public class Sku
    {
        [SerializeField] private string skuId;
        [SerializeField] private string name;
        [SerializeField] private string barcode;
        [SerializeField] private string category;
        [SerializeField] private float weightKg;
        [SerializeField] private Vector3 dimensionsMeters;

        public string SkuId => skuId;
        public string Name => name;
        public string Barcode => barcode;
        public string Category => category;
        public float WeightKg => weightKg;
        public Vector3 DimensionsMeters => dimensionsMeters;

        public Sku() { }

        public Sku(string id, string productName, string productBarcode, string productCategory, float weight, Vector3 dimensions)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("SKU ID cannot be null or empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(productName)) throw new ArgumentException("Product name cannot be null or empty.", nameof(productName));
            if (string.IsNullOrWhiteSpace(productBarcode)) throw new ArgumentException("Barcode cannot be null or empty.", nameof(productBarcode));

            skuId = id.Trim();
            name = productName.Trim();
            barcode = productBarcode.Trim();
            category = productCategory ?? "General";
            weightKg = Mathf.Max(0.01f, weight);
            dimensionsMeters = dimensions;
        }

        public override string ToString() => $"[SKU: {skuId}] {name} ({category})";
    }
}
