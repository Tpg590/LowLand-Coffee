FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/LowLandCoffee.Web/LowLandCoffee.Web.csproj", "src/LowLandCoffee.Web/"]
RUN dotnet restore "src/LowLandCoffee.Web/LowLandCoffee.Web.csproj"
COPY . .
WORKDIR "/src/src/LowLandCoffee.Web"
RUN dotnet build "LowLandCoffee.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LowLandCoffee.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LowLandCoffee.Web.dll"]
