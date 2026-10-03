FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/backend/services/Engagement.API/Engagement.API.csproj", "src/backend/services/Engagement.API/"]
RUN dotnet restore "src/backend/services/Engagement.API/Engagement.API.csproj"
COPY . .
WORKDIR "/src/src/backend/services/Engagement.API"
RUN dotnet build "Engagement.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Engagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Engagement.API.dll"]
