using Unity.Entities;
namespace SECS.AI.BT
{
    public struct BTNodeTraceEntry : IBufferElementData
    {
        public int NodeIndex; // 在 Blob 中的节点索引
        public byte State;    // 0=Success,1=Failure,2=Running
    }
}
