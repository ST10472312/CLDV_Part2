using System;
using System.Collections.Generic;
using System.Text;
using CoffeeNChillFunctions.Models;

namespace CoffeeNChillFunctions.Services
{
    public interface IBlobStorageService
    {
        Task<DocumentMetadata> UploadAsync(
            string fileName,
            Stream content,
            long length,
            string contentType);

        Task<IReadOnlyList<DocumentMetadata>> ListAsync();

        Task<(Stream content, string ContentType)?> DownloadAsync(string fileName);
    }
}
