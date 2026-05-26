#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Aitugan.EditorTools
{
    /// <summary>
    /// One-shot reimport of all AituganPNG assets so the postprocessor's
    /// quality settings (Point filter + Uncompressed + appropriate mesh type)
    /// actually apply to files that were already imported with old settings.
    /// Bumps the version to re-run.
    /// </summary>
    [InitializeOnLoad]
    static class ArtReimport
    {
        const string Key = "Aitugan.ArtReimportVersion";
        const int Version = 3;
        static readonly string[] Folders =
        {
            "Assets/Aitugan/Resources/AituganPNG",
            "Assets/Aitugan/Resources/Grounds",
        };

        static ArtReimport()
        {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Run;
        }

        [MenuItem("Aitugan/Force Reimport Art")]
        public static void Reimport()
        {
            EditorPrefs.SetInt(Key, 0);
            Run();
        }

        static void Run()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Run;
                return;
            }
            int total = 0;
            foreach (var folder in Folders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                total += guids.Length;
            }
            EditorPrefs.SetInt(Key, Version);
            Debug.Log($"[Aitugan] Reimported {total} art assets.");
        }
    }

    /// <summary>
    /// Auto-configures every PNG in Assets/Aitugan/Resources/AituganPNG so the
    /// authored art looks crisp at the scale we render it at. Two cases:
    ///
    /// 1. Ground tiles (Floor / Grass / Ground / LightGround) - need Full Rect
    ///    mesh + Repeat wrap so SpriteRenderer.drawMode = Tiled can repeat
    ///    them seamlessly.
    /// 2. Everything else - characters, props, paper, arrows. We force
    ///    Point filtering + no compression so they stay sharp when scaled up
    ///    instead of going blurry under bilinear + DXT compression.
    /// </summary>
    public class GroundTileImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath == null) return;
            bool isGroundTile = assetPath.Contains("/Grounds/");
            bool isCharacterArt = assetPath.Contains("AituganPNG");
            if (!isGroundTile && !isCharacterArt) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.alphaIsTransparency = true;

            if (isGroundTile)
            {
                // Tiled ground - need Full Rect mesh + Repeat wrap for seamless tiling.
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.filterMode = FilterMode.Point; // crisp tile edges; pixel art look
                ti.spritePixelsPerUnit = 32f;     // ~1u per tile
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(settings);
            }
            else
            {
                // Characters / props - keep them crisp at any scale.
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.filterMode = FilterMode.Point;
            }
        }
    }
}
#endif
