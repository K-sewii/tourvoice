# TourVoice

[![CI](https://github.com/K-sewii/tourvoice/actions/workflows/ci.yml/badge.svg)](https://github.com/K-sewii/tourvoice/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![MySQL](https://img.shields.io/badge/MySQL-8.4-4479A1?logo=mysql&logoColor=white)](https://www.mysql.com/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://docs.docker.com/compose/)

Hệ thống thuyết minh du lịch đa ngôn ngữ cho **Quận 6, TP.HCM**. Khách quét QR tại điểm đến, chọn một trong 5 ngôn ngữ và nghe thuyết minh ngay trên trình duyệt. Quản trị viên quản lý địa điểm, bản dịch, báo cáo và theo dõi khách online theo thời gian thực.

> Tài liệu nghiệp vụ (PRD, sơ đồ, slide) nằm trong [`docs/`](docs/).

> **Demo nhanh không cần cài gì:** mở repo bằng GitHub Codespace → `bash .devcontainer/setup.sh && docker compose up --build -d` → mở cổng 5173. Chi tiết ở mục [Demo trên GitHub Codespaces](#demo-trên-github-codespaces).

---

## Tính năng

**Dành cho du khách**

- 5 ngôn ngữ: Tiếng Việt, English, 中文, 한국어, 日本語 — ngôn ngữ nằm trên URL (`/{lang}/...`) và được ghi nhớ.
- Trang chủ, Khám phá (lọc + tìm kiếm không dấu), Ẩm thực & Văn hóa, Bản đồ Leaflet, chi tiết địa điểm/món ăn.
- Thuyết minh bằng Web Speech API, ghi nhận lượt nghe.
- Lối vào từ QR: `/q/{slug}` → tự chuyển đúng ngôn ngữ của máy.
- Đăng ký/đăng nhập, yêu thích, đã ghé thăm, lịch sử nghe.
- Heartbeat "đang online" qua SignalR.

**Dành cho quản trị viên**

- Quản trị địa điểm (thêm/sửa/xóa, publish hàng loạt), bản dịch (duyệt tay, dịch máy MyMemory).
- Báo cáo lượt nghe, QR, quota MyMemory, dashboard KPI và live presence.

**API**

- JWT HS256, phân quyền Customer/Admin, rate limiting (120 req/phút/thiết bị, 1.200 req/phút/IP).
- Lỗi theo RFC 7807 ProblemDetails kèm `traceId`.
- Giới hạn MyMemory 50.000 ký tự/ngày, chặn ở 45.000 (BR-06).

---

## Công nghệ

| Thành phần | Công nghệ |
|---|---|
| **Backend** | .NET 10 · ASP.NET Core Web API · EF Core 10 (MySQL) · SignalR · JWT · AutoMapper · FluentValidation |
| **Frontend** | React 19 · TypeScript · Vite · React Router 7 · i18next · Leaflet · React Query |
| **Database** | MySQL 8.4 (utf8mb4) |
| **Kiểm thử** | xUnit · Moq · NetArchTest · Vitest · Testing Library |
| **Hạ tầng** | Docker Compose · GitHub Actions (CI/CD) · GitHub Codespaces |

---

## Chạy nhanh

**Yêu cầu:** Docker + Docker Compose.

```bash
git clone https://github.com/K-sewii/tourvoice.git
cd tourvoice
cp .env.example .env        # điền DB_PASSWORD, DB_ROOT_PASSWORD, JWT_KEY (>= 32 ký tự)
docker compose up --build -d
```

Hoặc để script tự sinh secret ngẫu nhiên:

```bash
bash .devcontainer/setup.sh
docker compose up --build -d
```

Mở **http://localhost:5173**. Lần chạy đầu, API tự migrate và seed dữ liệu mẫu.

| Dịch vụ | Địa chỉ |
|---|---|
| Web (và API qua proxy) | http://localhost:5173 |
| API trực tiếp | http://localhost:5049 |
| Swagger UI | http://localhost:5173/swagger |
| Health check | http://localhost:5173/health |
| MySQL | localhost:3306 (chỉ khi dev) |

**Tài khoản mặc định:** `admin@tourvoice.local` / `Admin@12345` — đổi trước khi triển khai thật.

```bash
docker compose logs -f api   # xem log
docker compose down          # tắt, giữ dữ liệu
docker compose down -v       # tắt và xóa dữ liệu MySQL
```

### Demo trên GitHub Codespaces

Repo có sẵn `.devcontainer/` (Docker-in-Docker + forward cổng 5173/5049) nên Codespaces tự dựng container có Docker và Docker Compose.

1. GitHub → **Code → Codespaces → Create codespace on main**.
2. Codespace tự chạy `.devcontainer/setup.sh` (tạo `.env`, sinh secret, điền `WEB_ORIGIN`).
3. Trong terminal:

   ```bash
   docker compose up --build -d
   ```

4. Tab **Ports** → cổng **5173** → *Open in Browser*.
5. Cho người khác demo: chuột phải cổng 5173 → **Port Visibility → Public** (devcontainer đã đặt sẵn `public`).

Nginx của container `client` reverse proxy `/api`, `/hubs`, `/uploads`, `/swagger` sang container `api`, nên trình duyệt chỉ dùng **một origin** — không phải build lại `VITE_API_URL` theo domain và không vướng CORS. URL `https://<codespace>-5173.app.github.dev` cũng dùng được cho QR.

**Docker không chạy trong Codespace?**

| Triệu chứng | Cách xử lý |
|---|---|
| `docker: command not found` | Codespace đang dùng cấu hình cũ. `Ctrl+Shift+P` → **Codespaces: Rebuild Container**. |
| `Cannot connect to the Docker daemon` | Rebuild container; hoặc `sudo service docker start` rồi thử lại. |
| `permission denied ... docker.sock` | `sudo usermod -aG docker "$USER"`, thoát và mở lại terminal. |
| Build bị kill / OOM | **Codespaces → Change machine type** → máy 4 core. |

Kiểm tra nhanh: `docker info`.

---

## Cấu hình

Mọi biến nằm trong `.env` ở gốc repo (copy từ `.env.example`, **không commit**).

| Biến | Bắt buộc | Mô tả |
|---|:---:|---|
| `DB_PASSWORD` | ✅ | Mật khẩu user `tv` của MySQL. |
| `DB_ROOT_PASSWORD` | ✅ | Mật khẩu root MySQL. |
| `JWT_KEY` | ✅ | Khóa HS256, tối thiểu 32 byte. |
| `JWT_ISSUER` / `JWT_AUDIENCE` | — | Mặc định `TourVoice` / `TourVoiceClient`. |
| `MYMEMORY_EMAIL` | — | Email MyMemory → nâng hạn mức lên 50.000 ký tự/ngày. |
| `WEB_ORIGIN` | — | Origin của web cho CORS (Codespaces tự điền). |
| `VITE_API_URL` | — | Để **trống** = gọi API qua nginx cùng origin. Chỉ đặt khi web/API khác domain. |
| `VITE_TILE_URL` | — | Template tile Leaflet. Mặc định dùng CARTO (không cần key); đổi nếu tự host hoặc dùng OSM. |

---

## Kiến trúc

```
Browser ──HTTPS──▶ client (nginx :80) ──/api /hubs /uploads──▶ api (.NET :8080) ──▶ MySQL 8.4
                        │
                        └── SPA tĩnh (React build)
```

Backend theo kiến trúc 3 lớp **API → BLL → DAL**, phụ thuộc một chiều. `Program.cs` chỉ gọi `AddBusinessLayer()`, hàm này tự gọi `AddDataAccessLayer()` (ADR-01). Ranh giới các lớp được ép bằng `TourVoice.ArchitectureTests`.

### Cấu trúc thư mục

```
tourvoice/
├── client/                 # React + Vite + TS
│   ├── src/
│   │   ├── api/            # HTTP client, module theo nhóm endpoint
│   │   ├── auth/           # AuthContext (JWT, refresh token)
│   │   ├── components/     # Header, PlaceCard, NarrationPlayer, ...
│   │   ├── hooks/          # useAsync, usePresence, useNarrationPlayer, ...
│   │   ├── i18n/locales/   # vi, en, zh, ko, ja
│   │   ├── pages/          # trang khách + pages/admin/ (SCR-A1..A8)
│   │   └── lib/            # speech, geo, deviceId, safeReturn, ...
│   ├── nginx.conf          # SPA + reverse proxy /api, /hubs, /uploads
│   └── Dockerfile
├── server/
│   ├── src/
│   │   ├── TourVoice.API/  # Controllers, SignalR hub, rate limit, Program.cs
│   │   ├── TourVoice.BLL/  # Services, DTOs, dịch máy, validation
│   │   └── TourVoice.DAL/  # DbContext, Entities, Repositories, Migrations
│   └── tests/              # BLL.Tests, API.Tests, ArchitectureTests
├── .devcontainer/          # Docker-in-Docker + forward cổng cho GitHub Codespaces
├── deploy/                 # docker-compose.yml + .env.example cho máy chủ (CD dùng)
├── docs/                   # PRD, sơ đồ, slide
├── .github/workflows/      # ci.yml, cd.yml
├── docker-compose.yml
└── .env.example
```

---

## Phát triển (không dùng Docker)

**Yêu cầu:** .NET SDK 10, Node.js 22, MySQL 8.4.

**Backend** — http://localhost:5049 (cấu hình bằng user-secrets, chỉ đặt một lần):

```bash
API=server/src/TourVoice.API
dotnet user-secrets set "ConnectionStrings:TourVoice" "Server=localhost;Port=3306;Database=tourvoice;User=tv;Password=..." --project $API
dotnet user-secrets set "Jwt:Key" "<chuỗi ngẫu nhiên >= 32 ký tự>" --project $API
dotnet user-secrets set "Jwt:Issuer" "TourVoice" --project $API
dotnet user-secrets set "Jwt:Audience" "TourVoiceClient" --project $API
dotnet user-secrets set "MyMemory:Email" "team@example.com" --project $API
dotnet run --project $API --launch-profile http
```

**Frontend** — http://localhost:5173 (mặc định gọi API ở `localhost:5049`):

```bash
cd client && npm install && npm run dev
```

**Migration mới** (chạy trong `server/`):

```bash
dotnet ef migrations add <Ten> --project src/TourVoice.DAL --startup-project src/TourVoice.API
```

---

## Kiểm thử

```bash
cd server && dotnet test                          # unit test + kiểm tra kiến trúc
cd client && npm run lint && npm test && npm run build
```

CI (`.github/workflows/ci.yml`) chạy các bước trên cho mỗi pull request và push vào `main`. Khi CI xanh, `cd.yml` build và push 2 image (`tourvoice-api`, `tourvoice-web`) lên GHCR, deploy qua SSH rồi kiểm tra `/health`. Bước deploy tự bỏ qua nếu chưa cấu hình secret máy chủ; chạy tay workflow với `image_tag` để rollback.

---

## API

Tiền tố `/api/v1`. Endpoint công khai nhận `?lang=vi|en|zh|ko|ja` (mặc định `vi`).

| Nhóm | Endpoint | Quyền |
|---|---|---|
| Ngôn ngữ | `GET /languages` | Công khai |
| Địa điểm | `GET /places` · `GET /places/{slug}` · `GET /places/{slug}/foods` | Công khai |
| Khám phá | `GET /explore?category=&q=` (`Heritage`, `Market`, `FoodStreet`, `Park`, `food`) | Công khai |
| Bản đồ | `GET /map/markers` | Công khai |
| Lượt nghe | `POST /listen-events` | Công khai |
| Presence | `POST /presence/heartbeat` · `POST /presence/leave` | Công khai |
| Xác thực | `POST /auth/register` · `POST /auth/login` | Công khai |
| Tài khoản | `GET /me` · `GET/POST/DELETE /me/favorites/{placeId}` · `GET/POST /me/visits/{placeId}` · `GET /me/history` | Customer |
| Quản trị địa điểm | `GET/POST /admin/places` · `GET/PUT /admin/places/{id}` · `POST /admin/places/{id}/publish` · `POST /admin/places/{id}/hide` | Admin |
| Quản trị bản dịch | `POST /admin/places/{id}/translate` · `POST /admin/places/{id}/translations/{lang}/approve` | Admin |
| Presence | `GET /admin/presence` · SignalR `/hubs/presence` | Admin |

Đặc tả đầy đủ: Swagger UI tại `/swagger` khi chạy môi trường Development.

---

## Triển khai

### Docker Compose trên VPS

Cách nhanh cho demo (build tại chỗ, mở cả cổng dev):

```bash
cp .env.example .env && docker compose up --build -d
```

Với máy chủ public, dùng stack `deploy/docker-compose.yml` (image GHCR, chỉ mở cổng **8080**, MySQL/API nội bộ) — `cd.yml` tự copy file này lên. Đặt sau reverse proxy HTTPS (Caddy, Traefik, Nginx hoặc Cloudflare Tunnel):

```bash
cloudflared tunnel --url http://localhost:8080
```

### CD tự động (GitHub Actions)

Khi CI xanh trên `main`, `cd.yml` build 2 image (`tourvoice-api`, `tourvoice-web`) và push lên GHCR, rồi SSH vào máy chủ để `docker compose pull && up -d` và kiểm tra `/health`.

Cấu hình **Secrets** (Settings → Secrets and variables → Actions):

| Secret | Ý nghĩa |
|---|---|
| `DEPLOY_HOST` | IP/domain máy chủ. |
| `DEPLOY_USER` | User SSH. |
| `DEPLOY_SSH_KEY` | Private key SSH (thêm public key vào `~/.ssh/authorized_keys`). |
| `DEPLOY_PATH` | Thư mục trên máy chủ, ví dụ `/opt/tourvoice`. |
| `HEALTH_URL` | URL kiểm tra sau deploy, ví dụ `https://tourvoice.example.com/health`. |

Chuẩn bị máy chủ một lần:

```bash
mkdir -p /opt/tourvoice && cd /opt/tourvoice
# copy deploy/.env.example -> .env rồi điền DB_PASSWORD, JWT_KEY, WEB_ORIGIN...
# lan deploy dau dat INITIALIZE_ON_STARTUP=true de migrate + seed, sau do doi false
```

Nếu **chưa** cấu hình đủ secrets, bước deploy tự bỏ qua và pipeline vẫn xanh (job ghi notice) — phù hợp khi repo chỉ dùng Codespaces để demo. Chạy tay workflow với `image_tag` để rollback.

### Tách frontend (Vercel/Cloudflare Pages)

Các nền tảng này không chạy container .NET: deploy API riêng, build web với `VITE_API_URL=https://api.example.com`, và thêm domain web vào `Cors:Origins`.

---

## Tài liệu

- **PRD:** [`docs/PRD TourVoice – Quận 6.docx`](docs/) · **Slide:** [`docs/TourVoice_System_Presentation.html`](docs/TourVoice_System_Presentation.html)
- **Sơ đồ:** [`docs/TourVoice_PRD_Diagrams.drawio`](docs/TourVoice_PRD_Diagrams.drawio)
- **Quy ước & ADR:** [`AGENT.md`](AGENT.md)

---

## Liên hệ

- Email: `team.tourvoice@example.com`
- Khách: `http://localhost:5173` · Admin: `http://localhost:5173/admin/login`
