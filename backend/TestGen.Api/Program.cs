var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    });
}

var app = builder.Build();

// Serve static files from wwwroot/ (populated at Docker build time from frontend/)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SyntheticTestGenerator v1"));

if (app.Environment.IsDevelopment())
{
    app.UseCors();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
