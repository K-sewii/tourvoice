# TourVoice — Thuyết Minh Đa Ngôn Ngữ Quận 6

Hệ thống web ứng dụng ưu tiên điện thoại: khách quét QR tại điểm đến, chọn ngôn ngữ (5 ngón ngữ) và nghe thuyết minh về địa điểm, món ăn, văn hóa. Quản trị viên quản lý nội dung và bản dịch trên dashboard.

**[Tài liệu nghiệp vụ (PRD, sơ đồ, slide trình bày) nằm trong `docs/`](docs/)**

---

## Tính năng

### Web cho du khách (`client/`)

- **5 ngôn ngữ**: Tiếng Việt, English, 中文, 한국어, 日本語. Ngôn ngữ nằm trên URL (`/{lang}/...`), tự nhận diện lần đầu và ghi nhớ.
- **Trang chủ**, **Khám phá** (lọc theo danh mục, tìm kiếm không dấu), **Am thực & Văn hóa**, **Bản đồ** (Leaflet, tải lazy), **chi tiết địa điểm / món ăn**.
- **Thuyết minh bằng Web Speech API** của trình duyệt, ghi nhận lượt nghe (listen events).
- **Lối vào từ QR**: `/q/{slug}` → chuyển đến trang địa điểm theo ngôn ngữ của máy.
- **Đăng ký / đăng nhập khách hàng**: thích thú, đã ghé thăm, lịch sử.
- **Heartbeat "đang online"**: để quản trị viên theo dõi khách thời gian thực qua SignalR.
- **Tìm kiếm địa điểm** trên bản đồ.
- **Xem qua QR mã**: QR mã tại điểm chứa `/q/{slug}`, khách quét để nghe bài thuyết minh ngay.

### API (`server/`)

- Nội dung công khai, xác thực JWT, tài khoản khách hàng, quản trị địa điểm & bản dịch.
- **Dịch máy qua MyMemory**, có ghi log mức sử dụng; bản dịch cần duyệt trước khi công khai.
- **Presence thời gian thực qua SignalR** (`/hubs/presence`, chỉ Admin).
- **Giới hạn tốc độ**: 120 req/phút mỗi thiết bị, 1.200 req/phút mỗi IP, 10 req/phút cho đăng nhập/đăng ký.
- **Lỗi trả về dạng RFC 7807 ProblemDetails** kém `traceId`.
- **Hạn chế MyMemory**: 50.000 ký tự/ngày (cùng 1 email), chặn ở 45.000 ký tự/ngày (BR-06).

### Dashboard quản trị (đang develop)

- Quản trị địa điểm: thêm/sửa/xóa/batch publish.
- Quản trị bản dịch: duyệt/dịch máy/setting.
- Báo cáo lượt nghe, QR quét, quota MyMemory.
- Dashboard tổng quan với KPI cards, bar charts, live presence.

---

## Công nghệ

| Phần | Công nghệ |
|------|-----------|
| **Backend** | .NET 10, ASP.NET Core Web API, EF Core 10 + MySQL (`MySql.EntityFrameworkCore`), SignalR, JWT HS256, AutoMapper, FluentValidation |
| **Frontend** | React 19, TypeScript, Vite 8, React Router 7, i18next, Leaflet / react-leaflet, React Query |
| **Database** | MySQL 8.4 (utf8mb4) |
| **Kiểm thử** | xUnit, Moq, NetArchTest · Vitest, Testing Library |
| **Triển khai** | Docker Compose (db + api + client/nginx), GitHub Actions CI/CD |

---

## Cấu trúc thư mục

