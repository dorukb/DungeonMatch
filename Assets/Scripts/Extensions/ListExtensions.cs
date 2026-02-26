using System;
using System.Collections.Generic;

namespace DorkyProductions
{
    public static class ListExtensions 
    {
        private static readonly Random Rng = new Random();

        // The method MUST be static, and the first parameter must use the 'this' keyword
        public static void Shuffle<T>(this IList<T> list) 
        {
            // Fisher-Yates shuffle implementation here...
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Rng.Next(n + 1); 
            
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}