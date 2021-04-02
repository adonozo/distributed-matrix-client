using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Net.Client;

namespace Grpc.Client
{
    /// <summary>
    /// The GRPC Client. Calls remote methods on the GRPC server, Add and Multiply matrices. Both methods have a sync
    /// and async implementations.
    /// </summary>
    public class GrpcClient
    {
        private readonly List<string> grpcServers;

        public GrpcClient(List<string> grpcServers)
        {
            this.grpcServers = grpcServers;
        }

        public int ServersAvailable => this.grpcServers.Count;

        public async Task<int[][]> MultiplyMatrixAsync(IEnumerable<int[]> matrixA, IEnumerable<int[]> matrixB, int server = 0)
        {
            using var channel = GrpcChannel.ForAddress(grpcServers[server]);
            var client = new Multiplication.MultiplicationClient(channel);
            var matrices = new Matrices
            {
                MatrixA = {matrixA.Select(row => new Row {Item = {row}})},
                MatrixB = {matrixB.Select(row => new Row {Item = {row}})},
            };
            var reply = await client.MultiplyAsync(matrices);
            channel.Dispose();
            return reply.Result
                .Select(row => row.Item.ToArray())
                .ToArray();
        }

        public int[][] MultiplyMatrix(IEnumerable<int[]> matrixA, IEnumerable<int[]> matrixB, int server = 0)
        {
            using var channel = GrpcChannel.ForAddress(grpcServers[server]);
            var client = new Multiplication.MultiplicationClient(channel);
            var matrices = new Matrices
            {
                MatrixA = {matrixA.Select(row => new Row {Item = {row}})},
                MatrixB = {matrixB.Select(row => new Row {Item = {row}})},
            };
            var reply = client.Multiply(matrices);
            channel.Dispose();
            return reply.Result
                .Select(row => row.Item.ToArray())
                .ToArray();
        }

        public async Task<int[][]> MultiplyMatrixMultiThreadAsync(IEnumerable<int[]> matrixA, IEnumerable<int[]> matrixB, string server)
        {
            using var channel = GrpcChannel.ForAddress(server);
            var client = new MultiThreadMultiplication.MultiThreadMultiplicationClient(channel);
            var matrices = new Matrices
            {
                MatrixA = {matrixA.Select(row => new Row {Item = {row}})},
                MatrixB = {matrixB.Select(row => new Row {Item = {row}})},
            };
            var reply = await client.MultiplyAsync(matrices);
            channel.Dispose();
            return reply.Result
                .Select(row => row.Item.ToArray())
                .ToArray();
        }

        public async Task<int[][]> AddMatrixAsync(IEnumerable<int[]> matrixA, IEnumerable<int[]> matrixB, int server = 0)
        {
            using var channel = GrpcChannel.ForAddress(grpcServers[server]);
            var client = new Add.AddClient(channel);
            var matrices = new Matrices
            {
                MatrixA = {matrixA.Select(row => new Row {Item = {row}})},
                MatrixB = {matrixB.Select(row => new Row {Item = {row}})},
            };
            var reply = await client.AddAsync(matrices);
            channel.Dispose();
            return reply.Result
                .Select(row => row.Item.ToArray())
                .ToArray();
        }

        public int[][] AddMatrix(IEnumerable<int[]> matrixA, IEnumerable<int[]> matrixB, int server = 0)
        {
            using var channel = GrpcChannel.ForAddress(grpcServers[server]);
            var client = new Add.AddClient(channel);
            var matrices = new Matrices
            {
                MatrixA = {matrixA.Select(row => new Row {Item = {row}})},
                MatrixB = {matrixB.Select(row => new Row {Item = {row}})},
            };
            var reply = client.Add(matrices);
            channel.Dispose();
            return reply.Result
                .Select(row => row.Item.ToArray())
                .ToArray();
        }
    }
}