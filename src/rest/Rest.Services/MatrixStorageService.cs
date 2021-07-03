using System;
using System.Collections.Generic;
using System.Linq;

namespace Rest.Services
{
    /// <summary>
    /// Manages matrix storage and retrieval. Matrices are stored in a <see cref="Dictionary{TKey,TValue}"/> where
    /// each matrix has an associated <see cref="Guid"/> as key. 
    /// </summary>
    public class MatrixStorageService
    {
        private readonly Dictionary<Guid, int[][]> matrices;

        public MatrixStorageService()
        {
            this.matrices = new Dictionary<Guid, int[][]>();
        }

        public Guid SaveMatrix(int[][] matrix)
        {
            var id = Guid.NewGuid();
            this.matrices.TryAdd(id, matrix);
            return id;
        }

        public int[][] GetMatrixWithId(Guid id)
        {
            var valueExists = this.matrices.TryGetValue(id, out var matrix);
            if (!valueExists)
            {
                throw new KeyNotFoundException($"The ID {id} does not exist");
            }

            return matrix;
        }

        public Dictionary<Guid, int> GetMatricesList()
        {
            return this.matrices.ToDictionary(item => item.Key, item => item.Value.Length);
        }
    }
}