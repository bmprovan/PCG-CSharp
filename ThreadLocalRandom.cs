using System;
using System.Threading;
using static System.Int32;

namespace PCGRandom
{
    /**
        http://www.pcg-random.org/
        <summary> 
            Thread-Local random implementation of a PCG Random Number Generator. 
            Thread-Safety is gauranteed if all calls are via the .Current property
        </summary>
    */
    public class ThreadLocalRandom
    {
        private const uint HalfwayUint = uint.MaxValue / 2;
        private const uint IntMax = MaxValue + 1U;

        private static readonly ThreadLocal<ThreadLocalRandom> random_ =
            new ThreadLocal<ThreadLocalRandom>(() => new ThreadLocalRandom());
        
        private readonly ulong increment_;
        private ulong state_;

        public static ThreadLocalRandom Current => random_.Value;

        private ThreadLocalRandom()
        {
            var guidArray = Guid.NewGuid().ToByteArray();
            state_ = BitConverter.ToUInt64(guidArray, 0);
            increment_ = BitConverter.ToUInt64(guidArray, sizeof(ulong));
        }

        public int Next(int min, int max)
        {
            if (max <= min)
            {
                throw new ArgumentOutOfRangeException(
                    $"{nameof(min)} ({min}) must be greater than {nameof(max)} ({max}). Arguments reversed?");
            }

            uint range = unchecked((uint) (0L + max - min));
            return unchecked((int) (NextUint32(range) + min));
        }

        public float NextFloat()
        {
            return (NextUint32() >> 8) * 5.960465E-008F;
        }

        public float NextFloat(float max)
        {
            if (max <= 0)
            {
                throw new ArgumentOutOfRangeException($"Expected {max} to be positive.");
            }

            return BoundedFloat(max, NextFloat() * max);
        }

        public float NextFloat(float min, float max)
        {
            if (min >= max)
            {
                throw new ArgumentOutOfRangeException(
                    $"{nameof(min)} ({min}) must be greater than {nameof(max)} ({max}). Arguments reversed?");
            }
            return InternalNextFloat(min, max);
        }

        private float InternalNextFloat(float min, float max)
        {
            return BoundedFloat(max, NextFloat() * (max - min) + min);
        }

        private float BoundedFloat(float max, float value)
        {
            return (value < max)
                ? value
                : (BitConverter.ToSingle(
                    BitConverter.GetBytes(BitConverter.ToInt32(BitConverter.GetBytes(value), 0) - 1), 0));
        }

        public bool NextBool()
        {
            return NextUint32() < HalfwayUint;
        }

        public int Next(int max)
        {
            if (max <= 0)
            {
                throw new ArgumentOutOfRangeException($"Expected {max} to be positive");
            }

            return unchecked((int) NextUint32(unchecked((uint) max)));
        }

        public int Next()
        {
            return unchecked((int) NextUint32(IntMax));
        }

        private uint NextUint32(uint max)
        {
            var threshold = unchecked((uint)((0x100000000UL - max) % max));
            while (true)
            {
                uint randomValue = NextUint32();
                if (randomValue >= threshold)
                {
                    return (randomValue % max);
                }
            }
        }

        private uint NextUint32()
        {
            var oldState = state_;
            state_ = oldState * 6364136223846793005UL + increment_;
            var xorShifted = unchecked((uint) (((oldState >> 18) ^ oldState) >> 27));
            var rot = unchecked((int) (oldState >> 59));
            return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        }
    }
}