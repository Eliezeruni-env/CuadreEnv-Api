using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace crud_onion.Hubs;

[Authorize(Roles = "Admin,Manager,SuperAdmin,SysAdmin")]
public sealed class SecurityAlertsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var companyId = Context.User?.FindFirst("companyId")?.Value
            ?? Context.User?.FindFirst("CompanyId")?.Value;
        if (int.TryParse(companyId, out var id) && id > 0)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"company:{id}");

        await base.OnConnectedAsync();
    }
}
