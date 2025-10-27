#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using SECS.AI.BT.Editor.Graph;

namespace SECS.AI.BT.Editor
{
    public static class BTGraphHighlighter
    {
        public struct HighlightOptions
        {
            public bool ShowOrder;
            public bool FocusRunningBranch; 
        }
        
     
        private static bool TryGetNodeId(object userData, out string id)
        {
            id = null;
            if (userData is BTNodeMetadata metadata)
            {
                id = metadata.Id;
                return !string.IsNullOrEmpty(id);
            }
            return false;
        }
        public static void Highlight(BTGraphView view, BehaviorTreeAsset asset, IReadOnlyList<int> executedBlobIndices)
        {
            if (view == null || asset == null || asset.baked == null)
            {
                Debug.Log("[BTGraphHighlighter] Skip: view/asset/baked null");
                return;
            }
            var map = asset.baked.NodeIndexMap;
            if (map == null || map.Length == 0)
            {
                Debug.Log($"[BTGraphHighlighter] Skip: NodeIndexMap empty for asset={asset.name}. 请先执行 Bake 以生成映射。");
                return;
            }

            int graphNodeCount = 0; foreach (var _ in view.nodes) graphNodeCount++;
            Debug.Log($"[BTGraphHighlighter] Highlight call. executedCount={executedBlobIndices?.Count} nodesInGraph={graphNodeCount} mapLen={map.Length}");

          
            var idByIndex = new Dictionary<int, string>(map.Length);
            foreach (var e in map) idByIndex[e.index] = e.id;

            int matched = 0;
           
            foreach (var ge in view.nodes)
            {
                var n = ge as UnityEditor.Experimental.GraphView.Node;
                if (n == null) continue;
                if (!TryGetNodeId(n.userData, out var id)) continue;

                
                bool executed = false;
                for (int i = 0; i < executedBlobIndices.Count; i++)
                {
                    var bi = executedBlobIndices[i];
                    if (idByIndex.TryGetValue(bi, out var nid) && nid == id)
                    { executed = true; break; }
                }
                
                if (executed) matched++;
              
                var nodeType = GetNodeType(n.userData);
                var highlightColor = executed ? BTEditorConstants.HighlightColors.Success : Color.clear;
                ApplyHighlightColor(n, nodeType, highlightColor);
            }
            Debug.Log($"[BTGraphHighlighter] Highlight done. matched={matched}");
        }

