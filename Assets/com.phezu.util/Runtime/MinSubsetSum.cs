using System;
using System.Collections.Generic;
using System.Linq;

namespace Phezu.Util
{
    /// <summary>
    /// Gets the next subset with the minimum sum.
    /// </summary>
    public class MinSubsetSum
    {
        private float[] mArray;
        private float mArraySum;
        private int[][] mIteratorStates;
        private float[] mIteratorSums;
        private MinHeap<float, int, Dictionary<float, int>> mSumsMinHeap;
        private int Count { get { return mArray.Length; } }

        /// <summary>
        /// Array must be sorted.
        /// </summary>
        public MinSubsetSum(float[] array)
        {
            mArray = array;
            mArraySum = array.Sum();
            mIteratorStates = new int[Count - 1][]; 
            for (int i = 0; i < Count - 1; i++)
            {
                int arraySize = i + 1 < Count - i ?
                    i + 1 :
                    Count - i - 1;

                mIteratorStates[i] = new int[arraySize];

                for (int j = 0; j < arraySize; j++)
                    mIteratorStates[i][j] =
                        i + 1 < Count - i ? j : Count - (arraySize - j);
            }

            mIteratorSums = new float[Count - 1];
            mSumsMinHeap = new();
            for (int i = 0; i < Count - 1; i++)
            {
                UpdateIteratorSum(i);
                mSumsMinHeap.Add(mIteratorSums[i], i);
            }
        }

        private void IncrementIterator(int iteratorIndex)
        {
            if (iteratorIndex + 1 < Count - iteratorIndex)
                IncrementFrontIterator(iteratorIndex);
            else
                IncrementInverseIterator(iteratorIndex);

            UpdateIteratorSum(iteratorIndex);
        }

        private void UpdateIteratorSum(int iteratorIndex)
        {
            if (iteratorIndex + 1 < Count - iteratorIndex)
                SetFrontIteratorSum(iteratorIndex);
            else
                SetInverseIteratorSum(iteratorIndex);
        }

        private void SetFrontIteratorSum(int iteratorIndex)
        {
            mIteratorSums[iteratorIndex] = 0f;

            for (int i = 0; i < mIteratorStates[iteratorIndex].Length; i++)
                mIteratorSums[iteratorIndex] += mArray[mIteratorStates[iteratorIndex][i]];
        }

        private void SetInverseIteratorSum(int iteratorIndex)
        {
            mIteratorSums[iteratorIndex] = mArraySum;

            for (int i = 0; i < mIteratorStates[iteratorIndex].Length; i++)
                mIteratorSums[iteratorIndex] -= mArray[mIteratorStates[iteratorIndex][i]];
        }

        private void IncrementFrontIterator(int iteratorIndex)
        {
            int size = mIteratorStates[iteratorIndex].Length;
            List<int> options = new();

            for (int i = 0; i < size; i++)
            {
                if (i == size - 1)
                    if (mIteratorStates[iteratorIndex][i] == Count - 1)
                        continue;

                if (i != size - 1)
                    if (mIteratorStates[iteratorIndex][i] + 1 == mIteratorStates[iteratorIndex][i + 1])
                        continue;

                options.Add(i);
            }

            int indexToIncrement = options.OrderBy((x) => mArray[mIteratorStates[iteratorIndex][x + 1]])
                .GetEnumerator().Current;

            mIteratorStates[iteratorIndex][indexToIncrement]++;
        }

        private void IncrementInverseIterator(int iteratorIndex)
        {
            int size = mIteratorStates[iteratorIndex].Length;
            List<int> options = new();
            for (int i = 0; i < size; i++)
            {
                if (i == 0)
                    if (mIteratorStates[iteratorIndex][i] == 0)
                        continue;

                if (i != 0)
                    if (mIteratorStates[iteratorIndex][i] - 1 == mIteratorStates[iteratorIndex][i - 1])
                        continue;

                options.Add(i);
            }

            int indexToDecrement = options.OrderBy((x) => mArray[mIteratorStates[iteratorIndex][x - 1]])
                .ToArray()[^1];

            mIteratorStates[iteratorIndex][indexToDecrement]--;
        }

        private int[] GetNextSubsetIndicies()
        {
            KeyValuePair<float, int> minSubset;

            try {
                minSubset = mSumsMinHeap.ExtractMin();
            }
            catch (Exception _) {
                return null;
            }

            int iteratorIndex = minSubset.Value;
            int[] minSumSubsetIndicies = new int[mIteratorStates[iteratorIndex].Length];

            for (int i = 0; i < minSumSubsetIndicies.Length; i++)
                minSumSubsetIndicies[i] = mIteratorStates[iteratorIndex][i];

            IncrementIterator(iteratorIndex);

            mSumsMinHeap.Add(mIteratorSums[iteratorIndex], iteratorIndex);

            return minSumSubsetIndicies;
        }

        /// <summary>
        /// Gets the sum of the next subset with the minimum sum. 0 if no more subsets remaining.
        /// </summary>
        public float NextMinSum()
        {
            float[] minSumSubsetIndicies = NextMinSubset();

            if (minSumSubsetIndicies == null)
                return 0f;

            float minSum = 0f;

            foreach (float value in minSumSubsetIndicies)
                minSum += value;

            return minSum;
        }

        /// <summary>
        /// Gets the set of floats that have the next minimum sum. Null if no more subsets remaining.
        /// </summary>
        public float[] NextMinSubset()
        {
            int[] minSumSubsetIndicies = GetNextSubsetIndicies();

            if (minSumSubsetIndicies == null)
                return null;

            float[] minSubset = new float[minSumSubsetIndicies.Length];

            for (int i = 0; i < minSubset.Length; i++)
            {
                minSubset[i] = mArray[minSumSubsetIndicies[i]];
            }

            return minSubset;
        }

        /// <summary>
        /// Gets the indicies of floats that have the next minimum sum. Null if no more subsets remaining.
        /// </summary>
        public int[] NextMinSubsetIndicies()
        {
            return GetNextSubsetIndicies();
        }
    }
}