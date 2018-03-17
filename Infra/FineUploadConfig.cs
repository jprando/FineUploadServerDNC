using System;

namespace FineUploader
{
    public class FineUploaderConfig : IFineUploaderConfig
    {
        public string  UploadDir { get; set; } = "c:\\temp";
    }
}