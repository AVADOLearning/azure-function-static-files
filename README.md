# Azure Function for serving static files out of blob containers

Serve static files from all blob containers within a given storage account to clients over HTTP, respecting blobs' content types. Useful if you need to serve static files with App Service authentication.

---

## How it works

A request for the URL `/container/path` will yield the `path` object in the blob container `container`. If `path` were a nested container with an `index.html` object within we'd redirect to `/container/path/` and serve `index.html`. Note that all objects in all containers in the configured storage account will be accessible with no authorisation checks -- be sure that the data you're serving is intended to be public.

## Why not use...

* **Static websites on Azure Storage?** They don't provide for authentication, and doing "clever" routing via a VPN is too flakey.
* **Azure CDN?** We want to ensure that users accessing these files are employed by us.

## Requirements

* A dedicated storage account. Don't share a storage account between the Function App and your data as Azure persists your secrets to the Function App storage and these will be accessible to clients via the Function.
* If you're bringing your own domain, one of the D series plans. Consumption and shared plans don't allow for custom SSL bindings for your own certificates and will cause mismatched certificate common name errors. Microsoft ♥s Money.

## Running it

1. Create a new Azure Functions App using the .NET Core stack and Windows OS. The v3 host isn't yet available on Linux.
2. [Set the _Runtime version_ of the app to `~3`](https://docs.microsoft.com/en-us/azure/azure-functions/set-runtime-version).
3. Run `func azure functionapp publish <FunctionAppName>` from `/AzureFunctionStaticFiles` to build the app and publish it.
4. Add the following _Application settings_:
    * `AccountConnectionString` must contain a connection string for your storage account.
    * `IndexName` sets the name of the default index page (Apache's `DirectoryIndex`, nginx's `index`).
    * `HostName` sets the name that should be used in redirects  (optional; recommended if you're bringing your own domain or using a CDN).
5. Configure a custom domain and SSL binding. Note that you'll need to bring your own certificate in PKCS12 (`*.pfx`) format. To ensure redirect paths are correct, alter `HostName`.
6. Enable App Service Authentication with the Azure AD provider and make sure the default action for unauthenticated requests is to redirect users to it.

## Hacking

### Visual Studio

Grab a copy of Visual Studio 2019 and install the following workloads:

* Azure development
* .NET Core cross-platform development

Now continue on to the steps below.

### Everyone

You'll need a local [Node.js](https://nodejs.org/) and [Yarn](https://yarnpkg.com/) installation to run the Azure Functions SDK. From the root of the repository, install the Azure Functions SDK:

```console
yarn install
```

It's easiest to work with a local storage account running under the Azure Storage Emulator. Start the emulator by launching "Microsoft Azure Storage Emulator - v5.10" from the Start Menu. You can stop the emulator from the taskbar icon or by running the following in the resulting Command Prompt window:

```console
.\AzureStorageEmulator.exe stop
```

You can then use the Azure SDK:

```console
cd AzureFunctionStaticFiles/
yarn run func start
```

## Testing

### In Visual Studio

Open the Visual Studio Test Explorer and click Run.

### From the console

```console
dotnet test
```
