using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;

namespace FineUploader
{
    [ModelBinder(typeof(FineUploaderModelBinder))]
    public class FineUpload
    {
        private readonly string _uploadDir;

        public string Filename { get; set; }
        public Stream InputStream { get; set; }

        public long TotalFileSize { get; set; }
        public int PartIndex { get; set; }
        public int TotalParts { get; set; }
        public long PartByteOffSet { get; set; }
        public int ChunkSize { get; set; }
        public bool JoinFiles { get; set; }
        public Guid UUI { get; set; }

        public FineUpload(string uploadDir)
        {
            _uploadDir = uploadDir;
        }

        public async Task SaveAs(bool overwrite = false, bool autoCreateDirectory = true)
        {
            var destination = Path.Combine(_uploadDir, UUI.ToString("N"));
            if (!JoinFiles)
            {
                var directory = new DirectoryInfo(destination);
                destination = Path.Combine(destination, PartIndex.ToString("0000000000"));
                if (autoCreateDirectory)
                {
                    directory.Create();
                }
                using (var file = new FileStream(destination, overwrite ? FileMode.OpenOrCreate : FileMode.CreateNew))
                {
                    await InputStream.CopyToAsync(file);
                }
                if (TotalParts == 0)
                {
                    var moveTo = Path.Combine(directory.FullName, Filename);
                    File.Move(destination, moveTo);
                }
            }
            else
            {
                await JoinFilesInOne(destination);
            }
        }

        private async Task JoinFilesInOne(string destination)
        {
            DirectoryInfo dir = new DirectoryInfo(destination);
            var fileList = dir.GetFiles().OrderBy(i => i.Name);
            var endFileName = Path.Combine(destination, Filename);
            using (var endFile = new FileStream(endFileName, FileMode.Create))
            {
                foreach (var itemFile in fileList)
                {
                    using (var arquivoParte = itemFile.OpenRead())
                    {
                        await arquivoParte.CopyToAsync(endFile);
                    }
                    itemFile.Delete();
                }
            }
        }

        public class FineUploaderModelBinder : IModelBinder
        {
            public readonly string _uploadDirectory;
            public FineUploaderModelBinder(IConfiguration config)
            {
                _uploadDirectory = config.GetValue<string>("uploadDir");
            }
            public Task BindModelAsync(ModelBindingContext bindingContext)
            {
                var _context = bindingContext.HttpContext;
                var _request = _context.Request;
                var _files = _request.Form.Files;
                var formUpload = _files.Count > 0;

                // find filename
                string xFileName = _request.Headers["X-File-Name"];
                string qqFile = _request.Form["qqfilename"];
                var formFilename = formUpload ? Path.GetFileName(_files[0].FileName) : null;

                var upload = new FineUpload(_uploadDirectory)
                {
                    Filename = xFileName ?? qqFile ?? formFilename,
                    InputStream = formUpload ? _files[0].OpenReadStream() : _request.Body,
                    TotalFileSize = Convert.ToInt64(_request.Form["qqtotalfilesize"].ToString()),
                    PartIndex = Convert.ToInt32(_request.Form["qqpartindex"]),
                    TotalParts = Convert.ToInt32(_request.Form["qqtotalparts"]),
                    PartByteOffSet = Convert.ToInt64(_request.Form["qqpartbyteoffset"]),
                    ChunkSize = Convert.ToInt32(_request.Form["qqchunksize"]),
                    UUI = new Guid(_request.Form["qquuid"].ToString()),
                    JoinFiles = !formUpload
                };

                bindingContext.Result = ModelBindingResult.Success(upload);
                return Task.CompletedTask;
            }
        }

    }
}