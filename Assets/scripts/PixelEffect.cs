using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class PixelEffect : MonoBehaviour
{
    public Material effectMaterial;   
    public Material gameBoyMaterial;  

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Safety: If GameManager hasn't woken up yet, just show normal screen
        if (GameManager.Instance == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        float pSize = GameManager.Instance.pixelSize;
        bool gbEnabled = GameManager.Instance.useGameBoyFilter;

        // Create temporary texture
        RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height);
        
        // Ensure the temporary texture is clean
        temp.filterMode = FilterMode.Point;

        // 1. Apply Pixelation
        if (effectMaterial != null)
        {
            effectMaterial.SetFloat("_PixelSize", pSize);
            Graphics.Blit(source, temp, effectMaterial);
        }
        else
        {
            Graphics.Blit(source, temp);
        }

        // 2. Apply GameBoy Filter
        if (gbEnabled && gameBoyMaterial != null)
        {
            // We blit to destination using the GB material
            Graphics.Blit(temp, destination, gameBoyMaterial);
        }
        else
        {
            // Just output the pixelated version
            Graphics.Blit(temp, destination);
        }

        // Crucial: Release the memory immediately to prevent ghosting/stale frames
        RenderTexture.ReleaseTemporary(temp);
    }
}