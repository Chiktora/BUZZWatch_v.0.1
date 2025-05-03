using BuzzWatch.Application.Abstractions;
using BuzzWatch.Domain.Alerts;
using BuzzWatch.Domain.Enums;
using MediatR;

namespace BuzzWatch.Application.Alerts.Commands
{
    public record CreateAlertRuleCommand : IRequest<Guid>
    {
        public Guid DeviceId { get; init; }
        public string Metric { get; init; } = string.Empty;
        public BuzzWatch.Domain.Alerts.ComparisonOperator Operator { get; init; }
        public decimal Threshold { get; init; }
        public int DurationSeconds { get; init; }
    }
    
    public class CreateAlertRuleHandler : IRequestHandler<CreateAlertRuleCommand, Guid>
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAlertRuleRepository _alertRuleRepository;
        
        public CreateAlertRuleHandler(
            IDeviceRepository deviceRepository,
            IUnitOfWork unitOfWork,
            IAlertRuleRepository alertRuleRepository)
        {
            _deviceRepository = deviceRepository;
            _unitOfWork = unitOfWork;
            _alertRuleRepository = alertRuleRepository;
        }
        
        public async Task<Guid> Handle(CreateAlertRuleCommand request, CancellationToken cancellationToken)
        {
            // Verify device exists
            var device = await _deviceRepository.GetAsync(request.DeviceId, cancellationToken);
            if (device == null)
            {
                throw new InvalidOperationException($"Device with ID {request.DeviceId} not found");
            }
            
            // Create the alert rule
            var rule = BuzzWatch.Domain.Alerts.AlertRule.Create(
                request.DeviceId,
                request.Metric,
                request.Operator,
                request.Threshold,
                request.DurationSeconds);
                
            // Save to repository
            await _alertRuleRepository.AddAsync(rule, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return rule.Id;
        }
    }
} 