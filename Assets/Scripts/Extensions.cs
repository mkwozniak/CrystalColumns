using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Wozware.CrystalColumns
{
    public static class Extensions
    {
        public static int WeightedRandomAlias(this Random rnd, IEnumerable<int> probs)
        {
            int random = rnd.Next(probs.Sum());
            int sum = 0;
            int idx = 0;
            foreach (var p in probs)
            {
                sum += p;
                if (sum >= random)
                    break;
                idx++;
            }
            return idx;
        }
    }
}

