using Unity.Collections;
using Unity.Entities;
using SECS.AI.Core;

namespace SECS.AI.BT
{
   
    public static class BehaviorTreeExecutor<TContext> where TContext : struct
    {
        /// <summary>
        /// 执行行为树
        /// </summary>
        public static BTState Execute(
            ref BlobArray<BTNode> nodes,
            int startNodeIndex,
            Entity entity,
            DynamicBuffer<BTBlackboardEntry> blackboard,
            ref TContext context,
            BTActionRegistryManaged<TContext> registry)
        {
            if (startNodeIndex < 0 || startNodeIndex >= nodes.Length)
                return BTState.Failure;
            
            return ExecuteNode(ref nodes, startNodeIndex, entity, blackboard, ref context, registry);
        }
        
        /// <summary>
        /// 递归执行节点
        /// </summary>
        private static BTState ExecuteNode(
            ref BlobArray<BTNode> nodes,
            int nodeIndex,
            Entity entity,
            DynamicBuffer<BTBlackboardEntry> blackboard,
            ref TContext context,
            BTActionRegistryManaged<TContext> registry)
        {
            if (nodeIndex < 0 || nodeIndex >= nodes.Length)
                return BTState.Failure;
            
            var node = nodes[nodeIndex];
            
            // 控制流节点
            switch (node.Kind)
            {
                case BTNodeKind.Selector:
                    return ExecuteSelector(ref nodes, node, entity, blackboard, ref context, registry);
                    
                case BTNodeKind.Sequence:
                    return ExecuteSequence(ref nodes, node, entity, blackboard, ref context, registry);
                    
                case BTNodeKind.Parallel:
                    return ExecuteParallel(ref nodes, node, entity, blackboard, ref context, registry);
                    
                case BTNodeKind.Invert:
                    return ExecuteInvert(ref nodes, node, entity, blackboard, ref context, registry);
                    
                case BTNodeKind.Succeeder:
                    return ExecuteSucceeder(ref nodes, node, entity, blackboard, ref context, registry);
                    
                case BTNodeKind.Repeater:
                    return ExecuteRepeater(ref nodes, node, entity, blackboard, ref context, registry);
                    
                case BTNodeKind.Interrupt:
                    return ExecuteInterrupt(ref nodes, node, entity, blackboard, ref context, registry);
                    
                case BTNodeKind.Action:
                    // Action 节点 - 通过注册表执行
                    int actionHash = node.ParamI0;
                    if (registry.TryGet(actionHash, out var executor))
                    {
                       
                        return executor.Execute(in context);
                    }
                    return BTState.Failure;
                    
                case BTNodeKind.SetBlackboard:
                    return ExecuteSetBlackboard(node, blackboard);
                    
                case BTNodeKind.ClearBlackboard:
                    return ExecuteClearBlackboard(node, blackboard);
                    
                default:
                    return BTState.Failure;
            }
        }
        
        // ==================== 控制流节点实现 ====================
        
        private static BTState ExecuteSelector(
            ref BlobArray<BTNode> nodes,
            BTNode node,
            Entity entity,
            DynamicBuffer<BTBlackboardEntry> bb,
            ref TContext context,
            BTActionRegistryManaged<TContext> registry)
        {
            int childIndex = node.FirstChild;
            while (childIndex != -1)
            {
                var state = ExecuteNode(ref nodes, childIndex, entity, bb, ref context, registry);
                if (state == BTState.Success) return BTState.Success;
                if (state == BTState.Running) return BTState.Running;
                childIndex = nodes[childIndex].NextSibling;
            }
            return BTState.Failure;
        }
        
        private static BTState ExecuteSequence(
            ref BlobArray<BTNode> nodes,
            BTNode node,
            Entity entity,
            DynamicBuffer<BTBlackboardEntry> bb,
            ref TContext context,
            BTActionRegistryManaged<TContext> registry)
        {
            int childIndex = node.FirstChild;
            while (childIndex != -1)
            {
                var state = ExecuteNode(ref nodes, childIndex, entity, bb, ref context, registry);
                if (state == BTState.Failure) return BTState.Failure;
                if (state == BTState.Running) return BTState.Running;
                childIndex = nodes[childIndex].NextSibling;
            }
            return BTState.Success;
        }
        
