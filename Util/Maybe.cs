using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IORPG.Util
{
    /// <summary>
    /// This is just nullable for reference types. Note that Maybe.HasValue == true does not imply
    /// that Maybe.Value != null. This is the only way to make this struct actually useful.
    /// </summary>
    /// <typeparam name="T">The type of thing this holds</typeparam>
    public struct Maybe<T> where T : class
    {
        public bool HasValue { get; private set; }

        private readonly T _value;
        public T Value
        {
            get
            {
                if (!HasValue) { throw new InvalidOperationException(); }
                return _value;
            }
        }

        public Maybe(T value) : this()
        {
            HasValue = true;
            _value = value;
        }

        // better syntax
        public static explicit operator Maybe<T>(T val)
        {
            return new Maybe<T>(val);
        }
    }
}