        public struct HighlightStats
        {
            public int SuccessCount;
            public int FailureCount;
            public int RunningCount;
            public int TotalNodes;
        }

       
        public static HighlightStats Highlight(BTGraphView view, BehaviorTreeAsset asset, IReadOnlyList<(int nodeIndex, byte state)> executed, HighlightOptions options = default)
        {
            var stats = new HighlightStats();
            if (view == null || asset == null || asset.baked == null)
            {
                Debug.Log("[BTGraphHighlighter] (states) Skip: view/asset/baked null");
                return stats;
            }
            var map = asset.baked.NodeIndexMap;
            if (map == null || map.Length == 0)
            {
                Debug.Log($"[BTGraphHighlighter] (states) Skip: NodeIndexMap empty for asset={asset.name}. 请先执行 Bake 生成映射。");
                return stats;
            }

            int graphNodeCount = 0; foreach (var _ in view.nodes) graphNodeCount++;
            Debug.Log($"[BTGraphHighlighter] (states) Highlight call. executedCount={executed?.Count} nodesInGraph={graphNodeCount} mapLen={map.Length}");

            var idByIndex = new Dictionary<int, string>(map.Length);
            foreach (var e in map) idByIndex[e.index] = e.id;

            
            var stateByIndex = new Dictionary<int, byte>(executed.Count);
            for (int i = 0; i < executed.Count; i++)
            {
                var (idx, st) = executed[i];
                stateByIndex[idx] = st; 
            }

            int matchedSuccess = 0, matchedFailure = 0, matchedRunning = 0;
           
            int lastRunningIndex = -1; 
            if (options.FocusRunningBranch)
            {
                for (int i = executed.Count - 1; i >= 0; i--)
                {
                    if (executed[i].state == 2) { lastRunningIndex = executed[i].nodeIndex; break; }
                }
            }

            HashSet<int> focusSet = null;
            if (lastRunningIndex >= 0)
            {
                focusSet = new System.Collections.Generic.HashSet<int>();
                focusSet.Add(lastRunningIndex); 
            }

            
            var orderByIndex = new Dictionary<int,int>();
            if (options.ShowOrder)
            {
                int order = 1;
                for (int i = 0; i < executed.Count; i++)
                {
                    var (ni, st) = executed[i];
                    if (!orderByIndex.ContainsKey(ni)) orderByIndex[ni] = order++;
                }
            }
            foreach (var ge in view.nodes)
            {
                var n = ge as UnityEditor.Experimental.GraphView.Node;
                if (n == null) continue;
                if (!TryGetNodeId(n.userData, out var id)) continue;

                byte st = BTEditorConstants.STATE_UNEXECUTED;
                
                foreach (var kv in idByIndex)
                {
                    if (kv.Value == id && stateByIndex.TryGetValue(kv.Key, out var s))
                    { st = s; break; }
                }
                Color c = Color.clear;
                if (st == BTEditorConstants.STATE_UNEXECUTED)
                {
                    
                    if (options.FocusRunningBranch && focusSet != null)
                        c = BTEditorConstants.HighlightColors.Unexecuted;
                }
                else
                {
                    switch (st)
                    {
                        case 0: c = focusSet==null ? BTEditorConstants.HighlightColors.Success : 
                                   (focusSet.Contains(blobIndexForId(idByIndex, id)) ? BTEditorConstants.HighlightColors.SuccessFocused : BTEditorConstants.HighlightColors.SuccessDimmed); 
                                matchedSuccess++; break;
                        case 1: c = focusSet==null ? BTEditorConstants.HighlightColors.Failure : 
                                   (focusSet.Contains(blobIndexForId(idByIndex, id)) ? BTEditorConstants.HighlightColors.FailureFocused : BTEditorConstants.HighlightColors.FailureDimmed); 
                                matchedFailure++; break;
                        case 2: c = focusSet==null ? BTEditorConstants.HighlightColors.Running : 
                                   (focusSet.Contains(blobIndexForId(idByIndex, id)) ? BTEditorConstants.HighlightColors.RunningFocused : BTEditorConstants.HighlightColors.RunningDimmed); 
                                matchedRunning++; break;
                    }
                }
                
              
                var nodeType = GetNodeType(n.userData);
                ApplyHighlightColor(n, nodeType, c);

               
                if (options.ShowOrder && orderByIndex.Count > 0)
                {
                    int blobIdx = blobIndexForId(idByIndex, id);
                    if (blobIdx >=0 && orderByIndex.TryGetValue(blobIdx, out var ord))
                    {
                        var badgeName = "bt-order-badge";
                        var existing = n.titleContainer.Q<Label>(badgeName);
                        if (existing == null)
                        {
                            var lbl = new Label(ord.ToString());
                            lbl.name = badgeName;
                            lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                            lbl.style.fontSize = 14;  
                            lbl.style.backgroundColor = new Color(0,0,0,0.75f);  
                            lbl.style.color = Color.white;
                            lbl.style.marginLeft = 4;
                            lbl.style.paddingLeft = 4;  
                            lbl.style.paddingRight = 4; 
                            lbl.style.minWidth = 20;  
                            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                            n.titleContainer.Add(lbl);
                        }
                        else
                        {
                            existing.text = ord.ToString();
                        }
                    }
                }
            }
            
           
            foreach (var _ in view.nodes) stats.TotalNodes++;
            
            stats.SuccessCount = matchedSuccess;
            stats.FailureCount = matchedFailure;
            stats.RunningCount = matchedRunning;
            
            Debug.Log($"[BTGraphHighlighter] (states) Highlight done. success={matchedSuccess} failure={matchedFailure} running={matchedRunning}");
            return stats;
        }

        private static int blobIndexForId(Dictionary<int,string> map, string id)
        {
            foreach (var kv in map) if (kv.Value == id) return kv.Key; return -1;
        }
        
        /// <summary>
        /// 获取节点类型
        /// </summary>
        private static string GetNodeType(object userData)
        {
            if (userData is BTNodeMetadata metadata)
            {
                return metadata.Type;
            }
            return "Unknown";
        }
        
