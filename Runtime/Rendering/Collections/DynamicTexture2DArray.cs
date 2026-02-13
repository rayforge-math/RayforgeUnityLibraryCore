using Mono.Cecil.Cil;
using Rayforge.Core.ManagedResources.Abstractions;
using Rayforge.Core.ManagedResources.NativeMemory;
using System;
using UnityEditor;
using UnityEngine;
using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using static UnityEditor.ShaderData;

namespace Rayforge.Core.Rendering.Collections
{
    /// <summary>
    /// A dynamic implementation of <see cref="IDynamicArray{T}"/> that acts as a 
    /// controller for a <see cref="ManagedTexture2DArray"/>.
    /// </summary>
    public class DynamicTexture2DArray : IDynamicArray<RenderTexture>, IDisposable
    {
        private ManagedTexture2DArray _managedArray;

        public int Count => (_managedArray != null && _managedArray.IsCreated) ? _managedArray.Descriptor.Count : 0;
        public Texture2DArray InternalArray => _managedArray?.Buffer;

        /// <summary>
        /// Returns true if the underlying GPU resource is allocated and ready to use.
        /// </summary>
        public bool IsCreated => _managedArray != null && _managedArray.IsCreated;

        /// <summary>
        /// Initialized with a descriptor. The wrapper is created but the GPU resource 
        /// is allocated only after calling <see cref="Create"/>.
        /// </summary>
        public DynamicTexture2DArray(Texture2dArrayDescriptor baseSettings)
        {
            _managedArray = ManagedTexture2DArray.Create(baseSettings);
        }

        /// <summary>
        /// Creates or resizes the texture array while maintaining the base texture settings.
        /// </summary>
        public void Create(int count, bool preserve = false)
        {
            if (_managedArray.IsCreated)
                Release();

            count = Math.Max(count, 0);
            if (count == 0)
                return;

            var newDesc = _managedArray.Descriptor;
            newDesc.Count = count;
            var newArray = ManagedTexture2DArray.Create(newDesc);

            if (preserve && IsCreated)
            {
                int slicesToCopy = Mathf.Min(_managedArray.Descriptor.Count, newArray.Descriptor.Count);
                int mipCount = _managedArray.Descriptor.Descriptor.MipCount;

                for (int i = 0; i < slicesToCopy; i++)
                {
                    for (int m = 0; m < mipCount; m++)
                    {
                        Graphics.CopyTexture(_managedArray.Buffer, i, m, newArray.Buffer, i, m);
                    }
                }
            }

            _managedArray?.Release();
            _managedArray = newArray;
        }

        /// <summary>
        /// Copies a specific slice from the array into a provided RenderTexture.
        /// </summary>
        public void CopyElementTo(int index, ref RenderTexture element)
        {
            if (!IsCreated || index < 0 || index >= Count || element == null)
                return;

            Graphics.Blit(InternalArray, element, index, 0);
        }

        /// <summary>
        /// Sets the content of a specific slice using another texture.
        /// </summary>
        public void SetElement(int index, RenderTexture element)
        {
            if (!IsCreated || index < 0 || index >= Count || element == null)
                return;

            var settings = _managedArray.Descriptor.Descriptor;

            if (element.width != settings.Width || element.height != settings.Height)
            {
                throw new ArgumentException(
                    $"[DynamicTexture2DArray] Dimension mismatch! Expected {settings.Width}x{settings.Height}, " +
                    $"but got {element.width}x{element.height}. CopyTexture requires exact match.");
            }

            int mips = Mathf.Min(settings.MipCount, element.useMipMap ? element.mipmapCount : 1);
            for (int m = 0; m < mips; m++)
            {
                Graphics.CopyTexture(element, 0, m, InternalArray, index, m);
            }
        }

        /// <summary>
        /// Updates a slice from a standard Texture2D.
        /// </summary>
        public void SetSlice(int sliceIndex, Texture2D source)
        {
            if (!IsCreated || source == null || sliceIndex >= Count) return;

            var settings = _managedArray.Descriptor.Descriptor;
            int mips = Mathf.Min(settings.MipCount, source.mipmapCount);

            for (int m = 0; m < mips; m++)
            {
                Graphics.CopyTexture(source, 0, m, InternalArray, sliceIndex, m);
            }
        }

        public void Release()
        {
            _managedArray?.Release();
        }

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }
    }
}