using MMOAS.AuthorityService.Composition;
using MMOAS.AuthorityService.Debug;
using MMOAS.AuthorityService.Transport;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorityServicePhase01();

var app = builder.Build();

app.UseWebSockets();

app.MapAuthorityTransportEndpoints();
app.MapDebugEndpoints();

app.Run();

public partial class Program;
