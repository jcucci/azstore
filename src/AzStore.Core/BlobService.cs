using Microsoft.Extensions.Logging;

namespace AzStore.Core;

public class BlobService
{
    private readonly ILogger<BlobService> _logger;

    public BlobService(ILogger<BlobService> logger)
    {
        _logger = logger;
    }
}