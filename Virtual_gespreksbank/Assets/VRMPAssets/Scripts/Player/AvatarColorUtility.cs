using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>Kleurt afzonderlijke huid- en kledingmaterialen zonder gedeelde avatar-assets te wijzigen.</summary>
    public static class AvatarColorUtility
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int ColorProperty = Shader.PropertyToID("_Color");

        public static void SetColor(Renderer renderer, int materialIndex, Color color)
        {
            if (renderer == null || materialIndex < 0 || materialIndex >= renderer.sharedMaterials.Length) return;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, materialIndex);
            block.SetColor(BaseColor, color);
            block.SetColor(ColorProperty, color);
            renderer.SetPropertyBlock(block, materialIndex);
        }
    }
}
