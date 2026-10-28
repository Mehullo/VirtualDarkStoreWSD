using System.Collections.Generic;
using UnityEngine;

namespace VirtualDarkStore.Domain
{
    /// <summary>
    /// Provides a deterministic, verified catalog of 72 realistic dark-store grocery SKUs.
    /// Structured across 8 micro-fulfillment product categories with realistic weights and dimensions.
    /// </summary>
    public static class VerifiedSampleCatalog
    {
        public static List<Sku> CreateCatalog()
        {
            var skus = new List<Sku>(72);

            // Category 1: Beverages (10 SKUs)
            skus.Add(new Sku("SKU-0001", "Cold Brew Coffee 330ml", "890100000001", "Beverages", 0.35f, new Vector3(0.06f, 0.16f, 0.06f)));
            skus.Add(new Sku("SKU-0002", "Sparkling Water Lemon 500ml", "890100000002", "Beverages", 0.52f, new Vector3(0.07f, 0.22f, 0.07f)));
            skus.Add(new Sku("SKU-0003", "Orange Juice Fresh 1L", "890100000003", "Beverages", 1.05f, new Vector3(0.09f, 0.24f, 0.09f)));
            skus.Add(new Sku("SKU-0004", "Green Tea Unsweetened 500ml", "890100000004", "Beverages", 0.51f, new Vector3(0.07f, 0.21f, 0.07f)));
            skus.Add(new Sku("SKU-0005", "Energy Drink Berry 250ml", "890100000005", "Beverages", 0.27f, new Vector3(0.05f, 0.14f, 0.05f)));
            skus.Add(new Sku("SKU-0006", "Coconut Water Pure 330ml", "890100000006", "Beverages", 0.34f, new Vector3(0.06f, 0.15f, 0.06f)));
            skus.Add(new Sku("SKU-0007", "Apple Juice 100% 1L", "890100000007", "Beverages", 1.04f, new Vector3(0.09f, 0.24f, 0.09f)));
            skus.Add(new Sku("SKU-0008", "Ginger Ale Can 330ml", "890100000008", "Beverages", 0.36f, new Vector3(0.06f, 0.12f, 0.06f)));
            skus.Add(new Sku("SKU-0009", "Electrolyte Hydration Drink 500ml", "890100000009", "Beverages", 0.53f, new Vector3(0.07f, 0.22f, 0.07f)));
            skus.Add(new Sku("SKU-0010", "Kombucha Ginger Lemon 350ml", "890100000010", "Beverages", 0.37f, new Vector3(0.06f, 0.17f, 0.06f)));

            // Category 2: Dairy & Alternatives (10 SKUs)
            skus.Add(new Sku("SKU-0011", "Whole Milk 1L", "890100000011", "Dairy & Alternatives", 1.03f, new Vector3(0.08f, 0.22f, 0.08f)));
            skus.Add(new Sku("SKU-0012", "Almond Milk Unsweetened 1L", "890100000012", "Dairy & Alternatives", 1.02f, new Vector3(0.08f, 0.22f, 0.08f)));
            skus.Add(new Sku("SKU-0013", "Oat Milk Barista Edition 1L", "890100000013", "Dairy & Alternatives", 1.04f, new Vector3(0.08f, 0.22f, 0.08f)));
            skus.Add(new Sku("SKU-0014", "Greek Yogurt Plain 500g", "890100000014", "Dairy & Alternatives", 0.53f, new Vector3(0.11f, 0.09f, 0.11f)));
            skus.Add(new Sku("SKU-0015", "Salted Butter 250g", "890100000015", "Dairy & Alternatives", 0.26f, new Vector3(0.12f, 0.06f, 0.06f)));
            skus.Add(new Sku("SKU-0016", "Cheddar Cheese Block 200g", "890100000016", "Dairy & Alternatives", 0.21f, new Vector3(0.10f, 0.05f, 0.08f)));
            skus.Add(new Sku("SKU-0017", "Cottage Cheese Tub 400g", "890100000017", "Dairy & Alternatives", 0.42f, new Vector3(0.10f, 0.08f, 0.10f)));
            skus.Add(new Sku("SKU-0018", "Mozzarella Shredded 200g", "890100000018", "Dairy & Alternatives", 0.22f, new Vector3(0.14f, 0.20f, 0.04f)));
            skus.Add(new Sku("SKU-0019", "Soy Milk Vanilla 1L", "890100000019", "Dairy & Alternatives", 1.03f, new Vector3(0.08f, 0.22f, 0.08f)));
            skus.Add(new Sku("SKU-0020", "Sour Cream Tub 250g", "890100000020", "Dairy & Alternatives", 0.27f, new Vector3(0.09f, 0.07f, 0.09f)));

            // Category 3: Bakery & Breakfast (8 SKUs)
            skus.Add(new Sku("SKU-0021", "Whole Wheat Sandwich Bread 400g", "890100000021", "Bakery & Breakfast", 0.42f, new Vector3(0.12f, 0.14f, 0.26f)));
            skus.Add(new Sku("SKU-0022", "Multigrain Sourdough Loaf 500g", "890100000022", "Bakery & Breakfast", 0.52f, new Vector3(0.14f, 0.12f, 0.22f)));
            skus.Add(new Sku("SKU-0023", "Butter Croissants Pack of 4", "890100000023", "Bakery & Breakfast", 0.24f, new Vector3(0.18f, 0.08f, 0.24f)));
            skus.Add(new Sku("SKU-0024", "Rolled Oats Whole Grain 1kg", "890100000024", "Bakery & Breakfast", 1.02f, new Vector3(0.14f, 0.24f, 0.09f)));
            skus.Add(new Sku("SKU-0025", "Granola Honey Almond 400g", "890100000025", "Bakery & Breakfast", 0.43f, new Vector3(0.13f, 0.22f, 0.06f)));
            skus.Add(new Sku("SKU-0026", "Plain Bagels 6-Pack", "890100000026", "Bakery & Breakfast", 0.45f, new Vector3(0.12f, 0.28f, 0.12f)));
            skus.Add(new Sku("SKU-0027", "Pancake Mix Original 500g", "890100000027", "Bakery & Breakfast", 0.53f, new Vector3(0.12f, 0.18f, 0.05f)));
            skus.Add(new Sku("SKU-0028", "Maple Syrup Grade A 250ml", "890100000028", "Bakery & Breakfast", 0.38f, new Vector3(0.08f, 0.16f, 0.05f)));

            // Category 4: Snacks & Confectionery (10 SKUs)
            skus.Add(new Sku("SKU-0029", "Sea Salt Potato Chips 150g", "890100000029", "Snacks & Confectionery", 0.17f, new Vector3(0.16f, 0.28f, 0.08f)));
            skus.Add(new Sku("SKU-0030", "Tortilla Chips Jalapeno 200g", "890100000030", "Snacks & Confectionery", 0.22f, new Vector3(0.17f, 0.30f, 0.09f)));
            skus.Add(new Sku("SKU-0031", "Roasted Salted Almonds 200g", "890100000031", "Snacks & Confectionery", 0.21f, new Vector3(0.11f, 0.18f, 0.05f)));
            skus.Add(new Sku("SKU-0032", "Dark Chocolate Bar 70% 100g", "890100000032", "Snacks & Confectionery", 0.11f, new Vector3(0.08f, 0.18f, 0.01f)));
            skus.Add(new Sku("SKU-0033", "Gummy Bears Organic 150g", "890100000033", "Snacks & Confectionery", 0.16f, new Vector3(0.10f, 0.16f, 0.03f)));
            skus.Add(new Sku("SKU-0034", "Pretzel Crisps Original 200g", "890100000034", "Snacks & Confectionery", 0.22f, new Vector3(0.15f, 0.24f, 0.06f)));
            skus.Add(new Sku("SKU-0035", "Protein Bar Peanut Butter 60g", "890100000035", "Snacks & Confectionery", 0.07f, new Vector3(0.05f, 0.14f, 0.02f)));
            skus.Add(new Sku("SKU-0036", "Microwave Popcorn Butter 3-Pack", "890100000036", "Snacks & Confectionery", 0.31f, new Vector3(0.13f, 0.16f, 0.08f)));
            skus.Add(new Sku("SKU-0037", "Rice Crackers Tamari 100g", "890100000037", "Snacks & Confectionery", 0.12f, new Vector3(0.10f, 0.18f, 0.04f)));
            skus.Add(new Sku("SKU-0038", "Trail Mix Energy Blend 250g", "890100000038", "Snacks & Confectionery", 0.26f, new Vector3(0.12f, 0.20f, 0.05f)));

            // Category 5: Pantry & Canned Goods (10 SKUs)
            skus.Add(new Sku("SKU-0039", "Extra Virgin Olive Oil 500ml", "890100000039", "Pantry & Canned Goods", 0.65f, new Vector3(0.07f, 0.25f, 0.07f)));
            skus.Add(new Sku("SKU-0040", "Organic Diced Tomatoes Can 400g", "890100000040", "Pantry & Canned Goods", 0.46f, new Vector3(0.08f, 0.11f, 0.08f)));
            skus.Add(new Sku("SKU-0041", "Black Beans Canned 400g", "890100000041", "Pantry & Canned Goods", 0.45f, new Vector3(0.08f, 0.11f, 0.08f)));
            skus.Add(new Sku("SKU-0042", "Chickpeas Organic Can 400g", "890100000042", "Pantry & Canned Goods", 0.45f, new Vector3(0.08f, 0.11f, 0.08f)));
            skus.Add(new Sku("SKU-0043", "Spaghetti Pasta Durum 500g", "890100000043", "Pantry & Canned Goods", 0.52f, new Vector3(0.08f, 0.30f, 0.04f)));
            skus.Add(new Sku("SKU-0044", "Penne Rigate Pasta 500g", "890100000044", "Pantry & Canned Goods", 0.52f, new Vector3(0.14f, 0.20f, 0.06f)));
            skus.Add(new Sku("SKU-0045", "Basmati Rice Royal 1kg", "890100000045", "Pantry & Canned Goods", 1.03f, new Vector3(0.15f, 0.26f, 0.08f)));
            skus.Add(new Sku("SKU-0046", "Marinara Pasta Sauce Jar 500g", "890100000046", "Pantry & Canned Goods", 0.72f, new Vector3(0.09f, 0.16f, 0.09f)));
            skus.Add(new Sku("SKU-0047", "Peanut Butter Creamy 450g", "890100000047", "Pantry & Canned Goods", 0.51f, new Vector3(0.09f, 0.13f, 0.09f)));
            skus.Add(new Sku("SKU-0048", "Raw Forest Honey 350g", "890100000048", "Pantry & Canned Goods", 0.48f, new Vector3(0.08f, 0.12f, 0.08f)));

            // Category 6: Fresh Packaged Produce (8 SKUs)
            skus.Add(new Sku("SKU-0049", "Baby Spinach Washed Tub 200g", "890100000049", "Fresh Produce", 0.24f, new Vector3(0.18f, 0.10f, 0.18f)));
            skus.Add(new Sku("SKU-0050", "Cherry Tomatoes Punnet 250g", "890100000050", "Fresh Produce", 0.28f, new Vector3(0.14f, 0.07f, 0.10f)));
            skus.Add(new Sku("SKU-0051", "Avocados Ready-to-Eat 2-Pack", "890100000051", "Fresh Produce", 0.35f, new Vector3(0.12f, 0.09f, 0.18f)));
            skus.Add(new Sku("SKU-0052", "Organic Bananas Bunch 600g", "890100000052", "Fresh Produce", 0.64f, new Vector3(0.14f, 0.16f, 0.22f)));
            skus.Add(new Sku("SKU-0053", "Crisp Gala Apples Bag 1kg", "890100000053", "Fresh Produce", 1.05f, new Vector3(0.18f, 0.28f, 0.16f)));
            skus.Add(new Sku("SKU-0054", "Mini Cucumbers Snack Pack 300g", "890100000054", "Fresh Produce", 0.33f, new Vector3(0.12f, 0.06f, 0.16f)));
            skus.Add(new Sku("SKU-0055", "Button Mushrooms Fresh 250g", "890100000055", "Fresh Produce", 0.27f, new Vector3(0.13f, 0.08f, 0.13f)));
            skus.Add(new Sku("SKU-0056", "Yellow Lemons Net Bag 500g", "890100000056", "Fresh Produce", 0.53f, new Vector3(0.12f, 0.18f, 0.12f)));

            // Category 7: Personal Care & Hygiene (8 SKUs)
            skus.Add(new Sku("SKU-0057", "Liquid Hand Soap Lavender 300ml", "890100000057", "Personal Care", 0.36f, new Vector3(0.07f, 0.16f, 0.07f)));
            skus.Add(new Sku("SKU-0058", "Moisturizing Body Wash 500ml", "890100000058", "Personal Care", 0.56f, new Vector3(0.08f, 0.21f, 0.05f)));
            skus.Add(new Sku("SKU-0059", "Shampoo Hydrating Gentle 400ml", "890100000059", "Personal Care", 0.46f, new Vector3(0.07f, 0.20f, 0.05f)));
            skus.Add(new Sku("SKU-0060", "Fluoride Toothpaste Mint 100g", "890100000060", "Personal Care", 0.13f, new Vector3(0.05f, 0.19f, 0.04f)));
            skus.Add(new Sku("SKU-0061", "Toothbrush Soft Bristles 2-Pack", "890100000061", "Personal Care", 0.06f, new Vector3(0.04f, 0.22f, 0.02f)));
            skus.Add(new Sku("SKU-0062", "Hand Cream Shea Butter 75ml", "890100000062", "Personal Care", 0.09f, new Vector3(0.04f, 0.14f, 0.04f)));
            skus.Add(new Sku("SKU-0063", "Facial Cleanser Foam 150ml", "890100000063", "Personal Care", 0.19f, new Vector3(0.05f, 0.17f, 0.05f)));
            skus.Add(new Sku("SKU-0064", "Lip Balm SPF 15 2-Pack", "890100000064", "Personal Care", 0.03f, new Vector3(0.03f, 0.09f, 0.02f)));

            // Category 8: Household Cleaning (8 SKUs)
            skus.Add(new Sku("SKU-0065", "All-Purpose Cleaner Spray 750ml", "890100000065", "Household Cleaning", 0.82f, new Vector3(0.10f, 0.28f, 0.07f)));
            skus.Add(new Sku("SKU-0066", "Dishwashing Liquid Citrus 500ml", "890100000066", "Household Cleaning", 0.55f, new Vector3(0.08f, 0.22f, 0.05f)));
            skus.Add(new Sku("SKU-0067", "Laundry Detergent Pods 20-Pack", "890100000067", "Household Cleaning", 0.48f, new Vector3(0.18f, 0.14f, 0.10f)));
            skus.Add(new Sku("SKU-0068", "Disinfecting Surface Wipes 80-Pack", "890100000068", "Household Cleaning", 0.45f, new Vector3(0.11f, 0.21f, 0.11f)));
            skus.Add(new Sku("SKU-0069", "Kitchen Paper Towels 2-Roll Pack", "890100000069", "Household Cleaning", 0.36f, new Vector3(0.14f, 0.26f, 0.24f)));
            skus.Add(new Sku("SKU-0070", "Microfiber Cleaning Cloths 4-Pack", "890100000070", "Household Cleaning", 0.15f, new Vector3(0.16f, 0.18f, 0.04f)));
            skus.Add(new Sku("SKU-0071", "Heavy Duty Scrub Sponges 3-Pack", "890100000071", "Household Cleaning", 0.08f, new Vector3(0.10f, 0.16f, 0.06f)));
            skus.Add(new Sku("SKU-0072", "Trash Bags Drawstring 30L 20-Pack", "890100000072", "Household Cleaning", 0.32f, new Vector3(0.08f, 0.20f, 0.08f)));

            return skus;
        }
    }
}
