﻿
namespace ShapeEngine.Lib
{
    public static class ShapeID
    {
        private static IdCounter counter = new();
        public static uint InvalidId => IdCounter.InvalidId;
        public static uint NextID => counter.NextId;
    }

    public class IdCounter
    {
        public static readonly uint InvalidId = 0;
        private uint count = 10;
        public uint NextId => count++;
    }
}
