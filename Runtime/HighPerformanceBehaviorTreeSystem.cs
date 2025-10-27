using System;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using AOT;
using SECS.AI.Core;

namespace SECS.AI.BT.Samples
{
    /// <summary>
    /// 高性能行为树系统 
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct HighPerformanceBehaviorTreeSystem : ISystem
    {
        // ==================== Action 注册表 ====================
        
        private BTActionRegistry _registry;
        private EntityQuery _query;
        
        public void OnCreate(ref SystemState state)
        {
            _registry = new BTActionRegistry(16, Allocator.Persistent);
            
            // 注册所有 Action 
            RegisterActions();
            
            // 创建查询 
            _query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<BTTreeRef, BTBlackboardEntry>()
                .Build(ref state);
        }
        
        private void RegisterActions()
        {
            // 注册函数指针 
            _registry.Register(
                BTActionKindHash.Hash("MoveForward"),
                BurstCompiler.CompileFunctionPointer<ActionExecutorDelegate>(ExecuteMoveForward)
            );
            
            _registry.Register(
                BTActionKindHash.Hash("MoveToTarget"),
                BurstCompiler.CompileFunctionPointer<ActionExecutorDelegate>(ExecuteMoveToTarget)
            );
            
            _registry.Register(
                BTActionKindHash.Hash("RotateToDirection"),
                BurstCompiler.CompileFunctionPointer<ActionExecutorDelegate>(ExecuteRotateToDirection)
            );
            
            _registry.Register(
                BTActionKindHash.Hash("RandomSuccess"),
                BurstCompiler.CompileFunctionPointer<ActionExecutorDelegate>(ExecuteRandomSuccess)
            );
        }
        
        // ==================== Action 实现  ====================
        
        /// <summary>
        /// Action: 向前移动
        /// 参数: ParamF0 = 速度
        /// </summary>
        [BurstCompile]
        [MonoPInvokeCallback(typeof(ActionExecutorDelegate))]
        private static unsafe BTState ExecuteMoveForward(in BTNode node, ref ActionExecutionContext context)
        {
            var transformLookup = (ComponentLookup<LocalTransform>*)context.TransformLookup;
            var ecb = (EntityCommandBuffer.ParallelWriter*)context.Ecb;
            
            if (!transformLookup->HasComponent(context.CurrentEntity))
                return BTState.Failure;
            
            var transform = (*transformLookup)[context.CurrentEntity];
            float speed = node.ParamF0;
            
            transform.Position += new float3(0, 0, speed) * context.DeltaTime;
            
            ecb->SetComponent(context.SortKey, context.CurrentEntity, transform);
            return BTState.Success;
        }
        
        /// <summary>
        /// Action: 移动到目标位置
        /// </summary>
        [BurstCompile]
        [MonoPInvokeCallback(typeof(ActionExecutorDelegate))]
        private static unsafe BTState ExecuteMoveToTarget(in BTNode node, ref ActionExecutionContext context)
        {
            var transformLookup = (ComponentLookup<LocalTransform>*)context.TransformLookup;
            var ecb = (EntityCommandBuffer.ParallelWriter*)context.Ecb;
            
            if (!transformLookup->HasComponent(context.CurrentEntity))
                return BTState.Failure;
            
            var transform = (*transformLookup)[context.CurrentEntity];
            float3 targetPos = new float3(node.ParamF0, node.ParamF1, node.ParamF2);
            float speed = node.ParamF3;
            
            float distance = math.distance(transform.Position, targetPos);
            if (distance < 0.1f)
            {
                transform.Position = targetPos;
                ecb->SetComponent(context.SortKey, context.CurrentEntity, transform);
                return BTState.Success;
            }
            
            float3 direction = math.normalize(targetPos - transform.Position);
            transform.Position += direction * speed * context.DeltaTime;
            
            ecb->SetComponent(context.SortKey, context.CurrentEntity, transform);
            return BTState.Running;
        }
        
        /// <summary>
        /// Action: 旋转到目标方向
        /// </summary>
        [BurstCompile]
        [MonoPInvokeCallback(typeof(ActionExecutorDelegate))]
        private static unsafe BTState ExecuteRotateToDirection(in BTNode node, ref ActionExecutionContext context)
        {
            var transformLookup = (ComponentLookup<LocalTransform>*)context.TransformLookup;
            var ecb = (EntityCommandBuffer.ParallelWriter*)context.Ecb;
            
            if (!transformLookup->HasComponent(context.CurrentEntity))
                return BTState.Failure;
            
            var transform = (*transformLookup)[context.CurrentEntity];
            float3 targetDir = math.normalize(new float3(node.ParamF0, node.ParamF1, node.ParamF2));
            float rotationSpeed = node.ParamF3;
            
            if (math.lengthsq(targetDir) < 0.01f)
                return BTState.Failure;
            
            quaternion targetRotation = quaternion.LookRotationSafe(targetDir, new float3(0, 1, 0));
            transform.Rotation = math.slerp(transform.Rotation, targetRotation, rotationSpeed * context.DeltaTime);
            
            // 检查是否接近目标方向
            float angle = math.abs(math.dot(transform.Forward(), targetDir));
            if (angle > 0.99f)
            {
                transform.Rotation = targetRotation;
                ecb->SetComponent(context.SortKey, context.CurrentEntity, transform);
                return BTState.Success;
            }
            
            ecb->SetComponent(context.SortKey, context.CurrentEntity, transform);
            return BTState.Running;
        }
        
        /// <summary>
        /// Action: 随机成功/失败
        /// </summary>
        [BurstCompile]
        [MonoPInvokeCallback(typeof(ActionExecutorDelegate))]
        private static BTState ExecuteRandomSuccess(in BTNode node, ref ActionExecutionContext context)
        {
            float successRate = node.ParamF0;
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)context.CurrentEntity.Index);
            return random.NextFloat() < successRate ? BTState.Success : BTState.Failure;
        }
        
        // ==================== 系统更新 ====================
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // 获取 ComponentLookup
            var transformLookup = state.GetComponentLookup<LocalTransform>(true);
            
            // 创建 Job
            var job = new BehaviorTreeTickJob
            {
                DeltaTime = dt,
                EntityType = state.GetEntityTypeHandle(),
                TreeRefType = state.GetComponentTypeHandle<BTTreeRef>(true),
                BlackboardBufferType = state.GetBufferTypeHandle<BTBlackboardEntry>(false),
                TransformLookup = transformLookup,
                Ecb = ecb.AsParallelWriter(),
                Registry = _registry
            };
            
            state.Dependency = job.ScheduleParallel(_query, state.Dependency);
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _registry.Dispose();
        }
        
        // ==================== IJobChunk 实现 ====================
        
        [BurstCompile]
        private struct BehaviorTreeTickJob : IJobChunk
        {
            public float DeltaTime;
            
            // TypeHandles
            public EntityTypeHandle EntityType;
            [ReadOnly] public ComponentTypeHandle<BTTreeRef> TreeRefType;
            public BufferTypeHandle<BTBlackboardEntry> BlackboardBufferType;
            
            // Lookups
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            
            // ECB
            public EntityCommandBuffer.ParallelWriter Ecb;
            
            // Action Registry (非泛型 struct - Burst 兼容)
            [ReadOnly] public BTActionRegistry Registry;
            
            public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(EntityType);
                var treeRefs = chunk.GetNativeArray(ref TreeRefType);
                var blackboardAccessor = chunk.GetBufferAccessor(ref BlackboardBufferType);
                
                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    var entity = entities[i];
                    var treeRef = treeRefs[i];
                    
                    if (!treeRef.Value.IsCreated) continue;
                    
                    ref var nodes = ref treeRef.Value.Value.Nodes;
                    var blackboard = blackboardAccessor[i];
                    
                    // 创建上下文
                    var transformLookupPtr = TransformLookup;
                    var ecbPtr = Ecb;
                    
                    var context = new ActionExecutionContext
                    {
                        CurrentEntity = entity,
                        DeltaTime = DeltaTime,
                        SortKey = unfilteredChunkIndex * chunk.Count + i,
                        TransformLookup = &transformLookupPtr,
                        Ecb = &ecbPtr
                    };
                    
                    // 执行行为树
                    ExecuteTreeWithNodeUpdate(ref nodes, 0, entity, ref blackboard, ref context, Registry);
                }
            }
            
            // ==================== 执行逻辑 ====================
            
            [BurstCompile]
            private static BTState ExecuteTreeWithNodeUpdate(
                ref BlobArray<BTNode> nodes,
                int nodeIndex,
                in Entity entity,
                ref DynamicBuffer<BTBlackboardEntry> blackboard,
                ref ActionExecutionContext context,
                in BTActionRegistry registry)
            {
                if (nodeIndex < 0 || nodeIndex >= nodes.Length)
                    return BTState.Failure;
                
                var node = nodes[nodeIndex];
                
                switch (node.Kind)
                {
                    case BTNodeKind.Selector:
                        return ExecuteSelector(ref nodes, node, entity, ref blackboard, ref context, registry);
                    case BTNodeKind.Sequence:
                        return ExecuteSequence(ref nodes, node, entity, ref blackboard, ref context, registry); 
                    case BTNodeKind.Parallel:
                        return ExecuteParallel(ref nodes, node, entity, ref blackboard, ref context, registry);
                    case BTNodeKind.Invert:
                        return ExecuteInvert(ref nodes, node, entity, ref blackboard, ref context, registry);
                    case BTNodeKind.Succeeder:
                        return ExecuteSucceeder(ref nodes, node, entity, ref blackboard, ref context, registry);
                    case BTNodeKind.Repeater:
                        return ExecuteRepeater(ref nodes, node, entity, ref blackboard, ref context, registry); 
                    case BTNodeKind.Interrupt:
                        return ExecuteInterrupt(ref nodes, node, entity, ref blackboard, ref context, registry); 
                    case BTNodeKind.Action:
                        // 使用函数指针执行 Action
                        int actionHash = node.ParamI0;
                        return registry.Execute(actionHash, in node, ref context);
                    case BTNodeKind.SetBlackboard:
                        return ExecuteSetBlackboard(node, ref blackboard);
                    case BTNodeKind.ClearBlackboard:
                        return ExecuteClearBlackboard(node, ref blackboard);
                    default:
                        return BTState.Failure;
                }
            }
        
            
            // ==================== 控制流节点实现 ====================
            
            [BurstCompile]
            private static BTState ExecuteSelector(ref BlobArray<BTNode> nodes, in BTNode node, in Entity entity, 
                ref DynamicBuffer<BTBlackboardEntry> bb, ref ActionExecutionContext ctx, in BTActionRegistry reg)
            {
                int childIndex = node.FirstChild;
                while (childIndex != -1)
                {
                    var state = ExecuteTreeWithNodeUpdate(ref nodes, childIndex, entity, ref bb, ref ctx, reg);
                    if (state == BTState.Success) return BTState.Success;
                    if (state == BTState.Running) return BTState.Running;
                    childIndex = nodes[childIndex].NextSibling;
                }
                return BTState.Failure;
            }
            
            [BurstCompile]
            private static BTState ExecuteSequence(ref BlobArray<BTNode> nodes, in BTNode node, in Entity entity,
                ref DynamicBuffer<BTBlackboardEntry> bb, ref ActionExecutionContext ctx, in BTActionRegistry reg)
            {
                int childIndex = node.FirstChild;
                while (childIndex != -1)
                {
                    var state = ExecuteTreeWithNodeUpdate(ref nodes, childIndex, entity, ref bb, ref ctx, reg);
                    if (state == BTState.Failure) return BTState.Failure;
                    if (state == BTState.Running) return BTState.Running;
                    childIndex = nodes[childIndex].NextSibling;
                }
                return BTState.Success;
            }
            
            [BurstCompile]
            private static BTState ExecuteParallel(ref BlobArray<BTNode> nodes, in BTNode node, in Entity entity,
                ref DynamicBuffer<BTBlackboardEntry> bb, ref ActionExecutionContext ctx, in BTActionRegistry reg)
            {
                bool anyRunning = false;
                int childIndex = node.FirstChild;
                while (childIndex != -1)
                {
                    var state = ExecuteTreeWithNodeUpdate(ref nodes, childIndex, entity, ref bb, ref ctx, reg);
                    if (state == BTState.Failure) return BTState.Failure;
                    if (state == BTState.Running) anyRunning = true;
                    childIndex = nodes[childIndex].NextSibling;
                }
                return anyRunning ? BTState.Running : BTState.Success;
            }
            
            [BurstCompile]
            private static BTState ExecuteInvert(ref BlobArray<BTNode> nodes, in BTNode node, in Entity entity,
                ref DynamicBuffer<BTBlackboardEntry> bb, ref ActionExecutionContext ctx, in BTActionRegistry reg)
            {
                int childIndex = node.FirstChild;
                if (childIndex == -1) return BTState.Failure;
                var result = ExecuteTreeWithNodeUpdate(ref nodes, childIndex, entity, ref bb, ref ctx, reg);
                if (result == BTState.Success) return BTState.Failure;
                if (result == BTState.Failure) return BTState.Success;
                return result;
            }
            
            [BurstCompile]
            private static BTState ExecuteSucceeder(ref BlobArray<BTNode> nodes, in BTNode node, in Entity entity,
                ref DynamicBuffer<BTBlackboardEntry> bb, ref ActionExecutionContext ctx, in BTActionRegistry reg)
            {
                int childIndex = node.FirstChild;
                if (childIndex == -1) return BTState.Success;
                var result = ExecuteTreeWithNodeUpdate(ref nodes, childIndex, entity, ref bb, ref ctx, reg);
                return result == BTState.Running ? BTState.Running : BTState.Success;
            }
            
            [BurstCompile]
            private static BTState ExecuteRepeater(ref BlobArray<BTNode> nodes, in BTNode node, in Entity entity,
                ref DynamicBuffer<BTBlackboardEntry> bb, ref ActionExecutionContext ctx, in BTActionRegistry reg)
            {
                int childIndex = node.FirstChild;
                if (childIndex == -1) return BTState.Success;
                
                int targetCount = node.ParamI0;
                int counterKeyHash = -(childIndex + 1000);
                int currentCount = (int)GetBlackboardValue(counterKeyHash, ref bb);
                
                var childState = ExecuteTreeWithNodeUpdate(ref nodes, childIndex, entity, ref bb, ref ctx, reg);
                
                if (childState == BTState.Failure)
                {
                    SetBlackboardValue(counterKeyHash, 0, ref bb);
                    return BTState.Failure;
                }
                if (childState == BTState.Success)
                {
                    currentCount++;
                    if (targetCount >= 0 && currentCount >= targetCount)
                    {
                        SetBlackboardValue(counterKeyHash, 0, ref bb);
                        return BTState.Success;
                    }
                    SetBlackboardValue(counterKeyHash, currentCount, ref bb);
                    return BTState.Running;
                }
                return BTState.Running;
            }
            
            [BurstCompile]
            private static BTState ExecuteInterrupt(ref BlobArray<BTNode> nodes, in BTNode node, in Entity entity,
                ref DynamicBuffer<BTBlackboardEntry> bb, ref ActionExecutionContext ctx, in BTActionRegistry reg)
            {
                int childIndex = node.FirstChild;
                if (childIndex == -1) return BTState.Success;
                
                int interruptKeyHash = node.ParamI0;
                if (interruptKeyHash != 0)
                {
                    float value = GetBlackboardValue(interruptKeyHash, ref bb);
                    if (value != 0f)
                    {
                        SetBlackboardValue(interruptKeyHash, 0, ref bb);
                        return BTState.Failure;
                    }
                }
                return ExecuteTreeWithNodeUpdate(ref nodes, childIndex, entity, ref bb, ref ctx, reg);
            }
            
            // ==================== 黑板操作节点 ====================
            
            [BurstCompile]
            private static BTState ExecuteSetBlackboard(in BTNode node, ref DynamicBuffer<BTBlackboardEntry> bb)
            {
                int keyHash = node.ParamI0;
                float value = node.ParamF0;
                SetBlackboardValue(keyHash, value, ref bb);
                return BTState.Success;
            }
            
            [BurstCompile]
            private static BTState ExecuteClearBlackboard(in BTNode node, ref DynamicBuffer<BTBlackboardEntry> bb)
            {
                int keyHash = node.ParamI0;
                if (keyHash == 0)
                {
                    bb.Clear();
                }
                else
                {
                    for (int i = 0; i < bb.Length; i++)
                    {
                        if (bb[i].KeyHash == keyHash)
                        {
                            bb.RemoveAt(i);
                            break;
                        }
                    }
                }
                return BTState.Success;
            }
            
            // ==================== 黑板辅助方法 ====================
            
            [BurstCompile]
            private static float GetBlackboardValue(int keyHash, ref DynamicBuffer<BTBlackboardEntry> bb)
            {
                for (int i = 0; i < bb.Length; i++)
                    if (bb[i].KeyHash == keyHash) return bb[i].Value;
                return 0f;
            }
            
            [BurstCompile]
            private static void SetBlackboardValue(int keyHash, float value, ref DynamicBuffer<BTBlackboardEntry> bb)
            {
                for (int i = 0; i < bb.Length; i++)
                {
                    if (bb[i].KeyHash == keyHash)
                    {
                        var entry = bb[i];
                        entry.Value = value;
                        bb[i] = entry;
                        return;
                    }
                }
                bb.Add(new BTBlackboardEntry { KeyHash = keyHash, Value = value });
            }
        }
    }
}
