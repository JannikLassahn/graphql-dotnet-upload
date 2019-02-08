# graphql-dotnet-upload

[![Build Status](https://dev.azure.com/lassahn/graphql-dotnet-upload/_apis/build/status/JannikLassahn.graphql-dotnet-upload?branchName=master)](https://dev.azure.com/lassahn/graphql-dotnet-upload/_build/latest?definitionId=1&branchName=master)
[![NuGet](https://img.shields.io/nuget/v/GraphQL.Upload.AspNetCore.svg?style=flat)](https://www.nuget.org/packages/GraphQL.Upload.AspNetCore/)

This repository contains an experimental implementation of the [GraphQL multipart request spec](https://github.com/jaydenseric/graphql-multipart-request-spec) based on ASP.NET Core.

## Installation
You can install the latest version via [NuGet](https://www.nuget.org/packages/GraphQL.Upload.AspNetCore/).
```
PM> Install-Package GraphQL.Upload.AspNetCore
```

## Usage

Register the middleware in your Startup.cs (in this case we're also using [graphql-dotnet/server](https://github.com/graphql-dotnet/server)).
```csharp
public void ConfigureServices(IServiceCollection services)
{
  services.AddSingleton<MySchema>()
          .AddGraphQLUpload()
          .AddGraphQL();
}

public void Configure(IApplicationBuilder app)
{
  app.UseGraphQLUpload<MySchema>()
     .UseGraphQL<MySchema>();
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

This middleware implementation **only** parses multipart requests. The sample app uses additional middleware that handles other cases (e.g. `POST` with `application/json`).


## Roadmap
- [ ] Make sure the implementation behaves according to the spec
- [x] Add convenience extension methods for `ServiceCollection` and `ApplicationBuilder` that register the neccessary types
- [ ] Feature parity with the [reference implementation](https://github.com/graphql-dotnet/server)
- [ ] End to end sample with web based client
