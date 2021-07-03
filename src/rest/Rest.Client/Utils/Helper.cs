using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Rest.Services.Utils;

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
                    var firstRow = MatrixHelper.GetIntArray(line);
                    matrixSize = firstRow.Length;
                    matrix = new int[matrixSize][];
                    matrix[0] = firstRow;
                    continue;
                }

                matrix[index] = MatrixHelper.GetIntArray(line);
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
    }
}