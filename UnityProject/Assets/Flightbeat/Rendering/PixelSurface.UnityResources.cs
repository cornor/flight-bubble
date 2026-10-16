using UnityEngine;

namespace Flightbeat.Rendering
{
    public interface ISurfaceTextureRuntime
    {
        int Width(Texture texture);
        int Height(Texture texture);
    }

    internal sealed class UnitySurfaceTextureRuntime : ISurfaceTextureRuntime
    {
        public int Width(Texture texture) => texture.width;
        public int Height(Texture texture) => texture.height;
    }

    // Explicit Unity API boundaries. The production adapter calls the engine APIs;
    // rule fixtures supply declared results without constructing native objects.
    public interface ISurfaceResourceRuntime
    {
        Texture2D LoadTexture(string path);
        Material LoadMaterial(string path);
        Material CreateMaterial(Material template);
        bool NotNull(Object value);
        bool Alive(Object value);
        Texture2D WhiteTexture();
        Texture2D BlackTexture();
        void SetMaterial(PixelSurface surface, Material material);
        void SetRaycastTarget(PixelSurface surface, bool value);
        void SetTexture(Material material, string key, Texture texture);
        void LogError(string message);
        void Destroy(Object value);
        void DestroyGraphic(PixelSurface surface);
    }

    internal sealed class UnitySurfaceResourceRuntime : ISurfaceResourceRuntime
    {
        public Texture2D LoadTexture(string path) => Resources.Load<Texture2D>(path);
        public Material LoadMaterial(string path) => Resources.Load<Material>(path);
        public Material CreateMaterial(Material template) => new Material(template);
        public bool NotNull(Object value) => value != null;
        public bool Alive(Object value) => value;
        public Texture2D WhiteTexture() => Texture2D.whiteTexture;
        public Texture2D BlackTexture() => Texture2D.blackTexture;
        public void SetMaterial(PixelSurface surface, Material material) => surface.material = material;
        public void SetRaycastTarget(PixelSurface surface, bool value) => surface.raycastTarget = value;
        public void SetTexture(Material material, string key, Texture texture) => material.SetTexture(key, texture);
        public void LogError(string message) => Debug.LogError(message);
        public void Destroy(Object value) => Object.Destroy(value);
        public void DestroyGraphic(PixelSurface surface) => surface.DestroyGraphicBase();
    }
}
