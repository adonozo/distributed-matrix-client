using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Client;
using Microsoft.Extensions.Logging;
using Rest.Services.Utils;

namespace Rest.Services
{
    /// <summary>
    /// The Matrix Service. Supports all matrix multiplication modes but all actual multiplications are performed in the
    /// server. The methods from this service break the matrix and prepare it to send it over the network as a smaller
    /// chunk. This improves performance because reduces network time in large matrices, and allows to send chunks to
    /// different serves, dividing the processing work.
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
        /// <summary>
        /// Tries to guess the number of servers required to perform the multiplication within the deadline. The number
        /// of servers available are obtained from the application settings at startup.
        /// The client will first calculate how much time it takes to multiply one sub-matrix, then will get the number
        /// of required servers. 
        /// </summary>
        /// <param name="matrixA">The matrix A</param>
        /// <param name="matrixB">The matrix B</param>
        /// <param name="deadline">The deadline to meet, in milliseconds</param>
        /// <param name="minSubMatrixSize">Matrices A and B will be broken down until this size. Defaults to 16.</param>
        /// <returns>An awaitable int matrix with the multiplication result</returns>
        public async Task<int[][]> MultiplyMatricesFootprintAsync(int[][] matrixA, int[][] matrixB, long deadline, 
            int minSubMatrixSize = 16)
        {
            var footprint = this.GetMatrixMultiplicationTime(matrixA, matrixB, minSubMatrixSize);
            var multiplicationsRequired = Math.Pow(8, matrixA.Length / minSubMatrixSize - 1);
            var serversRequired = (int) Math.Ceiling(footprint * multiplicationsRequired / deadline);
            var serversToUse = Math.Min(serversRequired, serversAvailable);
            this.logger.LogInformation($"Required servers for this request: {serversRequired}" +
                                       $" | Servers Available : {serversAvailable} | Servers to use: {serversToUse}");
            return await this.MultiplyMatricesAsync(matrixA, matrixB, serversToUse, minSubMatrixSize);
        }
        
        public async Task<int[][]> MultiplyMatricesMultipleServersAsync(int[][] matrixA, int[][] matrixB, int minSubMatrixSize = 16)
        {
            return await this.MultiplyMatricesAsync(matrixA, matrixB, this.serversAvailable, minSubMatrixSize);
        }

        /// <summary>
        /// Performs matrix multiplication as a parallel task in the server. Only one server is called.
        /// This method is recursive, as defined in the divide and conquer multiplication approach. 
        /// </summary>
        /// <param name="matrixA">The matrix A</param>
        /// <param name="matrixB">The matrix B</param>
        /// <param name="server">The multicore server address</param>
        /// <param name="minSubMatrixSize">Matrices A and B will be broken down until this size. Defaults to 128</param>
        /// <returns>An awaitable int matrix with the multiplication result</returns>
        public async Task<int[][]> MultiplyMatricesMultiThreadsAsync(int[][] matrixA, int[][] matrixB, string server,
            int minSubMatrixSize = 128)
        {
            var size = matrixA.Length;
            if (size <= minSubMatrixSize)
            {
                return await grpcClient.MultiplyMatrixMultiThreadAsync(matrixA, matrixB, server);
            }
        
            var subMatrixSize = size / 2;
            var subMatricesA = MatrixHelper.BreakMatrix(matrixA, subMatrixSize);
            var subMatricesB = MatrixHelper.BreakMatrix(matrixB, subMatrixSize);
            var concurrentResult = new ConcurrentDictionary<int, int[][]>();
        
            await PerformMultiThreadMultiplication(subMatricesA, subMatricesB, concurrentResult, server, subMatrixSize);
            return concurrentResult.ToPlainMatrix();
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
            var subMatricesA = MatrixHelper.BreakMatrix(matrixA, subMatrixSize);
            var subMatricesB = MatrixHelper.BreakMatrix(matrixB, subMatrixSize);
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
        
        private async Task PerformMultiThreadMultiplication(ConcurrentDictionary<int, int[][]> subMatricesA,
            ConcurrentDictionary<int, int[][]> subMatricesB, ConcurrentDictionary<int, int[][]> concurrentResult,
            string server, int minSubMatrixSize)
        {
            await Task.WhenAll(Enumerable.Range(0, 4).Select(async index =>
            {
                switch (index)
                {
                    case 0:
                        concurrentResult[0] = await grpcClient.AddMatrixAsync(
                            await this.MultiplyMatricesMultiThreadsAsync(subMatricesA[0], subMatricesB[0], server, minSubMatrixSize),
                            await this.MultiplyMatricesMultiThreadsAsync(subMatricesA[1], subMatricesB[2], server, minSubMatrixSize)
                        );
                        break;
                    case 1:
                        concurrentResult[1] = await grpcClient.AddMatrixAsync(
                            await this.MultiplyMatricesMultiThreadsAsync(subMatricesA[0], subMatricesB[1], server, minSubMatrixSize),
                            await this.MultiplyMatricesMultiThreadsAsync(subMatricesA[1], subMatricesB[3], server, minSubMatrixSize)
                        );
                        break;
                    case 2:
                        concurrentResult[2] = await grpcClient.AddMatrixAsync(
                            await this.MultiplyMatricesMultiThreadsAsync(subMatricesA[2], subMatricesB[0], server, minSubMatrixSize),
                            await this.MultiplyMatricesMultiThreadsAsync(subMatricesA[3], subMatricesB[2], server, minSubMatrixSize)
                        );
                        break;
                    case 3:
                        concurrentResult[3] = await grpcClient.AddMatrixAsync(
                            await this.MultiplyMatricesMultiThreadsAsync(subMatricesA[2], subMatricesB[1], server, minSubMatrixSize),
                            await this.MultiplyMatricesMultiThreadsAsync(subMatricesA[3], subMatricesB[3], server, minSubMatrixSize)
                        );
                        break;
                }
            }));
        }

        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        private long GetMatrixMultiplicationTime(int[][] matrixA, int[][] matrixB, int minSubMatrixSize)
        {
            var subMatrixA = MatrixHelper.GetInitialSubMatrix(matrixA, minSubMatrixSize);
            var subMatrixB = MatrixHelper.GetInitialSubMatrix(matrixB, minSubMatrixSize);
            
            var watch = new Stopwatch();
            watch.Start();
            this.grpcClient.MultiplyMatrix(subMatrixA, subMatrixB);
            watch.Stop();
            var footprint = watch.ElapsedMilliseconds;
            this.logger.LogInformation($"Footprint time: {footprint} ms");
            return footprint;
        }
    }
}