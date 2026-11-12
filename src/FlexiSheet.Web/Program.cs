using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FlexiSheet.Web;
using FlexiSheet.Application.Abstractions;
using FlexiSheet.Infrastructure;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IWorkspaceRepository, BrowserWorkspaceRepository>();
builder.Services.AddScoped<FlexiSheet.Application.LoadWorkspace.Handler>();
builder.Services.AddScoped<FlexiSheet.Application.SaveWorkingPattern.Handler>();
builder.Services.AddScoped<FlexiSheet.Application.StartFlexiPeriod.Handler>();
builder.Services.AddScoped<FlexiSheet.Application.AdjustFlexiPeriod.Handler>();
builder.Services.AddScoped<FlexiSheet.Application.RecordSession.Handler>();
builder.Services.AddScoped<FlexiSheet.Application.DeleteSession.Handler>();
builder.Services.AddScoped<WorkspaceState>();

await builder.Build().RunAsync();
