using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Application.Features.AuditLogs.Models;
using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Enums;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.AuditLogs.Queries;

public sealed record GetAuditLogsQuery(int Page, int PageSize, int? Status, AuditType? AuditType)
    : IRequest<Result<PagedResult<AuditLogListItemDto>>>;

public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, Result<PagedResult<AuditLogListItemDto>>>
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IAuditLogRepository _auditLogRepository;

    public GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<PagedResult<AuditLogListItemDto>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);

        var (items, totalCount) = await _auditLogRepository.GetListAsync(page, pageSize, request.Status, request.AuditType, cancellationToken);

        var dtos = items.Select(AuditLogListItemDto.From).ToList();
        return Result<PagedResult<AuditLogListItemDto>>.Success(new PagedResult<AuditLogListItemDto>(dtos, page, pageSize, totalCount));
    }
}
