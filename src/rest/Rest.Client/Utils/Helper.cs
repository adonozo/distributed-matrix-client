using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Client.Utils
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