using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

namespace BAStudio.ECSSprite
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SpriteRenderingBufferMaintenenceSystem))]
    public class UpdateMatrixSystem : SystemBase
    {
        public static readonly bool UseLocalToWorldInsteadOfTranslation = false;
        EntityQuery query, fastQuery_TranslationChange, fastQuery_RotationChange, fastQuery_ScaleChange;
        SpriteRenderingSystem srs;
        protected override void OnCreate()
        {
            srs = World.GetExistingSystem<SpriteRenderingSystem>();
            if (UseLocalToWorldInsteadOfTranslation) query = GetEntityQuery(
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<Scale>(),
                ComponentType.ReadOnly<Rotation2D>(),
                ComponentType.ChunkComponentReadOnly<SpriteMatrixChangeTag>(),
                ComponentType.ReadWrite<SpriteMatrix>(),
                ComponentType.ReadOnly<BufferedRenderSubjectTag>());
            else query = GetEntityQuery(
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<Scale>(),
                ComponentType.ReadOnly<Rotation2D>(),
                ComponentType.ChunkComponentReadOnly<SpriteMatrixChangeTag>(),
                ComponentType.ReadWrite<SpriteMatrix>(),
                ComponentType.ReadOnly<BufferedRenderSubjectTag>());
        }
        protected override void OnUpdate()
        {
            srs.MatrixUpdateThisFrame = false;
            if (fastQuery_RotationChange == default(EntityQuery))
            {
                EntityQueryDesc desc = new EntityQueryDesc
                {
                    All = new ComponentType[] {
                    ComponentType.ReadOnly<Rotation2D>(),
                    ComponentType.ReadOnly<SpriteMatrix>(),
                    ComponentType.ReadOnly<BufferedRenderSubjectTag>(),
                },
                    None = new ComponentType[] { ComponentType.ChunkComponent<SpriteMatrixChangeTag>() }
                };
                fastQuery_RotationChange = GetEntityQuery(desc);
                fastQuery_RotationChange.AddChangedVersionFilter(typeof(Rotation2D));
            }
            if (fastQuery_TranslationChange == default(EntityQuery))
            {
                if (UseLocalToWorldInsteadOfTranslation)
                {
                    EntityQueryDesc desc = new EntityQueryDesc
                    {
                        All = new ComponentType[] {
                        ComponentType.ReadOnly<LocalToWorld>(),
                        ComponentType.ReadOnly<SpriteMatrix>(),
                        ComponentType.ReadOnly<BufferedRenderSubjectTag>(),
                    },
                        None = new ComponentType[] { ComponentType.ChunkComponent<SpriteMatrixChangeTag>() },

                    };
                    fastQuery_TranslationChange = GetEntityQuery(desc);
                    fastQuery_TranslationChange.AddChangedVersionFilter(typeof(LocalToWorld));
                }
                else
                {
                    EntityQueryDesc desc = new EntityQueryDesc
                    {
                        All = new ComponentType[] {
                        ComponentType.ReadOnly<Translation>(),
                        ComponentType.ReadOnly<SpriteMatrix>(),
                        ComponentType.ReadOnly<BufferedRenderSubjectTag>(),
                    },
                        None = new ComponentType[] { ComponentType.ChunkComponent<SpriteMatrixChangeTag>() },

                    };
                    fastQuery_TranslationChange = GetEntityQuery(desc);
                    fastQuery_TranslationChange.AddChangedVersionFilter(typeof(Translation));
                }
            }
            if (fastQuery_ScaleChange == default(EntityQuery))
            {
                EntityQueryDesc desc = new EntityQueryDesc
                {
                    All = new ComponentType[] {
                    ComponentType.ReadOnly<Scale>(),
                    ComponentType.ReadOnly<SpriteMatrix>(),
                    ComponentType.ReadOnly<BufferedRenderSubjectTag>(),
                },
                    None = new ComponentType[] { ComponentType.ChunkComponent<SpriteMatrixChangeTag>() }
                };
                fastQuery_ScaleChange = GetEntityQuery(desc);
                fastQuery_ScaleChange.AddChangedVersionFilter(typeof(Scale));
            }
            if (fastQuery_RotationChange.CalculateChunkCount() > 0) EntityManager.AddChunkComponentData(fastQuery_RotationChange, new SpriteMatrixChangeTag());
            if (fastQuery_TranslationChange.CalculateChunkCount() > 0) EntityManager.AddChunkComponentData(fastQuery_TranslationChange, new SpriteMatrixChangeTag());
            if (fastQuery_ScaleChange.CalculateChunkCount() > 0) EntityManager.AddChunkComponentData(fastQuery_ScaleChange, new SpriteMatrixChangeTag()); ;


            UpdateMatrixJob udpateJob = new UpdateMatrixJob
            {
                TranslationComponent_Type =  GetArchetypeChunkComponentType<Translation>(true),
                Rotation_Type = GetArchetypeChunkComponentType<Rotation2D>(true),
                Scale_Type = GetArchetypeChunkComponentType<Scale>(true),
                Matrix_Type = GetArchetypeChunkComponentType<SpriteMatrix>(),
                LastSystemVersion = this.LastSystemVersion,
            };
            srs.MatrixUpdateHandle = udpateJob.ScheduleParallel(query);
            srs.MatrixUpdateThisFrame = true;
        }

        [BurstCompile]
        struct UpdateMatrixJob_LocalToWorld : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> ComponentType_LocalToWorld;
            [ReadOnly] public ArchetypeChunkComponentType<Rotation2D> Rotation_Type;
            [ReadOnly] public ArchetypeChunkComponentType<Scale> Scale_Type;
            public ArchetypeChunkComponentType<SpriteMatrix> Matrix_Type;
            [ReadOnly] public uint LastSystemVersion;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                bool tChange = chunk.DidChange(ComponentType_LocalToWorld, LastSystemVersion);
                bool rChange = chunk.DidChange(Rotation_Type, LastSystemVersion);
                bool sChange = chunk.DidChange(Scale_Type, LastSystemVersion);
                if (!tChange && !rChange && !sChange) return;
                NativeArray<SpriteMatrix> matrixs = chunk.GetNativeArray<SpriteMatrix>(Matrix_Type);

                if (tChange && rChange && sChange)
                {
                    NativeArray<Rotation2D> rotations = chunk.GetNativeArray<Rotation2D>(Rotation_Type);
                    NativeArray<LocalToWorld> localToWorlds = chunk.GetNativeArray<LocalToWorld>(ComponentType_LocalToWorld);
                    NativeArray<Scale> scales = chunk.GetNativeArray<Scale>(Scale_Type);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        SpriteMatrix newMatrix = matrixs[i];
                        newMatrix.matrix.z = (rotations[i].angle / 180) * UnityEngine.Mathf.PI;
                        newMatrix.matrix.x = localToWorlds[i].Position.x;
                        newMatrix.matrix.y = localToWorlds[i].Position.y;
                        newMatrix.matrix.w = scales[i].Value;
                        matrixs[i] = newMatrix;
                    }
                }
                else if (tChange && rChange)
                {
                    NativeArray<Rotation2D> rotations = chunk.GetNativeArray<Rotation2D>(Rotation_Type);
                    NativeArray<LocalToWorld> localToWorlds = chunk.GetNativeArray<LocalToWorld>(ComponentType_LocalToWorld);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        SpriteMatrix newMatrix = matrixs[i];
                        newMatrix.matrix.z = (rotations[i].angle / 180) * UnityEngine.Mathf.PI;
                        newMatrix.matrix.x = localToWorlds[i].Position.x;
                        newMatrix.matrix.y = localToWorlds[i].Position.y;
                        matrixs[i] = newMatrix;
                    }
                }
                else if (tChange && sChange)
                {
                    NativeArray<LocalToWorld> localToWorlds = chunk.GetNativeArray<LocalToWorld>(ComponentType_LocalToWorld);
                    NativeArray<Scale> scales = chunk.GetNativeArray<Scale>(Scale_Type);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        SpriteMatrix newMatrix = matrixs[i];
                        newMatrix.matrix.x = localToWorlds[i].Position.x;
                        newMatrix.matrix.y = localToWorlds[i].Position.y;
                        newMatrix.matrix.w = scales[i].Value;
                        matrixs[i] = newMatrix;
                    }
                }
                else if (rChange && sChange)
                {
                    NativeArray<Rotation2D> rotations = chunk.GetNativeArray<Rotation2D>(Rotation_Type);
                    NativeArray<Scale> scales = chunk.GetNativeArray<Scale>(Scale_Type);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        SpriteMatrix newMatrix = matrixs[i];
                        newMatrix.matrix.z = (rotations[i].angle / 180) * UnityEngine.Mathf.PI;
                        newMatrix.matrix.w = scales[i].Value;
                        matrixs[i] = newMatrix;
                    }
                }
                else if (rChange)
                {
                    NativeArray<Rotation2D> rotations = chunk.GetNativeArray<Rotation2D>(Rotation_Type);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        SpriteMatrix newMatrix = matrixs[i];
                        newMatrix.matrix.z = (rotations[i].angle / 180) * UnityEngine.Mathf.PI;
                        matrixs[i] = newMatrix;
                    }
                }
                else if (sChange)
                {
                    NativeArray<Scale> scales = chunk.GetNativeArray<Scale>(Scale_Type);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        SpriteMatrix newMatrix = matrixs[i];
                        newMatrix.matrix.w = scales[i].Value;
                        matrixs[i] = newMatrix;
                    }
                }
                else if (tChange)
                {
                    NativeArray<LocalToWorld> localToWorlds = chunk.GetNativeArray<LocalToWorld>(ComponentType_LocalToWorld);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        SpriteMatrix newMatrix = matrixs[i];
                        newMatrix.matrix.x = localToWorlds[i].Position.x;
                        newMatrix.matrix.y = localToWorlds[i].Position.y;
                        matrixs[i] = newMatrix;
                    }
                }
            }
        }
    
        [BurstCompile]
        struct UpdateMatrixJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<Translation> TranslationComponent_Type;
            [ReadOnly] public ArchetypeChunkComponentType<Rotation2D> Rotation_Type;
            [ReadOnly] public ArchetypeChunkComponentType<Scale> Scale_Type;
            public ArchetypeChunkComponentType<SpriteMatrix> Matrix_Type;
            [ReadOnly] public uint LastSystemVersion;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                bool tChange = chunk.DidChange(TranslationComponent_Type, LastSystemVersion);
                bool rChange = chunk.DidChange(Rotation_Type, LastSystemVersion);
                bool sChange = chunk.DidChange(Scale_Type, LastSystemVersion);
                if (!tChange && !rChange && !sChange) return;
                NativeArray<SpriteMatrix> matrixs = chunk.GetNativeArray<SpriteMatrix>(Matrix_Type);

                if (tChange && rChange && sChange)
                {
                    NativeArray<Rotation2D> rotations = chunk.GetNativeArray<Rotation2D>(Rotation_Type);
                    NativeArray<Translation> translations = chunk.GetNativeArray<Translation>(TranslationComponent_Type);
                    NativeArray<Scale> scales = chunk.GetNativeArray<Scale>(Scale_Type);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        SpriteMatrix newMatrix = matrixs[i];
                        newMatrix.matrix.z = (rotations[i].angle / 180) * UnityEngine.Mathf.PI;
                        newMatrix.matrix.x = translations[i].Value.x;
                        newMatrix.matrix.y = translations[i].Value.y;
                        newMatrix.matrix.w = scales[i].Value;
                        matrixs[i] = newMatrix;
                    }
                }
                else if (tChange && rChange)
                {
                    NativeArray<Rotation2D> rotations = chunk.GetNativeArray<Rotation2D>(Rotation_Type);
                    NativeArray<Translation> translations = chunk.GetNativeArray<Translation>(TranslationComponent_Type);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        SpriteMatrix newMatrix = matrixs[i];
                        newMatrix.matrix.z = (rotations[i].angle / 180) * UnityEngine.Mathf.PI;
                        newMatrix.matrix.x = translations[i].Value.x;
                        newMatrix.matrix.y = translations[i].Value.y;
                        matrixs[i] = newMatrix;
                    }
                }
                else if (tChange && sChange)
                {
                    NativeArray<Translation> translations = chunk.GetNativeArray<Translation>(TranslationComponent_Type);
                    NativeArray<Scale> scales = chunk.GetNativeArray<Scale>(Scale_Type);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        SpriteMatrix newMatrix = matrixs[i];
                        newMatrix.matrix.x = translations[i].Value.x;
                        newMatrix.matrix.y = translations[i].Value.y;
                        newMatrix.matrix.w = scales[i].Value;
                        matrixs[i] = newMatrix;
                    }
                }
                else if (rChange && sChange)
                {
                    NativeArray<Rotation2D> rotations = chunk.GetNativeArray<Rotation2D>(Rotation_Type);
                    NativeArray<Scale> scales = chunk.GetNativeArray<Scale>(Scale_Type);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        SpriteMatrix newMatrix = matrixs[i];
                        newMatrix.matrix.z = (rotations[i].angle / 180) * UnityEngine.Mathf.PI;
                        newMatrix.matrix.w = scales[i].Value;
                        matrixs[i] = newMatrix;
                    }
                }
                else if (rChange)
                {
                    NativeArray<Rotation2D> rotations = chunk.GetNativeArray<Rotation2D>(Rotation_Type);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        SpriteMatrix newMatrix = matrixs[i];
                        newMatrix.matrix.z = (rotations[i].angle / 180) * UnityEngine.Mathf.PI;
                        matrixs[i] = newMatrix;
                    }
                }
                else if (sChange)
                {
                    NativeArray<Scale> scales = chunk.GetNativeArray<Scale>(Scale_Type);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        SpriteMatrix newMatrix = matrixs[i];
                        newMatrix.matrix.w = scales[i].Value;
                        matrixs[i] = newMatrix;
                    }
                }
                else if (tChange)
                {
                    NativeArray<Translation> translations = chunk.GetNativeArray<Translation>(TranslationComponent_Type);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        SpriteMatrix newMatrix = matrixs[i];
                        newMatrix.matrix.x = translations[i].Value.x;
                        newMatrix.matrix.y = translations[i].Value.y;
                        matrixs[i] = newMatrix;
                    }
                }
            }
        }
    }
}