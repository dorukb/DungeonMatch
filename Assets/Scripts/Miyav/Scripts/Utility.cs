using System;
using System.Collections.Generic;

namespace DorkyProductions
{
    public static class ListExtensions
    {
        public static void Shuffle<T>(this List<T> list)
        {
            // Fisher-Yates Shuffle Algorithm
            // The basic idea is to iterate over the list backwards, and for each element, swap it with a randomly chosen element that comes before it (including itself).
            // This guarantees that all possible orderings are equally probable.
            
            Random rand = new Random();
            int n = list.Count;

            for (int i = n - 1; i > 0; i--)
            {
                // Generate a random index 'before' this position.
                int j = rand.Next(0, i + 1);  
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}