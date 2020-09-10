using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace BAStudio.ECSSprite
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UpdateMatrixSystem))]
    public class SpriteRendererUpdateBufferSystem : SystemBase
    {
        EntityQuery query_Matrix, query_Color, query_Index;
        SpriteRenderingSystem renderingSystem;

        protected override void OnCreate()
        {
            query_Matrix = GetEntityQuery(ComponentType.ReadOnly<SpriteMatrix>(),
                                        ComponentType.ChunkComponentReadOnly<SpriteMatrixChangeTag>(),
                                        ComponentType.ReadOnly<SpriteSheetPointer>(),
                                        ComponentType.ReadOnly<SpriteRenderSubject>(),
                                        ComponentType.ReadOnly<BufferedRenderSubjectTag>());
            query_Color = GetEntityQuery(ComponentType.ReadOnly<SpriteColor>(),
                                        ComponentType.ReadOnly<SpriteSheetPointer>(),
                                        ComponentType.ReadOnly<SpriteRenderSubject>(),
                                        ComponentType.ReadOnly<BufferedRenderSubjectTag>());
            query_Index = GetEntityQuery(ComponentType.ReadOnly<SpriteRenderSubject>(),
                                        ComponentType.ReadOnly<SpriteSheetPointer>(),
                                        ComponentType.ReadOnly<BufferedRenderSubjectTag>());
            renderingSystem = World.GetExistingSystem<SpriteRenderingSystem>();
        }

        protected override void OnUpdate()
        {
            if (renderingSystem == null || renderingSystem.Baked == 0) return;
            if  (renderingSystem.MatrixUpdateThisFrame) renderingSystem.MatrixUpdateHandle.Complete();
            var colorType = GetArchetypeChunkComponentType<SpriteColor>(true);
            var renderSubjectType = GetArchetypeChunkComponentType<SpriteRenderSubject>(true);
            var matrixType = GetArchetypeChunkComponentType<SpriteMatrix>(true);
            JobHandle handle = Dependency;
            for (int i = 0; i < renderingSystem.Baked; i++)
            {
                query_Matrix.SetSharedComponentFilter(renderingSystem.filters[i]);
                if (query_Matrix.CalculateChunkCount() == 0) continue;
                ApplyMatrixDiff job = new ApplyMatrixDiff
                {
                    matrixBuffer = renderingSystem.sheetInstanceMatrixs[i],
                    Matrix_Type = matrixType,
                    RenderSubject_Type = renderSubjectType
                };
                handle = JobHandle.CombineDependencies(handle, job.ScheduleParallel(query_Matrix));
            }
            for (int i = 0; i < renderingSystem.Baked; i++)
            {
                query_Color.ResetFilter();
                query_Color.SetSharedComponentFilter(renderingSystem.filters[i]);
                if (!renderingSystem.BufferStructureChanged[i])
                {
                    query_Color.AddChangedVersionFilter(typeof(SpriteColor));
                }
                ApplyColorDiff job = new ApplyColorDiff
                {
                    colorBuffer = renderingSystem.sheetInstanceColors[i],
                    ColorComponent_Type = colorType,
                    RenderSubject_Type = renderSubjectType,
                    LastSystemVersion = LastSystemVersion
                };
                handle = JobHandle.CombineDependencies(handle, job.ScheduleParallel(query_Color));
            }
            for (int i = 0; i < renderingSystem.Baked; i++)
            {
                query_Index.ResetFilter();
                query_Index.SetSharedComponentFilter(renderingSystem.filters[i]);
                if (!renderingSystem.BufferStructureChanged[i])
                {
                    query_Index.AddChangedVersionFilter(typeof(SpriteRenderSubject));
                    if (query_Index.CalculateChunkCount() == 0) continue;
                }
                ApplyUVIndexDiff job = new ApplyUVIndexDiff
                {
                    uvIndexBuffer = renderingSystem.sheetInstanceUVPointers[i],
                    RenderSubject_Type = renderSubjectType,
                    LastSystemVersion = LastSystemVersion
                };
                handle = JobHandle.CombineDependencies(handle, job.ScheduleParallel(query_Index));
            }
            for (int i = 0; i < renderingSystem.Baked; i++) renderingSystem.BufferStructureChanged[i] = false;
            handle.Complete();
        }

        [BurstCompile]
        struct ApplyUVIndexDiff : IJobChunk
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<int> uvIndexBuffer;
            [ReadOnly] public ArchetypeChunkComponentType<SpriteRenderSubject> RenderSubject_Type;
            [ReadOnly] public uint LastSystemVersion;
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var renderSubjects = chunk.GetNativeArray<SpriteRenderSubject>(RenderSubject_Type);
                for (int i = 0; i < chunk.Count; i++)
                {
                    uvIndexBuffer[renderSubjects[i].bufferIndex] = renderSubjects[i].uvIndex;
                }
            }
        }

        [BurstCompile]
        struct ApplyColorDiff : IJobChunk
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<float4> colorBuffer;
            [ReadOnly] public ArchetypeChunkComponentType<SpriteColor> ColorComponent_Type;
            [ReadOnly] public ArchetypeChunkComponentType<SpriteRenderSubject> RenderSubject_Type;
            [ReadOnly] public uint LastSystemVersion;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var colors = chunk.GetNativeArray<SpriteColor>(ColorComponent_Type);
                var renderSubjects = chunk.GetNativeArray<SpriteRenderSubject>(RenderSubject_Type);
                for (int i = 0; i < chunk.Count; i++)
                {
                    colorBuffer[renderSubjects[i].bufferIndex] = colors[i].color;
                }
            }
        }

        [BurstCompile]
        struct ApplyMatrixDiff : IJobChunk
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<float4> matrixBuffer;
            [ReadOnly] public ArchetypeChunkComponentType<SpriteMatrix> Matrix_Type;
            [ReadOnly] public ArchetypeChunkComponentType<SpriteRenderSubject> RenderSubject_Type;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var matrixs = chunk.GetNativeArray<SpriteMatrix>(Matrix_Type);
                var renderSubjects = chunk.GetNativeArray<SpriteRenderSubject>(RenderSubject_Type);
                for (int i = 0; i < chunk.Count; i++)
                {
                    matrixBuffer[renderSubjects[i].bufferIndex] = matrixs[i].matrix;
                }
            }
        }
    }
}