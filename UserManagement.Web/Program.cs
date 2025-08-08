using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Data;
using UserManagement.Web.Extensions;
using UserManagement.Web.Mapper;
using Westwind.AspNetCore.Markdown;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddDataAccess()
    .AddDomainServices()
    .AddHelperServices()
    .AddMarkdown()
    .AddControllersWithViews();

builder.Services.AddDbContext<DataContext>(options =>
    options.UseInMemoryDatabase("UserManagement.Data.DataContext"));
builder.Services.AddAutoMapper(config =>
    config.AddProfile<MappingProfile>(), typeof(Program));

var app = builder.Build();

app.UseMarkdown();

app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();
