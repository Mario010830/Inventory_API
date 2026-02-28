# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["APICore.API/APICore.API.csproj", "APICore.API/"]
COPY ["APICore.Common/APICore.Common.csproj", "APICore.Common/"]
COPY ["APICore.Data/APICore.Data.csproj", "APICore.Data/"]
COPY ["APICore.Services/APICore.Services.csproj", "APICore.Services/"]

RUN dotnet restore "APICore.API/APICore.API.csproj"
COPY . .
RUN dotnet publish "APICore.API/APICore.API.csproj" -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Render inyecta PORT; la API ya usa Environment.GetEnvironmentVariable("PORT")
EXPOSE 5000

ENTRYPOINT ["dotnet", "APICore.API.dll"]
