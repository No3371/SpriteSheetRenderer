using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class BakedSpriteSheet
{
    private BakedSpriteSheet() {}
    public BakedSpriteSheet(int index, int subSprites, Material material)
    {
        BakedIndex = index;
        Material = material;
        args = new uint[] { 6, 0, 0, 0, 0 };
        ArgsBuffer = new ComputeBuffer(1, this.args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        BakedSubSprites = subSprites;
    }

    public Material Material { get; set; }
    public int BakedIndex { get; set; }
    public int BakedSubSprites;
    private uint[] args;
    public uint Users { get; set; }
    public uint Capacity { get; set; }

    public void UpdateInstanceDataBuffers (int instances, NativeArray<float4> colors, NativeArray<float4> matrixs, NativeArray<int> uvIndexs)
    {
        if (args[1] != instances)
        {
            args[1] = Users;
            ArgsBuffer.SetData(args);
        }
        if (UvIndexBuffer == null || UvIndexBuffer.count != uvIndexs.Length)
        {
            UvIndexBuffer?.Release();
            UvIndexBuffer = new ComputeBuffer(uvIndexs.Length, sizeof(int));
        }
        if (ColorsBuffer == null || ColorsBuffer.count != colors.Length)
        {
            ColorsBuffer?.Release();
            ColorsBuffer = new ComputeBuffer(colors.Length, 16);
        }
        if (MatrixsBuffer == null || MatrixsBuffer.count != matrixs.Length)
        {
            MatrixsBuffer?.Release();
            MatrixsBuffer = new ComputeBuffer(matrixs.Length, 16);
        }
        MatrixsBuffer.SetData(matrixs);
        ColorsBuffer.SetData(colors);
        UvIndexBuffer.SetData(uvIndexs);
        Material.SetBuffer("bufferIndexBuffer", UvIndexBuffer);
        Material.SetBuffer("colorsBuffer", ColorsBuffer);
        Material.SetBuffer("transformBuffer", MatrixsBuffer);
    }

    public void UpdateUVBuffers (NativeArray<float4> uvs)
    {
        if (UVBuffer == null || UVBuffer.count != uvs.Length)
        {
            UVBuffer?.Release();
            UVBuffer = new ComputeBuffer(uvs.Length, 16);
        }
        UVBuffer.SetData(uvs);
        Material.SetBuffer("uvBuffer", UVBuffer);
    }

    public ComputeBuffer ArgsBuffer { get; set; }
    ComputeBuffer MatrixsBuffer { get; set; }
    ComputeBuffer ColorsBuffer { get; set; }
    ComputeBuffer UvIndexBuffer { get; set; }
    ComputeBuffer UVBuffer { get; set; }
}
