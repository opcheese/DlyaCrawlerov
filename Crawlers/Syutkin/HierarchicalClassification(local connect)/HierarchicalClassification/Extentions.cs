using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HierarchicalClassification
{
    public static class Extentions
    {
        public static IEnumerable<string> Clone(this IEnumerable<string> o)
        {
            List<string> list = new List<string>();
            foreach (string item in o)
                list.Add(item);
            return list;
        }
        public static TOutput With<TInput, TOutput>(this TInput o, Func<TInput, TOutput> evaluator)
        {
            if (o == null)
                return default(TOutput);
            return evaluator(o);
        }
    }
}