        /// <summary>
        /// 应用高亮颜色
        /// </summary>
        private static void ApplyHighlightColor(UnityEditor.Experimental.GraphView.Node node, string nodeType, Color highlightColor)
        {
            if (highlightColor == Color.clear)
            {
               
                var typeColor = BTEditorConstants.GetNodeTypeColor(nodeType);
                node.titleContainer.style.backgroundColor = new StyleColor(typeColor);
                
                node.RemoveFromClassList("bt-node-success");
                node.RemoveFromClassList("bt-node-failure"); 
                node.RemoveFromClassList("bt-node-running");
            }
            else
            {
               
                var typeColor = BTEditorConstants.GetNodeTypeColor(nodeType);
                var blendedColor = BlendColors(typeColor, highlightColor);
                node.titleContainer.style.backgroundColor = new StyleColor(blendedColor);
                
             
                node.RemoveFromClassList("bt-node-success");
                node.RemoveFromClassList("bt-node-failure");
                node.RemoveFromClassList("bt-node-running");
                
                if (IsSuccessColor(highlightColor))
                    node.AddToClassList("bt-node-success");
                else if (IsFailureColor(highlightColor))
                    node.AddToClassList("bt-node-failure");
                else if (IsRunningColor(highlightColor))
                    node.AddToClassList("bt-node-running");
            }
        }
        
        /// <summary>
        /// 混合两种颜色
        /// </summary>
        private static Color BlendColors(Color baseColor, Color highlightColor)
        {
           
            var r = Mathf.Clamp01(baseColor.r + highlightColor.r * highlightColor.a);
            var g = Mathf.Clamp01(baseColor.g + highlightColor.g * highlightColor.a);
            var b = Mathf.Clamp01(baseColor.b + highlightColor.b * highlightColor.a);
            return new Color(r, g, b, baseColor.a);
        }
        
        /// <summary>
        /// 判断是否为成功状态颜色
        /// </summary>
        private static bool IsSuccessColor(Color color)
        {
            return ColorMatches(color, BTEditorConstants.HighlightColors.Success) ||
                   ColorMatches(color, BTEditorConstants.HighlightColors.SuccessFocused) ||
                   ColorMatches(color, BTEditorConstants.HighlightColors.SuccessDimmed);
        }
        
        /// <summary>
        /// 判断是否为失败状态颜色
        /// </summary>
        private static bool IsFailureColor(Color color)
        {
            return ColorMatches(color, BTEditorConstants.HighlightColors.Failure) ||
                   ColorMatches(color, BTEditorConstants.HighlightColors.FailureFocused) ||
                   ColorMatches(color, BTEditorConstants.HighlightColors.FailureDimmed);
        }
        
        /// <summary>
        /// 判断是否为运行状态颜色
        /// </summary>
        private static bool IsRunningColor(Color color)
        {
            return ColorMatches(color, BTEditorConstants.HighlightColors.Running) ||
                   ColorMatches(color, BTEditorConstants.HighlightColors.RunningFocused) ||
                   ColorMatches(color, BTEditorConstants.HighlightColors.RunningDimmed);
        }
        
        /// <summary>
        /// 颜色匹配判断
        /// </summary>
        private static bool ColorMatches(Color a, Color b)
        {
            const float tolerance = 0.01f;
            return Mathf.Abs(a.r - b.r) < tolerance &&
                   Mathf.Abs(a.g - b.g) < tolerance &&
                   Mathf.Abs(a.b - b.b) < tolerance &&
                   Mathf.Abs(a.a - b.a) < tolerance;
        }

       
        public static void ClearHighlight(BTGraphView view)
        {
            if (view == null) return;
            
            foreach (var ge in view.nodes)
            {
                var n = ge as UnityEditor.Experimental.GraphView.Node;
                if (n == null) continue;
                
              
                var nodeType = GetNodeType(n.userData);
                ApplyHighlightColor(n, nodeType, Color.clear);
                
             
                var badge = n.titleContainer.Q<Label>("bt-order-badge");
                if (badge != null)
                {
                    n.titleContainer.Remove(badge);
                }
                
                // 兼容
                var oldBadge = n.titleContainer.Q<Label>("orderBadge");
                if (oldBadge != null)
                {
                    n.titleContainer.Remove(oldBadge);
                }
            }
            
            Debug.Log("[BTGraphHighlighter] Cleared all highlights");
        }
    }
}
#endif
