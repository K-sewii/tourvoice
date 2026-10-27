# TourVoice

Hệ thống thuyết minh du lịch đa ngôn ngữ cho **Quận 6, TP. Hồ Chí Minh**. Du khách quét mã QR tại điểm đến (hoặc mở web), chọn ngôn ngữ và nghe thuyết minh về địa điểm, món ăn, văn hoá. Quản trị viên quản lý nội dung và bản dịch.

Tài liệu nghiệp vụ (PRD, sơ đồ, slide trình bày) nằm trong [`docs/`](docs/).

## Tính năng

**Web cho du khách** (`client/`)
- 5 ngôn ngữ: Tiếng Việt, English, 中文, 한국어, 日本語. Ngôn ngữ nằm trên URL (`/{lang}/...`), tự nhận diện lần đầu và ghi nhớ.
- Trang chủ, Khám phá (lọc theo danh mục, tìm kiếm không dấu), Ẩm thực & Văn hoá, Bản đồ (Leaflet, tải lazy), chi tiết địa điểm / món ăn.
- Thuyết minh bằng Web Speech API của trình duyệt, ghi nhận lượt nghe (listen events).
- Lối vào từ QR: `/q/{slug}` → chuyển đến trang địa điểm theo ngôn ngữ của máy.
- Đăng ký / đăng nhập khách hàng: yêu thích, đã ghé thăm, lịch sử.
- Heartbeat "đang online" để quản trị viên theo dõi khách thời gian thực.

**API** (`server/`)
- Nội dung công khai, xác thực JWT, tài khoản khách hàng, quản trị địa điểm & bản dịch.
- Dịch máy qua MyMemory, có ghi log mức sử dụng; bản dịch cần duyệt trước khi công khai.
- Presence thời gian thực qua SignalR (`/hubs/presence`, chỉ Admin).
- Giới hạn tốc độ: 120 req/phút mỗi thiết bị, 1.200 req/phút mỗi IP, 10 req/phút cho đăng nhập/đăng ký.
- Lỗi trả về dạng RFC 7807 ProblemDetails kèm `traceId`.

> Dashboard quản trị (giao diện web) chưa được xây dựng; hiện mới có API quản trị.

## Công nghệ

| Phần | Công nghệ |
|---|---|
| Backend | .NET 10, ASP.NET Core Web API, EF Core 10 + MySQL (`MySql.EntityFrameworkCore`), SignalR, JWT HS256 |
| Frontend | React 19, TypeScript, Vite 8, React Router 7, i18next, Leaflet / react-leaflet |
| Database | MySQL 8.4 (utf8mb4) |
| Kiểm thử | xUnit, Moq, NetArchTest · Vitest, Testing Library |
| Triển khai | Docker Compose (db + api + client/nginx), GitHub Actions CI |

## Cấu trúc thư mục

```
tourvoice/
├── client/                  # Web du khách (React + Vite)
│   └── src/
│       ├── api/             # Gọi REST API
│       ├── auth/            # AuthContext (JWT)
│       ├── components/      # Header, PlaceCard, NarrationPlayer, ...
│       ├── hooks/           # useNarrationPlayer, usePresence, useGeolocation, ...
│       ├── i18n/locales/    # vi, en, zh, ko, ja
│       ├── lib/             # speech, geo, deviceId, languages, ...
│       ├── pages/           # Home, Explore, FoodCulture, Map, PlaceDetail, Auth, Account
│       └── styles/
├── server/
│   ├── src/
│   │   ├── TourVoice.API/   # Controllers, SignalR hub, rate limit, Program.cs
│   │   ├── TourVoice.BLL/   # Services, DTOs, dịch máy, khởi tạo DB
│   │   └── TourVoice.DAL/   # DbContext, Entities, Repositories, Migrations, Seeder
│   └── tests/               # BLL.Tests, API.Tests, ArchitectureTests
├── docs/                    # PRD, sơ đồ drawio, slide
├── docker-compose.yml
└── .env.example
```

