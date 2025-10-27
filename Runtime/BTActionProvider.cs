using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using SECS.AI.Core;

namespace SECS.AI.BT
{

    /// <summary>
    /// 行为树 Action 提供器
    /// 
    /// 示例:
    /// <code>
    /// var provider = new BTActionProvider();
    /// provider.Register("MoveToTarget", (ctx) => {
    ///     // 简单的 Action 逻辑，无需考虑多线程
    ///     var transform = ctx.GetComponent<LocalTransform>();
    ///     transform.Position += new float3(0, 0, 1) * ctx.DeltaTime;
    ///     ctx.SetComponent(transform);
    ///     return BTState.Success;
    /// });
    /// </code>
    /// </summary>
    public class BTActionProvider
    {
        /// <summary>
        /// Action 执行上下文 (用户友好的接口)
        /// </summary>
        public struct ActionContext
        {
            public Entity Entity;
            public float DeltaTime;
            public BTNode Node; // 当前节点（包含所有参数）

            internal EntityCommandBuffer.ParallelWriter Ecb;
            internal int SortKey;
            internal EntityManager EntityManager;

          
            public T GetComponent<T>() where T : unmanaged, IComponentData
            {
                return EntityManager.GetComponentData<T>(Entity);
            }

          
            public void SetComponent<T>(T component) where T : unmanaged, IComponentData
            {
                Ecb.SetComponent(SortKey, Entity, component);
            }

            public bool HasComponent<T>() where T : unmanaged, IComponentData
            {
                return EntityManager.HasComponent<T>(Entity);
            }

           
            public float GetBlackboardValue(int keyHash, DynamicBuffer<BTBlackboardEntry> blackboard)
            {
                for (int i = 0; i < blackboard.Length; i++)
                {
                    if (blackboard[i].KeyHash == keyHash)
                        return blackboard[i].Value;
                }
                return 0f;
            }

         
            public void SetBlackboardValue(int keyHash, float value, DynamicBuffer<BTBlackboardEntry> blackboard)
            {
                for (int i = 0; i < blackboard.Length; i++)
                {
                    if (blackboard[i].KeyHash == keyHash)
                    {
                        var entry = blackboard[i];
                        entry.Value = value;
                        blackboard[i] = entry;
                        return;
                    }
                }
                blackboard.Add(new BTBlackboardEntry { KeyHash = keyHash, Value = value });
            }
        }

        
        public delegate BTState ActionFunc(ActionContext ctx, DynamicBuffer<BTBlackboardEntry> blackboard);

        private readonly Dictionary<int, ActionFunc> _actions = new();

      
        public void Register(string actionName, ActionFunc action)
        {
            int hash = BTActionKindHash.Hash(actionName);
            _actions[hash] = action;
        }

               public void Register(int actionHash, ActionFunc action)
        {
            _actions[actionHash] = action;
        }

      
        public BTState Execute(int actionHash, ActionContext ctx, DynamicBuffer<BTBlackboardEntry> blackboard)
        {
            if (_actions.TryGetValue(actionHash, out var action))
            {
                return action(ctx, blackboard);
            }
            return BTState.Failure;
        }

       
        public bool HasAction(int actionHash)
        {
            return _actions.ContainsKey(actionHash);
        }

              public void Clear()
        {
            _actions.Clear();
        }
    }
}
