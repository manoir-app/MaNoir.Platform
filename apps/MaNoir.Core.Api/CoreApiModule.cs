using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MaNoir.Core.Problems;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace MaNoir.Core.Api;

public static class CoreApiModule
{
    public static WebApplicationBuilder AddMaNoirCoreApi(this WebApplicationBuilder builder)
    {
        CoreApiAuthenticationOptions options = ResolveAuthenticationOptions(builder.Configuration, builder.Environment);

        builder.Services.AddSingleton(options);
        builder.Services.AddProblemDetails(problemDetailsOptions =>
        {
            problemDetailsOptions.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            };
        });
        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(CoreApiModule).Assembly);
        builder.Services.AddAuthorization();
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwtOptions =>
            {
                jwtOptions.MapInboundClaims = false;
                jwtOptions.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                jwtOptions.Events = new JwtBearerEvents()
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrWhiteSpace(context.Token)
                            && context.Request.Cookies.TryGetValue(options.CookieName, out string cookieToken)
                            && !string.IsNullOrWhiteSpace(cookieToken))
                        {
                            context.Token = cookieToken;
                        }

                        return System.Threading.Tasks.Task.CompletedTask;
                    }
                };
            });

        return builder;
    }

    public static WebApplication UseMaNoirCoreApi(this WebApplication app)
    {
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                IProblemDetailsService problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
                IExceptionHandlerFeature exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                Exception exception = exceptionFeature?.Error;
                int statusCode = ResolveStatusCode(exception);

                context.Response.StatusCode = statusCode;
                await problemDetailsService.WriteAsync(new ProblemDetailsContext()
                {
                    HttpContext = context,
                    ProblemDetails = new ProblemDetails()
                    {
                        Status = statusCode,
                        Title = ResolveTitle(statusCode, exception),
                        Type = ResolveType(statusCode, exception),
                        Detail = ResolveDetail(app.Environment, exception)
                    }
                });
            });
        });

        app.UseStatusCodePages();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }

    public static IEndpointRouteBuilder MapMaNoirCoreApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
        return endpoints;
    }

    private static CoreApiAuthenticationOptions ResolveAuthenticationOptions(IConfiguration configuration, IHostEnvironment environment)
    {
        CoreApiAuthenticationOptions options = new CoreApiAuthenticationOptions();
        configuration.GetSection(CoreApiAuthenticationOptions.ConfigurationSectionName).Bind(options);

        if (string.IsNullOrWhiteSpace(options.SigningKey))
            options.SigningKey = Environment.GetEnvironmentVariable("HOMEAUTOMATION_AUTH_JWT_SIGNING_KEY");

        if (string.IsNullOrWhiteSpace(options.SigningKey) && environment.IsDevelopment())
            options.SigningKey = CoreApiAuthenticationOptions.DevelopmentSigningKey;

        if (string.IsNullOrWhiteSpace(options.SigningKey) || options.SigningKey.Length < 32)
            throw new InvalidOperationException("Core API JWT authentication requires a signing key of at least 32 characters via configuration or the HOMEAUTOMATION_AUTH_JWT_SIGNING_KEY environment variable.");

        if (string.IsNullOrWhiteSpace(options.CookieName))
            options.CookieName = "manoir_users_access_token";

        if (options.AccessTokenLifetimeMinutes <= 0)
            options.AccessTokenLifetimeMinutes = 720;

        return options;
    }

    private static int ResolveStatusCode(Exception exception)
    {
        if (exception is CoreProblemException problemException)
            return problemException.StatusCode;

        return exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string ResolveTitle(int statusCode, Exception exception)
    {
        if (exception is CoreProblemException problemException)
            return problemException.ProblemTitle;

        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Invalid request",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Resource not found",
            _ => "An unexpected error occurred"
        };
    }

    private static string ResolveType(int statusCode, Exception exception)
    {
        if (exception is CoreProblemException problemException)
            return problemException.ProblemType;

        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "https://manoir.app/problems/common/invalid-request",
            StatusCodes.Status403Forbidden => "https://manoir.app/problems/common/forbidden",
            StatusCodes.Status404NotFound => "https://manoir.app/problems/common/resource-not-found",
            _ => "https://manoir.app/problems/common/unexpected-error"
        };
    }

    private static string ResolveDetail(IHostEnvironment environment, Exception exception)
    {
        if (exception == null)
            return null;

        return environment.IsDevelopment() || exception is CoreProblemException
            ? exception.Message
            : null;
    }
}