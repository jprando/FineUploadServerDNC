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
        public bool UnirArquivos { get; set; }
        public Guid UUI { get; set; }

        public FineUpload(string uploadDir)
        {
            _uploadDir = uploadDir;
        }

        public async Task<bool> SaveAs(bool overwrite = false, bool autoCreateDirectory = true)
        {
            var dir = _uploadDir; /// @"c:\temp";
            var filePath = Path.Combine(dir, UUI.ToString("N"));
            var destination = filePath;
            if (!UnirArquivos)
            {
                destination = Path.Combine(destination, PartIndex.ToString("0000000000"));
                if (autoCreateDirectory)
                {
                    var directory = new FileInfo(destination).Directory;
                    if (directory != null) directory.Create();
                }
                // using (var file = new FileStream(destination, overwrite ? FileMode.Create : FileMode.CreateNew))
                using (var file = new FileStream(destination, overwrite ? FileMode.OpenOrCreate : FileMode.CreateNew))
                {
                    await InputStream.CopyToAsync(file);
                    await file.FlushAsync();
                    file.Close();
                }

                if (TotalParts == 0)
                {
                    var toDir = new FileInfo(destination).Directory.FullName;
                    File.Move(destination, Path.Combine(toDir, Filename));
                }
            }
            else
            {
                DirectoryInfo di = new DirectoryInfo(destination);
                var listaPartes = di.GetFiles().OrderBy(i => i.Name);
                using (var arquivoFinal = new FileStream(Path.Combine(destination, Filename), FileMode.Create))
                {
                    foreach (var item in listaPartes)
                    {
                        using (var arquivoParte = item.OpenRead())
                        {
                            await arquivoParte.CopyToAsync(arquivoFinal);
                        }
                        item.Delete();
                    }
                }
            }
            return true;
        }

        public class FineUploaderModelBinder : IModelBinder
        {
            public readonly string _uploadDir;
            public FineUploaderModelBinder(IConfiguration config)
            {
                _uploadDir = config.GetValue<string>("uploadDir");
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

                var upload = new FineUpload(_uploadDir)
                {
                    Filename = xFileName ?? qqFile ?? formFilename,
                    InputStream = formUpload ? _files[0].OpenReadStream() : _request.Body,
                    TotalFileSize = Convert.ToInt64(_request.Form["qqtotalfilesize"].ToString()),
                    PartIndex = Convert.ToInt32(_request.Form["qqpartindex"]),
                    TotalParts = Convert.ToInt32(_request.Form["qqtotalparts"]),
                    PartByteOffSet = Convert.ToInt64(_request.Form["qqpartbyteoffset"]),
                    ChunkSize = Convert.ToInt32(_request.Form["qqchunksize"]),
                    UUI = new Guid(_request.Form["qquuid"].ToString()),
                    UnirArquivos = !formUpload
                };

                bindingContext.Result = ModelBindingResult.Success(upload);
                return Task.CompletedTask;
            }
        }

    }
}