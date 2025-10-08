using System.Collections.Generic;
using UnityEngine;

namespace DorkyProductions.Algorithms
{
    public class SubsetSumSolver
    {
        public List<List<MatchResult>> GetAllMatches(int[] arr, int sum)
        {
            bool isAnySubsetPossible = false;
            int n = arr.Length;
            
            List<List<MatchResult>> allResults = new List<List<MatchResult>>();
            
            MatchResult[] tempSubset = new MatchResult[n];
            for (int j = 0; j < n; j++)
            {
                tempSubset[j] = new MatchResult(0, j);
            }
            
            // Run a loop from 0 to 2^n
            for (int p = 0; p < (1 << n); p++)
            {
                for (int j = 0; j < n; j++)
                {
                    tempSubset[j].val = 0;
                    tempSubset[j].idx = j;
                }
                
                int k = 0;
                int m = 1; // m is used to check set bit in binary representation.
                for (int j = 0; j < n; j++)
                {
                    if ((p & m) > 0)
                    {
                        tempSubset[k].val = arr[j];
                        tempSubset[k].idx = j;
                        k++;
                    }

                    m = m << 1;
                }

                int localSum = 0;
                for (int j = 0; j < n; j++)
                {
                    localSum += tempSubset[j].val;
                }

                if (localSum == sum)
                {
                    // check if the sum is equal to the desired sum
                    isAnySubsetPossible = true;
                    List<MatchResult> oneResult = new List<MatchResult>();
                    // var subsetText = "";
                    for (int j = 0; j < n; j++)
                    {
                        if (tempSubset[j].val > 0)
                        {
                            // subsetText += $"({tempSubset[j].val},at:{tempSubset[j].idx}), ";
                            oneResult.Add(new MatchResult(tempSubset[j]));
                        }
                    }
                    allResults.Add(oneResult);
                    // Debug.Log(subsetText);
                }
            }

            if (!isAnySubsetPossible)
            {
                Debug.Log("There is no subset possible for the sum = " + sum);
                return null;
            }
            else return allResults;
        }
    }
}