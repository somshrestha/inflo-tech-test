
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Data.Interceptors;

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
            .AddDataAccess()
            .AddDomainServices();

        builder.Services.AddDbContext<DataContext>(options =>
                options.UseInMemoryDatabase("UserManagement.Data.DataContext")
                       .AddInterceptors(new AuditSaveChangesInterceptor()));

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
        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
