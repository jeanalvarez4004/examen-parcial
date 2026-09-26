# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY IncidenciasApp/IncidenciasApp.csproj IncidenciasApp/
RUN dotnet restore IncidenciasApp/IncidenciasApp.csproj
COPY . .
RUN dotnet publish IncidenciasApp/IncidenciasApp.csproj -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Render inyecta $PORT: se expande aqui (no dentro de una variable de entorno).
CMD ["/bin/sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:$PORT dotnet IncidenciasApp.dll"]
