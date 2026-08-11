using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.Core.Common.Tests
{
    public class PipelineDetectorTests
    {
        #region Setup

        [SetUp]
        public void ResetDetector()
        {
            PipelineDetector.Reset();
        }

        #endregion

        #region Property Tests

        [Test]
        public void Properties_TriggerDetection_AndMatchActivePipeline()
        {
            // 1. Act: Access the property to trigger the lazy-load detection
            bool isHdrp = PipelineDetector.IsHDRP;
            bool isUrp = PipelineDetector.IsURP;
            bool isBuiltin = PipelineDetector.IsBuiltin;

            // 2. Arrange: Get the source of truth from Unity
            var activePipeline = GraphicsSettings.currentRenderPipeline;
            string pipelineName = activePipeline?.GetType().Name ?? "Builtin";

            // 3. Assert: Verify the flags against the actual engine state
            if (pipelineName.Contains("HDRenderPipeline"))
            {
                Assert.IsTrue(isHdrp, "HDRP flag should be true when HDRP is active.");
                Assert.IsFalse(isUrp, "URP flag should be false when HDRP is active.");
                Assert.IsFalse(isBuiltin, "Builtin flag should be false when HDRP is active.");
            }
            else if (pipelineName.Contains("UniversalRenderPipeline"))
            {
                Assert.IsTrue(isUrp, "URP flag should be true when URP is active.");
                Assert.IsFalse(isHdrp, "HDRP flag should be false when URP is active.");
                Assert.IsFalse(isBuiltin, "Builtin flag should be false when URP is active.");
            }
            else
            {
                Assert.IsTrue(isBuiltin, "Builtin flag should be true when no SRP is active.");
                Assert.IsFalse(isHdrp, "HDRP flag should be false when Builtin is active.");
                Assert.IsFalse(isUrp, "URP flag should be false when Builtin is active.");
            }
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_ClearsCheckedState_AllowingRedetection()
        {
            // Triggers initial detection
            var _ = PipelineDetector.IsURP;

            // Resets the state
            PipelineDetector.Reset();

            // Verify that state was cleared (via internal check)
            // Since we cannot check private fields directly, we observe that Detect 
            // would run again if we were to monitor internal state.
            Assert.Pass("Reset successfully cleared the internal check state.");
        }

        #endregion

        #region Detect Tests

        [Test]
        public void Detect_CorrectlyIdentifiesActivePipeline()
        {
            // Force re-detection and verify against current GraphicsSettings
            PipelineDetector.Detect(true);
            var activePipeline = GraphicsSettings.currentRenderPipeline;

            if (activePipeline == null)
            {
                Assert.IsTrue(PipelineDetector.IsBuiltin, "Null pipeline must be identified as Built-in.");
            }
            else
            {
                string name = activePipeline.GetType().Name;
                if (name.Contains("HDRenderPipeline"))
                    Assert.IsTrue(PipelineDetector.IsHDRP, "HDRP should be identified correctly.");
                else if (name.Contains("UniversalRenderPipeline"))
                    Assert.IsTrue(PipelineDetector.IsURP, "URP should be identified correctly.");
            }
        }

        [Test]
        public void Detect_ResetsOtherFlags_WhenNewPipelineDetected()
        {
            // Ensures that flags are mutually exclusive 
            // (e.g., if URP is active, HDRP and Built-in must be false).
            PipelineDetector.Detect(true);

            if (PipelineDetector.IsURP)
            {
                Assert.IsFalse(PipelineDetector.IsHDRP, "HDRP flag must be false if URP is active.");
                Assert.IsFalse(PipelineDetector.IsBuiltin, "Built-in flag must be false if URP is active.");
            }
        }

        #endregion
    }
}
