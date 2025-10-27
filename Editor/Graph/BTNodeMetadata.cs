#if UNITY_EDITOR
using System;

namespace SECS.AI.BT.Editor.Graph
{
    /// <summary>
    /// 强类型节点元数据
    /// </summary>
    [Serializable]
    public sealed class BTNodeMetadata
    {
      
        public string Id { get; set; }
        
     
        public string Type { get; set; }
        
      
        public string JsonParams { get; set; }
        
      
        public string DisplayName { get; set; }
        
    
        public string Description { get; set; }
        
            public int Order { get; set; }

        public BTNodeMetadata()
        {
            Id = string.Empty;
            Type = string.Empty;
            JsonParams = "{}";
            DisplayName = string.Empty;
            Description = string.Empty;
            Order = 0;
        }

        public BTNodeMetadata(string id, string type)
        {
            Id = id ?? string.Empty;
            Type = type ?? string.Empty;
            JsonParams = "{}";
            DisplayName = string.Empty;
            Description = string.Empty;
            Order = 0;
        }

       

        public override string ToString()
        {
            return $"BTNode[{Id}] Type={Type}, Display={DisplayName}, Order={Order}";
        }
    }
}
#endif
