using UnityEditor;
using UnityEngine;

namespace Rayforge.Core.Editor.Tools
{
    public class Texture3DFixer : UnityEditor.Editor
    {
        [MenuItem("Assets/Rayforge/Add MipMaps to Texture3D")]
        private static void AddMips()
        {
            // 1. Get the selected Texture3D
            Texture3D original = Selection.activeObject as Texture3D;
            if (original == null) return;

            string path = AssetDatabase.GetAssetPath(original);

            // 2. Create a copy with Mip Chain enabled
            // Texture3D(width, height, depth, format, mipChain)
            Texture3D withMips = new Texture3D(
                original.width,
                original.height,
                original.depth,
                original.format,
                true
            );
            withMips.wrapMode = original.wrapMode;
            withMips.filterMode = original.filterMode;

            // 3. Copy pixels from LOD 0
            withMips.SetPixels(original.GetPixels());

            // 4. Force calculation of all Mip levels
            withMips.Apply(updateMipmaps: true);

            // 5. Overwrite the asset file
            AssetDatabase.CreateAsset(withMips, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Rayforge] MipMaps successfully added to {path}. " +
                      $"New MipCount: {withMips.mipmapCount}");
        }
    }
}
