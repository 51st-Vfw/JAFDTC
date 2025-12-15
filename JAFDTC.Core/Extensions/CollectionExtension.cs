using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAFDTC.Core.Extensions
{
    public static class CollectionExtension
    {
        //public static bool HasData(this ICollection value)
        //{
        //    return value != null && value.Count > 0;
        //}

        public static bool HasData<T>(this IEnumerable<T> value)
        {
            return value != null && value.Any();
        }

        //public static bool IsEmpty(this ICollection value)
        //{
        //    return !value.HasData();
        //}

        public static bool IsEmpty<T>(this IEnumerable<T> value)
        {
            return !value.HasData();
        }

    }
}
