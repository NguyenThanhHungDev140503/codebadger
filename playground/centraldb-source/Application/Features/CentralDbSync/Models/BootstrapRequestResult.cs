namespace Application.Features.CentralDbSync.Models;

public sealed record BootstrapRequestResult(
    BootstrapRequest Request,
    bool IsNewRequest);
