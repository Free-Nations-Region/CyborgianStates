using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;

namespace CyborgianStates.Interfaces
{
    public interface IHttpDataService
    {
        Task<HttpResponseMessage> ExecuteRequestAsync(HttpRequestMessage httpRequest, EventId eventId);
    }
}