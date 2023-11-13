using System.Threading;
using System.Threading.Tasks;

namespace TestSignalRSSE
{
    public interface IHubMethod
    {
        Task MessageX(string content, CancellationToken cancellationToken);
    }
}