Backend theo kiến trúc 3 lớp **API → BLL → DAL**: `Program.cs` chỉ tham chiếu BLL, BLL tự đăng ký DAL. Quy tắc phụ thuộc giữa các lớp được kiểm tra tự động trong `TourVoice.ArchitectureTests`.

## Chạy bằng Docker Compose

```bash
cp .env.example .env      # rồi sửa DB_PASSWORD, DB_ROOT_PASSWORD, JWT_KEY (>= 32 ký tự), MYMEMORY_EMAIL
docker compose up --build
```

| Dịch vụ | Địa chỉ |
|---|---|
| Web | http://localhost:5173 |
| API | http://localhost:5049 |
| Swagger UI | http://localhost:5049/swagger |
| Health check | http://localhost:5049/health |
| MySQL | localhost:3306 (chỉ dùng khi dev) |

Ở môi trường `Development`, API tự chạy migration và seed dữ liệu mẫu (ngôn ngữ, địa điểm, món ăn) cùng tài khoản quản trị:

- Email: `admin@tourvoice.local`
- Mật khẩu: `Admin@12345`

Đổi mật khẩu này trước khi triển khai thật.

## Chạy khi phát triển (không dùng Docker)

Yêu cầu: .NET SDK 10, Node.js 22, MySQL 8.4.

```bash
# Backend (http://localhost:5049) — cấu hình lưu bằng user-secrets, chỉ cần đặt một lần
API=server/src/TourVoice.API
dotnet user-secrets set "ConnectionStrings:TourVoice" "Server=localhost;Port=3306;Database=tourvoice;User=tv;Password=..." --project $API
dotnet user-secrets set "Jwt:Key" "<chuỗi ngẫu nhiên >= 32 ký tự>" --project $API
dotnet user-secrets set "Jwt:Issuer" "TourVoice" --project $API
dotnet user-secrets set "Jwt:Audience" "TourVoiceClient" --project $API
dotnet user-secrets set "MyMemory:Email" "team@example.com" --project $API
dotnet run --project $API --launch-profile http

# Frontend (http://localhost:5173) — mặc định gọi API ở localhost:5049
cd client && npm install && npm run dev
```

Thêm migration: `dotnet ef migrations add <Ten> --project src/TourVoice.DAL --startup-project src/TourVoice.API` (chạy trong `server/`).

## Kiểm thử

```bash
cd server && dotnet test                         # unit test + kiểm tra kiến trúc
cd client && npm run lint && npm test && npm run build
```

CI (`.github/workflows/ci.yml`) chạy đúng các bước trên cho mỗi pull request và push vào `main`.

## API

Tiền tố: `/api/v1`. Các endpoint công khai nhận `?lang=vi|en|zh|ko|ja` (mặc định `vi`).

| Nhóm | Endpoint | Quyền |
|---|---|---|
| Ngôn ngữ | `GET /languages` | Công khai |
| Địa điểm | `GET /places` · `GET /places/{slug}` · `GET /places/{slug}/foods` | Công khai |
| Khám phá | `GET /explore?category=&q=` (category: `Heritage`, `Market`, `FoodStreet`, `Park`, `food`) | Công khai |
| Bản đồ | `GET /map/markers` | Công khai |
| Lượt nghe | `POST /listen-events` | Công khai |
| Presence | `POST /presence/heartbeat` · `POST /presence/leave` | Công khai |
| Xác thực | `POST /auth/register` · `POST /auth/login` | Công khai |
| Tài khoản | `GET /me` · `GET/POST/DELETE /me/favorites/{placeId}` · `GET/POST /me/visits/{placeId}` · `GET /me/history` | Customer |
| Quản trị địa điểm | `GET/POST /admin/places` · `GET/PUT /admin/places/{id}` · `POST /admin/places/{id}/publish` · `POST /admin/places/{id}/hide` | Admin |
| Quản trị bản dịch | `POST /admin/places/{id}/translate` · `POST /admin/places/{id}/translations/{lang}/approve` | Admin |
| Presence | `GET /admin/presence` · SignalR `/hubs/presence` | Admin |

Đặc tả đầy đủ xem tại Swagger UI khi chạy ở môi trường Development.
