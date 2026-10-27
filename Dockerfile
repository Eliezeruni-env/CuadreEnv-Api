# 1. Base SDK para restaurar y compilar .NET 10
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivos de proyecto por capas para cache de Docker
COPY ["crud-onion/crud-onion.csproj", "crud-onion/"]
COPY ["BusinessLogic/Onion.BusinessLogic.csproj", "BusinessLogic/"]
COPY ["DataAccess/Onion.DataAccess.csproj", "DataAccess/"]
COPY ["Domain/Onion.Domain.csproj", "Domain/"]
COPY ["Onion.Common/Onion.Common.csproj", "Onion.Common/"]

RUN dotnet restore "crud-onion/crud-onion.csproj"

# Copiar código fuente completo y publicar
COPY . .
WORKDIR "/src/crud-onion"
RUN dotnet publish "crud-onion.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime ligero oficial ASP.NET Core 10
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Heroku inyecta la variable $PORT dinámicamente. ASPNETCORE_URLS le indica a Kestrel el puerto exacto
ENV ASPNETCORE_URLS=http://+:$PORT

# Comando de arranque
ENTRYPOINT ["dotnet", "crud-onion.dll"]
