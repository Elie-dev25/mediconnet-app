# Stage de base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Mediconnet-Backend.csproj", "."]
RUN dotnet restore "./Mediconnet-Backend.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Mediconnet-Backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage de publication
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Mediconnet-Backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mediconnet-Backend.dll"]