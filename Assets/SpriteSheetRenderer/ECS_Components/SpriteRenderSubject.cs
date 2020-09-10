using Unity.Entities;
namespace BAStudio.ECSSprite
{
    public struct SpriteRenderSubject : ISystemStateComponentData
    {
        public int bufferIndex;
        public int uvIndex;

        public SpriteRenderSubject(int uvIndex)
        {
            this.bufferIndex = -1;
            this.uvIndex = uvIndex;
        }
    }
}