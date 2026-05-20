using System.Text.Json;
using System.Text.Json.Serialization;
using TestGen.Core.Generation;
using TestGen.Core.Services;
using TestGen.Core.Validation;
using TestGen.Core.Validation.Safety;
using TestGen.Core.Validation.Semantic;
using TestGen.Core.Validation.Syntactic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SyntheticTestGenerator", Version = "v1" });
});

builder.Services.AddSingleton<IProfileService, FileProfileService>();

builder.Services.AddSingleton<IScenarioValidator, LayeredScenarioValidator>();

builder.Services.AddSingleton<IValidationRule, EngineOffSpeedGtZeroRule>();
builder.Services.AddSingleton<IValidationRule, GearParkSpeedGtZeroRule>();
builder.Services.AddSingleton<IValidationRule, IgnitionOffEcuOnRule>();
builder.Services.AddSingleton<IValidationRule, TimingOverflowRule>();
builder.Services.AddSingleton<IValidationRule, AsilDMissingCriticalAssertionsRule>();
builder.Services.AddSingleton<IValidationRule, LockedParameterOverriddenRule>();

builder.Services.AddSingleton<GeneratorPipeline>();

builder.Services.AddKeyedSingleton<IGenerationService, CartesianScenarioGenerator>("cartesian");
builder.Services.AddKeyedSingleton<IGenerationService, PairwiseScenarioGenerator>("pairwise");

builder.Services.AddSingleton<IGenerationService, ScenarioGeneratorRouter>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    });
}

if (builder.Environment.IsProduction())
{
    // TODO: restrict to frontend URL before final deployment
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    });
}

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SyntheticTestGenerator v1"));

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseCors();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
