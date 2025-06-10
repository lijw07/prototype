using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Services;
using Prototype.Services.DataParser;
using Prototype.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Services (Dependency Injection)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SentinelContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Application Services
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddScoped<IEntityCreationFactoryService, EntityCreationFactoryService>();
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped(typeof(IEntitySaveService<>), typeof(EntitySaveService<>));

// Register Data Dump Parsers
builder.Services.AddScoped<DataDumpParserFactoryService>();
builder.Services.AddTransient<CsvDataDumpParserService>();
builder.Services.AddTransient<ExcelDataDumpParserService>();
builder.Services.AddTransient<JsonDataDumpParserService>();
builder.Services.AddTransient<XmlDataDumpParserService>();

var app = builder.Build();

// 2. Configure Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Swagger only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Prototype API V1");
    });
}

// 3. Configure Endpoints
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

// 4. Run Migrations at Startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SentinelContext>();
    db.Database.Migrate();
}

// 5. Run the Application
app.Run();