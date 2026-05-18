# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["ElasticsearchArticlesApiNetcore.csproj", "./"]
RUN dotnet restore "ElasticsearchArticlesApiNetcore.csproj"

# Copy all source and build
COPY . .
RUN dotnet build "ElasticsearchArticlesApiNetcore.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ElasticsearchArticlesApiNetcore.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .

# Set non-root user (chiseled/distroless images provide a non-root user)
USER $APP_UID

# Expose HTTP and HTTPS ports
EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "ElasticsearchArticlesApiNetcore.dll"]
