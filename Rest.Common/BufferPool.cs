using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rest.Common
{
    public class BufferPool
    {
        private readonly List<byte[]> pool = new List<byte[]>();

        public BufferPool()
        { }

        /// <summary>
        /// Gets a buffer of at least the specified size.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public byte[] GetBuffer(int size)
        {
            // TODO: maybe use Monitor.TryEnter() and just create a new buffer if failing to acquire lock.
            if (size <= 0)
                return new byte[0];
            lock (pool)
            {
                int i = GetListIndex(size);
                if (i < pool.Count)
                {
                    return pool[i];
                }
                else
                {
                    var buffer = new byte[size];
                    pool.Add(buffer);
                    return buffer;
                }
            }
        }

        public void ReleaseBuffer(ref byte[] buffer)
        {
            if (buffer == null)
                return;
            lock (pool)
            {
                int i = GetListIndex(buffer.Length);
                pool.Insert(i, buffer);
            }
            buffer = null;
        }

        private int GetListIndex(int size)
        {
            // Buffers are stored in order from smallest to largest for O(log(n)) access of the best-suited buffer.
            if (pool.Count == 0)
                return 0;
            int i = (pool.Count - 1) / 2;
            int dist = pool.Count / 4;
            while (i > 0 && i < pool.Count && dist > 1)
            {
                var currentSize = pool[i].Length;
                if (currentSize == size)
                    return i;
                else if (currentSize < size)
                    i -= dist;
                else
                    i += dist;
                dist /= 2;
            }
            while (i < pool.Count && pool[i].Length < size)
                i++;
            return i;
        }
    }
}
