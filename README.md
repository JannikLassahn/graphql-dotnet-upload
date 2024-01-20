# graphql-dotnet-upload

[![Build Status](https://github.com/JannikLassahn/graphql-dotnet-upload/actions/workflows/ci.yml/badge.svg)](https://github.com/JannikLassahn/graphql-dotnet-upload/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/GraphQL.Upload.AspNetCore.svg?style=flat)](https://www.nuget.org/packages/GraphQL.Upload.AspNetCore/)

This repository contains an experimental implementation of the [GraphQL multipart request spec](https://github.com/jaydenseric/graphql-multipart-request-spec) based on ASP.NET Core.

## Installation
You can install the latest version via [NuGet](https://www.nuget.org/packages/GraphQL.Upload.AspNetCore/).
```
PM> Install-Package GraphQL.Upload.AspNetCore
```
Preview versions from the develop branch are available via [GitHub Packages](https://github.com/JannikLassahn/graphql-dotnet-upload/packages).

## Usage

Register the middleware in your Startup.cs.

This middleware inherits from `GraphQLHttpMiddleware` and as such supports all of the base functionality provied by `GraphQL.Server.Transports.AspNetCore`.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddGraphQL(b => b
        .AddSchema<MySchema>()
        .AddSystemTextJson()
        .AddGraphQLUpload());
}

public void Configure(IApplicationBuilder app)
{
    app.UseGraphQLUpload<MySchema>();
}
```

Use the upload scalar in your resolvers. Files are exposed as `IFormFile`. 
```csharp
Field<StringGraphType>(
    "singleUpload",
    arguments: new QueryArguments(
        new QueryArgument<UploadGraphType> { Name = "file" }),
    resolve: context =>
    {
        var file = context.GetArgument<IFormFile>("file");
        return file.FileName;
    });
```

## Testing
Take a look at the tests and the sample and run some of the cURL requests the spec lists if you are curious.

CMD:
```shell
curl localhost:54234/graphql ^
	-F operations="{ \"query\": \"mutation ($file: Upload!) { singleUpload(file: $file) { name } }\", \"variables\": { \"file\": null } }" ^
	-F map="{ \"0\": [\"variables.file\"] }" ^
	-F 0=@a.txt
```
Bash:
```shell
curl localhost:54234/graphql \
	-F operations='{ "query": "mutation ($file: Upload!) { singleUpload(file: $file) { name } }", "variables": { "file": null } }' \
	-F map='{ "0": ["variables.file"] }' \
	-F 0=@a.txt
```
