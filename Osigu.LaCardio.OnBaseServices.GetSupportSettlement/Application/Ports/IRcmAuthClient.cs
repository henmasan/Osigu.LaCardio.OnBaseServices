using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports
{
    public class RcmAuthToken
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; }
    }

    public interface IRcmAuthClient
    {
        Task<RcmAuthToken> GetTokenAsync();
    }
}
