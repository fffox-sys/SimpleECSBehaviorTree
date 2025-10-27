#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SECS.AI.BT.Editor.Graph
{
    [Serializable]
    public class BTGraphData
    {
        public List<NodeData> nodes = new();
        public List<EdgeData> edges = new();

        [Serializable]
        public class NodeData
        {
            public string id;
            public string type; 
            public Vector2 position;
            public string jsonParams; // 各节点的参数
            public string displayName; // 自定义显示名称
            public string description; // 自定义描述
            public int order; // 子序号
        }

        [Serializable]
        public class EdgeData
        {
            public string outNodeId;
            public int outPortIndex; 
            public string inNodeId;
            public int inPortIndex; 
        }
    }
}
#endif
