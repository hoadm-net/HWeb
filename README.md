# Demo E-Commerce Website - ASP.NET Core MVC

## Giới thiệu dự án

Đây là một ứng dụng web bán hàng demo được xây dựng bằng ASP.NET Core MVC, dành cho việc học tập và giảng dạy sinh viên năm 2. Dự án sử dụng SQLite làm cơ sở dữ liệu, Bootstrap cho giao diện và jQuery cho tương tác client-side.

## Công nghệ sử dụng

- **Backend:** ASP.NET Core MVC 8.0
- **Database:** SQLite với Entity Framework Core
- **Frontend:** Bootstrap 5, jQuery
- **Authentication:** ASP.NET Core Identity
- **IDE:** JetBrains Rider (khuyến khích)

## Tính năng chính

### Admin Panel
- ✅ Đăng nhập/đăng xuất Admin
- ✅ Quản lý danh mục sản phẩm (CRUD)
- ✅ Quản lý sản phẩm (CRUD, upload ảnh)
- ✅ Quản lý tag sản phẩm
- ✅ Quản lý đơn hàng (xem, cập nhật trạng thái)
- ✅ Dashboard thống kê (doanh thu, số đơn hàng, top sản phẩm)

### Khách hàng
- ✅ Đăng ký/đăng nhập khách hàng
- ✅ Xem danh sách sản phẩm (có phân trang, lọc theo danh mục)
- ✅ Xem chi tiết sản phẩm
- ✅ Thêm vào giỏ hàng
- ✅ Quản lý giỏ hàng (xem, sửa, xóa)
- ✅ Đặt hàng và thanh toán đơn giản
- ✅ Tìm kiếm sản phẩm theo tên
- ✅ Đánh giá sản phẩm (rating 1-5 sao)
- ✅ Lịch sử đơn hàng
- ✅ Quản lý profile khách hàng
- ✅ Hiển thị sản phẩm liên quan

## Thứ tự thực hiện

### Phase 1: Setup dự án
1. Tạo project ASP.NET Core MVC
2. Cài đặt các NuGet packages cần thiết
3. Cấu hình SQLite và Entity Framework Core
4. Setup Bootstrap và jQuery

### Phase 2: Database và Models
1. Thiết kế Entity models (User, Product, Category, Order, etc.)
2. Cấu hình DbContext
3. Tạo và chạy migrations
4. Seed dữ liệu mẫu

### Phase 3: Authentication & Authorization
1. Cấu hình ASP.NET Core Identity
2. Tạo User roles (Admin, Customer)
3. Implement đăng ký/đăng nhập
4. Setup authorization policies

### Phase 4: Admin Panel
1. Tạo Admin Layout
2. Dashboard với thống kê cơ bản
3. CRUD Categories
4. CRUD Products (với upload ảnh)
5. CRUD Tags
6. Quản lý Orders

### Phase 5: Customer Frontend
1. Tạo Customer Layout
2. Trang Home với danh sách sản phẩm
3. Chi tiết sản phẩm
4. Tìm kiếm và lọc sản phẩm
5. Shopping Cart functionality

### Phase 6: Order Processing
1. Checkout process
2. Order history
3. Order status tracking
4. Review system

### Phase 7: Polish & Testing
1. Responsive design
2. Error handling
3. Data validation
4. Performance optimization

## Cài đặt và chạy

```bash
# Clone repository
git clone [repository-url]

# Navigate to project directory
cd ECommerceDemo

# Restore packages
dotnet restore

# Create and run migrations
dotnet ef migrations add InitialCreate
dotnet ef database update

# Run the application
dotnet run
```

## Cấu trúc thư mục

```
├── Controllers/
├── Models/
├── Views/
├── Data/
├── Services/
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── images/
├── Migrations/
└── Areas/
    └── Admin/
```

## Tài khoản mặc định

- **Admin:** admin@demo.com / Admin123!
- **Customer:** user@demo.com / User123!

## Ghi chú cho sinh viên

Dự án này được thiết kế để học các khái niệm:
- MVC Pattern
- Entity Framework Core
- Authentication & Authorization
- CRUD Operations
- File Upload
- Session Management
- Basic Statistics