        private static BTState ExecuteParallel(
            ref BlobArray<BTNode> nodes,
            BTNode node,
            Entity entity,
            DynamicBuffer<BTBlackboardEntry> bb,
            ref TContext context,
            BTActionRegistryManaged<TContext> registry)
        {
            bool anyRunning = false;
            int childIndex = node.FirstChild;
            while (childIndex != -1)
            {
                var state = ExecuteNode(ref nodes, childIndex, entity, bb, ref context, registry);
                if (state == BTState.Failure) return BTState.Failure;
                if (state == BTState.Running) anyRunning = true;
                childIndex = nodes[childIndex].NextSibling;
            }
            return anyRunning ? BTState.Running : BTState.Success;
        }
        
        private static BTState ExecuteInvert(
            ref BlobArray<BTNode> nodes,
            BTNode node,
            Entity entity,
            DynamicBuffer<BTBlackboardEntry> bb,
            ref TContext context,
            BTActionRegistryManaged<TContext> registry)
        {
            int childIndex = node.FirstChild;
            if (childIndex == -1) return BTState.Failure;
            
            var result = ExecuteNode(ref nodes, childIndex, entity, bb, ref context, registry);
            if (result == BTState.Success) return BTState.Failure;
            if (result == BTState.Failure) return BTState.Success;
            return result;
        }
        
        private static BTState ExecuteSucceeder(
            ref BlobArray<BTNode> nodes,
            BTNode node,
            Entity entity,
            DynamicBuffer<BTBlackboardEntry> bb,
            ref TContext context,
            BTActionRegistryManaged<TContext> registry)
        {
            int childIndex = node.FirstChild;
            if (childIndex == -1) return BTState.Success;
            
            var result = ExecuteNode(ref nodes, childIndex, entity, bb, ref context, registry);
            return result == BTState.Running ? BTState.Running : BTState.Success;
        }
        
        private static BTState ExecuteRepeater(
            ref BlobArray<BTNode> nodes,
            BTNode node,
            Entity entity,
            DynamicBuffer<BTBlackboardEntry> bb,
            ref TContext context,
            BTActionRegistryManaged<TContext> registry)
        {
            int childIndex = node.FirstChild;
            if (childIndex == -1) return BTState.Success;
            
            int targetCount = node.ParamI0;
            int counterKeyHash = -(childIndex + 1000);
            
            // 读取当前计数
            int currentCount = (int)GetBlackboardValue(counterKeyHash, bb);
            
            // 执行子节点
            var childState = ExecuteNode(ref nodes, childIndex, entity, bb, ref context, registry);
            
            if (childState == BTState.Failure)
            {
                SetBlackboardValue(counterKeyHash, 0, bb);
                return BTState.Failure;
            }
            
            if (childState == BTState.Success)
            {
                currentCount++;
                if (targetCount >= 0 && currentCount >= targetCount)
                {
                    SetBlackboardValue(counterKeyHash, 0, bb);
                    return BTState.Success;
                }
                SetBlackboardValue(counterKeyHash, currentCount, bb);
                return BTState.Running;
            }
            
            return BTState.Running;
        }
        
        private static BTState ExecuteInterrupt(
            ref BlobArray<BTNode> nodes,
            BTNode node,
            Entity entity,
            DynamicBuffer<BTBlackboardEntry> bb,
            ref TContext context,
            BTActionRegistryManaged<TContext> registry)
        {
            int childIndex = node.FirstChild;
            if (childIndex == -1) return BTState.Success;
            
            int interruptKeyHash = node.ParamI0;
            if (interruptKeyHash != 0)
            {
                float value = GetBlackboardValue(interruptKeyHash, bb);
                if (value != 0f)
                {
                    SetBlackboardValue(interruptKeyHash, 0, bb);
                    return BTState.Failure;
                }
            }
            
            return ExecuteNode(ref nodes, childIndex, entity, bb, ref context, registry);
        }
        
        // ==================== 黑板操作节点 ====================
        
        private static BTState ExecuteSetBlackboard(BTNode node, DynamicBuffer<BTBlackboardEntry> bb)
        {
            int keyHash = node.ParamI0;
            float value = node.ParamF0;
            SetBlackboardValue(keyHash, value, bb);
            return BTState.Success;
        }
        
        private static BTState ExecuteClearBlackboard(BTNode node, DynamicBuffer<BTBlackboardEntry> bb)
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
        
        private static float GetBlackboardValue(int keyHash, DynamicBuffer<BTBlackboardEntry> bb)
        {
            for (int i = 0; i < bb.Length; i++)
            {
                if (bb[i].KeyHash == keyHash)
                    return bb[i].Value;
            }
            return 0f;
        }
        
        private static void SetBlackboardValue(int keyHash, float value, DynamicBuffer<BTBlackboardEntry> bb)
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
            
            // 不存在则添加
            bb.Add(new BTBlackboardEntry { KeyHash = keyHash, Value = value });
        }
    }
}
