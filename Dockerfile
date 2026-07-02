FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CardLedger.slnx ./
COPY global.json ./
COPY Directory.Build.props ./
COPY src/CardLedger.Domain/CardLedger.Domain.csproj src/CardLedger.Domain/
COPY src/CardLedger.Application/CardLedger.Application.csproj src/CardLedger.Application/
COPY src/CardLedger.Infrastructure/CardLedger.Infrastructure.csproj src/CardLedger.Infrastructure/
COPY src/CardLedger.Api/CardLedger.Api.csproj src/CardLedger.Api/

RUN dotnet restore src/CardLedger.Api/CardLedger.Api.csproj

COPY src/ src/
RUN dotnet publish src/CardLedger.Api/CardLedger.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CardLedger.Api.dll"]
