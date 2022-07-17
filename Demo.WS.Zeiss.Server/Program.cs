using Demo.WS.Core.contract;
using Demo.WS.Core.mid;
using Demo.WS.Core.service;
using Demo.WS.Zeiss.Server.mid;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddWebSocketConnections();
builder.Services.AddSingleton<IHostedService, HeartbeatService>();
builder.Services.AddSingleton<IMachineService, MachineService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.UseWebSocketConnections("/ws");

app.Run();
