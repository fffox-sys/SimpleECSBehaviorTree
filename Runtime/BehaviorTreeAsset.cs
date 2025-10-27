using UnityEngine;

namespace SECS.AI.BT
{
    
    public class BehaviorTreeAsset : ScriptableObject
    {
        public string treeName = "NewBehaviorTree";
        [TextArea]
        public string description;
        [SerializeField] private string serializedGraphJson;
       
        public BehaviorTreeBlobAsset baked;

        public void SetSerializedGraph(string json) => serializedGraphJson = json;
        public string GetSerializedGraph() => serializedGraphJson;
    }
}
