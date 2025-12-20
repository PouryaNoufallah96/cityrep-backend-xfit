using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XFit.Services._File
{
    public interface IFileService
    {
        Task<MemoryStream> GetFileAsync(string fileName);

        Task<string> UploadFileAsync(string fileName, IFormFile file);

        bool DeleteFile(string fileName);
    }
}
