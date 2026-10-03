FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/frontend/CoffeeShop.WebRazorPages/CoffeeShop.WebRazorPages.csproj", "src/frontend/CoffeeShop.WebRazorPages/"]
RUN dotnet restore "src/frontend/CoffeeShop.WebRazorPages/CoffeeShop.WebRazorPages.csproj"
COPY . .
WORKDIR "/src/src/frontend/CoffeeShop.WebRazorPages"
RUN dotnet build "CoffeeShop.WebRazorPages.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoffeeShop.WebRazorPages.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoffeeShop.WebRazorPages.dll"]
