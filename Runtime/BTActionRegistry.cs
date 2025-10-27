using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using SECS.AI.Core;

namespace SECS.AI.BT
{
    /// <summary>
    /// Burst 兼容的 Action 函数指针委托
    /// </summary>
    public unsafe delegate BTState ActionExecutorDelegate(in BTNode node, ref ActionExecutionContext context);
    
    /// <summary>
    /// Action 执行上下文
    /// 包含执行 Action 所需的所有数据
    /// </summary>
    public struct ActionExecutionContext
    {
        public Entity CurrentEntity;
        public float DeltaTime;
        public int SortKey;
        
        // ComponentLookup 指针
        public unsafe void* TransformLookup;
        public unsafe void* Ecb;
        
        // 预留扩展字段
        public unsafe void* UserData0;
        public unsafe void* UserData1;
    }
    
    /// <summary>
    /// Burst 兼容的行为树 Action 注册表
    /// </summary>
    public struct BTActionRegistry : IDisposable
    {
        private NativeHashMap<int, FunctionPointer<ActionExecutorDelegate>> _executors;
        
        /// <summary>
        /// 创建注册表
        /// </summary>
        public BTActionRegistry(int initialCapacity, Allocator allocator)
        {
            _executors = new NativeHashMap<int, FunctionPointer<ActionExecutorDelegate>>(initialCapacity, allocator);
        }
        
        /// <summary>
        /// 注册 Action
        /// </summary>
        public void Register(int actionHash, FunctionPointer<ActionExecutorDelegate> functionPointer)
        {
            _executors[actionHash] = functionPointer;
        }
        
        /// <summary>
        /// 执行 Action
        /// </summary>
        public BTState Execute(int actionHash, in BTNode node, ref ActionExecutionContext context)
        {
            if (_executors.TryGetValue(actionHash, out var funcPtr))
            {
                return funcPtr.Invoke(in node, ref context);
            }
            return BTState.Failure;
        }
        
        /// <summary>
        /// 尝试获取 Action 函数指针
        /// </summary>
        public bool TryGet(int actionHash, out FunctionPointer<ActionExecutorDelegate> funcPtr)
        {
            return _executors.TryGetValue(actionHash, out funcPtr);
        }
        
        /// <summary>
        /// 移除 Action
        /// </summary>
        public bool Unregister(int actionHash)
        {
            return _executors.Remove(actionHash);
        }
        
        /// <summary>
        /// 清空所有注册
        /// </summary>
        public void Clear()
        {
            _executors.Clear();
        }
        
        /// <summary>
        /// 获取已注册的 Action 数量
        /// </summary>
        public int Count => _executors.Count;
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_executors.IsCreated)
                _executors.Dispose();
        }
        
        public bool IsCreated => _executors.IsCreated;
    }
    
    /// <summary>
    /// 旧版
    /// </summary>
    /// <typeparam name="TContext">Action上下文类型</typeparam>
    public class BTActionRegistryManaged<TContext> where TContext : struct
    {
        private readonly System.Collections.Generic.Dictionary<int, IBTActionExecutor<TContext>> _executors = new();

        public void Register(int actionHash, IBTActionExecutor<TContext> executor)
        {
            _executors[actionHash] = executor;
        }

        public bool TryGet(int actionHash, out IBTActionExecutor<TContext> executor)
        {
            return _executors.TryGetValue(actionHash, out executor);
        }

        public bool Unregister(int actionHash)
        {
            return _executors.Remove(actionHash);
        }

        public void Clear()
        {
            _executors.Clear();
        }

        public int Count => _executors.Count;
    }
    
    /// <summary>
    /// 通用行为树节点执行器注册表
    /// </summary>
    public static class BTNodeExecutorRegistry
    {
        public delegate BTState NodeExecutorDelegate(
            ref BlobArray<BTNode> nodes,
            int nodeIndex,
            in Entity self,
            DynamicBuffer<BTBlackboardEntry> bbBuffer,
            ref BTBlackboard bbComponent,
            float deltaTime,
            object contextBox); 

        private static readonly System.Collections.Generic.Dictionary<BTNodeKind, NodeExecutorDelegate> _executors = new();

        public static void Register(BTNodeKind kind, NodeExecutorDelegate executor)
        {
            _executors[kind] = executor;
        }

        public static bool TryGet(BTNodeKind kind, out NodeExecutorDelegate executor)
        {
            return _executors.TryGetValue(kind, out executor);
        }

        public static void RegisterBlackboardOperations()
        {
            // SetBlackboard
            Register(BTNodeKind.SetBlackboard, (ref BlobArray<BTNode> nodes, int idx, in Entity self,
                DynamicBuffer<BTBlackboardEntry> bbBuffer, ref BTBlackboard bbComp,
                float dt, object ctx) =>
            {
                var n = nodes[idx];
                if (bbBuffer.IsCreated)
                    BTBlackboardHelper.SetValue(bbBuffer, n.ParamI0, n.ParamF0);
                else
                    bbComp.Flags = (int)n.ParamF0;
                return BTState.Success;
            });

            // ClearBlackboard
            Register(BTNodeKind.ClearBlackboard, (ref BlobArray<BTNode> nodes, int idx, in Entity self,
                DynamicBuffer<BTBlackboardEntry> bbBuffer, ref BTBlackboard bbComp,
                float dt, object ctx) =>
            {
                var n = nodes[idx];
                if (bbBuffer.IsCreated)
                {
                    if (n.ParamI0 == 0)
                        BTBlackboardHelper.ClearAll(bbBuffer);
                    else
                        BTBlackboardHelper.RemoveKey(bbBuffer, n.ParamI0);
                }
                else
                {
                    bbComp.Flags = 0;
                }
                return BTState.Success;
            });
        }
    }
}