```
tourvoice/
├── client/                   # Web du khách (React + Vite + TS)
│   ├── src/
│   │   ├── api/              # Gọi REST API (http client)
│   │   ├── auth/             # AuthContext (JWT, refresh token)
│   │   ├── components/       # Header, PlaceCard, NarrationPlayer, QrCodeModal, ...
│   │   ├── hooks/            # useNarrationPlayer, usePresence, useGeolocation, useLang, useAsync, ...
│   │   ├── i18n/locales/     # vi, en, zh, ko, ja (i18next)
│   │   ├── lib/              # speech, geo, deviceId, languages, labels, safeReturn, ...
│   │   ├── pages/            # Home, Explore, FoodCulture, Map, PlaceDetail, Auth, Account, NotFound
│   │   ├── styles/           # global.css, responsive, components
│   │   ├── hooks/            # hook collections
│   │   └── App.tsx           # Route configuration, AuthProvider
│   ├── package.json
│   ├── vite.config.ts
│   ├── Dockerfile
│   └── public/
├── server/
│   ├── src/
│   │   ├── TourVoice.API/    # Controllers, SignalR hub, rate limit, Program.cs
│   │   ├── TourVoice.BLL/    # Services, DTOs, dịch máy (MyMemory), khởi tạo DB, validation
│   │   ├── TourVoice.DAL/    # DbContext, Entities, Repositories, Migrations, Seeder
│   │   └── tests/            # BLL.Tests, API.Tests, ArchitectureTests (layer dependency)
│   └── docker-compose.yml
├── docs/                     # PRD, sơ đồ drawio, slide trình bày
├── docker-compose.yml
├── .env.example
└── README.md
```

**Backend theo kiến trúc 3 lớp API → BLL → DAL**: `Program.cs` chỉ gọi `builder.Services.AddBusinessLayer(config)`, hàm này tự gọi `AddDataAccessLayer()`. Quy tắc phụ thuộc giữa các lớp được kiểm tra tự động trong `TourVoice.ArchitectureTests`.

---

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

**Đổi mật khẩu này trước khi triển khai thật.**

---

## Chạy khi phát triển (không dùng Docker)

**Yêu cầu**: .NET SDK 10, Node.js 22, MySQL 8.4.

### Backend (http://localhost:5049) — cấu hình lưu bằng user-secrets, chỉ cần đặt một lần

```bash
API=server/src/TourVoice.API
dotnet user-secrets set "ConnectionStrings:TourVoice" "Server=localhost;Port=3306;Database=tourvoice;User=tv;Password=..." --project $API
dotnet user-secrets set "Jwt:Key" "<chuỗi ngẫu nhiên >= 32 ký tự>" --project $API
dotnet user-secrets set "Jwt:Issuer" "TourVoice" --project $API
dotnet user-secrets set "Jwt:Audience" "TourVoiceClient" --project $API
dotnet user-secrets set "MyMemory:Email" "team@example.com" --project $API
dotnet run --project $API --launch-profile http
```

### Frontend (http://localhost:5173) — mặc định gọi API ở localhost:5049

```bash
cd client && npm install && npm run dev
```

### Thêm migration:

```bash
dotnet ef migrations add <Ten> --project src/TourVoice.DAL --startup-project src/TourVoice.API
```
(chạy trong `server/`).

---

## Kiểm thử

```bash
cd server && dotnet test                         # unit test + kiểm tra kiến trúc
cd client && npm run lint && npm test && npm run build
```

CI (`.github/workflows/ci.yml`) chạy đúng các bước trên cho mỗi pull request và push vào `main`.

---

## API

**Tiền tố**: `/api/v1`. Các endpoint công khai nhận `?lang=vi|en|zh|ko|ja` (mặc định `vi`).

| Nhóm | Endpoint | Quyền |
|---|---|---|
| **Ngôn ngữ** | `GET /languages` | Công khai |
| **Địa điểm** | `GET /places` · `GET /places/{slug}` · `GET /places/{slug}/foods` | Công khai |
| **Khám phá** | `GET /explore?category=&q=` (category: `Heritage`, `Market`, `FoodStreet`, `Park`, `food`) | Công khai |
| **Bản đồ** | `GET /map/markers` | Công khai |
| **Lượt nghe** | `POST /listen-events` | Công khai |
| **Presence** | `POST /presence/heartbeat` · `POST /presence/leave` | Công khai |
| **Xác thực** | `POST /auth/register` · `POST /auth/login` | Công khai |
| **Tài khoản** | `GET /me` · `GET/POST/DELETE /me/favorites/{placeId}` · `GET/POST /me/visits/{placeId}` · `GET /me/history` | Customer |
| **Quản trị địa điểm** | `GET/POST /admin/places` · `GET/PUT /admin/places/{id}` · `POST /admin/places/{id}/publish` · `POST /admin.places/{id}/hide` | Admin |
| **Quản trị bản dịch** | `POST /admin/places/{id}/translate` · `POST /admin.places/{id}/translations/{lang}/approve` | Admin |
| **Presence** | `GET /admin/presence` · SignalR `/hubs/presence` | Admin |

