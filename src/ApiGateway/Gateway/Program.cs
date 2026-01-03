using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig.ReadFrom.Configuration(context.Configuration);
});

// Add YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Load YARP configuration from yarp.json
builder.Configuration.AddJsonFile("yarp.json", optional: false, reloadOnChange: true);

// Configure Authentication with Keycloak
var keycloakSettings = builder.Configuration.GetSection("Authentication:Keycloak");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = keycloakSettings["Authority"];
    options.Audience = keycloakSettings["Audience"];
    options.RequireHttpsMetadata = keycloakSettings.GetValue<bool>("RequireHttpsMetadata", false);
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = keycloakSettings.GetValue<bool>("ValidateIssuer", true),
        ValidateAudience = keycloakSettings.GetValue<bool>("ValidateAudience", true),
        ValidateLifetime = keycloakSettings.GetValue<bool>("ValidateLifetime", true),
        ValidIssuer = keycloakSettings["Authority"],
        ValidAudience = keycloakSettings["Audience"],
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("Authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Log.Debug("Token validated for user: {User}", context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        }
    };
});

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("default", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
    
    options.AddPolicy("anonymous", policy =>
    {
        policy.RequireAssertion(_ => true);
    });
    
    options.AddPolicy("catalog-admin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("catalog-admin");
    });
    
    options.AddPolicy("order-admin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("order-admin");
    });
    
    options.AddPolicy("customer", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("customer");
    });
});

// Configure Rate Limiting
var rateLimitSettings = builder.Configuration.GetSection("RateLimiting");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Fixed Window Rate Limiter
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = rateLimitSettings.GetValue<int>("Fixed:PermitLimit", 100);
        limiterOptions.Window = rateLimitSettings.GetValue<TimeSpan>("Fixed:Window", TimeSpan.FromMinutes(1));
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = rateLimitSettings.GetValue<int>("Fixed:QueueLimit", 10);
    });
    
    // Sliding Window Rate Limiter
    options.AddSlidingWindowLimiter("sliding", limiterOptions =>
    {
        limiterOptions.PermitLimit = rateLimitSettings.GetValue<int>("Sliding:PermitLimit", 100);
        limiterOptions.Window = rateLimitSettings.GetValue<TimeSpan>("Sliding:Window", TimeSpan.FromMinutes(1));
        limiterOptions.SegmentsPerWindow = rateLimitSettings.GetValue<int>("Sliding:SegmentsPerWindow", 4);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = rateLimitSettings.GetValue<int>("Sliding:QueueLimit", 10);
    });
    
    // Concurrency Limiter
    options.AddConcurrencyLimiter("concurrency", limiterOptions =>
    {
        limiterOptions.PermitLimit = rateLimitSettings.GetValue<int>("Concurrency:PermitLimit", 10);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = rateLimitSettings.GetValue<int>("Concurrency:QueueLimit", 5);
    });
    
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
        }
        
        Log.Warning("Rate limit exceeded for {Path} from {IP}",
            context.HttpContext.Request.Path,
            context.HttpContext.Connection.RemoteIpAddress);
            
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests",
            message = "Rate limit exceeded. Please try again later.",
            retryAfter = retryAfter.TotalSeconds
        }, cancellationToken);
    };
});

// Configure CORS
var corsSettings = builder.Configuration.GetSection("Cors");
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"];
        var methods = corsSettings.GetSection("AllowedMethods").Get<string[]>() ?? ["GET", "POST", "PUT", "DELETE"];
        var headers = corsSettings.GetSection("AllowedHeaders").Get<string[]>() ?? ["Content-Type", "Authorization"];
        var exposedHeaders = corsSettings.GetSection("ExposedHeaders").Get<string[]>() ?? ["X-Correlation-Id"];
        
        policy.WithOrigins(origins)
              .WithMethods(methods)
              .WithHeaders(headers)
              .WithExposedHeaders(exposedHeaders);
              
        if (corsSettings.GetValue<bool>("AllowCredentials", false))
        {
            policy.AllowCredentials();
        }
    });
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddUrlGroup(
        new Uri(builder.Configuration["Services:Catalog:BaseUrl"] + "/health"),
        name: "catalog-api",
        tags: ["services"])
    .AddUrlGroup(
        new Uri(builder.Configuration["Services:Order:BaseUrl"] + "/health"),
        name: "order-api",
        tags: ["services"]);

var app = builder.Build();

// Configure middleware pipeline
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        
        if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId.ToString());
        }
    };
});

// Correlation ID middleware
app.Use(async (context, next) =>
{
    const string correlationIdHeader = "X-Correlation-Id";
    
    if (!context.Request.Headers.ContainsKey(correlationIdHeader))
    {
        context.Request.Headers.Append(correlationIdHeader, Guid.NewGuid().ToString());
    }
    
    var correlationId = context.Request.Headers[correlationIdHeader].ToString();
    context.Response.Headers.Append(correlationIdHeader, correlationId);
    
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});

app.UseCors();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Map YARP endpoints
app.MapReverseProxy();

// Health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        
        await context.Response.WriteAsJsonAsync(result);
    }
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("services"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        
        var result = new
        {
            status = report.Status.ToString(),
            services = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds
            })
        };
        
        await context.Response.WriteAsJsonAsync(result);
    }
});

// Root endpoint
app.MapGet("/", () => Results.Ok(new
{
    service = "API Gateway",
    version = "1.0.0",
    endpoints = new
    {
        catalog = "/api/catalog",
        orders = "/api/orders",
        health = "/health",
        ready = "/health/ready"
    }
}));

Log.Information("API Gateway starting...");
app.Run();
