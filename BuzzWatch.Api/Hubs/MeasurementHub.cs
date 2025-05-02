using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BuzzWatch.Api.Hubs
{
    [Authorize]
    public class MeasurementHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // JWT claim "deviceIds": "id1,id2"
            var ids = Context.User?
                          .Claims.FirstOrDefault(c => c.Type == "deviceIds")?
                          .Value
                          .Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (ids != null)
                foreach (var id in ids)
                    await Groups.AddToGroupAsync(Context.ConnectionId, id);

            await base.OnConnectedAsync();
        }
    }
} 