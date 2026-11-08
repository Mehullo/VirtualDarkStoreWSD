using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Interaction;
using VirtualDarkStore.Management;
using VirtualDarkStore.Player;

namespace VirtualDarkStore.Tests.Environment
{
    /// <summary>
    /// Verification suite for Physical Location Labels and signage across the dark store.
    /// Verifies:
    /// 1. Aisle header labels exist and display correct human- and machine-readable identifiers.
    /// 2. Rack/bay placards correspond to actual StoreLocation data.
    /// 3. Shelf-level identifiers are correctly aligned with shelf tiers.
    /// 4. Product location shelf tickets match the exact simulation location and SKU IDs.
    /// 5. Labels have zero colliders and non-blocking raycast settings to prevent interference.
    /// 6. Desktop picker interaction prompt surfaces the exact location ID on hover.
    /// </summary>
    [TestFixture]
    public class PhysicalLocationLabelTests
    {
        private GameObject testRoot;

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("[Test_LocationLabels]");
        }

        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
            {
                Object.DestroyImmediate(testRoot);
            }
        }

        [Test]
        public void AisleHeaderLabel_InitializesWithCorrectLocationIdAndProperties()
        {
            var labelObj = PhysicalLocationLabel.CreateLabelObject(
                "Aisle01_Header",
                testRoot.transform,
                Vector3.zero,
                Quaternion.identity,
                LocationLabelType.AisleHeader,
                "AISLE A01",
                "PICKING AISLE",
                "A01",
                aisle: 1,
                width: 2.4f,
                height: 0.28f);

            var label = labelObj.GetComponent<PhysicalLocationLabel>();
            Assert.IsNotNull(label);
            Assert.AreEqual(LocationLabelType.AisleHeader, label.LabelType);
            Assert.AreEqual("AISLE A01", label.PrimaryText);
            Assert.AreEqual("PICKING AISLE", label.SecondaryText);
            Assert.AreEqual("A01", label.LocationId);
            Assert.AreEqual(1, label.AisleNumber);

            // Verify Canvas is WorldSpace
            var canvas = labelObj.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);

            // Verify no collider exists on the label
            Assert.IsNull(labelObj.GetComponent<Collider>(), "Labels must NOT have colliders to avoid NavMesh and physics interference.");

            // Verify all graphic components have raycastTarget = false
            var graphics = labelObj.GetComponentsInChildren<Graphic>();
            Assert.Greater(graphics.Length, 0);
            foreach (var g in graphics)
            {
                Assert.IsFalse(g.raycastTarget, $"Graphic {g.name} must have raycastTarget=false to allow picker interaction to pass through.");
            }
        }

        [Test]
        public void BayRackPlacard_MatchesStructuredLocationHierarchy()
        {
            int aisle = 3;
            ShelfSide side = ShelfSide.Left;
            int bay = 2;
            string expectedBayId = $"A{aisle:D2}-L-B{bay:D2}";

            var labelObj = PhysicalLocationLabel.CreateLabelObject(
                "Placard_A03_L_B02",
                testRoot.transform,
                new Vector3(1f, 2f, 3f),
                Quaternion.Euler(0f, -90f, 0f),
                LocationLabelType.BayRack,
                expectedBayId,
                "BAY 02 • LEFT",
                expectedBayId,
                aisle: aisle,
                side: side,
                bay: bay,
                width: 0.60f,
                height: 0.22f);

            var label = labelObj.GetComponent<PhysicalLocationLabel>();
            Assert.IsNotNull(label);
            Assert.AreEqual(LocationLabelType.BayRack, label.LabelType);
            Assert.AreEqual(expectedBayId, label.PrimaryText);
            Assert.AreEqual(expectedBayId, label.LocationId);
            Assert.AreEqual(aisle, label.AisleNumber);
            Assert.AreEqual(side, label.Side);
            Assert.AreEqual(bay, label.BayIndex);

            // Verify text component content
            var textComp = labelObj.transform.Find("PrimaryText").GetComponent<Text>();
            Assert.IsNotNull(textComp);
            Assert.AreEqual(expectedBayId, textComp.text);
        }

        [Test]
        public void ShelfLevelTag_AlignsWithTierIndices()
        {
            int aisle = 2;
            ShelfSide side = ShelfSide.Right;
            int bay = 1;
            int tier = 3;
            string expectedTierId = $"A{aisle:D2}-R-B{bay:D2}-T{tier:D2}";

            var labelObj = PhysicalLocationLabel.CreateLabelObject(
                "Tag_Level_03",
                testRoot.transform,
                Vector3.zero,
                Quaternion.identity,
                LocationLabelType.ShelfLevel,
                $"LEVEL {tier:D2}",
                $"TIER {tier:D2}",
                expectedTierId,
                aisle: aisle,
                side: side,
                bay: bay,
                tier: tier,
                width: 0.24f,
                height: 0.055f);

            var label = labelObj.GetComponent<PhysicalLocationLabel>();
            Assert.IsNotNull(label);
            Assert.AreEqual(LocationLabelType.ShelfLevel, label.LabelType);
            Assert.AreEqual("LEVEL 03", label.PrimaryText);
            Assert.AreEqual(expectedTierId, label.LocationId);
            Assert.AreEqual(tier, label.TierLevel);
        }

        [Test]
        public void ProductSlotTicket_MatchesSimulationLocationAndSkuMapping()
        {
            var config = StoreConfiguration.CreateQuickCommerce6AisleConfig();
            var storeObj = new GameObject("[Test_Store]");
            storeObj.transform.SetParent(testRoot.transform);
            var store = storeObj.AddComponent<StoreManager>();
            store.InitializeStore(config);

            // Verify first SKU from catalog
            var sku = store.GetAllSkus()[0];
            Assert.IsTrue(store.TryGetSlotForSku(sku.SkuId, out var slot));

            var labelObj = PhysicalLocationLabel.CreateLabelObject(
                $"ShelfTicket_{sku.SkuId}",
                testRoot.transform,
                slot.WorldPosition,
                Quaternion.identity,
                LocationLabelType.ProductSlot,
                slot.SlotId,
                $"{sku.SkuId} • {sku.Name}",
                slot.SlotId,
                aisle: slot.AisleNumber,
                side: slot.Side,
                bay: slot.BayIndex,
                tier: slot.TierLevel,
                slot: slot.SlotIndex,
                width: 0.22f,
                height: 0.05f);

            var label = labelObj.GetComponent<PhysicalLocationLabel>();
            Assert.IsNotNull(label);
            Assert.AreEqual(LocationLabelType.ProductSlot, label.LabelType);
            Assert.AreEqual(slot.SlotId, label.LocationId);
            Assert.AreEqual(slot.SlotId, label.PrimaryText);

            // Validate that parsing the ticket's LocationId yields the identical structured StoreLocation
            Assert.IsTrue(StoreLocation.TryParse(label.LocationId, out var parsedLoc, out string err), err);
            Assert.AreEqual(slot.AisleNumber, parsedLoc.AisleNumber);
            Assert.AreEqual(slot.Side, parsedLoc.Side);
            Assert.AreEqual(slot.BayIndex, parsedLoc.BayIndex);
            Assert.AreEqual(slot.TierLevel, parsedLoc.TierLevel);
            Assert.AreEqual(slot.SlotIndex, parsedLoc.SlotIndex);
        }

        [Test]
        public void PickerHoverPrompt_IncludesExactSlotLocationId()
        {
            // Create a mock pickable product
            var prodObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prodObj.transform.SetParent(testRoot.transform);
            prodObj.transform.position = new Vector3(0f, 1.5f, 1.5f);

            string skuId = "SKU-0001";
            string skuName = "Fresh Organic Milk 1L";
            string slotId = "A01-L-B01-T01-S01";

            var visual = prodObj.AddComponent<PickableItemVisual>();
            visual.Initialize(skuId, skuName, slotId);

            // Setup picker with camera facing product
            var pickerObj = new GameObject("Picker");
            pickerObj.transform.SetParent(testRoot.transform);
            pickerObj.transform.position = new Vector3(0f, 1.5f, 0f);

            var cam = pickerObj.AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 1.5f, 0f);
            cam.transform.LookAt(prodObj.transform.position);

            var picker = pickerObj.AddComponent<PickerInteraction>();

            Physics.SyncTransforms();

            // Perform raycast directly via reflection or invoke Update
            var raycastMethod = typeof(PickerInteraction).GetMethod("PerformRaycast", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(raycastMethod);
            raycastMethod.Invoke(picker, null);

            Assert.AreSame(visual, picker.HoveredItem);
            Assert.IsTrue(picker.CurrentPrompt.Contains(slotId),
                $"Picker prompt '{picker.CurrentPrompt}' must include the exact slot ID '{slotId}'.");
            Assert.IsTrue(picker.CurrentPrompt.Contains(skuId));
            Assert.IsTrue(picker.CurrentPrompt.Contains(skuName));
        }

        [Test]
        public void InternalAisleSignage_OrientationMatchesPickerViewingDirection_WithoutHorizontalMirroring()
        {
            // Verify Left Rack internal label orientation (-90 deg yaw)
            var leftSign = PhysicalLocationLabel.CreateLabelObject(
                "Test_LeftSign",
                testRoot.transform,
                new Vector3(-1.2f, 2.0f, 0f),
                Quaternion.Euler(0f, -90f, 0f),
                LocationLabelType.BayRack,
                "A01-L-B01",
                "BAY 01 • LEFT",
                "A01-L-B01",
                side: ShelfSide.Left);

            // A picker looking at the Left rack from corridor center faces -X
            Vector3 pickerToLeftGaze = new Vector3(-1f, 0f, 0f);
            Vector3 pickerCameraRightForLeft = Vector3.Cross(Vector3.up, pickerToLeftGaze); // (0, 0, 1) (+Z)
            Vector3 leftLabelRight = leftSign.transform.right;

            // Label right vector must align with camera right vector (+Z) so text flows left-to-right
            Assert.Greater(Vector3.Dot(leftLabelRight, pickerCameraRightForLeft), 0.99f,
                "Left rack sign's local right vector must align with picker camera right (+Z) to prevent horizontal mirroring.");

            // Verify Right Rack internal label orientation (+90 deg yaw)
            var rightSign = PhysicalLocationLabel.CreateLabelObject(
                "Test_RightSign",
                testRoot.transform,
                new Vector3(1.2f, 2.0f, 0f),
                Quaternion.Euler(0f, 90f, 0f),
                LocationLabelType.BayRack,
                "A01-R-B01",
                "BAY 01 • RIGHT",
                "A01-R-B01",
                side: ShelfSide.Right);

            // A picker looking at the Right rack from corridor center faces +X
            Vector3 pickerToRightGaze = new Vector3(1f, 0f, 0f);
            Vector3 pickerCameraRightForRight = Vector3.Cross(Vector3.up, pickerToRightGaze); // (0, 0, -1) (-Z)
            Vector3 rightLabelRight = rightSign.transform.right;

            // Label right vector must align with camera right vector (-Z) so text flows left-to-right
            Assert.Greater(Vector3.Dot(rightLabelRight, pickerCameraRightForRight), 0.99f,
                "Right rack sign's local right vector must align with picker camera right (-Z) to prevent horizontal mirroring.");

            // Verify Front-facing sign preserves Quaternion.identity and aligns with picker looking North (+Z)
            var frontSign = PhysicalLocationLabel.CreateLabelObject(
                "Test_FrontSign",
                testRoot.transform,
                new Vector3(0f, 2.5f, -4f),
                Quaternion.identity,
                LocationLabelType.AisleHeader,
                "AISLE A01",
                "PICKING CORRIDOR",
                "A01");

            Vector3 pickerToFrontGaze = new Vector3(0f, 0f, 1f);
            Vector3 pickerCameraRightForFront = Vector3.Cross(Vector3.up, pickerToFrontGaze); // (1, 0, 0) (+X)
            Vector3 frontLabelRight = frontSign.transform.right;

            Assert.Greater(Vector3.Dot(frontLabelRight, pickerCameraRightForFront), 0.99f,
                "Front-facing sign must preserve Quaternion.identity and align with camera right (+X).");
        }
    }
}
