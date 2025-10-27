using UnityEngine;

namespace SECS.AI.BT
{
    
    public class BehaviorTreeBlobAsset : ScriptableObject
    {
        [HideInInspector]
        [SerializeField] private byte[] data;
        [SerializeField] private int version = 1; 

        public byte[] Data => data;
        public int Version => version;

#if UNITY_EDITOR
        [System.Serializable]
        public struct EditorNodeIndexMapEntry { public string id; public int index; }
        [SerializeField] private EditorNodeIndexMapEntry[] nodeIndexMap;
        public EditorNodeIndexMapEntry[] NodeIndexMap => nodeIndexMap;

        public void SetBytes(byte[] bytes, int ver = 1)
        {
            data = bytes;
            version = ver;
        }

        public void SetNodeIndexMap(EditorNodeIndexMapEntry[] map)
        {
            nodeIndexMap = map;
        }
#endif
    }
}