# CoffeeHRM

GitHub la noi luu source code. Neu muon "chay duoc" thi ban can them cach khoi dong app va database, khong chi moi push code.

Repo nay da co:

- ASP.NET Core 8 backend
- React/Vite frontend
- EF Core migrations
- DbSeeder tao tai khoan mac dinh
- Health check endpoint: `/health`
- Dockerfile va docker-compose de chay ca app + SQL Server

## Chay bang Docker

Yeu cau:

- Docker Desktop
- Bat Linux containers

Lenh chay:

```bash
docker compose up --build
```

Sau khi chay:

- Ung dung: `http://localhost:8080`
- Health check: `http://localhost:8080/health`
- SQL Server: `localhost,1433`
- Tai khoan mac dinh: `admin`
- Mat khau mac dinh: `admin123`
- Mat khau SQL trong `docker-compose.yml` chi dung cho local/demo, khong nen giu nguyen khi deploy that

Backend se tu dong:

- tao database neu chua co
- chay EF Core migrations
- seed du lieu mac dinh

## Chay thu cong khong dung Docker

1. Sua connection string trong `NguyenDacQuan_2123110483/appsettings.json` hoac set bien moi truong `ConnectionStrings__DefaultConnection`.
   Co the copy `.env.example` thanh `.env` neu ban muon quan ly bien moi truong local.
2. Chay backend:

```bash
dotnet run --project NguyenDacQuan_2123110483/NguyenDacQuan_2123110483.csproj
```

3. Chay frontend:

```bash
cd "frontend coffee demo"
npm install
npm run dev
```

Frontend dev mac dinh proxy API sang `https://localhost:7060`.

## Nhung gi nen dua len GitHub cho giang vien

- Source code backend/frontend
- Thu muc `Migrations`
- `docker-compose.yml`
- `Dockerfile`
- `.env.example`
- README huong dan chay

Khong nen dua len GitHub:

- file `.env` that
- thong tin SQL that
- publish profile
- file log tam

## Neu giang vien hoi "deploy online len GitHub"

Can hieu dung la:

- `GitHub`: noi luu source code
- `Deploy online`: noi chay app that su

Huong dung de nop bai:

- Push source code len GitHub
- Dung Docker de chung minh repo clone ve la chay duoc
- Neu can link online, deploy container nay len Azure App Service, Render, Railway hoac VPS
- Khi deploy online, dat lai `ConnectionStrings__DefaultConnection` tro toi SQL Server/Azure SQL that

## Kiem thu

```bash
dotnet test NguyenDacQuan_2123110483.sln
```
