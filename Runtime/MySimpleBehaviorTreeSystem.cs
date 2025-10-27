using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using SECS.AI.BT;

namespace SECS.AI.BT.Samples
{
    /// <summary>
    /// 示例
    /// 
    public partial class MySimpleBehaviorTreeSystem : SimpleBehaviorTreeSystem
    {
        protected override void RegisterCustomActions()
        {
            // 1. MoveToTarget: 移动到目标位置
            Provider.Register("MoveToTarget", (ctx, blackboard) =>
            {
                // 读取参数
                float3 targetPos = new float3(ctx.Node.ParamF0, ctx.Node.ParamF1, ctx.Node.ParamF2);
                float speed = ctx.Node.ParamF3;
                
                // 获取当前变换
                if (!ctx.HasComponent<LocalTransform>())
                    return BTState.Failure;
                    
                var transform = ctx.GetComponent<LocalTransform>();
                
                // 计算移动
                float3 direction = targetPos - transform.Position;
                float distance = math.length(direction);
                
                if (distance < 0.1f)
                {
                    return BTState.Success; // 到达目标
                }
                
                // 移动
                float3 movement = math.normalize(direction) * speed * ctx.DeltaTime;
                if (math.length(movement) > distance)
                {
                    movement = direction; // 避免超过
                }
                
                transform.Position += movement;
                ctx.SetComponent(transform);
                
                return BTState.Running;
            });
            
            // 2. RotateToTarget: 旋转朝向目标
            Provider.Register("RotateToTarget", (ctx, blackboard) =>
            {
                float3 targetPos = new float3(ctx.Node.ParamF0, ctx.Node.ParamF1, ctx.Node.ParamF2);
                float rotationSpeed = ctx.Node.ParamF3;
                
                if (!ctx.HasComponent<LocalTransform>())
                    return BTState.Failure;
                    
                var transform = ctx.GetComponent<LocalTransform>();
                
                float3 direction = targetPos - transform.Position;
                direction.y = 0;
                
                if (math.lengthsq(direction) < 0.001f)
                {
                    return BTState.Success;
                }
                
                quaternion targetRotation = quaternion.LookRotationSafe(direction, math.up());
                float t = rotationSpeed * ctx.DeltaTime;
                transform.Rotation = math.slerp(transform.Rotation, targetRotation, t);
                
                ctx.SetComponent(transform);
                
                // 检查是否到达
                float angle = math.degrees(math.acos(math.dot(transform.Rotation, targetRotation)));
                return angle < 1f ? BTState.Success : BTState.Running;
            });
            
            // 3. CheckDistance: 检查距离
            Provider.Register("CheckDistance", (ctx, blackboard) =>
            {
                float3 targetPos = new float3(ctx.Node.ParamF0, ctx.Node.ParamF1, ctx.Node.ParamF2);
                float threshold = ctx.Node.ParamF3;
                
                if (!ctx.HasComponent<LocalTransform>())
                    return BTState.Failure;
                    
                var transform = ctx.GetComponent<LocalTransform>();
                float distance = math.distance(transform.Position, targetPos);
                
                return distance <= threshold ? BTState.Success : BTState.Failure;
            });
            
            // 4. Log: 打印日志
            Provider.Register("Log", (ctx, blackboard) =>
            {
                // ParamI0 存储字符串哈希
                UnityEngine.Debug.Log($"BT Log: Entity {ctx.Entity.Index}");
                return BTState.Success;
            });
            
            // 5. RandomSuccess: 随机成功/失败
            Provider.Register("RandomSuccess", (ctx, blackboard) =>
            {
                float probability = ctx.Node.ParamF0; // 0.0 ~ 1.0
                float random = new Unity.Mathematics.Random((uint)(ctx.Entity.Index + UnityEngine.Time.frameCount)).NextFloat();
                return random < probability ? BTState.Success : BTState.Failure;
            });
            
            // 6. WaitRandom: 随机等待
            Provider.Register("WaitRandom", (ctx, blackboard) =>
            {
                int bbKey = ctx.Node.ParamI0;
                float minDuration = ctx.Node.ParamF0;
                float maxDuration = ctx.Node.ParamF1;
                
                // 读取黑板：是否已初始化等待时间
                float targetDuration = ctx.GetBlackboardValue(bbKey, blackboard);
                if (targetDuration == 0f)
                {
                    // 首次执行，生成随机时长
                    var random = new Unity.Mathematics.Random((uint)(ctx.Entity.Index + UnityEngine.Time.frameCount));
                    targetDuration = random.NextFloat(minDuration, maxDuration);
                    ctx.SetBlackboardValue(bbKey, targetDuration, blackboard);
                }
                
                // 读取已等待时间
                int elapsedKey = bbKey + 1;
                float elapsed = ctx.GetBlackboardValue(elapsedKey, blackboard);
                elapsed += ctx.DeltaTime;
                
                if (elapsed >= targetDuration)
                {
                    // 清除黑板
                    for (int i = blackboard.Length - 1; i >= 0; i--)
                    {
                        if (blackboard[i].KeyHash == bbKey || blackboard[i].KeyHash == elapsedKey)
                        {
                            blackboard.RemoveAt(i);
                        }
                    }
                    return BTState.Success;
                }
                else
                {
                    ctx.SetBlackboardValue(elapsedKey, elapsed, blackboard);
                    return BTState.Running;
                }
            });
        }
    }
}
