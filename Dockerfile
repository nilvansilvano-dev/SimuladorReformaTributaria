FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/Api.csproj .
RUN dotnet restore
COPY backend/ .
RUN dotnet publish Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-5000} ./Api"]
