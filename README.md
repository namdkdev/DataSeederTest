# DataSeederTest

## VI - Hướng dẫn sử dụng

### 1) Mô tả

Ứng dụng console .NET để:

- Tạo/kiểm tra schema PostgreSQL qua EF Core
- Seed dữ liệu mẫu lớn (User/Product/Order/OrderDetail/ActivityLog)
- Benchmark LINQ và đo thời gian truy vấn theo milliseconds

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
var runSeeder = true;
```

- `false`: không seed, chỉ benchmark
- `true`: chạy seed trước, sau đó benchmark

Thông số seed mặc định nằm trong `SeedProgram.cs` (class `DataSeederRunner`):

- `userTarget = 10_000_000`
- `userBatchSize = 20_000`
- `productCount = 5_000`
- `productBatchSize = 5_000`
- `orderCount = 1_000_000`
- `orderBatchSize = 20_000`
- `activityLogTarget = orderCount * 5`
- `activityLogBatchSize = 20_000`

### 5) Lưu ý về schema/entity

- Tất cả khóa chính và khóa ngoại chính đã dùng `Guid`
- Không tạo index phụ, chỉ giữ khóa chính `Id`
- Bảng chính: `Users`, `Products`, `Orders`, `OrderDetails`, `ActivityLogs`

### 6) Chạy ứng dụng

Tại thư mục project:

```bash
dotnet run
```

### 7) Output benchmark

App in ra:

- Thời gian đọc users (ms)
- JSON preview của users (theo logic hiện tại trong `Program.cs`)

### 8) Lưu ý

- Lần đầu seed 10 triệu users có thể tốn nhiều thời gian và I/O
- Tránh bật `runSeeder = true` trong mỗi lần test benchmark nếu đã có dữ liệu
- Lỗi `28P01` = sai username/password PostgreSQL

---

## EN - Usage Guide

### 1) Overview

This .NET console app is used to:

- Create/ensure PostgreSQL schema using EF Core
- Seed large sample data (User/Product/Order/OrderDetail/ActivityLog)
- Benchmark LINQ queries and print elapsed milliseconds

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
var runSeeder = true;
```

- `false`: skip seeding, run benchmark only
- `true`: run seeding first, then benchmark

Default seed settings are now in `SeedProgram.cs` (`DataSeederRunner`):

- `userTarget = 10_000_000`
- `userBatchSize = 20_000`
- `productCount = 5_000`
- `productBatchSize = 5_000`
- `orderCount = 1_000_000`
- `orderBatchSize = 20_000`
- `activityLogTarget = orderCount * 5`
- `activityLogBatchSize = 20_000`

### 5) Schema/entity notes

- All primary and related key fields use `Guid`
- No secondary indexes are configured, only the primary key `Id`
- Main tables: `Users`, `Products`, `Orders`, `OrderDetails`, `ActivityLogs`

### 6) Run the app

From project folder:

```bash
dotnet run
```

### 7) Benchmark output

The app prints:

- Users query elapsed time (ms)
- Users JSON preview (current logic in `Program.cs`)

### 8) Notes

- First run with 10 million users can take significant time and I/O
- Keep `runSeeder = false` for repeat benchmark runs after data is seeded
- `28P01` error means invalid PostgreSQL username/password
