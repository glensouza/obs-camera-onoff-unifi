FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["UniFiCameraControl.csproj", "./"]
RUN dotnet restore "UniFiCameraControl.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "UniFiCameraControl.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UniFiCameraControl.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0
WORKDIR /home/site/wwwroot

COPY --from=publish /app/publish .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
