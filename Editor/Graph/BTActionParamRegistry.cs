#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SECS.AI.BT.Editor.Graph
{
    
    [Serializable]
    public class BTActionParamDescriptor
    {
        public string name;          
                public string label;        
        public BTActionParamType type;
        public string defaultValue;  
        public string tooltip;
    }

    public enum BTActionParamType { Float, Int, Bool, String, Enum, AbilityId }

   
    public static class BTActionParamRegistry
    {
        private static readonly Dictionary<string, List<BTActionParamDescriptor>> s_ActionParams = new();
        private static bool s_Inited;

        private static void EnsureInit()
        {
            if (s_Inited) return;
            s_Inited = true;
            AutoRegisterFromDefinitions();
        }
        
        /// <summary>
        /// 旧版兼容
        /// </summary>
        private static void AutoRegisterFromDefinitions()
        {
            var allDefinitions = SECS.AI.BT.Editor.BTActionRegistry.GetAllDefinitions();
            
            foreach (var def in allDefinitions)
            {
                
                if (s_ActionParams.ContainsKey(def.Name))
                {
                    s_ActionParams[def.Name].Clear();
                }
                
                foreach (var param in def.Parameters)
                {
                    Register(def.Name, param);
                }
            }
        }

        public static void Register(string actionName, BTActionParamDescriptor desc)
        {
            if (!s_ActionParams.TryGetValue(actionName, out var list)) s_ActionParams[actionName] = list = new List<BTActionParamDescriptor>();
            list.Add(desc);
        }

        public static IReadOnlyList<BTActionParamDescriptor> Get(string actionName)
        {
            EnsureInit();
            return s_ActionParams.TryGetValue(actionName, out var list) ? list : Array.Empty<BTActionParamDescriptor>();
        }

        public static IEnumerable<string> GetAllRegisteredActionNames()
        {
            EnsureInit();
            return s_ActionParams.Keys;
        }
    }
}
#endif