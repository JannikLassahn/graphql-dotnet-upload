# graphql-dotnet-upload

This repository contains an experimental implementation of the [GraphQL multipart request spec](https://github.com/jaydenseric/graphql-multipart-request-spec) based on ASP.NET Core.

## Test it
Take a look at the tests and the sample and run some of the curl requests the spec lists if you are curious.

### cURL
CMD:
```shell
curl localhost:54234/graphql ^
	-F operations="{ \"query\": \"mutation($file: Upload) { singleUpload(file: $file){ name }}\", \"variables\": { \"file\": null }}" ^
	-F map="{\"0\":[\"variables.file\"]}" ^
	-F 0=@a.txt
```
Bash:
```shell
curl localhost:54234/graphql \
  -F operations='{ "query": "mutation ($file: Upload!) { singleUpload(file: $file) { id } }", "variables": { "file": null } }' \
  -F map='{ "0": ["variables.file"] }' \
  -F 0=@a.txt
```

This middleware implementation **only** parses multipart requests. The sample app uses additional middleware that handles other cases (e.g. `POST` with `application/json`).


## Roadmap
- [ ] Make sure the implementation behaves according to the spec
- [ ] Add convenience extension methods for `ServiceCollection` and `ApplicationBuilder` that register the neccessary types
- [ ] Feature parity with the [reference implementation](https://github.com/graphql-dotnet/server)
- [ ] End to end sample with web based client