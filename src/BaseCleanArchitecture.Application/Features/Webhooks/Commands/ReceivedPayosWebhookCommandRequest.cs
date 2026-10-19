using BaseCleanArchitecture.Application.Common.Constants;
using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace BaseCleanArchitecture.Application.Features.Webhooks.Commands
{
    public class ReceivedPayosWebhookCommandRequest : IRequest<Unit>
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object Data { get; set; } = new {};
    }

    public class ReceivedPayosWebhookCommandRequestHandler : IRequestHandler<ReceivedPayosWebhookCommandRequest, Unit>
    {
        private readonly IWebhookLogsRepository _webhookLogsRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ReceivedPayosWebhookCommandRequestHandler(IWebhookLogsRepository webhookLogsRepository, IUnitOfWork unitOfWork)
        {
            _webhookLogsRepository = webhookLogsRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(ReceivedPayosWebhookCommandRequest request, CancellationToken cancellationToken)
        {
            WebhookEvent webhookLogs = WebhookEvent.CreateWebhookEvent(PartnerIntegrationData.Payos.PartnerCode, request.Code, request.Message, JsonSerializer.Serialize(request.Data));
            await _webhookLogsRepository.AddAsync(webhookLogs, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
