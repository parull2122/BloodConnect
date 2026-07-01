# BloodConnect - Real-Time Blood Donor-Recipient Matching Platform

A minor project built with **ASP.NET Core 8 (Blazor Server)** that matches blood
donors to recipients in real time based on blood group compatibility, distance,
and donation eligibility.

## How to Run

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) if you don't have it.
2. Extract this project folder.
3. Open a terminal in the project folder and run:
   ```
   dotnet restore
   dotnet run
   ```
4. Open the URL shown in the terminal (e.g. `http://localhost:5008`) in your browser.

> Tip: Open the Dashboard (`/`) in two browser tabs side by side. Raise a
> request in one tab and watch the other tab update instantly — this is the
> core "wow" moment for your demo/viva.

## Project Structure

```
BloodConnect/
├─ Models/
│   ├─ BloodGroup.cs        → enum + compatibility matrix (who can donate to whom)
│   └─ Entities.cs          → Donor, BloodRequest, DonationResponse, DonorMatch
├─ Services/
│   └─ BloodBankService.cs  → in-memory data store + matching engine (core logic)
├─ Hubs/
│   └─ NotificationHub.cs   → SignalR hub used to broadcast events server-side
├─ Components/Pages/
│   ├─ Home.razor           → live dashboard of active requests + matches
│   ├─ CreateRequest.razor  → form to raise a blood request
│   ├─ RegisterDonor.razor  → form to register as a donor
│   └─ Donors.razor         → list of all registered donors
└─ Program.cs               → app startup, service registration
```

## How the Matching Works

When a blood request is submitted, `BloodBankService.FindMatches()`:

1. **Filters by compatibility** — uses a standard blood-donation compatibility
   matrix (e.g. O- is a universal donor, AB+ is a universal recipient).
2. **Filters by eligibility** — a donor must not have donated in the last 90
   days (standard real-world rule).
3. **Filters by distance** — computes real distance using the **Haversine
   formula** (great-circle distance from latitude/longitude) and keeps donors
   within 25 km.
4. **Ranks** the remaining donors by distance, closest first.

This is the "smart" part of the project worth highlighting in your report/viva
— it's a genuine small algorithm, not just a database filter.

## Real-Time Notification (SignalR)

- `NotificationHub` is a real ASP.NET Core SignalR hub, and `BloodBankService`
  broadcasts `NewBloodRequest` / `RequestResponded` events through it whenever
  something changes — the same technique you'd use to push notifications to a
  mobile app, external dashboard, or JavaScript frontend.
- The Blazor Server dashboard itself refreshes live via a lightweight C# event
  (`BloodBankService.OnChanged`), since Blazor Server already maintains its own
  persistent connection to the browser. Functionally this gives you the same
  "live update" demo effect you'd get from a SignalR client.

## Data Storage — Important Note

To keep the project runnable **without any external database setup** (useful
under today's deadline), donor/request data is stored **in memory** via a
singleton service, and seeded with 6 demo donors around a sample city on
startup. Data resets when the app restarts.

**For a stronger submission or future extension**, swap `BloodBankService`'s
internal `List<T>` fields for an `Microsoft.EntityFrameworkCore` + SQL Server
or SQLite backed repository — the public method signatures are already
written so this is a clean drop-in replacement. Mention this as a "future
enhancement" in your report if asked.

## Suggested Report/Viva Talking Points

- **Problem statement**: manual blood donor search during emergencies is slow;
  this automates matching + notification.
- **Core algorithm**: compatibility matrix + Haversine distance + eligibility
  ranking (explain with a live example).
- **Real-time architecture**: SignalR hub + event-driven UI updates.
- **Tech stack**: ASP.NET Core 8, Blazor Server, C#.
- **Future scope**: persistent database (EF Core), SMS/email alerts (Twilio/
  MailKit), map-based UI (Leaflet.js), hospital-verified donation records.

## Demo Script

1. Go to `/donors` — show the 6 pre-seeded donors with different blood groups
   and locations.
2. Open `/` (Dashboard) in a second tab.
3. Go to `/create-request` — submit a request for, say, **O+** blood near
   coordinates `22.72, 75.86` (close to the seeded donors).
4. Switch to the Dashboard tab — the new request and its matched donors appear
   automatically, ranked by distance.
5. Click **"Simulate Accept"** next to a donor — explain this represents the
   donor accepting via a push notification in a full mobile/SMS-enabled build.
