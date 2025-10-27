using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SECS.AI.BT
{
    // 运行时核心
    public enum BTState : byte { Success, Failure, Running }

    // 结构/控制类节点 + 通用 Action/Blackboard 操作
    public enum BTNodeKind : byte { Selector, Sequence, Parallel, Invert, Succeeder, Repeater, Interrupt, Action, SetBlackboard, ClearBlackboard }

    /// <summary>
    /// 行为树节点运行时结构
    /// </summary>
    public struct BTNode
    {
        public BTNodeKind Kind;
        public int FirstChild;   // -1 表示无子节点
        public int NextSibling;  // -1 表示无兄弟节点

        /// <summary>
        /// 参数整数0 - 用途根据节点类型而定:
        /// - Action: ActionKindHash (行为种类哈希)
        /// - Repeater: 重复次数 (-1 = 无限)
        /// - Interrupt: 黑板键哈希 (中断条件)
        /// - SetBlackboard/ClearBlackboard: 黑板键哈希
        /// </summary>
        public int ParamI0;
        public int ParamI1;      // 扩展整数参数1
        public int ParamI2;      // 扩展整数参数2
        public float ParamF0;    // 浮点参数0
        public float ParamF1;    // 浮点参数1
        public float ParamF2;    // 浮点参数2 
        public float ParamF3;    // 浮点参数3 
    }

    /// <summary>
    /// 行为树 Blob 数据结构
    /// </summary>
    public struct BTTreeBlob
    {
        public BlobArray<BTNode> Nodes;
    }

    /// <summary>
    /// 行为树引用组件
    /// </summary>
    public struct BTTreeRef : IComponentData
    {
        public BlobAssetReference<BTTreeBlob> Value;
    }
}
