using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using XFit.Services._File;
using XFit.Utilities.Api;
using XFit.Utilities.Attributes;
using XFit.Utilities.Filters;

namespace XFit.Controllers.V1
{
    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class FileController(IFileService _fileService) : ApiBaseController
    {
        [HttpGet("[action]/{fileName}")]
        public async Task<IActionResult> DownloadFileAsync([FromRoute] string fileName)
        {
            var memory = await _fileService.GetFileAsync(fileName);
            var fileSuffix = fileName.Split(".").LastOrDefault();
            var fileMediaType = fileSuffix?.ToLower() == "png" || fileSuffix?.ToLower() == "jpg" || fileSuffix?.ToLower() == "jpeg" ? "image" : "application";
            return fileMediaType == "image" ? File(memory, $"{fileMediaType}/{fileSuffix}") : File(memory, $"{fileMediaType}/{fileSuffix}", fileName);
        }

        [HttpPost("[action]")]
        [FileSizeLimit(15 * 1024 * 1024)]
        [Security(disable: true)]
        //[Authorize]
        public async Task<string> UploadFileAsync([FromQuery] string fileName, IFormFile file)
        {
            return await _fileService.UploadFileAsync(fileName, file);
        }


        [HttpDelete("[action]")]
        //[Authorize]
        public bool DeleteFile([FromRoute] string fileName)
        {
            return _fileService.DeleteFile(fileName);
        }
    }
}
