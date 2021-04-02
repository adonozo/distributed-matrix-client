using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Rest.Client.Utils
{
    public static class Helper
    {
        /// <summary>
        /// Parse a matrix from a comma-separated file without headers. The matrix must have a size of a power of 2 and
        /// be square.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static async Task<int[][]> GetMatrixFromFile(IFormFile file)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var firstLineRead = true;
            var matrix = Array.Empty<int[]>();
            var matrixSize = 0;
            var index = 1;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (firstLineRead)
                {
                    firstLineRead = false;
                    var firstRow = GetIntArray(line);
                    matrixSize = firstRow.Length;
                    matrix = new int[matrixSize][];
                    matrix[0] = firstRow;
                    continue;
                }

                matrix[index] = GetIntArray(line);
                if (matrix[index].Length != matrixSize)
                {
                    throw new ArgumentException("The matrix is not square");
                }

                index++;
            }

            if (matrixSize != index || (matrixSize & (matrixSize - 1)) != 0)
            {
                throw new ArgumentException("The matrix is not square or does not have a size of a power of 2.");
            }

            return matrix;
        }

        /// <summary>
        /// Breaks a square power of 2 matrix into smaller matrices. Performs Parallel computation.
        /// </summary>
        /// <param name="matrix">The matrix to divide.</param>
        /// <param name="blocksSize">The size of the sub-matrix. Must be of a power of 2.</param>
        /// <returns>A ConcurrentDictionary with the sub-matrix position as key and the sub-matrix as value.</returns>
        public static ConcurrentDictionary<int, int[][]> BreakMatrix(int[][] matrix, int blocksSize)
        {
            var blocksInOneSide = matrix.Length / blocksSize;
            var blocks = (int)Math.Pow(2, blocksInOneSide);
            var matrixBlocks = new ConcurrentDictionary<int, int[][]>();
            Parallel.For(0, blocks, index =>
            {
                var subMatrix = new int[blocksSize][];
                var rowBlock = index / blocksInOneSide;
                var columnBlock = index % blocksInOneSide;
                var row = rowBlock * blocksSize;
                var column = columnBlock * blocksSize;
                for (int i = row, k = 0; i < row + blocksSize; i++, k++)
                {
                    subMatrix[k] = new int[blocksSize];
                    Array.Copy(matrix[i], column, subMatrix[k], 0, blocksSize);
                }

                matrixBlocks[index] = subMatrix;
            });
            
            return matrixBlocks;
        }

        /// <summary>
        /// Merges a ConcurrentDictionary of matrices into a single matrix. All sub-matrices must be of the same size with
        /// a size of a power of 2.
        /// </summary>
        /// <param name="concurrentMatrices">The ConcurrentDictionary with the sub-matrices, ordered by index.</param>
        /// <returns>A square matrix with a size of a power of 2</returns>
        public static int[][] ToPlainMatrix(this ConcurrentDictionary<int, int[][]> concurrentMatrices)
        {
            var blocks = concurrentMatrices.Count;
            var blocksInOneSide = (int)Math.Sqrt(concurrentMatrices.Count);
            var blocksSize = concurrentMatrices[0].Length;
            var matrixSize = blocksInOneSide * blocksSize;
            var result = new int[matrixSize][];
            for (var i = 0; i < matrixSize; i++)
            {
                result[i] = new int[matrixSize];
            }
            Parallel.For(0, blocks, index =>
            {
                var rowBlock = index / blocksInOneSide;
                var columnBlock = index % blocksInOneSide;
                var row = rowBlock * blocksSize;
                var column = columnBlock * blocksSize;
                for (int i = row, k = 0; i < row + blocksSize; i++, k++)
                {
                    Array.Copy(concurrentMatrices[index][k], 0, result[i], column, blocksSize);
                }
            });

            return result;
        }

        public static IEnumerable<int[]> GetInitialSubMatrix(int[][] matrix, int size)
        {
            var subMatrix = new int[size][];
            for (var i = 0; i < size; i++)
            {
                subMatrix[i] = new int[size];
                Array.Copy(matrix[i], subMatrix[i], size);
            }
            return subMatrix;
        }
        
        #nullable enable
        private static int[] GetIntArray(string? line)
        {
            if (line == null)
            {
                throw new ArgumentException("The line is empty");
            }

            int[] row;
            try
            {
                row = line.Split(',').Select(int.Parse).ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new ArgumentException("The line is not a number array");
            }

            return row;
        }
    }
}