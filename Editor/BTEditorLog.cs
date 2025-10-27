#if UNITY_EDITOR
using UnityEngine;

namespace SECS.AI.BT.Editor
{
    /// <summary>
    /// 行为树编辑器统一日志系统
    /// </summary>
    public static class BTEditorLog
    {
        /// <summary>
        /// 记录信息日志
        /// </summary>
        public static void Info(string message, Object context = null)
        {
            if (!BTEditorConstants.EnableVerboseLogging)
                return;
            
            Log(BTLogLevel.Info, message, context);
        }
        
        /// <summary>
        /// 记录警告日志
        /// </summary>
        public static void Warning(string message, Object context = null)
        {
            Log(BTLogLevel.Warning, message, context);
        }
        
        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void Error(string message, Object context = null)
        {
            Log(BTLogLevel.Error, message, context);
        }
        
        /// <summary>
        /// 记录高亮系统日志
        /// </summary>
        public static void Highlight(string message)
        {
            if (!BTEditorConstants.EnableVerboseLogging)
                return;
            
            Debug.Log($"{BTEditorConstants.LOG_PREFIX}[Highlight] {message}");
        }
        
        /// <summary>
        /// 记录烘焙系统日志
        /// </summary>
        public static void Bake(string message)
        {
            Debug.Log($"{BTEditorConstants.LOG_PREFIX}[Bake] {message}");
        }
        
        /// <summary>
        /// 记录图形视图日志
        /// </summary>
        public static void GraphView(string message)
        {
            if (!BTEditorConstants.EnableVerboseLogging)
                return;
            
            Debug.Log($"{BTEditorConstants.LOG_PREFIX}[GraphView] {message}");
        }
        
        /// <summary>
        /// 记录追踪系统日志
        /// </summary>
        public static void Trace(string message)
        {
            if (!BTEditorConstants.EnableVerboseLogging)
                return;
            
            Debug.Log($"{BTEditorConstants.LOG_PREFIX}[Trace] {message}");
        }
        
        /// <summary>
        /// 记录验证系统日志
        /// </summary>
        public static void Validation(string message, bool isError = false)
        {
            if (isError)
                Debug.LogError($"{BTEditorConstants.LOG_PREFIX}[Validation] {message}");
            else
                Debug.LogWarning($"{BTEditorConstants.LOG_PREFIX}[Validation] {message}");
        }
        
        /// <summary>
        /// 核心日志方法
        /// </summary>
        private static void Log(BTLogLevel level, string message, Object context)
        {
            string formattedMessage = $"{BTEditorConstants.LOG_PREFIX}[{level}] {message}";
            
            switch (level)
            {
                case BTLogLevel.Info:
                    if (context != null)
                        Debug.Log(formattedMessage, context);
                    else
                        Debug.Log(formattedMessage);
                    break;
                    
                case BTLogLevel.Warning:
                    if (context != null)
                        Debug.LogWarning(formattedMessage, context);
                    else
                        Debug.LogWarning(formattedMessage);
                    break;
                    
                case BTLogLevel.Error:
                    if (context != null)
                        Debug.LogError(formattedMessage, context);
                    else
                        Debug.LogError(formattedMessage);
                    break;
            }
        }
        
        /// <summary>
        /// 格式化节点信息用于日志
        /// </summary>
        public static string FormatNode(Graph.BTNodeMetadata node)
        {
            if (node == null)
                return "<null>";
            
            string displayName = string.IsNullOrEmpty(node.DisplayName) ? node.Type : node.DisplayName;
            return $"Node[{node.Id}]({displayName})";
        }
        
        /// <summary>
        /// 启用详细日志
        /// </summary>
        public static void EnableVerbose()
        {
            BTEditorConstants.EnableVerboseLogging = true;
            Debug.Log($"{BTEditorConstants.LOG_PREFIX} 详细日志已启用");
        }
        
        /// <summary>
        /// 禁用详细日志
        /// </summary>
        public static void DisableVerbose()
        {
            BTEditorConstants.EnableVerboseLogging = false;
            Debug.Log($"{BTEditorConstants.LOG_PREFIX} 详细日志已禁用");
        }
    }
}
#endif
