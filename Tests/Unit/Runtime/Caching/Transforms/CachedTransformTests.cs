using NUnit.Framework;
using Rayforge.Core.Caching.Abstractions;
using Rayforge.Core.Caching.Abstractions.Tests;
using UnityEngine;

namespace Rayforge.Core.Caching.Transforms.Tests
{
    [TestFixture]
    public class CachedTransformTests : CachedTransformContractTests<CachedTransform>
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
