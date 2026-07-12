FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY tic-tac-toe-api.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render assigns the listen port via $PORT at runtime; default to 8080 for local `docker run`.
EXPOSE 8080
CMD ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet tic-tac-toe-api.dll
