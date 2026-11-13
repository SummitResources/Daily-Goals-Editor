using Daily_Goals_Editor.Web;
using Daily_Goals_Editor.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<BrowserStorage>();
builder.Services.AddScoped<BrowserDayPlanStore>();
builder.Services.AddScoped<TextScale>();

await builder.Build().RunAsync();
