#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace SECS.AI.BT.Editor.Graph
{
    [Serializable] public struct RepeaterBakeParams { public int count; }
    [Serializable] public struct InterruptBakeParams { public string key; }
    /// <summary>
    /// 通用 Action 数据容器
    /// </summary>
    [Serializable]
    public class GenericActionData
    {
        public string task = "Move";
        public List<KV> parameters = new();

        [Serializable]
        public class KV
        {
            public string k;
            public string v;
        }

        /// <summary>
        /// 获取参数值
        /// </summary>
        public string Get(string key, string defaultValue)
        {
            var kv = parameters.Find(p => p.k == key);
            return kv != null ? kv.v : defaultValue;
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        public void Set(string key, string value)
        {
            var kv = parameters.Find(p => p.k == key);
            if (kv == null)
            {
                kv = new KV { k = key, v = value };
                parameters.Add(kv);
            }
            else
            {
                kv.v = value;
            }
        }

        public Dictionary<string, string> ToDict()
        {
            var dict = new Dictionary<string, string>();
            foreach (var kv in parameters)
            {
                dict[kv.k] = kv.v;
            }
            return dict;
        }

    }
}
#endif
