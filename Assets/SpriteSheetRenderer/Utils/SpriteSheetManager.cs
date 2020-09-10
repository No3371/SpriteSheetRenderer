using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct BakedSpriteAnimationUser : IComponentData
{
    public int animationIndex;
    public int playbackPosition;
}
