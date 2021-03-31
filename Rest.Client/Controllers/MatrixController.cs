using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rest.Client.Enums;
using Rest.Client.Services;
using Rest.Client.Utils;

namespace Rest.Client.Controllers
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

        // /// <summary>
        // /// Uploads a matrix and saves it in memory. It can read any comma separated file, but the matrix must be
        // /// square with a size equal to a power of 2 (size = n^2).
        // /// </summary>
        // /// <returns></returns>
        // // ReSharper disable TemplateIsNotCompileTimeConstantProblem
        // [HttpPost]
        // [Route("/matrix")]
        // public async Task<IActionResult> UploadMatrixFile()
        // {
        //     int[][] matrix;
        //     try
        //     {
        //         var file = await this.GetFirstFile();
        //         matrix = await Helper.GetMatrixFromFile(file);
        //     }
        //     catch (FileLoadException e)
        //     {
        //         logger.LogWarning(e, e.Message);
        //         return BadRequest(e.Message);
        //     }
        //     catch (ArgumentException e)
        //     {
        //         logger.LogWarning(e, e.Message);
        //         return BadRequest(e.Message);
        //     }
        //     catch (Exception)
        //     {
        //         logger.LogWarning("The matrix cannot be parsed");
        //         return BadRequest("The matrix cannot be parsed");
        //     }
        //     
        //     var matrixResult = await matrixService.MultiplyMatricesMultipleServersAsync(matrix, matrix, 8);
        //     var response = matrixResult.Stringify();
        //     return Ok(response);
        // }
        
        /// <summary>
        /// Uploads a matrix and saves it in memory. It can read any comma separated file, but the matrix must be
        /// square with a size equal to a power of 2 (size = n^2).
        /// </summary>
        /// <returns>The matrix ID if inserted.</returns>
        // ReSharper disable TemplateIsNotCompileTimeConstantProblem
        [HttpPost]
        [Route("/matrices")]
        public async Task<IActionResult> PostMatrix()
        {
            int[][] matrix;
            try
            {
                var file = await this.GetFirstFile();
                matrix = await Helper.GetMatrixFromFile(file);
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
            catch (Exception)
            {
                logger.LogWarning("The matrix cannot be parsed");
                return BadRequest("The matrix cannot be parsed");
            }

            var id = this.storageService.SaveMatrix(matrix);
            return Ok(id);
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

        // TODO test this...
        [HttpGet]
        [Route("/matrices/multiply")]
        public async Task<IActionResult> MultiplyMatrices([FromQuery] string matrixAId, [FromQuery] string matrixBId,
            [FromQuery] MatrixMultiplicationMode mode, [FromQuery] int matrixSize = 16)
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
                
                var response = matrixResult.Stringify();
                return Ok(response);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        private async Task<IFormFile> GetFirstFile()
        {
            var formCollection = await Request.ReadFormAsync();
            var file = formCollection.Files[0];
            if (file.Length == 0)
            {
                throw new FileLoadException("The file is empty");
            }

            return file;
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