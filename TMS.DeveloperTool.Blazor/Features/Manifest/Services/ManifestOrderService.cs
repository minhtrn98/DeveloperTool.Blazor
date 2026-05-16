using TMS.DeveloperTool.Blazor.Features.Manifest.Contracts;
using TMS.DeveloperTool.Blazor.Features.Manifest.Models;
using TMS.DeveloperTool.Blazor.Services;

namespace TMS.DeveloperTool.Blazor.Features.Manifest.Services;

public sealed class ManifestOrderService(
    OrderRepository orderRepository,
    MyEmployeeService employeeService,
    IManifestApi manifestApi)
{
    public Task<List<Order>> GetOrdersAsync(CancellationToken cancellationToken)
        => orderRepository.GetOrdersAsync(cancellationToken);

    public Task<List<OrderItem>> GetOrderItemsAsync(string orderId, CancellationToken cancellationToken)
        => orderRepository.GetOrderItemsByOrderIdAsync(orderId, cancellationToken);

    public async Task<List<Employee>> GetEmployeesAsync(CancellationToken cancellationToken)
    {
        IEnumerable<Employee> employees = await employeeService.GetAllEmployeesAsync(cancellationToken);
        return employees.OrderBy(e => e.Code).ToList();
    }

    public async Task<CommitDeliveryManifestResult> CommitManifestAsync(
        CreateManifest manifest, CancellationToken cancellationToken)
    {
        if (manifest.Driver is null)
            throw new InvalidOperationException("Driver chưa được chọn.");

        CommitDeliveryManifestRequest request = new()
        {
            DeliveryMode = manifest.DeliveryMode,
            HandoverConditionId = null,
            ReceiverEvidences = [],
            Orders = manifest.Orders.Select(o => new CommitDeliveryOrderInput
            {
                OrderId = o.OrderId,
                OrderType = o.OrderType,
                Items = o.Items.Select(i => new CommitDeliveryItemInput
                {
                    OrderItemId = i.OrderItemId,
                    IsLoaded = i.IsLoaded,
                }).ToList(),
            }).ToList(),
            Participants =
            [
                new CommitDeliveryParticipantInput
                {
                    EmployeeId = manifest.Driver.EmployeeId,
                    EmployeeCode = manifest.Driver.Code,
                    EmployeeName = manifest.Driver.Name,
                    EmployeeAvatarUrl = null,
                    IsResponsible = true,
                    ParticipationRate = 100m,
                },
            ],
        };

        string authorization = $"Bearer {manifest.Driver.BearerToken}";
        return await manifestApi.CommitAsync(request, authorization, cancellationToken);
    }
}
