using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BAStudio.ECSSprite
{
  public struct SpriteMatrix : IComponentData {
    public float4 matrix;
  }
}