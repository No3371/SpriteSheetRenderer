using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BAStudio.ECSSprite
{
  public struct SpriteColor : IComponentData {
    public float4 color;
  }
}