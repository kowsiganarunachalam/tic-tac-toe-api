# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY tic-tac-toe-api.csproj ./
RUN dotnet restore

# Copy source code
COPY . .

# Publish application
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Render injects the PORT environment variable.
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "tic-tac-toe-api.dll"]