# Stage de base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --system --gid 1001 appgroup \
    && useradd --system --uid 1001 --gid appgroup --shell /usr/sbin/nologin appuser
EXPOSE 8080
EXPOSE 8081

# Stage de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Mediconnet-Backend.csproj", "."]
RUN dotnet restore "./Mediconnet-Backend.csproj"
# Copie ciblee des sources (exclusion des fichiers sensibles via .dockerignore)
COPY Configuration/ ./Configuration/
COPY Controllers/ ./Controllers/
COPY Core/ ./Core/
COPY Data/ ./Data/
COPY DTOs/ ./DTOs/
COPY Helpers/ ./Helpers/
COPY Hubs/ ./Hubs/
COPY Infrastructure/ ./Infrastructure/
COPY Migrations/ ./Migrations/
COPY Properties/ ./Properties/
COPY Services/ ./Services/
COPY Validators/ ./Validators/
COPY Program.cs appsettings*.json ./
RUN dotnet build "./Mediconnet-Backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage de publication
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Mediconnet-Backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage final
FROM base AS final
WORKDIR /app
COPY --from=publish --chown=appuser:appgroup /app/publish .
RUN chmod -R 0500 /app
USER appuser
ENTRYPOINT ["dotnet", "Mediconnet-Backend.dll"]