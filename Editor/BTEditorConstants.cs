#if UNITY_EDITOR
using UnityEngine;

namespace SECS.AI.BT.Editor
{
    /// <summary>
    /// 行为树编辑器常量定义
    /// </summary>
    public static class BTEditorConstants
    {
        // ==================== 时间相关 ====================
        
        /// <summary>自动刷新间隔（秒）</summary>
        public const float AUTO_REFRESH_INTERVAL = 0.2f;
        
        /// <summary>自动刷新频率（Hz）</summary>
        public const float AUTO_REFRESH_RATE_HZ = 5f;
        
        // ==================== 节点索引相关 ====================
        
        /// <summary>无效的节点索引标记</summary>
        public const int INVALID_NODE_INDEX = -1;
        
        /// <summary>未执行状态标记</summary>
        public const byte STATE_UNEXECUTED = 255;
        
        // ==================== 节点尺寸 ====================
        
        /// <summary>默认节点宽度</summary>
        public const float DEFAULT_NODE_WIDTH = 180f;
        
        /// <summary>默认节点高度</summary>
        public const float DEFAULT_NODE_HEIGHT = 120f;
        
        // ==================== 颜色方案 ====================
        
        /// <summary>节点类型基础颜色</summary>
        public static class NodeTypeColors
        {
            /// <summary>控制流节点颜色（蓝色系）</summary>
            public static readonly Color ControlFlow = new Color(0.3f, 0.5f, 0.8f, 1.0f);
            
            /// <summary>装饰器节点颜色（紫色系）</summary>
            public static readonly Color Decorator = new Color(0.7f, 0.4f, 0.8f, 1.0f);
            
            /// <summary>叶子节点颜色（绿色系）</summary>
            public static readonly Color Leaf = new Color(0.4f, 0.7f, 0.4f, 1.0f);
            
            /// <summary>条件节点颜色（橙色系）</summary>
            public static readonly Color Condition = new Color(0.8f, 0.6f, 0.2f, 1.0f);
            
            /// <summary>默认节点颜色（灰色系）</summary>
            public static readonly Color Default = new Color(0.6f, 0.6f, 0.6f, 1.0f);
        }
        
        /// <summary>节点高亮颜色</summary>
        public static class HighlightColors
        {
            /// <summary>成功状态颜色（绿色）</summary>
            public static readonly Color Success = new Color(0.2f, 0.8f, 0.2f, 0.40f);
            
            /// <summary>失败状态颜色（红色）</summary>
            public static readonly Color Failure = new Color(0.9f, 0.25f, 0.25f, 0.45f);
            
            /// <summary>运行中状态颜色（橙黄色）</summary>
            public static readonly Color Running = new Color(0.95f, 0.75f, 0.2f, 0.45f);
            
            /// <summary>未执行状态颜色（透明）</summary>
            public static readonly Color Unexecuted = new Color(0f, 0f, 0f, 0.02f);
            
            /// <summary>背景淡化颜色（用于聚焦时）</summary>
            public static readonly Color Dimmed = new Color(0f, 0f, 0f, 0.02f);
            
            // 聚焦模式变体：突出显示
            /// <summary>成功状态聚焦高亮</summary>
            public static readonly Color SuccessFocused = new Color(0.2f, 0.8f, 0.2f, 0.75f);  // 0.50 → 0.75
            /// <summary>失败状态聚焦高亮</summary>
            public static readonly Color FailureFocused = new Color(0.9f, 0.25f, 0.25f, 0.80f); // 0.55 → 0.80
            /// <summary>运行中状态聚焦高亮</summary>
            public static readonly Color RunningFocused = new Color(0.95f, 0.75f, 0.2f, 0.85f); // 0.65 → 0.85
            
            // 聚焦模式变体：淡化显示
            /// <summary>成功状态淡化</summary>
            public static readonly Color SuccessDimmed = new Color(0.2f, 0.8f, 0.2f, 0.08f);
            /// <summary>失败状态淡化</summary>
            public static readonly Color FailureDimmed = new Color(0.9f, 0.25f, 0.25f, 0.10f);
            /// <summary>运行中状态淡化</summary>
            public static readonly Color RunningDimmed = new Color(0.95f, 0.75f, 0.2f, 0.12f);
        }
        
        /// <summary>顺序徽章颜色</summary>
        public static class OrderBadgeColors
        {
            public static readonly Color Background = new Color(0f, 0f, 0f, 0.6f);
            public static readonly Color Text = Color.white;
        }
        
