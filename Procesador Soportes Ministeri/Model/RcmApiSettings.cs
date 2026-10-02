using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorSoportesMinisterio.Model
{
    public class RcmApiSettings
    {
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AuthPath { get; set; }
        public string UploadPath { get; set; }
        public int Retries { get; set; }
        public int MaxConcurrentUploads { get; set; }
        public int TokenExpiration { get; set; }
    }
}
