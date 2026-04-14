using NUnit.Framework;
using Rayforge.Core.Caching.Abstractions;
using Rayforge.Core.Caching.Transforms;
using Rayforge.Core.Tests.Caching.Abstraction;
using UnityEngine;

namespace Rayforge.Core.Tests.Caching.Transforms
{
    [TestFixture]
    public class CachedTransformTests : CachedTransformContractTestBase<CachedTransform>
    {
        protected override CachedTransform CallCreateFactory(string name)
        {
            return CachedTransform.Create(name);
        }

        protected override CachedTransform CallTemplateCreateFactory(string name, ICachedTransform parent = null)
        {
            return CachedTransform.Create(name, parent);
        }

        protected override CachedTransform CreateInstance(GameObject go)
        {
            return new CachedTransform(go);
        }
    }
}
