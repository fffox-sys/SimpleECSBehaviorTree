using Unity.Entities;

namespace SECS.AI.BT
{
    /// <summary>
    /// 通用行为树 Tick 执行器
    /// </summary>
    public static class BTTickExecutor
    {
        /// <summary>
        /// Action 执行委托
        /// </summary>
        public delegate BTState ActionExecutorDelegate(
            ref Unity.Entities.BlobArray<BTNode> nodes,
            int nodeIndex,
            object userContext);

        /// <summary>
        /// 黑板操作委托
        /// </summary>
        public delegate BTState BlackboardExecutorDelegate(
            int keyHash,
            float value,
            object userContext);

        /// <summary>
        /// 执行行为树节点
        /// </summary>
        /// <param name="nodes">行为树节点数组</param>
        /// <param name="nodeIndex">当前节点索引</param>
        /// <param name="userContext">用户上下文 </param>
        /// <param name="actionExecutor">Action 执行委托</param>
        /// <param name="blackboardExecutor">黑板操作委托 </param>
        /// <param name="traceCallback">轨迹记录回调</param>
        /// <returns>节点执行结果</returns>
        public static BTState TickNode(
            ref Unity.Entities.BlobArray<BTNode> nodes,
            int nodeIndex,
            object userContext,
            ActionExecutorDelegate actionExecutor,
            BlackboardExecutorDelegate blackboardExecutor = null,
            System.Action<int, BTState> traceCallback = null)
        {
            if (nodeIndex < 0 || nodeIndex >= nodes.Length)
                return BTState.Failure;

            var node = nodes[nodeIndex];
            BTState result;

            switch (node.Kind)
            {
                

                case BTNodeKind.Selector:
                    result = ExecuteSelector(ref nodes, node, userContext, actionExecutor, blackboardExecutor, traceCallback);
                    break;

                case BTNodeKind.Sequence:
                    result = ExecuteSequence(ref nodes, node, userContext, actionExecutor, blackboardExecutor, traceCallback);
                    break;

                case BTNodeKind.Parallel:
                    result = ExecuteParallel(ref nodes, node, userContext, actionExecutor, blackboardExecutor, traceCallback);
                    break;

                case BTNodeKind.Invert:
                    result = ExecuteInvert(ref nodes, node, userContext, actionExecutor, blackboardExecutor, traceCallback);
                    break;

                case BTNodeKind.Succeeder:
                    result = ExecuteSucceeder(ref nodes, node, userContext, actionExecutor, blackboardExecutor, traceCallback);
                    break;

                case BTNodeKind.Repeater:
                    result = ExecuteRepeater(ref nodes, node, nodeIndex, userContext, actionExecutor, blackboardExecutor, traceCallback);
                    break;

                case BTNodeKind.Interrupt:
                    result = ExecuteInterrupt(ref nodes, node, userContext, actionExecutor, blackboardExecutor, traceCallback);
                    break;

               

                case BTNodeKind.SetBlackboard:
                    result = blackboardExecutor?.Invoke(node.ParamI0, node.ParamF0, userContext) ?? BTState.Failure;
                    break;

                case BTNodeKind.ClearBlackboard:
                    result = blackboardExecutor?.Invoke(node.ParamI0, 0f, userContext) ?? BTState.Failure;
                    break;

               

                default:
                    result = actionExecutor?.Invoke(ref nodes, nodeIndex, userContext) ?? BTState.Failure;
                    break;
            }

           
            traceCallback?.Invoke(nodeIndex, result);

            return result;
        }

      
        private static BTState ExecuteSelector(
            ref Unity.Entities.BlobArray<BTNode> nodes,
            BTNode node,
            object userContext,
            ActionExecutorDelegate actionExecutor,
            BlackboardExecutorDelegate blackboardExecutor,
            System.Action<int, BTState> traceCallback)
        {
            int childIndex = node.FirstChild;
            while (childIndex != -1)
            {
                var state = TickNode(ref nodes, childIndex, userContext, actionExecutor, blackboardExecutor, traceCallback);
                if (state == BTState.Success) return BTState.Success;
                if (state == BTState.Running) return BTState.Running;
                childIndex = nodes[childIndex].NextSibling;
            }
            return BTState.Failure;
        }

