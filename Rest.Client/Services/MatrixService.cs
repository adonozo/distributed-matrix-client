using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Client;
using Microsoft.Extensions.Logging;
using Rest.Client.Utils;

namespace Rest.Client.Services
{
    /// <summary>
    /// The Matrix Service. It manages matrices multiplication.
    /// </summary>
    public class MatrixService
    {
        private readonly ILogger<MatrixService> logger;
        private readonly GrpcClient grpcClient;
        private readonly int serversAvailable;

        public MatrixService(ILogger<MatrixService> logger, GrpcClient grpcClient)
        {
            this.logger = logger;
            this.grpcClient = grpcClient;
            serversAvailable = this.grpcClient.ServersAvailable;
        }

        public async Task<int[][]> MultiplyMatricesMultipleServersAsync(int[][] matrixA, int[][] matrixB, int minSubMatrixSize = 16)
        {
            return await this.MultiplyMatricesAsync(matrixA, matrixB, minSubMatrixSize, true);
        }

        public async Task<int[][]> MultiplyMatricesSingleServerAsync(int[][] matrixA, int[][] matrixB, int minSubMatrixSize = 16)
        {
            return await this.MultiplyMatricesAsync(matrixA, matrixB, minSubMatrixSize);
        }

        private async Task<int[][]> MultiplyMatricesAsync(int[][] matrixA, int[][] matrixB, int minSubMatrixSize = 16,
            bool multipleServers = false, int serverInUse = 0)
        {
            var size = matrixA.Length;
            if (size <= minSubMatrixSize)
            {
                var server = multipleServers ? serverInUse % serversAvailable : 0;
                return await grpcClient.MultiplyMatrixAsync(matrixA, matrixB, server);
            }
        
            var subMatrixSize = size / 2;
            var subMatricesA = Helper.BreakMatrix(matrixA, subMatrixSize);
            var subMatricesB = Helper.BreakMatrix(matrixB, subMatrixSize);
            var concurrentResult = new ConcurrentDictionary<int, int[][]>();
        
            await PerformMatrixMultiplication(subMatricesA, subMatricesB, concurrentResult, subMatrixSize, multipleServers);
            return concurrentResult.ToPlainMatrix();
        }

        private async Task PerformMatrixMultiplication(ConcurrentDictionary<int, int[][]> subMatricesA,
            ConcurrentDictionary<int, int[][]> subMatricesB, ConcurrentDictionary<int, int[][]> concurrentResult,
            int minSubMatrixSize, bool multipleServers = false, int serverInUse = 0)
        {
            await Task.WhenAll(Enumerable.Range(0, 4).Select(async index =>
            {
                switch (index)
                {
                    case 0:
                        concurrentResult[0] = await grpcClient.AddMatrixAsync(
                            await this.MultiplyMatricesAsync(subMatricesA[0], subMatricesB[0], minSubMatrixSize, multipleServers, ++serverInUse),
                            await this.MultiplyMatricesAsync(subMatricesA[1], subMatricesB[2], minSubMatrixSize, multipleServers, ++serverInUse)
                        );
                        break;
                    case 1:
                        concurrentResult[1] = await grpcClient.AddMatrixAsync(
                            await this.MultiplyMatricesAsync(subMatricesA[0], subMatricesB[1], minSubMatrixSize, multipleServers, ++serverInUse),
                            await this.MultiplyMatricesAsync(subMatricesA[1], subMatricesB[3], minSubMatrixSize, multipleServers, ++serverInUse)
                        );
                        break;
                    case 2:
                        concurrentResult[2] = await grpcClient.AddMatrixAsync(
                            await this.MultiplyMatricesAsync(subMatricesA[2], subMatricesB[0], minSubMatrixSize, multipleServers, ++serverInUse),
                            await this.MultiplyMatricesAsync(subMatricesA[3], subMatricesB[2], minSubMatrixSize, multipleServers, ++serverInUse)
                        );
                        break;
                    case 3:
                        concurrentResult[3] = await grpcClient.AddMatrixAsync(
                            await this.MultiplyMatricesAsync(subMatricesA[2], subMatricesB[1], minSubMatrixSize, multipleServers, ++serverInUse),
                            await this.MultiplyMatricesAsync(subMatricesA[3], subMatricesB[3], minSubMatrixSize, multipleServers, ++serverInUse)
                        );
                        break;
                }
            }));
        }
    }
}