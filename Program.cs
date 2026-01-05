using api.Interfaces;
using Data;
using api.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using api.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using api.Services;
using Supabase;
using api.Middleware;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Film API", Version = "v1" });

    // Enum'ları string olarak göster
    option.MapType<SortByOptions>(() => new OpenApiSchema
    {
        Type = "string",
        Enum = new List<IOpenApiAny>
        {
            new OpenApiString(SortByOptions.Name.ToString()),
            new OpenApiString(SortByOptions.Genre.ToString()),
            new OpenApiString(SortByOptions.ReleaseYear.ToString())
        }
    });

    // Güvenlik tanımları ekle
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});



builder.Services.AddControllers().AddNewtonsoftJson(options => {
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    options.SerializerSettings.Converters.Add(new StringEnumConverter());
});

builder.Services.AddDbContext<ApplicationDBContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Upstash Redis Cache Configuration
var redisUrl = builder.Configuration["Redis:Url"];
var redisToken = builder.Configuration["Redis:Token"];

if (!string.IsNullOrEmpty(redisUrl) && !string.IsNullOrEmpty(redisToken))
{
    // Upstash Redis connection string format
    var connectionString = $"{redisUrl.Replace("https://", "").Replace("http://", "")}:6379,password={redisToken},ssl=True,abortConnect=False";
    
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = connectionString;
        options.InstanceName = "WatchHub_";
    });
}
else
{
    // Fallback to in-memory cache if Redis is not configured
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

// Supabase Configuration
var supabaseUrl = builder.Configuration["Supabase:Url"] ?? "https://nevoszkwoejobfokkbjt.supabase.co";
var supabaseKey = builder.Configuration["Supabase:ServiceRole"];
var supabaseJwtSecret = builder.Configuration["Supabase:JwtSecret"];

if (!string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey))
{
    builder.Services.AddScoped<Client>(_ =>
        new Client(
            supabaseUrl,
            supabaseKey,
            new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            }
        )
    );
}

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDBContext>();

// JWT Authentication Configuration - Supabase ES256 with manual JWKS fetch
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme =
    options.DefaultChallengeScheme =
    options.DefaultScheme =
    options.DefaultSignInScheme =
    options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer(options =>{
    // Disable default Microsoft JWT claim mapping
    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
    
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    
    options.TokenValidationParameters = new TokenValidationParameters{
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidIssuer = $"{supabaseUrl}/auth/v1",
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        // Keys will be resolved dynamically from JWKS endpoint
        IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
        {
            try
            {
                // Fetch JWKS from Supabase
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                var jwksJson = httpClient.GetStringAsync($"{supabaseUrl}/auth/v1/.well-known/jwks.json").Result;
                
                var jwks = new JsonWebKeySet(jwksJson);
                return jwks.GetSigningKeys();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching JWKS: {ex.Message}");
                return new List<SecurityKey>();
            }
        }
    };
    
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            Console.WriteLine($"Authentication failed: {context.Exception.GetType().Name}");
            Console.WriteLine($"Message: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("✅ Token validated successfully!");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddScoped<IFilmRepository, FilmRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<ICommentLikePortfolioRepository, CommentLikePortfolioRepository>();

// Configure HttpClient for TmdbService with proper settings
builder.Services.AddHttpClient<ITmdbService, TmdbService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "WatchHub/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    UseProxy = false, // Bypass proxy
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
});

var app = builder.Build();

// Add CORS for frontend
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Your API v1"));
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();