        private static BTState ExecuteSequence(
            ref Unity.Entities.BlobArray<BTNode> nodes,
            BTNode node,
            object userContext,
            ActionExecutorDelegate actionExecutor,
            BlackboardExecutorDelegate blackboardExecutor,
            System.Action<int, BTState> traceCallback)
        {
            int childIndex = node.FirstChild;
            while (childIndex != -1)
            {
                var state = TickNode(ref nodes, childIndex, userContext, actionExecutor, blackboardExecutor, traceCallback);
                if (state == BTState.Failure) return BTState.Failure;
                if (state == BTState.Running) return BTState.Running;
                childIndex = nodes[childIndex].NextSibling;
            }
            return BTState.Success;
        }

        private static BTState ExecuteParallel(
            ref Unity.Entities.BlobArray<BTNode> nodes,
            BTNode node,
            object userContext,
            ActionExecutorDelegate actionExecutor,
            BlackboardExecutorDelegate blackboardExecutor,
            System.Action<int, BTState> traceCallback)
        {
            bool anyRunning = false;
            int childIndex = node.FirstChild;
            while (childIndex != -1)
            {
                var state = TickNode(ref nodes, childIndex, userContext, actionExecutor, blackboardExecutor, traceCallback);
                if (state == BTState.Failure) return BTState.Failure;
                if (state == BTState.Running) anyRunning = true;
                childIndex = nodes[childIndex].NextSibling;
            }
            return anyRunning ? BTState.Running : BTState.Success;
        }

        private static BTState ExecuteInvert(
            ref Unity.Entities.BlobArray<BTNode> nodes,
            BTNode node,
            object userContext,
            ActionExecutorDelegate actionExecutor,
            BlackboardExecutorDelegate blackboardExecutor,
            System.Action<int, BTState> traceCallback)
        {
            int childIndex = node.FirstChild;
            if (childIndex == -1) return BTState.Failure;

            var result = TickNode(ref nodes, childIndex, userContext, actionExecutor, blackboardExecutor, traceCallback);
            if (result == BTState.Success) return BTState.Failure;
            if (result == BTState.Failure) return BTState.Success;
            return result;
        }

        private static BTState ExecuteSucceeder(
            ref Unity.Entities.BlobArray<BTNode> nodes,
            BTNode node,
            object userContext,
            ActionExecutorDelegate actionExecutor,
            BlackboardExecutorDelegate blackboardExecutor,
            System.Action<int, BTState> traceCallback)
        {
            int childIndex = node.FirstChild;
            if (childIndex == -1) return BTState.Success;

            var result = TickNode(ref nodes, childIndex, userContext, actionExecutor, blackboardExecutor, traceCallback);
            return result == BTState.Running ? BTState.Running : BTState.Success;
        }

        private static BTState ExecuteRepeater(
            ref Unity.Entities.BlobArray<BTNode> nodes,
            BTNode node,
            int nodeIndex,
            object userContext,
            ActionExecutorDelegate actionExecutor,
            BlackboardExecutorDelegate blackboardExecutor,
            System.Action<int, BTState> traceCallback)
        {
            // ⚠️ 注意：这是简化实现，不支持跨帧状态保持
            // 实际使用时，建议在具体System中实现Repeater逻辑
            // 参考 SimpleBTExecutionSystem.ExecuteRepeater 获取完整实现
            
            int childIndex = node.FirstChild;
            if (childIndex == -1) return BTState.Success;

            // 简化版本：只执行一次子节点
            // 如果子节点成功，返回Running以便下一帧继续
            // 如果子节点失败，Repeater失败
            var state = TickNode(ref nodes, childIndex, userContext, actionExecutor, blackboardExecutor, traceCallback);
            
            if (state == BTState.Failure) return BTState.Failure;
            if (state == BTState.Running) return BTState.Running;
            
            // 子节点成功，Repeater保持Running（需要用户自行管理计数和终止条件）
            return BTState.Running;
        }

        private static BTState ExecuteInterrupt(
            ref Unity.Entities.BlobArray<BTNode> nodes,
            BTNode node,
            object userContext,
            ActionExecutorDelegate actionExecutor,
            BlackboardExecutorDelegate blackboardExecutor,
            System.Action<int, BTState> traceCallback)
        {
            // ⚠️ 注意：这是简化实现，无法正确读取黑板值判断中断
            // 实际使用时，建议在具体System中实现Interrupt逻辑
            // 参考 SimpleBTExecutionSystem.ExecuteInterrupt 获取完整实现
            
            int childIndex = node.FirstChild;
            if (childIndex == -1) return BTState.Success;

            // 简化版本：直接执行子节点，不检查中断条件
            // 实际应用需要读取黑板值(node.ParamI0)来判断是否中断
            var state = TickNode(ref nodes, childIndex, userContext, actionExecutor, blackboardExecutor, traceCallback);
            
            return state;
        }
    }
}
