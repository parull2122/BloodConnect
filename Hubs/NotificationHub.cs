using Microsoft.AspNetCore.SignalR;

namespace BloodConnect.Hubs;

public class NotificationHub : Hub
{
    // Clients don't need to call anything on this hub directly for the demo —
    // the server pushes "NewBloodRequest" and "RequestResponded" events to everyone.
    // Kept as a real Hub (not just a broadcast endpoint) so it's easy to extend
    // with group-based targeting (e.g. donors joining a "BloodGroup_OPositive" group).
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}
