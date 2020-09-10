using System;
using Unity.Entities;

namespace BAStudio.ECSSprite
{
    public struct SpriteSheetPointer : ISystemStateSharedComponentData, IEquatable<SpriteSheetPointer>
    {
        public int spriteSheetIndex;

        public bool Equals(SpriteSheetPointer other)
        {
            return spriteSheetIndex == other.spriteSheetIndex;
        }

        public override int GetHashCode()
        {
            return spriteSheetIndex;
        }
    }
}