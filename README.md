# DataSeederTest

## VI - Hướng dẫn sử dụng

### 1) Mô tả

Ứng dụng console .NET để:

- Tạo/kiểm tra schema PostgreSQL qua EF Core
- Seed dữ liệu mẫu lớn (User/Product/Order/OrderDetail)
- Benchmark LINQ đọc toàn bộ dữ liệu và in thời gian theo milliseconds

### 2) Yêu cầu

- .NET SDK 10
- PostgreSQL đang chạy
- NuGet packages đã được cài trong project

### 3) Cấu hình kết nối database

Ứng dụng đọc chuỗi kết nối từ biến môi trường:

- `PG_CONNECTION_STRING`

Nếu không set, app dùng giá trị mặc định trong `Program.cs`.

Ví dụ (macOS/Linux):

```bash
export PG_CONNECTION_STRING="Host=localhost;Port=5432;Database=dataseeder_test;Username=postgres;Password=your_password"
```

Nếu dùng pgAdmin trong Docker, có thể cần host:

- `host.docker.internal`

### 4) Bật/tắt seed bằng true/false

Trong `Program.cs`:

```csharp
var runSeeder = false;
```

- `false`: không seed, chỉ benchmark
- `true`: chạy seed trước, sau đó benchmark

Thông số seed mặc định trong `Program.cs`:

- `userTarget = 10_000_000`
- `userBatchSize = 20_000`
- `productCount = 5_000`
- `orderCount = 100_000`

### 5) Chạy ứng dụng

Tại thư mục project:

```bash
dotnet restore
dotnet run --project DataSeederTest.csproj
```

### 6) Output benchmark

App in ra:

- Tong so dong doc duoc moi bang
- Thoi gian doc tung bang (ms)
- Tong thoi gian full read benchmark (ms)

### 7) Lưu ý

- Lần đầu seed 10 triệu users có thể tốn nhiều thời gian và I/O
- Tránh bật `runSeeder = true` trong mỗi lần test benchmark nếu đã có dữ liệu
- Lỗi `28P01` = sai username/password PostgreSQL

---

## EN - Usage Guide

### 1) Overview

This .NET console app is used to:

- Create/ensure PostgreSQL schema using EF Core
- Seed large sample data (User/Product/Order/OrderDetail)
- Benchmark LINQ full-table reads and print elapsed milliseconds

### 2) Requirements

- .NET SDK 10
- Running PostgreSQL instance
- NuGet packages already installed in the project

### 3) Database connection config

The app reads connection string from environment variable:

- `PG_CONNECTION_STRING`

If not set, it falls back to the default value in `Program.cs`.

Example (macOS/Linux):

```bash
export PG_CONNECTION_STRING="Host=localhost;Port=5432;Database=dataseeder_test;Username=postgres;Password=your_password"
```

If pgAdmin is running in Docker, you may need host:

- `host.docker.internal`

### 4) Enable/disable seeding with true/false

In `Program.cs`:

```csharp
var runSeeder = false;
```

- `false`: skip seeding, run benchmark only
- `true`: run seeding first, then benchmark

Default seed settings in `Program.cs`:

- `userTarget = 10_000_000`
- `userBatchSize = 20_000`
- `productCount = 5_000`
- `orderCount = 100_000`

### 5) Run the app

From project folder:

```bash
dotnet restore
dotnet run --project DataSeederTest.csproj
```

### 6) Benchmark output

The app prints:

- Number of rows read per table
- Per-table elapsed time (ms)
- Total full read benchmark time (ms)

### 7) Notes

- First run with 10 million users can take significant time and I/O
- Keep `runSeeder = false` for repeat benchmark runs after data is seeded
- `28P01` error means invalid PostgreSQL username/password
