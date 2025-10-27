using Unity.Entities;

namespace SECS.AI.BT
{
    /// <summary>
    /// 行为树Action执行器接口
    /// </summary>
    /// <typeparam name="TContext">自定义的Action上下文类型</typeparam>
    public interface IBTActionExecutor<TContext> where TContext : struct
    {
        BTState Execute(in TContext context);
    }

    /// <summary>
    /// 通用Action上下文
    /// </summary>
    public struct BTActionContextBase
    {
       
        public int NodeIndex;
        public Entity Self;
        public float DeltaTime;

        // 行为树参数 
        public int ParamI0;
        public int ParamI1;
        public int ParamI2;
        public float ParamF0;
        public float ParamF1;

        // ECB访问 
        public EntityCommandBuffer.ParallelWriter Ecb;
        public int SortKey;
    }
}
