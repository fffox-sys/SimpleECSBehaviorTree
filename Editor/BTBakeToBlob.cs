#if UNITY_EDITOR 
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace SECS.AI.BT.Editor
{
    public static class BTBakeToBlob
    {
        /// <summary>
        /// 检测行为树图中是否存在循环引用
        /// </summary>
        /// <returns>如果存在循环返回true</returns>
        private static bool DetectCycle(
            SECS.AI.BT.Editor.Graph.BTGraphData graph, 
            Dictionary<string, int> id2index, 
            out string cycleInfo)
        {
            cycleInfo = "";
            if (graph.nodes.Count == 0) return false;
            
            
            var adjacency = new Dictionary<int, List<int>>();
            for (int i = 0; i < graph.nodes.Count; i++)
                adjacency[i] = new List<int>();
            
            foreach (var edge in graph.edges)
            {
                if (id2index.TryGetValue(edge.outNodeId, out int parent) && 
                    id2index.TryGetValue(edge.inNodeId, out int child))
                {
                    adjacency[parent].Add(child);
                }
            }
            
            // DFS检测循环
            var visitState = new int[graph.nodes.Count];
            var path = new List<int>();
            
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                if (visitState[i] == 0)
                {
                    if (DFSCheckCycle(i, adjacency, visitState, path, graph, out cycleInfo))
                        return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// DFS递归检测循环
        /// </summary>
        private static bool DFSCheckCycle(
            int nodeIndex, 
            Dictionary<int, List<int>> adjacency,
            int[] visitState, 
            List<int> path,
            SECS.AI.BT.Editor.Graph.BTGraphData graph,
            out string cycleInfo)
        {
            cycleInfo = "";
            
          
            visitState[nodeIndex] = 1;
            path.Add(nodeIndex);
            
          
            if (adjacency.TryGetValue(nodeIndex, out var children))
            {
                foreach (var child in children)
                {
                    if (visitState[child] == 1)
                    {
                        
                        int cycleStartIndex = path.IndexOf(child);
                        var cycleNodes = new List<string>();
                        
                        for (int i = cycleStartIndex; i < path.Count; i++)
                        {
                            int idx = path[i];
                            string nodeName = idx < graph.nodes.Count ? 
                                $"{graph.nodes[idx].type}[{idx}]" : $"Node[{idx}]";
                            cycleNodes.Add(nodeName);
                        }
                        
                        
                        cycleNodes.Add(cycleNodes[0]);
                        
                        cycleInfo = $"循环路径: {string.Join(" → ", cycleNodes)}";
                        return true;
                    }
                    else if (visitState[child] == 0)
                    {
                       
                        if (DFSCheckCycle(child, adjacency, visitState, path, graph, out cycleInfo))
                            return true;
                    }
                }
            }
            
            // 回溯：标记为已完成
            visitState[nodeIndex] = 2;
            path.RemoveAt(path.Count - 1);
            
            return false;
        }

    

        [MenuItem("AI/Bake Selected To Blob")] private static void MenuBake() => BakeSelectedAsset();

        public static void BakeSelectedAsset()
        {
            var asset = Selection.activeObject as SECS.AI.BT.BehaviorTreeAsset;
            if (asset == null) { EditorUtility.DisplayDialog("BT Bake", "请选择一个 BehaviorTreeAsset", "OK"); return; }
            Bake(asset);
        }

        public static void Bake(SECS.AI.BT.BehaviorTreeAsset asset)
        {
            var json = asset.GetSerializedGraph();
            var graph = string.IsNullOrEmpty(json) ? new SECS.AI.BT.Editor.Graph.BTGraphData() : JsonUtility.FromJson<SECS.AI.BT.Editor.Graph.BTGraphData>(json);
            
            // 构建id到索引的映射（提前构建用于循环检测）
            var id2index = new Dictionary<string, int>();
            for (int i = 0; i < graph.nodes.Count; i++) 
                id2index[graph.nodes[i].id] = i;
            
            // 检测循环引用
            if (DetectCycle(graph, id2index, out string cycleInfo))
            {
                EditorUtility.DisplayDialog(
                    "行为树 Bake 失败", 
                    $"检测到循环引用！\n\n{cycleInfo}\n\n请修复节点连接后重试。", 
                    "确定");
                Debug.LogError($"[BTBake] 循环检测失败: {cycleInfo}");
                return;
            }
            
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<SECS.AI.BT.BTTreeBlob>();
            var arr = builder.Allocate(ref root.Nodes, math.max(1, graph.nodes.Count));
            if (graph.nodes.Count == 0)
            {
                arr[0] = new SECS.AI.BT.BTNode { Kind = SECS.AI.BT.BTNodeKind.Selector, FirstChild = -1, NextSibling = -1 };
            }
            else
            {
                for (int i = 0; i < graph.nodes.Count; i++)
                {
                    var gn = graph.nodes[i]; var type = gn.type;
                    SECS.AI.BT.BTNodeKind nodeKind;
                    int pI0 = 0, pI1 = 0, pI2 = 0;
                    float pF0 = 0f, pF1 = 0f, pF2 = 0f, pF3 = 0f;

                    if (type == "Action")
                    {
                        nodeKind = SECS.AI.BT.BTNodeKind.Action;
                       SECS.AI.BT.Editor.Graph.GenericActionData gad;
                        try { gad = string.IsNullOrEmpty(gn.jsonParams) ? new SECS.AI.BT.Editor.Graph.GenericActionData() : JsonUtility.FromJson<SECS.AI.BT.Editor.Graph.GenericActionData>(gn.jsonParams); }
                        catch { gad = new SECS.AI.BT.Editor.Graph.GenericActionData(); }

                        
                        var mapper = BTActionRegistry.GetBakeMapper(gad.task);
                        if (mapper != null)
                        {
                            var ctx = new BTActionDefinition.BakeContext();
                            ctx.Kind = SECS.AI.BT.BTNodeKind.Action;
                            ctx.ParamI0 = 0; 

                            mapper(gad, ctx);

                            nodeKind = ctx.Kind;
                            pI0 = ctx.ParamI0;
                            pI1 = ctx.ParamI1;
                            pI2 = ctx.ParamI2;
                            pF0 = ctx.ParamF0;
                            pF1 = ctx.ParamF1;
                            pF2 = ctx.ParamF2;
                            pF3 = ctx.ParamF3;
                        }
                        else
                        {
                            // 未注册的Action，默认Idle
                            BTEditorLog.Warning($"Action '{gad.task}' 未注册，默认使用 Idle");
                            nodeKind = SECS.AI.BT.BTNodeKind.Action;
                            pI0 = (int)0;
                            pI1 = 0;
                            pI2 = 0;
                            pF0 = 0f;
                            pF1 = 0f;
                            pF2 = 0f;
                            pF3 = 0f;
                        }
                    }
                    else
                    {
                        nodeKind = type switch
                        {
                            "Selector" => SECS.AI.BT.BTNodeKind.Selector,
                            "Sequence" => SECS.AI.BT.BTNodeKind.Sequence,
                            "Parallel" => SECS.AI.BT.BTNodeKind.Parallel,
                            "Invert" => SECS.AI.BT.BTNodeKind.Invert,
                            "Succeeder" => SECS.AI.BT.BTNodeKind.Succeeder,
                            "Repeater" => SECS.AI.BT.BTNodeKind.Repeater,
                            "Interrupt" => SECS.AI.BT.BTNodeKind.Interrupt,
                            _ => SECS.AI.BT.BTNodeKind.Selector
                        };
                        if (nodeKind == SECS.AI.BT.BTNodeKind.Repeater)
                        {
                            var rp = string.IsNullOrEmpty(gn.jsonParams) ? new SECS.AI.BT.Editor.Graph.RepeaterBakeParams { count = -1 } : JsonUtility.FromJson<SECS.AI.BT.Editor.Graph.RepeaterBakeParams>(gn.jsonParams);
                            pI0 = rp.count;
                        }
                        else if (nodeKind == SECS.AI.BT.BTNodeKind.Interrupt)
                        {
                            var ip = string.IsNullOrEmpty(gn.jsonParams) ? new SECS.AI.BT.Editor.Graph.InterruptBakeParams { key = "" } : JsonUtility.FromJson<SECS.AI.BT.Editor.Graph.InterruptBakeParams>(gn.jsonParams);
                            pI0 = SECS.AI.BT.StableHashUtility.HashBlackboardKey(ip.key);
                        }
                    }
                    arr[i] = new SECS.AI.BT.BTNode
                    {
                        Kind = nodeKind,
                        FirstChild = -1,
                        NextSibling = -1,
                        ParamI0 = pI0,
                        ParamI1 = pI1,
                        ParamI2 = pI2,
                        ParamF0 = pF0,
                        ParamF1 = pF1,
                        ParamF2 = pF2,
                        ParamF3 = pF3
                    };
                }
                var tmpByParent = new Dictionary<int, List<(int child, int outPortIndex, int order)>>();
                foreach (var e in graph.edges)
                {
                    if (!id2index.TryGetValue(e.outNodeId, out var p)) continue;
                    if (!id2index.TryGetValue(e.inNodeId, out var c)) continue;
                    if (!tmpByParent.TryGetValue(p, out var list)) { list = new List<(int child, int outPortIndex, int order)>(); tmpByParent[p] = list; }
                    int order = 0;
                    if (c >= 0 && c < graph.nodes.Count) order = graph.nodes[c].order;
                    list.Add((c, e.outPortIndex, order));
                }
                foreach (var kv in tmpByParent)
                {
                    var parent = kv.Key; var pairs = kv.Value;
                    pairs.Sort((a, b) =>
                    {
                        int cmp = a.order.CompareTo(b.order);
                        if (cmp != 0) return cmp;
                        return a.outPortIndex.CompareTo(b.outPortIndex);
                    });
                    if (pairs.Count == 0) continue;
                    var parentNode = arr[parent]; parentNode.FirstChild = pairs[0].child; arr[parent] = parentNode;
                    for (int j = 0; j < pairs.Count - 1; j++)
                    { var ci = pairs[j].child; var ni = pairs[j + 1].child; var cn = arr[ci]; cn.NextSibling = ni; arr[ci] = cn; }
                }
            }
            var blob = builder.CreateBlobAssetReference<SECS.AI.BT.BTTreeBlob>(Allocator.Persistent); builder.Dispose();
            byte[] bytes = null; if (blob.IsCreated) { unsafe { var ptr = blob.GetUnsafePtr(); var size = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<SECS.AI.BT.BTTreeBlob>() + blob.Value.Nodes.Length * Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<SECS.AI.BT.BTNode>(); bytes = new byte[size]; fixed (byte* dst = bytes) { System.Buffer.MemoryCopy(ptr, dst, size, size); } } }
            var path = AssetDatabase.GetAssetPath(asset); var dir = System.IO.Path.GetDirectoryName(path); var name = System.IO.Path.GetFileNameWithoutExtension(path); var bakedPath = System.IO.Path.Combine(dir, name + ".btblob.asset");
            BehaviorTreeBlobAsset bakedAsset = asset.baked; if (bakedAsset == null) { bakedAsset = ScriptableObject.CreateInstance<BehaviorTreeBlobAsset>(); AssetDatabase.CreateAsset(bakedAsset, bakedPath); AssetDatabase.ImportAsset(bakedPath); asset.baked = bakedAsset; EditorUtility.SetDirty(asset); }
            if (bytes != null) { bakedAsset.SetBytes(bytes, 1); var map = new BehaviorTreeBlobAsset.EditorNodeIndexMapEntry[graph.nodes.Count]; for (int i = 0; i < graph.nodes.Count; i++) map[i] = new BehaviorTreeBlobAsset.EditorNodeIndexMapEntry { id = graph.nodes[i].id, index = i }; bakedAsset.SetNodeIndexMap(map); EditorUtility.SetDirty(bakedAsset); Debug.Log($"[BTBake] Wrote NodeIndexMap length={map.Length} for asset={asset.name}"); }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); Debug.Log($"[BTBake] 完成 Bake (generic actions) nodeCount={(blob.IsCreated ? blob.Value.Nodes.Length : 0)}");
        }

      
    }
}
#endif