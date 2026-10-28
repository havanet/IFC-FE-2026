FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY src/IFC.Facturacion.Api/IFC.Facturacion.Api.csproj src/IFC.Facturacion.Api/
COPY src/IFC.Facturacion.Application/IFC.Facturacion.Application.csproj src/IFC.Facturacion.Application/
COPY src/IFC.Facturacion.Domain/IFC.Facturacion.Domain.csproj src/IFC.Facturacion.Domain/
COPY src/IFC.Facturacion.Infrastructure/IFC.Facturacion.Infrastructure.csproj src/IFC.Facturacion.Infrastructure/
RUN dotnet restore src/IFC.Facturacion.Api/IFC.Facturacion.Api.csproj

COPY src/ src/
RUN dotnet publish src/IFC.Facturacion.Api/IFC.Facturacion.Api.csproj \
    --configuration Release --no-restore --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "IFC.Facturacion.Api.dll"]
