using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using SECS.AI.Core;
namespace SECS.AI.BT
{
   
    
   
    /// <summary>
    /// Layer 3: 简化行为树系统
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class SimpleBehaviorTreeSystem : SystemBase
    {
        private BTActionProvider _provider;
        private EndSimulationEntityCommandBufferSystem _ecbSystem;
        
        protected override void OnCreate()
        {
            _provider = new BTActionProvider();
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            // 注册默认 Action
            RegisterDefaultActions();
            
            // 注册用户自定义 Action
            RegisterCustomActions();
        }
        
        /// <summary>
        /// 注册默认 Action
        /// </summary>
        protected virtual void RegisterDefaultActions()
        {
            // Wait节点
            _provider.Register("Wait", ExecuteWait);
        }
        
        /// <summary>
        /// 供用户在子类中注册自定义 Action
        /// 
        /// 只需要重写此方法，使用 Provider.Register() 注册即可
        /// </summary>
        protected virtual void RegisterCustomActions()
        {
            // 在子类中重写此方法
        }
        
        /// <summary>
        /// 暴露 Provider 给子类
        /// </summary>
        protected BTActionProvider Provider => _provider;
        
        protected override void OnUpdate()
        {
            var dt = SystemAPI.Time.DeltaTime;
            var ecb = _ecbSystem.CreateCommandBuffer();
            var entityManager = EntityManager;
            
            // 主线程执行
            Entities
                .WithAll<BTTreeRef>()
                .ForEach((Entity entity, 
                    in BTTreeRef treeRef,
                    in DynamicBuffer<BTBlackboardEntry> blackboard) =>
                {
                    if (!treeRef.Value.IsCreated)
                        return;
                    
                    ref var nodes = ref treeRef.Value.Value.Nodes;
                    
                    // 创建上下文
                    var context = new BTActionProvider.ActionContext
                    {
                        Entity = entity,
                        DeltaTime = dt,
                        Ecb = ecb.AsParallelWriter(),
                        SortKey = entity.Index,
                        EntityManager = entityManager
                    };
                    
                    // 获取可写黑板
                    var mutableBlackboard = entityManager.GetBuffer<BTBlackboardEntry>(entity);
                    
                    // 执行行为树
                    ExecuteNode(ref nodes, 0, context, mutableBlackboard);
                    
                }).WithoutBurst().WithStructuralChanges().Run();
        }
        
        /// <summary>
        /// 执行节点
        /// </summary>
        private BTState ExecuteNode(
            ref BlobArray<BTNode> nodes, 
            int nodeIndex, 
            BTActionProvider.ActionContext context,
            DynamicBuffer<BTBlackboardEntry> blackboard)
        {
            if (nodeIndex < 0 || nodeIndex >= nodes.Length)
                return BTState.Failure;
            
            var node = nodes[nodeIndex];
            context.Node = node;
            
            // 控制流节点
            switch (node.Kind)
            {
                case BTNodeKind.Selector:
                    return ExecuteSelector(ref nodes, node, context, blackboard);
                case BTNodeKind.Sequence:
                    return ExecuteSequence(ref nodes, node, context, blackboard);
                case BTNodeKind.Parallel:
                    return ExecuteParallel(ref nodes, node, context, blackboard);
                case BTNodeKind.Invert:
                    return ExecuteInvert(ref nodes, node, context, blackboard);
                case BTNodeKind.Succeeder:
                    return ExecuteSucceeder(ref nodes, node, context, blackboard);
                case BTNodeKind.Repeater:
                    return ExecuteRepeater(ref nodes, node, context, blackboard);
                case BTNodeKind.Interrupt:
                    return ExecuteInterrupt(ref nodes, node, context, blackboard);
                case BTNodeKind.Action:
                    // Action 节点通过 ParamI0 查找注册的 Action
                    return _provider.Execute(node.ParamI0, context, blackboard);
                default:
                    return BTState.Failure;
            }
        }
        
        // 控制流节点实现
        private BTState ExecuteSelector(ref BlobArray<BTNode> nodes, BTNode node, BTActionProvider.ActionContext ctx, DynamicBuffer<BTBlackboardEntry> bb)
        {
            int childIndex = node.FirstChild;
            while (childIndex != -1)
            {
                var state = ExecuteNode(ref nodes, childIndex, ctx, bb);
                if (state == BTState.Success) return BTState.Success;
                if (state == BTState.Running) return BTState.Running;
                childIndex = nodes[childIndex].NextSibling;
            }
            return BTState.Failure;
        }
        
        private BTState ExecuteSequence(ref BlobArray<BTNode> nodes, BTNode node, BTActionProvider.ActionContext ctx, DynamicBuffer<BTBlackboardEntry> bb)
        {
            int childIndex = node.FirstChild;
            while (childIndex != -1)
            {
                var state = ExecuteNode(ref nodes, childIndex, ctx, bb);
                if (state == BTState.Failure) return BTState.Failure;
                if (state == BTState.Running) return BTState.Running;
                childIndex = nodes[childIndex].NextSibling;
            }
            return BTState.Success;
        }
        
        private BTState ExecuteParallel(ref BlobArray<BTNode> nodes, BTNode node, BTActionProvider.ActionContext ctx, DynamicBuffer<BTBlackboardEntry> bb)
        {
            bool anyRunning = false;
            int childIndex = node.FirstChild;
            while (childIndex != -1)
            {
                var state = ExecuteNode(ref nodes, childIndex, ctx, bb);
                if (state == BTState.Failure) return BTState.Failure;
                if (state == BTState.Running) anyRunning = true;
                childIndex = nodes[childIndex].NextSibling;
            }
            return anyRunning ? BTState.Running : BTState.Success;
        }
        
        private BTState ExecuteInvert(ref BlobArray<BTNode> nodes, BTNode node, BTActionProvider.ActionContext ctx, DynamicBuffer<BTBlackboardEntry> bb)
        {
            int childIndex = node.FirstChild;
            if (childIndex == -1) return BTState.Failure;
            var result = ExecuteNode(ref nodes, childIndex, ctx, bb);
            if (result == BTState.Success) return BTState.Failure;
            if (result == BTState.Failure) return BTState.Success;
            return result;
        }
        
        private BTState ExecuteSucceeder(ref BlobArray<BTNode> nodes, BTNode node, BTActionProvider.ActionContext ctx, DynamicBuffer<BTBlackboardEntry> bb)
        {
            int childIndex = node.FirstChild;
            if (childIndex == -1) return BTState.Success;
            var result = ExecuteNode(ref nodes, childIndex, ctx, bb);
            return result == BTState.Running ? BTState.Running : BTState.Success;
        }
        
        private BTState ExecuteRepeater(ref BlobArray<BTNode> nodes, BTNode node, BTActionProvider.ActionContext ctx, DynamicBuffer<BTBlackboardEntry> bb)
        {
            int childIndex = node.FirstChild;
            if (childIndex == -1) return BTState.Success;
            
            int targetCount = node.ParamI0;
            int counterKeyHash = -(childIndex + 1000);
            
            
            int currentCount = (int)ctx.GetBlackboardValue(counterKeyHash, bb);
            
            var childState = ExecuteNode(ref nodes, childIndex, ctx, bb);
            
            if (childState == BTState.Failure)
            {
                ctx.SetBlackboardValue(counterKeyHash, 0, bb);
                return BTState.Failure;
            }
            
            if (childState == BTState.Success)
            {
                currentCount++;
                if (targetCount >= 0 && currentCount >= targetCount)
                {
                    ctx.SetBlackboardValue(counterKeyHash, 0, bb);
                    return BTState.Success;
                }
                ctx.SetBlackboardValue(counterKeyHash, currentCount, bb);
                return BTState.Running;
            }
            
            return BTState.Running;
        }
        
        private BTState ExecuteInterrupt(ref BlobArray<BTNode> nodes, BTNode node, BTActionProvider.ActionContext ctx, DynamicBuffer<BTBlackboardEntry> bb)
        {
            int childIndex = node.FirstChild;
            if (childIndex == -1) return BTState.Success;
            
            int interruptKeyHash = node.ParamI0;
            if (interruptKeyHash != 0)
            {
                float value = ctx.GetBlackboardValue(interruptKeyHash, bb);
                if (value != 0f)
                {
                    ctx.SetBlackboardValue(interruptKeyHash, 0, bb);
                    return BTState.Failure;
                }
            }
            
            return ExecuteNode(ref nodes, childIndex, ctx, bb);
        }
        
        /// <summary>
        /// Wait 节点实现
        /// </summary>
        private BTState ExecuteWait(BTActionProvider.ActionContext ctx, DynamicBuffer<BTBlackboardEntry> blackboard)
        {
            float duration = ctx.Node.ParamF0;
            int nodeIndex = ctx.Node.ParamI0;
            
            float elapsed = ctx.GetBlackboardValue(nodeIndex, blackboard);
            elapsed += ctx.DeltaTime;
            
            if (elapsed >= duration)
            {
                // 清除黑板
                for (int i = 0; i < blackboard.Length; i++)
                {
                    if (blackboard[i].KeyHash == nodeIndex)
                    {
                        blackboard.RemoveAt(i);
                        break;
                    }
                }
                return BTState.Success;
            }
            else
            {
                ctx.SetBlackboardValue(nodeIndex, elapsed, blackboard);
                return BTState.Running;
            }
        }
    }
}
