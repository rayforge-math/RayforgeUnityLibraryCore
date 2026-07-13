using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions.Tests;
using Rayforge.Core.Environment.Spatial;
using Rayforge.Core.Environment.Spatial.Chunks;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    public class GridRadiusEdgeStateTests : IIterationLogicTests<Vector3Int, GridRadiusEdgeState>
    {
        #region Create Test Env

        protected override IterationTestData<Vector3Int, GridRadiusEdgeState> CreateLogic(int count)
        {
            var keys = new Vector3Int[count];

            for (int i = 0; i < count; ++i)
            {
                keys[i] = new Vector3Int(i, 0, 0);
            }

            var localCentre = new Vector3Int(0, 0, 0);
            var radius = count;
            var gridSize = new Vector3(1, 1, 1);
            var axes = SpatialAxes.Voxel;

            GridRadiusEdgeState logic;
            if (count == 0)
            {
                logic = new GridRadiusEdgeState(
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(-1, -1, -1),
                    localCentre,
                    radius,
                    gridSize,
                    axes);
            }
            else
            {
                logic = new GridRadiusEdgeState(
                    keys[0],
                    keys[keys.Length - 1],
                    localCentre,
                    radius,
                    gridSize,
                    axes);
            }

            return new IterationTestData<Vector3Int, GridRadiusEdgeState>
            {
                expected = keys,
                logic = logic
            };
        }

        #endregion
    }
}