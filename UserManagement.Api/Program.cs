using Microsoft.EntityFrameworkCore;
using UserManagement.Api.Middleware;

namespace UserManagement.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services
            .AddDomainServices();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        builder.Services.AddDataAccess(connectionString);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowBlazor", policy =>
            {
                policy.WithOrigins("https://localhost:7063")
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowBlazor");
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
