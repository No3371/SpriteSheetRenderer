using System.Collections.Generic;
using UnityEngine;

namespace BAStudio.ECSSprite
{
    [System.Serializable]
    public abstract class SpriteSheetAnimationData : ScriptableObject
    {
        public string animationName;
        public SpriteRenderSubject[] sprites;
        public int startIndex;
        public bool playOnStart = true;
        public int samples = 12;
        public SpriteSheetAnimation.RepetitionType repetition = SpriteSheetAnimation.RepetitionType.Loop;
    }
}