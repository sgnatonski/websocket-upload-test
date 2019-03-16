using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace fileuploadtest.Controllers
{
    public static class Helpers
    {
        public static async Task<int> ReadStream(Stream stream, int bufferSize)
        {
            var buffer = new byte[bufferSize];

            int bytesRead;
            int totalBytes = 0;

            do
            {
                bytesRead = await stream.ReadAsync(buffer, 0, bufferSize);
                totalBytes += bytesRead;
            } while (bytesRead > 0);
            return totalBytes;
        }

        public static string GetBoundary(string contentType)
        {
            if (contentType == null)
                throw new ArgumentNullException(nameof(contentType));

            var elements = contentType.Split(' ');
            var element = elements.First(entry => entry.StartsWith("boundary="));
            var boundary = element.Substring("boundary=".Length);

            boundary = HeaderUtilities.RemoveQuotes(boundary).ToString();

            return boundary;
        }
    }


    [Route("api/[controller]")]
    [ApiController]
    public class U1Controller : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> UploadMultipartUsingIFormFile(IFormFile file)
        {
            var bufferSize = 32 * 1024;
            var totalBytes = await Helpers.ReadStream(file.OpenReadStream(), bufferSize);

            return Ok();
        }

    }
    [Route("api/[controller]")]
    [ApiController]
    public class U2Controller : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> UploadMultipartUsingReader()
        {
            var boundary = Helpers.GetBoundary(Request.ContentType);
            var reader = new MultipartReader(boundary, Request.Body, 80 * 1024);

            MultipartSection section;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var contentDispo = section.GetContentDispositionHeader();

                if (contentDispo.IsFileDisposition())
                {
                    var fileSection = section.AsFileSection();
                    var bufferSize = 32 * 1024;
                    await Helpers.ReadStream(fileSection.FileStream, bufferSize);
                }
            }

            return Ok();
        }
    }
}
