# 10-cli-instructions.md - Cẩm Nang Lệnh CLI Thường Dùng (Cheatsheet)

Tài liệu tổng hợp đầy đủ các lệnh .NET CLI, EF Core, Docker Compose và Git dành cho các thành viên trong nhóm phát triển dự án **LowLand Coffee Microservices**.

---

## 📑 Mục Lục
1. [Khởi Tạo Dự Án & Quản Lý Solution](#1-khởi-tạo-dự-án--quản-lý-solution)
2. [Cài Đặt & Quản Lý Thư Viện (NuGet Packages)](#2-cài-đặt--quản-lý-thư-viện-nuget-packages)
3. [Entity Framework Core (Migrations & Database)](#3-entity-framework-core-migrations--database)
4. [Build, Chạy Thử & Hot Reload (Debug)](#4-build-chạy-thử--hot-reload-debug)
5. [Docker & Docker Compose](#5-docker--docker-compose)
6. [Lệnh Git Phối Hợp Nhóm (Git Flow)](#6-lệnh-git-phối-hợp-nhóm-git-flow)

---

## 1. Khởi Tạo Dự Án & Quản Lý Solution

> Chạy từ thư mục gốc của repository: `LowLand-Coffee/`

### 🔹 Khởi tạo file Solution Backend:
```bash
dotnet new sln -n CoffeeShop.backend -o src/backend
```

### 🔹 Khởi tạo Web API Service (.NET 8):
```bash
# Cú pháp tổng quát:
dotnet new webapi -n <TênProject> -o src/backend/services/<TênProject> --framework net8.0 --use-controllers

# 1. Khởi tạo Auth.API (Khang):
dotnet new webapi -n Auth.API -o src/backend/services/Auth.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Auth.API/Auth.API.csproj

# 2. Khởi tạo Product.API (Phú):
dotnet new webapi -n Product.API -o src/backend/services/Product.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Product.API/Product.API.csproj

# 3. Khởi tạo Order.API (Tuấn):
dotnet new webapi -n Order.API -o src/backend/services/Order.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Order.API/Order.API.csproj

# 4. Khởi tạo Engagement.API (Trí):
dotnet new webapi -n Engagement.API -o src/backend/services/Engagement.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Engagement.API/Engagement.API.csproj

# 5. Khởi tạo Notification.API (Thành):
dotnet new webapi -n Notification.API -o src/backend/services/Notification.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Notification.API/Notification.API.csproj
```

### 🔹 Khởi tạo API Gateway (YARP):
```bash
dotnet new web -n ApiGateway -o src/backend/gateways/ApiGateway --framework net8.0
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/gateways/ApiGateway/ApiGateway.csproj
```

### 🔹 Khởi tạo Web Frontend (Razor Pages):
```bash
# 1. Tạo file Solution Frontend:
dotnet new sln -n CoffeeShop.frontend -o src/frontend

# 2. Tạo project Web Razor Pages:
dotnet new webapp -n CoffeeShop.WebRazorPages -o src/frontend/CoffeeShop.WebRazorPages --framework net8.0

# 3. Thêm project vào Solution Frontend:
dotnet sln src/frontend/CoffeeShop.frontend.sln add src/frontend/CoffeeShop.WebRazorPages/CoffeeShop.WebRazorPages.csproj
```

---

## 2. Cài Đặt & Quản Lý Thư Viện (NuGet Packages)

### 🔹 Cài các gói cho Backend Web API:
```bash
# 1. Entity Framework Core & SQL Server:
dotnet add src/backend/services/product.api package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/backend/services/product.api package Microsoft.EntityFrameworkCore.Tools
dotnet add src/backend/services/product.api package Microsoft.EntityFrameworkCore.Design

# 2. JWT Authentication (cho Auth.API & ApiGateway):
dotnet add src/backend/services/auth.api package Microsoft.AspNetCore.Authentication.JwtBearer

# 3. YARP Reverse Proxy (cho ApiGateway):
dotnet add src/backend/gateways/ApiGateway package Yarp.ReverseProxy

# 4. SignalR & Swagger:
dotnet add src/backend/services/product.api package Swashbuckle.AspNetCore
```

---

## 3. Entity Framework Core (Migrations & Database)

```bash
# 1. Tạo Migration mới:
dotnet ef migrations add <TenMigration> --project src/backend/services/product.api --startup-project src/backend/services/product.api

# 2. Cập nhật schema vào SQL Server:
dotnet ef database update --project src/backend/services/product.api --startup-project src/backend/services/product.api
```

---

## 4. Build, Chạy Thử & Hot Reload (Debug)

```bash
# Chạy Web Frontend:
dotnet run --project src/frontend/CoffeeShop.WebRazorPages/CoffeeShop.WebRazorPages.csproj

# Chạy ApiGateway:
dotnet run --project src/backend/gateways/ApiGateway/ApiGateway.csproj

# Chạy Auth.API:
dotnet run --project src/backend/services/auth.api/Auth.API.csproj

# Chạy Hot Reload:
dotnet watch --project src/frontend/CoffeeShop.WebRazorPages/CoffeeShop.WebRazorPages.csproj

# Build toàn bộ Backend:
dotnet build src/backend/CoffeeShop.backend.sln
```

---

## 5. Docker & Docker Compose

```bash
# Khởi động toàn bộ container chạy ngầm:
docker compose up -d

# Build lại và khởi chạy:
docker compose up --build -d

# Xem log:
docker compose logs -f

# Tắt container:
docker compose down
```

---

## 6. Lệnh Git Phối Hợp Nhóm (Git Flow)

```bash
# 1. Lấy code mới nhất:
git checkout main
git pull origin main

# 2. Tạo nhánh làm tính năng:
git checkout -b feature/<ten-tinh-nang>

# 3. Commit & Push:
git add .
git commit -m "feat(auth): hoan thanh dang nhap va jwt token"
git push origin feature/<ten-tinh-nang>
```
