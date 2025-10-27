#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace SECS.AI.BT.Editor
{
    /// <summary>
    /// 行为树 Action 定义构建器
    /// </summary>
    public class BTActionDefinition
    {
        private string _name;
        private int _actionKind;
        private readonly List<Graph.BTActionParamDescriptor> _parameters = new();
        private Action<SECS.AI.BT.Editor.Graph.GenericActionData, BakeContext> _bakeMapper;
        
       
        public string Name => _name;
        
      
        public int ActionKind => _actionKind;
        
    
        public IReadOnlyList<Graph.BTActionParamDescriptor> Parameters => _parameters;
        
    
        public Action<SECS.AI.BT.Editor.Graph.GenericActionData, BakeContext> BakeMapper => _bakeMapper;
        
        private BTActionDefinition(string name, int actionKind)
        {
            _name = name;
            _actionKind = actionKind;
        }
        
        /// <summary>
        /// 创建新的 Action 定义
        /// </summary>
        public static BTActionDefinition Create(string name, int actionKind)
        {
            return new BTActionDefinition(name, actionKind);
        }
        
      
        
      
        public BTActionDefinition Float(string paramName, string defaultValue, string tooltip, string label = null)
        {
            _parameters.Add(new Graph.BTActionParamDescriptor
            {
                name = paramName,
                label = label ?? paramName,
                type = Graph.BTActionParamType.Float,
                defaultValue = defaultValue,
                tooltip = tooltip
            });
            return this;
        }
        
   
        public BTActionDefinition Int(string paramName, string defaultValue, string tooltip, string label = null)
        {
            _parameters.Add(new Graph.BTActionParamDescriptor
            {
                name = paramName,
                label = label ?? paramName,
                type = Graph.BTActionParamType.Int,
                defaultValue = defaultValue,
                tooltip = tooltip
            });
            return this;
        }
        
     
        public BTActionDefinition Bool(string paramName, string defaultValue, string tooltip, string label = null)
        {
            _parameters.Add(new Graph.BTActionParamDescriptor
            {
                name = paramName,
                label = label ?? paramName,
                type = Graph.BTActionParamType.Bool,
                defaultValue = defaultValue,
                tooltip = tooltip
            });
            return this;
        }
        
      
        public BTActionDefinition String(string paramName, string defaultValue, string tooltip, string label = null)
        {
            _parameters.Add(new Graph.BTActionParamDescriptor
            {
                name = paramName,
                label = label ?? paramName,
                type = Graph.BTActionParamType.String,
                defaultValue = defaultValue,
                tooltip = tooltip
            });
            return this;
        }
        
     
        public BTActionDefinition Enum(string paramName, string defaultValue, string tooltip, string label = null)
        {
            _parameters.Add(new Graph.BTActionParamDescriptor
            {
                name = paramName,
                label = label ?? paramName,
                type = Graph.BTActionParamType.Enum,
                defaultValue = defaultValue,
                tooltip = tooltip
            });
            return this;
        }
        
    
        public BTActionDefinition AbilityId(string paramName, string defaultValue, string tooltip, string label = null)
        {
            _parameters.Add(new Graph.BTActionParamDescriptor
            {
                name = paramName,
                label = label ?? paramName,
                type = Graph.BTActionParamType.AbilityId,
                defaultValue = defaultValue,
                tooltip = tooltip
            });
            return this;
        }
        
     
        
               public BTActionDefinition Bake(Action<SECS.AI.BT.Editor.Graph.GenericActionData, BakeContext> mapper)
        {
            _bakeMapper = mapper;
            return this;
        }
        
      
        public BTActionDefinition Register()
        {
            BTActionRegistry.RegisterDefinition(this);
            return this;
        }
        
      
        public class BakeContext
        {
            public SECS.AI.BT.BTNodeKind Kind;
            public int ParamI0;
            public int ParamI1;
            public int ParamI2;
            public float ParamF0;
            public float ParamF1;
            public float ParamF2;    // 扩展参数2 (用于 float3 等)
            public float ParamF3;    // 扩展参数3 (用于速度等)
            
            public BakeContext()
            {
                Kind = SECS.AI.BT.BTNodeKind.Action;
                ParamI0 = 0;
                ParamI1 = 0;
                ParamI2 = 0;
                ParamF0 = 0f;
                ParamF1 = 0f;
                ParamF2 = 0f;
                ParamF3 = 0f;
            }
        }
    }
    
    /// <summary>
    /// 行为树 Action 全局注册表
    /// </summary>
    public static class BTActionRegistry
    {
        private static readonly Dictionary<string, BTActionDefinition> s_Definitions = new();
        private static bool s_Initialized = false;
        
        /// <summary>
        /// 初始化所有内置 Action
        /// </summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            if (s_Initialized) return;
            s_Initialized = true;
            
            s_Definitions.Clear();
            
            // 在这里注册所有内置 Action
            RegisterBuiltInActions();
        }
        
        /// <summary>
        /// 注册 Action 定义
        /// </summary>
        internal static void RegisterDefinition(BTActionDefinition definition)
        {
            if (!s_Initialized) Initialize();
            
            if (s_Definitions.ContainsKey(definition.Name))
            {
                BTEditorLog.Warning($"Action '{definition.Name}' 已存在，将被覆盖");
            }
            
            s_Definitions[definition.Name] = definition;
            BTEditorLog.Info($"注册 Action: {definition.Name}");
        }
        
        /// <summary>
        /// 获取 Action 定义
        /// </summary>
        public static BTActionDefinition GetDefinition(string name)
        {
            if (!s_Initialized) Initialize();
            return s_Definitions.TryGetValue(name, out var def) ? def : null;
        }
        
        /// <summary>
        /// 获取所有已注册的 Action 名称
        /// </summary>
        public static IEnumerable<string> GetAllActionNames()
        {
            if (!s_Initialized) Initialize();
            return s_Definitions.Keys;
        }
        
        /// <summary>
        /// 获取所有已注册的 Action 定义
        /// </summary>
        public static IEnumerable<BTActionDefinition> GetAllDefinitions()
        {
            if (!s_Initialized) Initialize();
            return s_Definitions.Values;
        }
        
        /// <summary>
        /// 获取 Action 的参数定义
        /// </summary>
        public static IReadOnlyList<Graph.BTActionParamDescriptor> GetParameters(string actionName)
        {
            var def = GetDefinition(actionName);
            return def?.Parameters ?? Array.Empty<Graph.BTActionParamDescriptor>();
        }
        
        /// <summary>
        /// 获取 Action 的烘焙映射器
        /// </summary>
        public static Action<SECS.AI.BT.Editor.Graph.GenericActionData, BTActionDefinition.BakeContext> GetBakeMapper(string actionName)
        {
            var def = GetDefinition(actionName);
            return def?.BakeMapper;
        }
        
        /// <summary>
        /// 注册所有内置 Action
        /// </summary>
        private static void RegisterBuiltInActions()
        {
            
            // ==================== 工具类 ====================
          
            
            BTActionDefinition.Create("Wait", BTActionKindHash.Wait)
                .Float("seconds", "0.5", "等待时长（秒）", "Wait Duration")
                .Bake((data, ctx) => {
                    ctx.ParamF0 = BTBakeHelper.ParseF(data.Get("seconds", "0.5"));
                })
                .Register();
            
            // ==================== 动画相关 ====================
            
            BTActionDefinition.Create("AnimatorSetState", BTActionKindHash.AnimatorSetState)
                .String("state", "Idle", "Animator 状态名", "State Name")
                .Float("fade", "0.1", "淡入时长（秒）", "Fade Duration")
                .Bake((data, ctx) => {
                    var stateName = data.Get("state", "Idle");
                    ctx.ParamI1 = SECS.AI.BT.StableHashUtility.HashAnimatorParameter(stateName);
                    ctx.ParamF0 = BTBakeHelper.ParseF(data.Get("fade", "0.1"));
                })
                .Register();
            
            BTActionDefinition.Create("AnimatorSetFloat", BTActionKindHash.AnimatorSetFloat)
                .String("param", "Speed", "Animator 参数名", "Parameter")
                .Float("value", "1.0", "参数值", "Value")
                .Bake((data, ctx) => {
                    var paramName = data.Get("param", "Speed");
                    ctx.ParamI1 = SECS.AI.BT.StableHashUtility.HashAnimatorParameter(paramName);
                    ctx.ParamF0 = BTBakeHelper.ParseF(data.Get("value", "1.0"));
                })
                .Register();
            
            BTActionDefinition.Create("AnimatorSetInt", BTActionKindHash.AnimatorSetInt)
                .String("param", "Phase", "Animator 参数名", "Parameter")
                .Int("value", "0", "参数值", "Value")
                .Bake((data, ctx) => {
                    var paramName = data.Get("param", "Phase");
                    ctx.ParamI1 = SECS.AI.BT.StableHashUtility.HashAnimatorParameter(paramName);
                    ctx.ParamF0 = BTBakeHelper.ParseF(data.Get("value", "0"));
                })
                .Register();
            
            BTActionDefinition.Create("AnimatorSetBool", BTActionKindHash.AnimatorSetBool)
                .String("param", "IsRunning", "Animator 参数名", "Parameter")
                .Bool("value", "true", "参数值", "Value")
                .Bake((data, ctx) => {
                    var paramName = data.Get("param", "IsRunning");
                    ctx.ParamI1 = SECS.AI.BT.StableHashUtility.HashAnimatorParameter(paramName);
                    ctx.ParamF0 = BTBakeHelper.ParseF(data.Get("value", "1"));
                })
                .Register();
            
            BTActionDefinition.Create("AnimatorSetTrigger", BTActionKindHash.AnimatorSetTrigger)
                .String("param", "Fire", "Animator 参数名", "Parameter")
                .Bake((data, ctx) => {
                    var paramName = data.Get("param", "Fire");
                    ctx.ParamI1 = SECS.AI.BT.StableHashUtility.HashAnimatorParameter(paramName);
                })
                .Register();
            
            BTActionDefinition.Create("WaitAnimEvent", BTActionKindHash.WaitAnimEvent)
                .String("event", "OnRecover", "等待的动画事件名", "Event Name")
                .Bake((data, ctx) => {
                    var eventName = data.Get("event", "OnRecover");
                    ctx.ParamI1 = SECS.AI.BT.StableHashUtility.HashAnimatorParameter(eventName);
                })
                .Register();
            
           
            

            
            // ==================== 黑板操作 ====================

            BTActionDefinition.Create("SetBlackboard", BTActionKindHash.SetBlackboard)  
                .String("key", "", "黑板键", "Key")
                .Float("value", "0", "值", "Value")
                .Bake((data, ctx) => {
                    ctx.Kind = SECS.AI.BT.BTNodeKind.SetBlackboard;
                    ctx.ParamI0 = SECS.AI.BT.StableHashUtility.HashBlackboardKey(data.Get("key", ""));
                    ctx.ParamF0 = BTBakeHelper.ParseF(data.Get("value", "0"));
                })
                .Register();

            BTActionDefinition.Create("ClearBlackboard", BTActionKindHash.ClearBlackboard)  
                .String("key", "", "黑板键（为空则清除全部）", "Key")
                .Bake((data, ctx) => {
                    ctx.Kind = SECS.AI.BT.BTNodeKind.ClearBlackboard;
                    ctx.ParamI0 = SECS.AI.BT.StableHashUtility.HashBlackboardKey(data.Get("key", ""));
                })
                .Register();
            
           
        }
    }
    
    public static class BTBakeHelper
    {
        public static float ParseF(string s)
        {
            return float.TryParse(s, out var f) ? f : 0f;
        }
        
        public static int ParseI(string s)
        {
            return int.TryParse(s, out var i) ? i : 0;
        }
        
        public static bool ParseB(string s)
        {
            return s == "1" || s == "true" || s == "True";
        }
    }
}
#endif
