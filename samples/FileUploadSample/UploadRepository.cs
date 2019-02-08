using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FileUploadSample
{
    public class UploadRepository
    {
        private readonly ConcurrentBag<File> _files;
        private readonly string _uploadDirectory;

        public UploadRepository()
        {
            _files = new ConcurrentBag<File>();
            _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            Directory.CreateDirectory(_uploadDirectory);
        }

        public async Task<File> Save(IFormFile formFile)
        {
            var id = Guid.NewGuid().ToString().Substring(0, 8);
            var path = Path.Combine(_uploadDirectory, id + Path.GetExtension(formFile.FileName));

            using (var fs = formFile.OpenReadStream())
            using (var ws = System.IO.File.Create(path))
            {
                await fs.CopyToAsync(ws);
            }

            var file = new File
            {
                Id = id,
                MimeType = formFile.ContentType,
                Name = formFile.FileName,
                Path = path
            };
            _files.Add(file);

            return file;
        }

        public IEnumerable<File> Files => _files;
    }
}
