using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using fineupload.Models;
using FineUploader;

namespace fineupload.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(FineUpload upload)
        {
            if (upload != null)
            {
                try
                {
                    await upload.SaveAs(true);
                }
                catch (Exception ex)
                {
                    return new FineUploaderResult(false, error: ex.Message);
                }

                return new FineUploaderResult(true);
            }
            return new FineUploaderResult(false);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
