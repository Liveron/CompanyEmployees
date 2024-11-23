﻿using Asp.Versioning;
using AspNetCoreRateLimit;
using Entities.ConfigurationModels;
using Entities.Models;
using Marvin.Cache.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Service;
using Service.Contracts;
using System.Text;

namespace CompanyEmployees.Extensions;

public static class ServiceExtensions
{
    public static IMvcBuilder AddCustomCSVFormatter(this IMvcBuilder builder)
    {
        return builder.AddMvcOptions(config => config.OutputFormatters.Add(new CsvOutputFormatter()));
    }

    public static void ConfigureContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddNpgsql<RepositoryContext>(configuration.GetConnectionString("postgreSql"));
    }

    public static void ConfigureServiceManager(this IServiceCollection services)
    {
        services.AddScoped<IServiceManager, ServiceManager>();
    }

    public static void ConfigureRepositoryManager(this IServiceCollection services)
    {
        services.AddScoped<IRepositoryManager, RepositoryManager>();
    }

    public static void ConfigureLoggerService(this IServiceCollection services)
    {
        services.AddSingleton<ILoggerManager, LoggerManager>(); 
    }

    public static void ConfigureCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder =>
            builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("X-Pagination"));
        });
    }

    public static void ConfigureIISIntegration(this IServiceCollection services)
    {
        services.Configure<IISOptions>(options => { });
    }

    public static void ConfigureVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(opt =>
        {
            opt.ReportApiVersions = true;
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.ApiVersionReader = new HeaderApiVersionReader("api-version");
        }).AddMvc();
    }

    public static void ConfigureResponseCaching(this IServiceCollection services)
    {
        services.AddResponseCaching();
    }

    public static void AddCustomMediaTypes(this IServiceCollection services)
    {
        services.Configure<MvcOptions>(config =>
        {
            SystemTextJsonOutputFormatter? jsonFormatter = config.OutputFormatters
            .OfType<SystemTextJsonOutputFormatter>()?.FirstOrDefault();

            jsonFormatter?.SupportedMediaTypes.Add("application/vnd.codemaze.hateoas+json");
            jsonFormatter?.SupportedMediaTypes.Add("application/vnd.codemaze.apiroot+json");

            XmlDataContractSerializerOutputFormatter? xmlFormatter = config.OutputFormatters
            .OfType<XmlDataContractSerializerOutputFormatter>()?.FirstOrDefault();

            xmlFormatter?.SupportedMediaTypes.Add("application/vnd.codemaze.hateoas+xml");
            xmlFormatter?.SupportedMediaTypes.Add("application/vnd.codemaze.apiroot+xml");
        });
    }

    public static void ConfigureHttpCacheHeaders(this IServiceCollection services)
    {
        services.AddHttpCacheHeaders(expirationOpt =>
        {
            expirationOpt.MaxAge = 65;
            expirationOpt.CacheLocation = CacheLocation.Private;
        }, validationOpt =>
        {
            validationOpt.MustRevalidate = true;
        });
    }

    public static void ConfigureRateLimitingOptions(this IServiceCollection services)
    {
        List<RateLimitRule> rateLimitRules =
            [
                new RateLimitRule
                {
                    Endpoint = "*",
                    Limit = 30,
                    Period = "5m"
                }
            ];

        services.Configure<IpRateLimitOptions>(opt => opt.GeneralRules = rateLimitRules);
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
    }

    public static void ConfigureIdentity(this IServiceCollection services)
    {
        services.AddIdentity<User, IdentityRole>(o =>
        {
            o.Password.RequireDigit = true;
            o.Password.RequireLowercase = false;
            o.Password.RequireUppercase = false;
            o.Password.RequireNonAlphanumeric = false;
            o.Password.RequiredLength = 10;
            o.User.RequireUniqueEmail = true;
        }).AddEntityFrameworkStores<RepositoryContext>()
        .AddDefaultTokenProviders();
    }

    public static void ConfigureJWT(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtConfiguration = new JwtConfiguration();
        configuration.Bind(jwtConfiguration.Section, jwtConfiguration);

        string? secretKey = Environment.GetEnvironmentVariable("SECRET");

        services.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtConfiguration.ValidIssuer,
                ValidAudience = jwtConfiguration.ValidAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            };
        });
    }

    public static void AddJwtConfiguration(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtConfiguration>(configuration.GetSection("JwtSettings"));
    }

    public static void ConfigureSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(s =>
        {
            s.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "Code Maze API", 
                Version = "v1",
                Description = "CompanyEmployees API by CodeMaze",
                TermsOfService = new Uri("https://example.com/terms"),
                Contact = new OpenApiContact
                {
                    Name = "John Doe",
                    Email = "John.Doe@gmail.com",
                    Url = new Uri("https://twitter.com/johndoe"),
                },
                License = new OpenApiLicense
                {
                    Name = "CompanyEmployees API LICX",
                    Url = new Uri("https://example.com/license"),
                }
            });
            s.SwaggerDoc("v2", new OpenApiInfo { Title = "Code Maze Api", Version = "v2" });

            s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Place to add JWT with Bearer",
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
            });

            s.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                        Name = "Bearer",
                    },
                    new List<string>()
                }
            });
        });
    }
}