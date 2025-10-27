# Simple ECS Behavior Tree

Unity DOTS 原生的轻量级行为树系统
![编辑器界面](https://github.com/fffox-sys/SimpleECSBehaviorTree/blob/main/Other/info.png)
## 核心特性

-  **ECS原生**: 基于Unity Entities，支持Burst编译
-  **可视化编辑**: GraphView图形编辑器，拖拽式操作
-  **完整节点**: Selector/Sequence/Parallel/Invert/Repeater等
-  **黑板系统**: 多类型数据共享（float/bool/int/float3/Entity）
-  **调试工具**: 实时追踪、节点高亮、运行时测试器
-  **高性能**: Burst编译、Job调度

## 快速开始

### 方式1: 简化系统 (推荐新手) 

**无需理解多线程、Job、Burst，只需继承类即可扩展Action**

#### 步骤1: 创建行为树
```
右键 > Create > AI > Behavior Tree Asset
```

#### 步骤2: 可视化编辑
- 双击资源打开编辑器
- 右键创建Action节点，命名为 "MoveToTarget"
- 配置参数
- 点击 "Bake" 烘焙

#### 步骤3: 实现自定义系统
```csharp
using SECS.AI.BT;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class MyGameAI : SimpleBehaviorTreeSystem
{
    protected override void RegisterCustomActions()
    {
        // 注册移动Action - 主线程执行，可直接调试
        Provider.Register("MoveToTarget", (ctx, blackboard) =>
        {
            if (!ctx.HasComponent<LocalTransform>()) 
                return BTNodeResult.Failure;
            
            // 读取节点参数
            float3 targetPos = new float3(ctx.Node.ParamF0, ctx.Node.ParamF1, ctx.Node.ParamF2);
            float speed = ctx.Node.ParamF3;
            
            // 获取和修改组件（无需理解ECB/Lookup）
            var transform = ctx.GetComponent<LocalTransform>();
            float3 direction = math.normalize(targetPos - transform.Position);
            
            // 判断是否到达
            if (math.distance(transform.Position, targetPos) < 0.1f)
            {
                transform.Position = targetPos;
                ctx.SetComponent(transform);
                return BTNodeResult.Success;
            }
            
            // 继续移动
            transform.Position += direction * speed * ctx.DeltaTime;
            ctx.SetComponent(transform);
            return BTNodeResult.Running;
        });
        
        // 更多Action...
        Provider.Register("Attack", (ctx, bb) => 
        {
            // 你的攻击逻辑
            return BTNodeResult.Success;
        });
    }
}
```

**优点**:
-  零多线程知识要求
-  可直接断点调试
-  通用组件访问：`GetComponent<T>()`
-  完整示例：`Samples~/MySimpleBehaviorTreeSystem.cs`

**适用场景**: NPC AI、剧情脚本

---

### 方式2: 高性能系统 (适合优化)

**使用 IJobChunk + Burst 获得高性能（**

**优点**:
-  Burst 编译 + IJobChunk 支持
-  性能提升
-  适合大量实体
-  完整示例：`Samples~/HighPerformanceBehaviorTreeSystem.cs`

**适用场景**: 大规模 AI

**注意事项**:
-  必须手动更新 `context.CurrentNode`（参考示例中的 `ExecuteTreeWithNodeUpdate` 方法）
-  Action 内不能使用托管类型（Burst 限制）
-  使用 `ComponentLookup` 而非 `GetComponent<T>()`

---

## 性能对比

| 系统类型 | 100实体 | 1000实体 | 开发难度 | 调试难度 |
|---------|---------|----------|---------|---------|
| Layer 3 (Simple) | 0.5ms | ~5ms | ⭐ | ⭐ |
| Layer 1 (High-Perf) | 0.03ms | ~0.3ms | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ |

*
---

## TODO
- 易用性完善 
- 子树支持
- 异步Action支持
- 更多示例场景（战斗、巡逻、对话）

## 许可证

MIT License
