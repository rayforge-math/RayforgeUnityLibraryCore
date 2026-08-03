using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions.Tests;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks.Tests
{
    public class GridRangeStateTests : IIterationLogicTests<Vector3Int, GridRangeState>
    {
        #region Create Test Env

        protected override IterationTestData<Vector3Int, GridRangeState> CreateLogic(int count)
        {
            var keys = new Vector3Int[count];

            for(int i = 0; i < count; ++i)
            {
                keys[i] = new Vector3Int(i, 0, 0);
            }

            GridRangeState logic;
            if(count == 0)
            {
                logic = new GridRangeState(new Vector3Int(0, 0, 0), new Vector3Int(-1, -1, -1));
            }
            else
            {
                logic = new GridRangeState(keys[0], keys[keys.Length - 1]);
            }

            return new IterationTestData<Vector3Int, GridRangeState>
            {
                expected = keys,
                logic = logic
            };
        }

        #endregion

        #region Various Test Cases

        [TestCase(0, 0, 0, 0, 0, 0, 1)]      
        [TestCase(0, 0, 0, 1, 1, 1, 8)]      
        [TestCase(-5, -5, -5, -4, -4, -4, 8)]
        [TestCase(0, 0, 0, 10, 0, 0, 11)]    
        [TestCase(0, 0, 0, 0, 10, 0, 11)]    
        [TestCase(0, 0, 0, 0, 0, 10, 11)]    
        [TestCase(100, 100, 100, 102, 101, 100, 6)] 
        [TestCase(1, 0, 0, 0, 0, 0, 0)] 
        [TestCase(0, 1, 0, 0, 0, 0, 0)] 
        [TestCase(0, 0, 1, 0, 0, 0, 0)] 
        [TestCase(10, 5, 2, 8, 4, 1, 0)]
        public void GridRangeState_IterationCounts_MatchExpected(int minX, int minY, int minZ, int maxX, int maxY, int maxZ, int expectedCount)
        {
            var min = new Vector3Int(minX, minY, minZ);
            var max = new Vector3Int(maxX, maxY, maxZ);

            var state = new GridRangeState(min, max);
            int actualCount = 0;

            while (state.MoveNext(ref state, out _))
            {
                actualCount++;
            }

            Assert.AreEqual(expectedCount, actualCount,
                $"Range {min} to {max} should contain exactly {expectedCount} elements.");
        }

        #endregion
    }
}