        // ==================== 字体和样式 ====================
        
        /// <summary>顺序徽章字体大小</summary>
        public const int ORDER_BADGE_FONT_SIZE = 10;
        
        /// <summary>顺序徽章最小宽度</summary>
        public const float ORDER_BADGE_MIN_WIDTH = 16f;
        
        /// <summary>顺序徽章内边距</summary>
        public const float ORDER_BADGE_PADDING = 2f;
        
        /// <summary>顺序徽章左边距</summary>
        public const float ORDER_BADGE_MARGIN_LEFT = 4f;
        
        /// <summary>顺序徽章名称</summary>
        public const string ORDER_BADGE_NAME = "bt-order-badge";
        
        // ==================== 缩放和视图 ====================
        
        /// <summary>GraphView 默认最小缩放</summary>
        public const float MIN_ZOOM_SCALE = 0.25f;
        
        /// <summary>GraphView 默认最大缩放</summary>
        public const float MAX_ZOOM_SCALE = 2.0f;
        
        // ==================== 日志配置 ====================
        
        /// <summary>是否启用详细日志</summary>
        public static bool EnableVerboseLogging = false;
        
        /// <summary>日志前缀</summary>
        public const string LOG_PREFIX = "[BTEditor]";
        
        // ==================== 节点类型相关 ====================
        
        /// <summary>默认节点类型</summary>
        public const string DEFAULT_NODE_TYPE = "Selector";
        
        /// <summary>动作节点类型名称</summary>
        public const string ACTION_NODE_TYPE = "Action";
        
        // ==================== 端口配置 ====================
        
        /// <summary>输入端口名称</summary>
        public const string INPUT_PORT_NAME = "In";
        
        /// <summary>输出端口名称</summary>
        public const string OUTPUT_PORT_NAME = "Out";
        
        // ==================== 拖拽数据键 ====================
        
        /// <summary>节点类型拖拽数据键</summary>
        public const string DRAG_NODE_TYPE_KEY = "BT_NodeType";
        
        // ==================== 缓存配置 ====================
        
        /// <summary>节点缓存初始容量</summary>
        public const int NODE_CACHE_INITIAL_CAPACITY = 64;
        
        /// <summary>参数映射初始容量</summary>
        public const int PARAM_MAP_INITIAL_CAPACITY = 32;
        
        // ==================== Ghost 节点 ====================
        
        /// <summary>Ghost 节点透明度</summary>
        public const float GHOST_NODE_OPACITY = 0.4f;
        
        // ==================== 验证配置 ====================
        
        /// <summary>是否在保存时自动验证</summary>
        public static bool AutoValidateOnSave = true;
        
        /// <summary>是否在 Bake 前自动验证</summary>
        public static bool AutoValidateBeforeBake = true;
        
        // ==================== 节点类型分类方法 ====================
        
        /// <summary>
        /// 根据节点类型获取对应的颜色
        /// </summary>
        public static Color GetNodeTypeColor(string nodeType)
        {
            return nodeType switch
            {
                // 控制流节点（蓝色系）
                "Selector" or "Sequence" or "Parallel" => NodeTypeColors.ControlFlow,
                
                // 装饰器节点（紫色系）
                "Invert" or "Succeeder" or "Repeater" or "Interrupt" or "Cooldown" => NodeTypeColors.Decorator,
                
                // 条件节点（橙色系）
                "Condition" => NodeTypeColors.Condition,
                
                // 叶子节点（绿色系）
                "Action" or "Wait" => NodeTypeColors.Leaf,
                
                // 默认颜色
                _ => NodeTypeColors.Default
            };
        }
        
        /// <summary>
        /// 判断节点是否为控制流节点
        /// </summary>
        public static bool IsControlFlowNode(string nodeType)
        {
            return nodeType is "Selector" or "Sequence" or "Parallel";
        }
        
        /// <summary>
        /// 判断节点是否为装饰器节点
        /// </summary>
          public static bool IsDecoratorNode(string nodeType)
        {
            return nodeType is "Invert" or "Succeeder" or "Repeater" or "Interrupt" or "Cooldown";
        }
        
        /// <summary>
        /// 判断节点是否为叶子节点
        /// </summary>
            public static bool IsLeafNode(string nodeType)
        {
            return nodeType is "Action" or "Wait" or "Condition";
        }
    }
    
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum BTLogLevel
    {
        Info,
        Warning,
        Error
    }
}
#endif
