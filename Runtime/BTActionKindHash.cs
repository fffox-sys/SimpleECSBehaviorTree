using Unity.Collections;

namespace SECS.AI.BT
{
    /// <summary>
    /// 行为树Action种类的Hash定义
    /// </summary>
    public static class BTActionKindHash
    {
        // 基础工具Action
        public static readonly int Wait = Hash("Wait");
        
        // 动画相关Action
        public static readonly int AnimatorSetState = Hash("AnimatorSetState");
        public static readonly int AnimatorSetFloat = Hash("AnimatorSetFloat");
        public static readonly int AnimatorSetInt = Hash("AnimatorSetInt");
        public static readonly int AnimatorSetBool = Hash("AnimatorSetBool");
        public static readonly int AnimatorSetTrigger = Hash("AnimatorSetTrigger");
        public static readonly int WaitAnimEvent = Hash("WaitAnimEvent");
        
        // 黑板操作Action
        public static readonly int SetBlackboard = Hash("SetBlackboard");
        public static readonly int ClearBlackboard = Hash("ClearBlackboard");

        /// <summary>
        /// 生成Action名称哈希值
        /// </summary>
        public static int Hash(string actionName)
        {
            return StableHashUtility.GetStableHashCode(actionName);
        }

        /// <summary>
        /// 生成FixedString的哈希值
        /// </summary>
        public static int Hash(in FixedString64Bytes actionName)
        {
            return StableHashUtility.GetStableHashCode(actionName);
        }
    }

  
}
