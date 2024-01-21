using FileUploadSample;
using GraphQL;
using GraphQL.Types;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<UploadRepository>();

builder.Services.AddGraphQL(builder => builder
    .AddSchema<SampleSchema>()
    .AddGraphTypes()
    .AddGraphQLUpload()
    .AddErrorInfoProvider(opt => opt.ExposeExceptionDetails = true)
    .AddSystemTextJson());

builder.Services.AddCors();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

app.UseCors(b => b
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// register the middleware
app.UseGraphQLUpload();
app.UseGraphQLPlayground("/");

await app.RunAsync();
