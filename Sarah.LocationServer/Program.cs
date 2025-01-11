using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;


// Define the entry point of the application and read config from env vars
var pfxFilePath = Environment.GetEnvironmentVariable("SARAH_CERTIFICATE_FILE");
var pfxPassword = Environment.GetEnvironmentVariable("SARAH_CERTIFICATE_PASSWORD");
var port = Environment.GetEnvironmentVariable("SARAH_CERTIFICATE_PORT");

if(String.IsNullOrWhiteSpace(pfxFilePath) || String.IsNullOrWhiteSpace(pfxPassword) || String.IsNullOrWhiteSpace(port))
{
    throw new InvalidOperationException("Please set the environment variables SARAH_CERTIFICATE_FILE, SARAH_CERTIFICATE_PASSWORD and SARAH_CERTIFICATE_PORT");
}


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
//builder.WebHost.UseUrls("https://*:5002");

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureKestrel(options => 
{
    // Configure Kestrel to listen on all IP addresses and use a specific port
    options.Listen(IPAddress.Any, Int32.Parse(port), listenOptions =>
    {
        // Enable support for HTTP1 and HTTP2 (required if you want to host gRPC endpoints)
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        // Configure Kestrel to use a certificate from a local .PFX file for hosting HTTPS
        listenOptions.UseHttps(pfxFilePath, pfxPassword);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI(); 
app.UseExceptionHandler("/Error");
//}

app.UseHttpsRedirection();


app.MapControllers();


app.Run();
