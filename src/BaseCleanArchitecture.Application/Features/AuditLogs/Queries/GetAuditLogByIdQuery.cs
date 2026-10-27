using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Application.Features.AuditLogs.Models;
using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.AuditLogs.Queries;

public sealed record GetAuditLogByIdQuery(Guid Id) : IRequest<Result<AuditLogDetailDto>>;

public sealed class GetAuditLogByIdQueryHandler : IRequestHandler<GetAuditLogByIdQuery, Result<AuditLogDetailDto>>
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetAuditLogByIdQueryHandler(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<AuditLogDetailDto>> Handle(GetAuditLogByIdQuery request, CancellationToken cancellationToken)
    {
        var log = await _auditLogRepository.GetByIdAsync(request.Id, cancellationToken);

        return log is null
            ? Result<AuditLogDetailDto>.Failure(404, "Audit log not found.")
            : Result<AuditLogDetailDto>.Success(AuditLogDetailDto.From(log));
    }
}
