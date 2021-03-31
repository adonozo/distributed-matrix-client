using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

        public MatrixController(ILogger<MatrixController> logger, MatrixService matrixService)
        {
            this.logger = logger;
            this.matrixService = matrixService;
        }

        /// <summary>
        /// Uploads a matrix and saves it in memory. It can read any comma separated file, but the matrix must be
        /// square with a size equal to a power of 2 (size = n^2).
        /// </summary>
        /// <returns></returns>
        // ReSharper disable TemplateIsNotCompileTimeConstantProblem
        [HttpPost]
        [Route("/matrix")]
        public async Task<IActionResult> UploadMatrixFile()
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
            
            var matrixResult = await matrixService.MultiplyMatricesMultipleServersAsync(matrix, matrix, 8);
            var response = matrixResult.Stringify();
            return Ok(response);
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
    }
}