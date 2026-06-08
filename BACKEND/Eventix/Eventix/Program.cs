using Eventix.Common.Constants.SystemData;
using Eventix.Common.Models;
using Eventix.Common.Settings;
using Eventix.Data;
using Eventix.Extensions;
using Eventix.Infrastructure.Email;
using Eventix.Modules.Auth.Interfaces;
using Eventix.Modules.Auth.Services;
using Eventix.Modules.CategoryModule.Interfaces;
using Eventix.Modules.CategoryModule.Services;
using Eventix.Modules.EventModule.Interfaces;
using Eventix.Modules.EventModule.Services;
using Eventix.Modules.OrganizerModule.Interfaces;
using Eventix.Modules.OrganizerModule.Services;
using Eventix.Modules.SeatModule.Interfaces;
using Eventix.Modules.SeatModule.Services;
using Eventix.Modules.UserModule.Interfaces;
using Eventix.Modules.UserModule.Service;
using Eventix.Modules.VenueModule.Interfaces;
using Eventix.Modules.VenueModule.Services;
using EventTicketingSystem.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Eventix
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add settings
            builder.Services.Configure<JwtSettings>(
                builder.Configuration.GetSection(JwtSettings.SectionName));
            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection(EmailSettings.SectionName));

            // Configure JWT Authentication
            var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>();

            if (jwtSettings != null && !string.IsNullOrWhiteSpace(jwtSettings.Key))
            {
                var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

                builder.Services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(options =>
                    {
                        options.RequireHttpsMetadata = false;
                        options.SaveToken = true;

                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(key),

                            ValidateIssuer = true,
                            ValidIssuer = jwtSettings.Issuer,

                            ValidateAudience = true,
                            ValidAudience = jwtSettings.Audience,

                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.Zero
                        };

                        options.Events = new JwtBearerEvents
                        {
                            OnAuthenticationFailed = context =>
                            {
                                var logger = context.HttpContext.RequestServices
                                    .GetRequiredService<ILogger<Program>>();

                                logger.LogError(context.Exception, "JWT authentication failed");

                                return Task.CompletedTask;
                            },

                            OnChallenge = async context =>
                            {
                                context.HandleResponse();

                                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                context.Response.ContentType = "application/json";

                                var response = new ApiResponseModel<object>(
                                    SystemError.UNAUTHORIZED,
                                    null);

                                await context.Response.WriteAsJsonAsync(response);
                            },

                            OnForbidden = async context =>
                            {
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                context.Response.ContentType = "application/json";

                                var response = new ApiResponseModel<object>(
                                    SystemError.ACCESS_DENIED,
                                    null);

                                await context.Response.WriteAsJsonAsync(response);
                            }
                        };
                    });

                builder.Services.AddAuthorization();
            }

            builder.Services.AddApplicationHealthChecks(builder.Configuration);

            // Configure Database
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DB")));

            // Register Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IVenueService, VenueService>();
            builder.Services.AddScoped<ISeatService, SeatService>();
            builder.Services.AddScoped<IOrganizerProfileService, OrganizerProfileService>();
            builder.Services.AddScoped<IEventService, EventService>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowNextApp", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseGlobalExceptionHandler();

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseCors("AllowNextApp");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.UseApplicationHealthChecks();

            app.Run();
        }
    }
}
