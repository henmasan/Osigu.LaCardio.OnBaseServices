namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration
{
    public class RcmApiSettings
    {
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AuthPath { get; set; }
        public string UploadPath { get; set; }
        public int Retries { get; set; }
        public int DelayBetweenRetriesMs { get; set; }
    }
}
