using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Client.Enums;
using Client.Utils;
using Rest.Services;

namespace Client.Controllers
{
    /// <summary>
    /// The Matrix controller. Has endpoints to upload matrices, list, and multiply them.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class MatrixController : ControllerBase
    {
        private readonly ILogger<MatrixController> logger;
        private readonly MatrixService matrixService;
        private readonly MatrixStorageService storageService;

        public MatrixController(ILogger<MatrixController> logger, MatrixService matrixService, MatrixStorageService storageService)
        {
            this.logger = logger;
            this.matrixService = matrixService;
            this.storageService = storageService;
        }

        /// <summary>
        /// Uploads a matrix and saves it in memory. It can read any comma separated file, but the matrix must be
        /// square with a size equal to a power of 2 (size = n^2).
        /// </summary>
        /// <returns>The matrix ID if inserted.</returns>
        // ReSharper disable TemplateIsNotCompileTimeConstantProblem
        [HttpPost]
        [Route("/matrices")]
        public async Task<IActionResult> PostMatrix([FromForm] IFormFile file)
        {
            try
            {
                // var file = await this.GetFirstFile();
                var matrix = await Helper.GetMatrixFromFile(file);
                var id = this.storageService.SaveMatrix(matrix);
                return Ok(id);
            }
            catch (FileLoadException e)
            {
                logger.LogWarning(e, e.Message);
                return BadRequest(e.Message);
            }
            catch (ArgumentException e)
            {
                logger.LogWarning(e, e.Message);
                return BadRequest(e.Message);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "The matrix cannot be parsed: " + e.Message);
                return BadRequest("The matrix cannot be parsed");
            }
        }

        /// <summary>
        /// Gets the list of matrices saved in a Dictionary.
        /// </summary>
        /// <returns>A Dictionary with the matrix ID and its size.</returns>
        [HttpGet]
        [Route("/matrices")]
        public IActionResult GetMatrixList()
        {
            return Ok(this.storageService.GetMatricesList());
        }

        [HttpGet]
        [Route("/matrices/multiply")]
        public async Task<IActionResult> MultiplyMatrices([FromQuery] string matrixAId, [FromQuery] string matrixBId,
            [FromQuery] MatrixMultiplicationMode mode, [FromQuery] int deadline, [FromQuery] string server, [FromQuery] int matrixSize = 16)
        {
            if (!Guid.TryParse(matrixAId, out var idA) || !Guid.TryParse(matrixBId, out var idB))
            {
                return BadRequest("The matrices ID are not well formed.");
            }

            try
            {
                var (matrixA, matrixB) = this.GetMatrices(idA, idB);
                int[][] matrixResult;
                switch (mode)
                {
                    case MatrixMultiplicationMode.SingleServerMultiThread:
                        matrixResult = await matrixService.MultiplyMatricesMultiThreadsAsync(matrixA, matrixB, server, matrixSize);
                        break;
                    case MatrixMultiplicationMode.MultipleServersFootprint:
                        matrixResult = await matrixService.MultiplyMatricesFootprintAsync(matrixA, matrixB, deadline, matrixSize);
                        break;
                    case MatrixMultiplicationMode.MultipleSevers :
                        matrixResult = await matrixService.MultiplyMatricesMultipleServersAsync(matrixA, matrixB, matrixSize);
                        break;
                    case MatrixMultiplicationMode.SingleServerSubMatrices :
                        matrixResult = await matrixService.MultiplyMatricesSingleServerAsync(matrixA, matrixB, matrixSize);
                        break;
                    default:
                    {
                        matrixSize = matrixA.Length;
                        matrixResult = await matrixService.MultiplyMatricesSingleServerAsync(matrixA, matrixB, matrixSize);
                        break;
                    }
                }
                
                var stream = new MemoryStream();
                var streamWriter = new StreamWriter(stream);
                foreach (var row in matrixResult)
                {
                    foreach (var t in row)
                    {
                        await streamWriter.WriteAsync(t + " ");
                    }

                    await streamWriter.WriteLineAsync();
                }

                await streamWriter.FlushAsync();
                stream.Position = 0;
                return new FileStreamResult(stream, new MediaTypeHeaderValue("text/plain"));
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (Exception e)
            {
                this.logger.LogWarning(e, "There was an error multiplying the matrices");
                return StatusCode(500);
            }
        }

        private Tuple<int[][], int[][]> GetMatrices(Guid idA, Guid idB)
        {
            var matrixA = storageService.GetMatrixWithId(idA);
            var matrixB = storageService.GetMatrixWithId(idB);
            if (matrixA.Length != matrixB.Length)
            {
                throw new ArgumentException($"The matrices don't the same size. Matrix A: {matrixA.Length}, Matrix B: {matrixB.Length}");
            }

            return new Tuple<int[][], int[][]>(matrixA, matrixB);
        }
    }
}