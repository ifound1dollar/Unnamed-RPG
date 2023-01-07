using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

[CustomEditor(typeof(CustomTile))]
public class CustomTileIcons : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
    public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
    {
        try
        {
            Tile Target = target as Tile;
            if (Target.sprite != null)
            {
                Texture2D newIcon = new(width, height);
                Texture2D spritePreview = AssetPreview.GetAssetPreview(Target.sprite);
                EditorUtility.CopySerialized(spritePreview, newIcon);
                EditorUtility.SetDirty(Target);
                return newIcon;
            }
        }
        catch
        {

        }
        

        return base.RenderStaticPreview(assetPath, subAssets, width, height);
    }
}
