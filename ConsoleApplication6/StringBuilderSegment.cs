using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication6
{
    public struct StringBuilderSegment
    {
        private StringBuilder _sb;
        private int _offset;
        private int _count;

        public StringBuilderSegment(StringBuilder sb)
        {
            if (sb == null)
                throw new ArgumentNullException("array");

            this._sb = sb;
            _offset = 0;
            _count = sb.Length;
        }

        public StringBuilderSegment(StringBuilder sb, int offset, int count)
        {
            if (sb == null)
                throw new ArgumentNullException("array");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (sb.Length - offset < count)
                throw new ArgumentException();
            this._sb = sb;
            _offset = offset;
            _count = count;
        }

        public StringBuilder SB
        {
            get
            {
                return _sb;
            }
        }

        public int Offset
        {
            get
            {
                return _offset;
            }
        }

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public override int GetHashCode()
        {
            return null == _sb
                        ? 0
                        : _sb.GetHashCode() ^ _offset ^ _count;
        }

        public override bool Equals(object obj)
        {
            if (obj is StringBuilderSegment)
                return Equals((StringBuilderSegment)obj);
            else
                return false;
        }

        public bool Equals(StringBuilderSegment obj)
        {
            return obj._sb == _sb && obj._offset == _offset && obj._count == _count;
        }

        public static bool operator ==(StringBuilderSegment a, StringBuilderSegment b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(StringBuilderSegment a, StringBuilderSegment b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return SB.ToString().Substring(_offset, _count);
        }

        public int Length  { get { return _count; } }
    }
}
