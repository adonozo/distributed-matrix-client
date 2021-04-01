using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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

        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        public async Task<int[][]> MultiplyMatricesFingerPrintAsync(int[][] matrixA, int[][] matrixB, long deadline, 
            int minSubMatrixSize = 16)
        {
            var fingerprint = this.GetMatrixMultiplicationTime(matrixA, matrixB, minSubMatrixSize);
            var multiplicationsRequired = Math.Pow(8, matrixA.Length / minSubMatrixSize - 1);
            var serversRequired = (int) Math.Ceiling(fingerprint * multiplicationsRequired / deadline);
            var serversToUse = Math.Min(serversRequired, serversAvailable);
            this.logger.LogInformation($"Required servers for this request: {serversRequired}" +
                                       $" | Servers Available : {serversAvailable} | Servers to use: {serversToUse}");
            return await this.MultiplyMatricesAsync(matrixA, matrixB, serversToUse, minSubMatrixSize);
        }
        
        public async Task<int[][]> MultiplyMatricesMultipleServersAsync(int[][] matrixA, int[][] matrixB, int minSubMatrixSize = 16)
        {
            return await this.MultiplyMatricesAsync(matrixA, matrixB, this.serversAvailable, minSubMatrixSize);
        }

        public async Task<int[][]> MultiplyMatricesSingleServerAsync(int[][] matrixA, int[][] matrixB, int minSubMatrixSize = 16)
        {
            return await this.MultiplyMatricesAsync(matrixA, matrixB, 1, minSubMatrixSize);
        }

        private async Task<int[][]> MultiplyMatricesAsync(int[][] matrixA, int[][] matrixB, int serversToUse, 
            int minSubMatrixSize = 16, int serverInUse = 0)
        {
            var size = matrixA.Length;
            if (size <= minSubMatrixSize)
            {
                var server = serversToUse > 1 ? serverInUse % serversToUse : 0;
                return await grpcClient.MultiplyMatrixAsync(matrixA, matrixB, server);
            }
        
            var subMatrixSize = size / 2;
            var subMatricesA = Helper.BreakMatrix(matrixA, subMatrixSize);
            var subMatricesB = Helper.BreakMatrix(matrixB, subMatrixSize);
            var concurrentResult = new ConcurrentDictionary<int, int[][]>();
        
            await PerformMatrixMultiplication(subMatricesA, subMatricesB, concurrentResult, serversToUse, subMatrixSize);
            return concurrentResult.ToPlainMatrix();
        }

        private async Task PerformMatrixMultiplication(ConcurrentDictionary<int, int[][]> subMatricesA,
            ConcurrentDictionary<int, int[][]> subMatricesB, ConcurrentDictionary<int, int[][]> concurrentResult,
            int serversToUse, int minSubMatrixSize, int serverInUse = 0)
        {
            await Task.WhenAll(Enumerable.Range(0, 4).Select(async index =>
            {
                switch (index)
                {
                    case 0:
                        concurrentResult[0] = await grpcClient.AddMatrixAsync(
                            await this.MultiplyMatricesAsync(subMatricesA[0], subMatricesB[0], serversToUse, minSubMatrixSize, ++serverInUse),
                            await this.MultiplyMatricesAsync(subMatricesA[1], subMatricesB[2], serversToUse, minSubMatrixSize, ++serverInUse)
                        );
                        break;
                    case 1:
                        concurrentResult[1] = await grpcClient.AddMatrixAsync(
                            await this.MultiplyMatricesAsync(subMatricesA[0], subMatricesB[1], serversToUse, minSubMatrixSize, ++serverInUse),
                            await this.MultiplyMatricesAsync(subMatricesA[1], subMatricesB[3], serversToUse, minSubMatrixSize, ++serverInUse)
                        );
                        break;
                    case 2:
                        concurrentResult[2] = await grpcClient.AddMatrixAsync(
                            await this.MultiplyMatricesAsync(subMatricesA[2], subMatricesB[0], serversToUse, minSubMatrixSize, ++serverInUse),
                            await this.MultiplyMatricesAsync(subMatricesA[3], subMatricesB[2], serversToUse, minSubMatrixSize, ++serverInUse)
                        );
                        break;
                    case 3:
                        concurrentResult[3] = await grpcClient.AddMatrixAsync(
                            await this.MultiplyMatricesAsync(subMatricesA[2], subMatricesB[1], serversToUse, minSubMatrixSize, ++serverInUse),
                            await this.MultiplyMatricesAsync(subMatricesA[3], subMatricesB[3], serversToUse, minSubMatrixSize, ++serverInUse)
                        );
                        break;
                }
            }));
        }

        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        private long GetMatrixMultiplicationTime(int[][] matrixA, int[][] matrixB, int minSubMatrixSize)
        {
            var subMatrixA = Helper.GetInitialSubMatrix(matrixA, minSubMatrixSize);
            var subMatrixB = Helper.GetInitialSubMatrix(matrixB, minSubMatrixSize);
            
            var watch = new Stopwatch();
            watch.Start();
            this.grpcClient.MultiplyMatrix(subMatrixA, subMatrixB);
            watch.Stop();
            var fingerprint = watch.ElapsedMilliseconds;
            this.logger.LogInformation($"Fingerprint time: {fingerprint} ms");
            return fingerprint;
        }
    }
}