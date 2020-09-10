using Unity.Entities;
using Unity.Collections;

namespace BAStudio.ECSSprite
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [AlwaysUpdateSystem]
    public class SpriteRenderingBufferMaintenenceSystem : SystemBase
    {
        SpriteRenderingSystem spriteRenderingSystem;
        EntityQuery matrixChangedChunkQuery;
        public EntityQuery query;
        public static bool Logging;

        protected override void OnCreate()
        {
            spriteRenderingSystem = World.GetExistingSystem<SpriteRenderingSystem>();
            query = GetEntityQuery(ComponentType.ReadOnly<SpriteRenderSubject>(),
                                   ComponentType.ReadOnly<SpriteSheetPointer>(),
                                   ComponentType.Exclude<BufferedRenderSubjectTag>());
        }

        protected override void OnDestroy()
        {
        }
        protected override void OnUpdate()
        {
            if (matrixChangedChunkQuery == default(EntityQuery))
                matrixChangedChunkQuery = GetEntityQuery(ComponentType.ChunkComponent<SpriteMatrixChangeTag>());

            EntityManager.RemoveChunkComponentData<SpriteMatrixChangeTag>(matrixChangedChunkQuery);

            for (int sheet = 0; sheet < spriteRenderingSystem.Baked; sheet++)
            {
                var filter = spriteRenderingSystem.filters[sheet];
                query.SetSharedComponentFilter(filter);
                if (query.CalculateEntityCount() == 0)
                {
                    continue;
                }
                spriteRenderingSystem.BufferStructureChanged[sheet] = true;
                var toBeBuffered = query.ToEntityArray(Allocator.TempJob);
                if (Logging) UnityEngine.Debug.Log(string.Format("ToBeBuffered: {0}.", toBeBuffered.Length));
                BakedSpriteSheet bss = spriteRenderingSystem.bakedSpriteSheets[sheet];
                int pooledIndexs = spriteRenderingSystem.freeIndexs[sheet].Length;
                uint currentCap = bss.Capacity;
                uint usedIndexRight = bss.Users;
                if (toBeBuffered.Length <= pooledIndexs)
                {
                    for (int i = 0; i < toBeBuffered.Length; i++)
                    {
                        SpriteRenderSubject srs = EntityManager.GetComponentData<SpriteRenderSubject>(toBeBuffered[i]);
                        srs.bufferIndex = spriteRenderingSystem.freeIndexs[sheet][pooledIndexs - 1];
                        if (Logging) UnityEngine.Debug.Log(string.Format("Entitiy {1} is taking pooled index {0}. (pool enough)", srs.bufferIndex, toBeBuffered[i].Index));
                        spriteRenderingSystem.freeIndexs[sheet].Resize(spriteRenderingSystem.freeIndexs[sheet].Length - 1, NativeArrayOptions.ClearMemory);
                        EntityManager.SetComponentData(toBeBuffered[i], srs);
                        EntityManager.AddComponentData(toBeBuffered[i], new BufferedRenderSubjectTag());
                        pooledIndexs--;
                    }
                    if (Logging) UnityEngine.Debug.Log(string.Format("Reused {0} indexs.", toBeBuffered.Length));
                }
                else
                {
                    int needIndexs = toBeBuffered.Length - pooledIndexs;
                    if (needIndexs > currentCap - usedIndexRight) // Need to grow
                    {
                        uint properCap = currentCap;
                        while (properCap <= currentCap + needIndexs) properCap *= 2;
                        spriteRenderingSystem.GrowBuffersForNewUser(sheet, (int)properCap);
                        if (Logging) UnityEngine.Debug.Log(string.Format("Extended buffers' length of baked sprite sheet#{0} to {1}.", sheet, properCap));
                    }

                    uint prevIndexRight = usedIndexRight;
                    if (Logging) UnityEngine.Debug.Log(string.Format("Reusing {0}/{1}.", pooledIndexs, toBeBuffered.Length));
                    for (int i = 0; i < toBeBuffered.Length; i++)
                    {
                        SpriteRenderSubject srs = EntityManager.GetComponentData<SpriteRenderSubject>(toBeBuffered[i]);
                        if (pooledIndexs > 0)
                        {
                            srs.bufferIndex = spriteRenderingSystem.freeIndexs[sheet][pooledIndexs - 1];
                            spriteRenderingSystem.freeIndexs[sheet].Resize(spriteRenderingSystem.freeIndexs[sheet].Length - 1, NativeArrayOptions.ClearMemory);
                            if (Logging) UnityEngine.Debug.Log(string.Format("Entitiy {1} is taking pooled index {0}. (pool inenough)", srs.bufferIndex, toBeBuffered[i].Index));
                            pooledIndexs--;
                        }
                        else
                        {
                            srs.bufferIndex = (int)usedIndexRight;
                            if (Logging) UnityEngine.Debug.Log(string.Format("Entitiy {1} is taking new index {0}.", srs.bufferIndex, toBeBuffered[i].Index));
                            usedIndexRight++;
                        }
                        EntityManager.SetComponentData(toBeBuffered[i], srs);
                        EntityManager.AddComponentData(toBeBuffered[i], new BufferedRenderSubjectTag());
                        EntityManager.AddChunkComponentData<SpriteMatrixChangeTag>(toBeBuffered[i]);
                    }
                    if (Logging) UnityEngine.Debug.Log(string.Format("UsedIndex: {0} -> {1}.", prevIndexRight, usedIndexRight));
                    bss.Users = usedIndexRight;
                }
                if (Logging) UnityEngine.Debug.Log(string.Format("Added {0} users to the buffer of sprite sheet#{1}.", toBeBuffered.Length, sheet));
                toBeBuffered.Dispose();
                if (Logging) UnityEngine.Debug.Log(string.Format("MatrixChangedChunks: " + matrixChangedChunkQuery.CalculateChunkCount()));
            }
        }
    }
}