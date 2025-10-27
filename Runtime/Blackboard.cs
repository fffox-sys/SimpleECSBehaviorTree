//#define HAS_ENTITIES
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace SECS.AI.Core
{
    /// <summary>
    /// 行为树黑板主体组件，存储常用固定字段
    /// </summary>
    public struct BTBlackboard : IComponentData
    {
        public float3 LastKnownTargetPos;  // 最后已知目标位置
        public int Flags;                   // 通用标志位
    }

    /// <summary>
    /// 黑板键值对条目，用于动态存储
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct BTBlackboardEntry : IBufferElementData
    {
        public int KeyHash;    
                public float Value;     
        
        public BTBlackboardEntry(int keyHash, float value)
        {
            KeyHash = keyHash;
            Value = value;
        }
    }

    // 扩展：多类型黑板缓冲
    [InternalBufferCapacity(4)]
    public struct BTBBoolEntry : IBufferElementData
    {
        public int KeyHash; public byte Value;
    }
    [InternalBufferCapacity(4)]
    public struct BTBIntEntry : IBufferElementData
    {
        public int KeyHash; public int Value;
    }
    [InternalBufferCapacity(4)]
    public struct BTBFloat3Entry : IBufferElementData
    {
        public int KeyHash; public float3 Value;
    }
    [InternalBufferCapacity(2)]
    public struct BTBEntityEntry : IBufferElementData
    {
        public int KeyHash; public Entity Value;
    }

    /// <summary>
    /// 黑板操作辅助类
    /// </summary>
    public static class BTBlackboardHelper
    {
        /// <summary>
        /// 计算键名的稳定哈希值
        /// </summary>
        public static int HashKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;
            
            const uint FNV_OFFSET_BASIS = 2166136261;
            const uint FNV_PRIME = 16777619;
            
            uint hash = FNV_OFFSET_BASIS;
            for (int i = 0; i < key.Length; i++)
            {
                hash ^= key[i];
                hash *= FNV_PRIME;
            }
            
            return unchecked((int)hash);
        }

        /// <summary>
        /// 在缓冲区中查找键值
        /// </summary>
    public static bool TryGetValue(DynamicBuffer<BTBlackboardEntry> buffer, int keyHash, out float value)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].KeyHash == keyHash)
                {
                    value = buffer[i].Value;
                    return true;
                }
            }
            value = 0f;
            return false;
        }

        /// <summary>
        /// 在缓冲区中设置键值
        /// </summary>
        public static void SetValue(DynamicBuffer<BTBlackboardEntry> buffer, int keyHash, float value)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].KeyHash == keyHash)
                {
                    buffer[i] = new BTBlackboardEntry(keyHash, value);
                    return;
                }
            }
            buffer.Add(new BTBlackboardEntry(keyHash, value));
        }

        /// <summary>
        /// 从缓冲区中移除键
        /// </summary>
        public static bool RemoveKey(DynamicBuffer<BTBlackboardEntry> buffer, int keyHash)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].KeyHash == keyHash)
                {
                    buffer.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 清空所有动态条目
        /// </summary>
        public static void ClearAll(DynamicBuffer<BTBlackboardEntry> buffer)
        {
            buffer.Clear();
        }

        public static bool TryGetBool(DynamicBuffer<BTBBoolEntry> buf, int keyHash, out bool v)
        {
            for (int i = 0; i < buf.Length; i++) if (buf[i].KeyHash == keyHash) { v = buf[i].Value != 0; return true; }
            v = false; return false;
        }
        public static void SetBool(DynamicBuffer<BTBBoolEntry> buf, int keyHash, bool v)
        {
            for (int i = 0; i < buf.Length; i++) if (buf[i].KeyHash == keyHash) { buf[i] = new BTBBoolEntry{ KeyHash = keyHash, Value = (byte)(v?1:0) }; return; }
            buf.Add(new BTBBoolEntry{ KeyHash = keyHash, Value = (byte)(v?1:0) });
        }

        public static bool TryGetInt(DynamicBuffer<BTBIntEntry> buf, int keyHash, out int v)
        {
            for (int i = 0; i < buf.Length; i++) if (buf[i].KeyHash == keyHash) { v = buf[i].Value; return true; }
            v = 0; return false;
        }
        public static void SetInt(DynamicBuffer<BTBIntEntry> buf, int keyHash, int v)
        {
            for (int i = 0; i < buf.Length; i++) if (buf[i].KeyHash == keyHash) { buf[i] = new BTBIntEntry{ KeyHash = keyHash, Value = v }; return; }
            buf.Add(new BTBIntEntry{ KeyHash = keyHash, Value = v });
        }

        public static bool TryGetFloat3(DynamicBuffer<BTBFloat3Entry> buf, int keyHash, out float3 v)
        {
            for (int i = 0; i < buf.Length; i++) if (buf[i].KeyHash == keyHash) { v = buf[i].Value; return true; }
            v = default; return false;
        }
        public static void SetFloat3(DynamicBuffer<BTBFloat3Entry> buf, int keyHash, float3 v)
        {
            for (int i = 0; i < buf.Length; i++) if (buf[i].KeyHash == keyHash) { buf[i] = new BTBFloat3Entry{ KeyHash = keyHash, Value = v }; return; }
            buf.Add(new BTBFloat3Entry{ KeyHash = keyHash, Value = v });
        }

        public static bool TryGetEntity(DynamicBuffer<BTBEntityEntry> buf, int keyHash, out Entity v)
        {
            for (int i = 0; i < buf.Length; i++) if (buf[i].KeyHash == keyHash) { v = buf[i].Value; return true; }
            v = Entity.Null; return false;
        }
        public static void SetEntity(DynamicBuffer<BTBEntityEntry> buf, int keyHash, Entity v)
        {
            for (int i = 0; i < buf.Length; i++) if (buf[i].KeyHash == keyHash) { buf[i] = new BTBEntityEntry{ KeyHash = keyHash, Value = v }; return; }
            buf.Add(new BTBEntityEntry{ KeyHash = keyHash, Value = v });
        }
    }
}