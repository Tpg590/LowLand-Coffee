FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/LowLandCoffee.ApiGateway/LowLandCoffee.ApiGateway.csproj", "src/LowLandCoffee.ApiGateway/"]
RUN dotnet restore "src/LowLandCoffee.ApiGateway/LowLandCoffee.ApiGateway.csproj"
COPY . .
WORKDIR "/src/src/LowLandCoffee.ApiGateway"
RUN dotnet build "LowLandCoffee.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LowLandCoffee.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LowLandCoffee.ApiGateway.dll"]
