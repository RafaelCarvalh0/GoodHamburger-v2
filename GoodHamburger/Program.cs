using GoodHamburger;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

string currentDirectory = Directory.GetCurrentDirectory();
string dataDirectoryPath = Path.Combine(currentDirectory, "Database\\GoodHamburger.sqlite");
var connectionString = $"Data Source={dataDirectoryPath}";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IOrderManager, OrderManager>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Good Hamburger API",
        Description = "ASP.NET Core Web API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Good Hamburger",
            Email = "sac@goodhamburger.com",
            Url = new Uri("https://goodhamburger.com.br/"),
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