Đầy đủ spec xem tại **Swagger UI** khi chạy ở môi trường Development (http://localhost:5049/swagger).

---

## Triển khai (Deployment)

### 1. Sử dụng Docker Compose (Khuyên dùng cho Dev & Demo)

```bash
cp .env.example .env
docker compose up --build
```

Cấu hình Cloudflare Tunnel miễn phí để expose API ra Internet (không cần public IP):
1. Cài Cloudflare Tunnel client: `cloudflared tunnel --url http://localhost:5049`
2. Tunnel sẽ cấp URL dạng `*.trycloudflare.com`
3. Cấu hình DNS record trỏ domain cá nhân nếu có.

Ưu điểm: Khác với Vercel/Cloudflare Pages (không chạy container .NET), Docker Compose giữ toàn bộ logic .NET, MySQL, SignalR hoạt động thật.

### 2. Deploy lên Vercel / Cloudflare Pages (Cho Frontend Chỉ)

⚠️ **Lưu ý**: Vercel và Cloudflare Pages **không chạy container .NET**, vì vậy chỉ dùng cho Frontend (React + Vite).

- **Build Frontend**: `npm run build` → deploy Vercel/Cloudflare.
- **API tách riêng**: Dùng Node.js/Go/Python thay thế API .NET, hoặc dùng API hiện có và chỉ deploy Frontend qua Vercel.
- **Dữ liệu**: Cần cấu hình database bên ngoài (PlanetScale, Supabase, AWS RDS) vì MySQL container không chạy trên Vercel.

### 3. Production Setup (Full)

1. **Database**: MySQL 8.4 trên VPS hoặc managed service (AWS RDS, PlanetScale, Supabase).
2. **API**: Deploy `.exe` hoặc Docker container lên VPS/Render.com/Azure App Service.
3. **Frontend**: Deploy Next.js/Vercel/Cloudflare Pages (hoặc giữ nguyên React + Vite + Docker).
4. **HTTPS**: Tự động qua Cloudflare DNS + SSL hoặc Let's Encrypt.
5. **Monitoring**: Setup health checks, logs qua Docker ou thành phần bên ngoài.

---

## Kiểm thử

```bash
cd server && dotnet test                         # unit test + kiểm tra kiến trúc (ArchitectureTests)
cd client && npm run lint && npm test && npm run build
```

CI/CD pipeline (`.github/workflows/ci.yml`) chạy đúng các bước trên cho mỗi pull request và push vào `main`. Kết quả test hiển thị badge trên repository.

---

## Tài liệu chi tiết

- **PRD (Product Requirements)**: `docs/PRD_TourVoice.docx` hoặc `docs/system-presentation-standalone.html`
- **Slide thuyết trình**: `docs/system-presentation-standalone.html` (cấu trúc: Tổng quan, Kiến trúc, Nội dung, Thuyết minh, Audio, Bản đồ, Auth, Admin, Data/API, UI, CI/CD)
- **Sơ đồ Draw.io**: `docs/` (quy trình dữ liệu, quy trình dịch máy, quy trình admin)
- **Architecture ADRs**: Xem trong `server/tests/TourVoice.ArchitectureTests/LayerDependencyTests.cs`

---

## Liên hệ / Câu hỏi

- Email: `team.tourvoice@example.com`
- Khách hàng truy cập: `http://localhost:5173` (hoặc domain production)
- Admin: `http://localhost:5173/admin/login` (mặc định: `admin@tourvoice.local` / `Admin@12345`)