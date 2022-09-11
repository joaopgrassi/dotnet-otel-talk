using System.IdentityModel.Tokens.Jwt;
using API;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

var app = builder
        .ConfigureServices()
        .ConfigurePipeline();

app.Run();
