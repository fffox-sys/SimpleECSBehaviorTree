using System.Runtime.CompilerServices;
using Unity.Collections;

namespace SECS.AI.BT
{
    
    /// <summary>
    /// 稳定哈希工具类
    /// 提供跨平台一致的字符串哈希算法
    /// </summary>
    public static class StableHashUtility
    {
      
        private const uint FNV_OFFSET_BASIS = 2166136261;
        private const uint FNV_PRIME = 16777619;
        
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetStableHashCode(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;
            
            uint hash = FNV_OFFSET_BASIS;
            
            for (int i = 0; i < str.Length; i++)
            {
                hash ^= str[i];
                hash *= FNV_PRIME;
            }
            
            // 转换为有符号整数
            return unchecked((int)hash);
        }
        
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetStableHashCode(in FixedString64Bytes str)
        {
            if (str.Length == 0)
                return 0;
            
            uint hash = FNV_OFFSET_BASIS;
            
            for (int i = 0; i < str.Length; i++)
            {
                hash ^= str[i];
                hash *= FNV_PRIME;
            }
            
            
            return unchecked((int)hash);
        }
        
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetStableHashCodeUnsigned(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;
            
            uint hash = FNV_OFFSET_BASIS;
            
            for (int i = 0; i < str.Length; i++)
            {
                hash ^= str[i];
                hash *= FNV_PRIME;
            }
            
            return hash;
        }
        
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetStableHashCodeUnsigned(in FixedString64Bytes str)
        {
            if (str.Length == 0)
                return 0;
            
            uint hash = FNV_OFFSET_BASIS;
            
            for (int i = 0; i < str.Length; i++)
            {
                hash ^= str[i];
                hash *= FNV_PRIME;
            }
            
            return hash;
        }
        
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetStableHashCode64(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;
            
            // FNV-1a 64-bit
            const ulong FNV64_OFFSET_BASIS = 14695981039346656037;
            const ulong FNV64_PRIME = 1099511628211;
            
            ulong hash = FNV64_OFFSET_BASIS;
            
            for (int i = 0; i < str.Length; i++)
            {
                hash ^= str[i];
                hash *= FNV64_PRIME;
            }
            
            return unchecked((long)hash);
        }
        
     
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetStableHashCode64(in FixedString64Bytes str)
        {
            if (str.Length == 0)
                return 0;
            
        
            const ulong FNV64_OFFSET_BASIS = 14695981039346656037;
            const ulong FNV64_PRIME = 1099511628211;
            
            ulong hash = FNV64_OFFSET_BASIS;
            
            for (int i = 0; i < str.Length; i++)
            {
                hash ^= str[i];
                hash *= FNV64_PRIME;
            }
            
            return unchecked((long)hash);
        }
        
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CombineHash(int hash1, int hash2)
        {
            unchecked
            {
               
                uint rol5 = ((uint)hash1 << 5) | ((uint)hash1 >> 27);
                return ((int)rol5 + hash1) ^ hash2;
            }
        }
        
       
        public static int CombineHash(params int[] hashes)
        {
            if (hashes == null || hashes.Length == 0)
                return 0;
            
            int result = hashes[0];
            for (int i = 1; i < hashes.Length; i++)
            {
                result = CombineHash(result, hashes[i]);
            }
            
            return result;
        }
        
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashBlackboardKey(string key)
        {
            
            return GetStableHashCode(key);
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashAnimatorParameter(string paramName)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return UnityEngine.Animator.StringToHash(paramName);
#else
            return GetStableHashCode(paramName);
#endif
        }
    }
}
