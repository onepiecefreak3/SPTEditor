using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SPTEditor.IO
{
    public static class Extensions
    {
        public static string ReadUntil(this StringReader reader, char limiter)
        {
            string result = "";

            while (reader.Peek() != -1 && Convert.ToChar(reader.Peek()) != limiter)
                result += Convert.ToChar(reader.Read());

            return result;
        }

        public static string ReadUntilIncluded(this StringReader reader, char limiter)
        {
            return reader.ReadUntil(limiter) + Convert.ToChar(reader.Read());
        }

        public static bool Contains<T, T1>(this Dictionary<T, T1> source, Predicate<T1> predicate)
        {
            foreach (var item in source)
                if (predicate(item.Value))
                    return true;

            return false;
        }

        public static bool OnlyConsistsOf(this string source, char search)
        {
            foreach (var c in source)
                if (c != search)
                    return false;

            return true;
        }
    }
}
