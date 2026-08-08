using NUnit.Framework;
using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Environment.Spatial.Components;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering.Tests
{
    [TestFixture]
    public class LodAtlasLayoutTests
    {
        #region Initialize Tests

        [Test]
        public void Initialize_NullMaxCapacities_ThrowsArgumentNullException()
        {
            // Arrange
            var layout = new LodAtlasLayout();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => layout.Initialize(null, PowerOfTwoResolution.Res512));
        }

        [Test]
        public void Initialize_EmptyMaxCapacities_ThrowsArgumentException()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int[] capacities = Array.Empty<int>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => layout.Initialize(capacities, PowerOfTwoResolution.Res512));
        }

        [Test]
        public void Initialize_InsufficientDownscales_ThrowsInvalidOperationException()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int[] capacities = { 10, 10, 10 };

            // Res1 cannot be downscaled further for 3 LOD levels
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => layout.Initialize(capacities, PowerOfTwoResolution.Res1));
        }

        [Test]
        public void Initialize_ValidParameters_CalculatesCorrectLayoutProperties()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int expectedLodCount = 2;
            int[] capacities = { 16, 8 };

            // Act
            layout.Initialize(capacities, PowerOfTwoResolution.Res512);

            // Assert
            Assert.AreEqual(expectedLodCount, layout.LodCount);
            Assert.AreEqual(PowerOfTwoResolution.Res512, layout.BaseResolution);
            Assert.IsTrue(layout.RequiredSliceCount > 0, "Required slice count should be greater than zero.");
            Assert.IsTrue(layout.TotalCombinedCapacity >= 24, "Total capacity should accommodate requested capacities.");
        }

        [Test]
        public void Initialize_MultipleCalls_ReusesStructuresSuccessfully()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int[] initialCapacities = { 10 };

            layout.Initialize(initialCapacities, PowerOfTwoResolution.Res512);
            int firstCapacity = layout.TotalCombinedCapacity;

            int[] newCapacities = { 50, 25 };

            // Act
            layout.Initialize(newCapacities, PowerOfTwoResolution.Res1024);

            // Assert
            Assert.AreEqual(2, layout.LodCount);
            Assert.AreEqual(PowerOfTwoResolution.Res1024, layout.BaseResolution);
            Assert.AreNotEqual(firstCapacity, layout.TotalCombinedCapacity);
        }

        [Test]
        public void UninitializedLayout_ReturnsDefaultPropertyValues()
        {
            // Arrange
            var layout = new LodAtlasLayout();

            // Act & Assert
            Assert.IsFalse(layout.IsInitialized, "A newly created layout must not be initialized.");
            Assert.AreEqual(0, layout.RequiredSliceCount, "Uninitialized layout should require zero slices.");
            Assert.AreEqual(0, layout.TotalCombinedCapacity, "Uninitialized layout should have zero capacity.");
            Assert.AreEqual(0, layout.LodCount, "Uninitialized layout should have zero LOD levels.");
        }

        [Test]
        public void Initialize_ValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int expectedLodCount = 3;
            int[] maxCapacities = { 16, 8, 4 };
            var baseRes = PowerOfTwoResolution.Res1024;

            // Act
            layout.Initialize(maxCapacities, baseRes);

            // Assert
            Assert.IsTrue(layout.IsInitialized, "Layout should be marked as initialized after a successful call.");
            Assert.AreEqual(expectedLodCount, layout.LodCount, "LodCount must match the configured amount.");
            Assert.AreEqual(baseRes, layout.BaseResolution, "BaseResolution must match the provided base resolution.");
            Assert.Greater(layout.RequiredSliceCount, 0, "RequiredSliceCount must be greater than zero.");
            Assert.Greater(layout.TotalCombinedCapacity, 0, "TotalCombinedCapacity must be greater than zero.");
        }

        [Test]
        public void Initialize_LevelWithZeroCapacity_HandlesCorrectly()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int[] capacities = { 16, 0, 4 };

            // Act
            layout.Initialize(capacities, PowerOfTwoResolution.Res512);

            // Assert
            Assert.IsTrue(layout.IsInitialized);
            Assert.AreEqual(3, layout.LodCount);
            Assert.AreEqual(0, layout.GetLodCapacity(1));
        }

        [Test]
        public void Initialize_MultipleCallsSameLodCount_ReusesLevelArrayWithoutAllocation()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int[] initialCapacities = { 10, 5 };
            layout.Initialize(initialCapacities, PowerOfTwoResolution.Res512);

            int initialCapacityLevel0 = layout.GetLodCapacity(0);

            int[] newCapacities = { 20, 10 };

            // Act - Same lod count (2), should reuse m_Levels array internally
            layout.Initialize(newCapacities, PowerOfTwoResolution.Res512);

            // Assert
            Assert.AreEqual(2, layout.LodCount);
            Assert.AreNotEqual(initialCapacityLevel0, layout.GetLodCapacity(0));
        }

        #endregion

        #region Property Tests

        [Test]
        public void IsInitialized_Uninitialized_ReturnsFalse()
        {
            // Arrange
            var layout = new LodAtlasLayout();

            // Act & Assert
            Assert.IsFalse(layout.IsInitialized, "Uninitialized layout should return false for IsInitialized.");
        }

        [Test]
        public void IsInitialized_Initialized_ReturnsTrue()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            layout.Initialize(new[] { 10 }, PowerOfTwoResolution.Res512);

            // Act & Assert
            Assert.IsTrue(layout.IsInitialized, "Initialized layout should return true for IsInitialized.");
        }

        [Test]
        public void RequiredSliceCount_Uninitialized_ReturnsZero()
        {
            // Arrange
            var layout = new LodAtlasLayout();

            // Act & Assert
            Assert.AreEqual(0, layout.RequiredSliceCount, "Uninitialized layout must have a RequiredSliceCount of 0.");
        }

        [Test]
        public void RequiredSliceCount_Initialized_ReturnsValidCount()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            layout.Initialize(new[] { 16 }, PowerOfTwoResolution.Res512);

            // Act & Assert
            Assert.Greater(layout.RequiredSliceCount, 0, "Initialized layout must require at least one slice.");
        }

        [Test]
        public void BaseResolution_Uninitialized_ReturnsDefault()
        {
            // Arrange
            var layout = new LodAtlasLayout();

            // Act & Assert
            Assert.AreEqual(default(PowerOfTwoResolution), layout.BaseResolution, "Uninitialized layout should have default BaseResolution.");
        }

        [Test]
        public void BaseResolution_Initialized_ReturnsConfiguredResolution()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            var expectedResolution = PowerOfTwoResolution.Res1024;
            layout.Initialize(new[] { 10 }, expectedResolution);

            // Act & Assert
            Assert.AreEqual(expectedResolution, layout.BaseResolution, "BaseResolution must match the value passed during initialization.");
        }

        [Test]
        public void TotalCombinedCapacity_Uninitialized_ReturnsZero()
        {
            // Arrange
            var layout = new LodAtlasLayout();

            // Act & Assert
            Assert.AreEqual(0, layout.TotalCombinedCapacity, "Uninitialized layout must have a TotalCombinedCapacity of 0.");
        }

        [Test]
        public void TotalCombinedCapacity_Initialized_ReturnsValidCapacity()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int[] capacities = { 16, 8 };
            layout.Initialize(capacities, PowerOfTwoResolution.Res512);

            // Act & Assert
            Assert.GreaterOrEqual(layout.TotalCombinedCapacity, 24, "TotalCombinedCapacity must accommodate or exceed requested capacities.");
        }

        [Test]
        public void LodCount_Uninitialized_ReturnsZero()
        {
            // Arrange
            var layout = new LodAtlasLayout();

            // Act & Assert
            Assert.AreEqual(0, layout.LodCount, "Uninitialized layout must report a LodCount of 0.");
        }

        [Test]
        public void LodCount_Initialized_ReturnsArrayLength()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int[] capacities = { 10, 20, 30 };
            layout.Initialize(capacities, PowerOfTwoResolution.Res512);

            // Act & Assert
            Assert.AreEqual(3, layout.LodCount, "LodCount must match the length of the maxCapacities array.");
        }

        #endregion

        #region GetLodCapacity Tests

        [Test]
        public void GetLodCapacity_UninitializedLayout_ThrowsInvalidOperationException()
        {
            // Arrange
            var layout = new LodAtlasLayout();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => layout.GetLodCapacity(0));
        }

        [Test]
        public void GetLodCapacity_NegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int[] capacities = { 10 };
            layout.Initialize(capacities, PowerOfTwoResolution.Res512);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetLodCapacity(-1));
        }

        [Test]
        public void GetLodCapacity_IndexOutOfBounds_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int[] capacities = { 10, 20 };
            layout.Initialize(capacities, PowerOfTwoResolution.Res512);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetLodCapacity(2)); // Valid indices are 0 and 1
        }

        [Test]
        public void GetLodCapacity_ValidIndex_ReturnsCorrectCapacity()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int[] capacities = { 16 };
            layout.Initialize(capacities, PowerOfTwoResolution.Res512);

            int expectedCapacity = layout.GetLodCapacity(0);

            // Act
            int capacity = layout.GetLodCapacity(0);

            // Assert
            Assert.AreEqual(expectedCapacity, capacity, "GetLodCapacity should return the correct total capacity for the specified LOD level.");
        }

        #endregion

        #region GetMapping Tests

        [Test]
        public void GetMapping_UninitializedLayout_ThrowsInvalidOperationException()
        {
            // Arrange
            var layout = new LodAtlasLayout();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => layout.GetMapping(0, 0));
        }

        [Test]
        public void GetMapping_NegativeLodIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int[] capacities = { 16 };
            layout.Initialize(capacities, PowerOfTwoResolution.Res512);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetMapping(-1, 0));
        }

        [Test]
        public void GetMapping_LodIndexOutOfBounds_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int[] capacities = { 16 };
            layout.Initialize(capacities, PowerOfTwoResolution.Res512);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetMapping(1, 0));
        }

        [Test]
        public void GetMapping_ValidParameters_ReturnsCorrectMappingData()
        {
            // Arrange
            var layout = new LodAtlasLayout();
            int[] capacities = { 1, 4 };

            // LOD 0 (Base) = Res512 (scale 1.0f)
            // LOD 1 = Res256 (automatically derived via Downscale) -> SlotsPerDim = 512 / 256 = 2 -> scale = 0.5f
            layout.Initialize(capacities, PowerOfTwoResolution.Res512);

            // Act -> Testing LOD 1 where scale is 0.5f
            var mappingSlot0 = layout.GetMapping(1, 0);
            var mappingSlot1 = layout.GetMapping(1, 1);

            // Assert
            Assert.AreEqual(1, mappingSlot0.SliceIndex); // LOD 0 takes 1 slice, so LOD 1 starts at slice 1
            Assert.AreEqual(0.5f, mappingSlot0.RelativeScale);
            Assert.AreEqual(Vector2.zero, mappingSlot0.RelativeOffset);

            Assert.AreEqual(1, mappingSlot1.SliceIndex);
            Assert.AreEqual(0.5f, mappingSlot1.RelativeScale);
            Assert.AreEqual(new Vector2(0.5f, 0f), mappingSlot1.RelativeOffset);
        }

        #endregion
    }
}