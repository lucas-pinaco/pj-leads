# Dockerfile corrigido baseado na estrutura real do projeto
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj file - ajustado para estrutura real
COPY ["api/Leads.API/Leads.API.csproj", "Leads.API/"]
RUN dotnet restore "Leads.API/Leads.API.csproj"

# Copy all source files
COPY api/Leads.API/ Leads.API/
WORKDIR "/src/Leads.API"
RUN dotnet build "Leads.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Leads.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Configurar para usar a porta do Railway
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Leads.API.dll"]