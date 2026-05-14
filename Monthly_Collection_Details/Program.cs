using Monthly_Collection_Details.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

// Services
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("ConnectionStrings")
);

builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<PdfReportService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<OutstandingCustomerService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<Service1>();
builder.Services.AddScoped<Service2>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

var app = builder.Build();


app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReact");
app.MapControllers();

app.Run();