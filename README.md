# Daily Tracker

App một người dùng: lên plan (tuần) + tự quan sát hàng ngày (ngủ, tâm trạng, hiệu suất, habit). Spec đầy đủ: [`spec/daily-tracker-spec.md`](spec/daily-tracker-spec.md) — nguồn sự thật duy nhất.

## Stack

- **Backend:** .NET 10 · HotChocolate (GraphQL) · MongoDB.Driver — `backend/`
- **Frontend:** Vue 3 + TypeScript · Vite · PrimeVue · villus — `frontend/`
- **DB:** MongoDB Atlas M0 (free)
- **Auth:** secret key trong header `X-Secret-Key`

## Chạy local

```bash
# Backend (cần backend/src/Api/.env — xem .env.example)
cd backend/src/Api && dotnet run

# Chạy migration thủ công
dotnet run -- migrate

# Frontend
cd frontend && npm install && npm run dev
```

Mongo local để dev (khi chưa có Atlas) — port **27018** vì 27017 đã bị Mongo của CoverGo workspace chiếm:

```bash
docker run -d --name tracker-mongo -p 27018:27017 mongo:8
```

Backend chạy ở `http://localhost:5199` (launchSettings), frontend dev ở `http://localhost:5173`. Secret key dev nằm trong `backend/src/Api/.env` (gitignored).

## Addendum so với spec

- `metric_definitions` có thêm field **`dayOffset`** (mặc định 0): giá trị nhập ở check-in ngày D được ghi vào document của ngày `D + dayOffset`. `screen_time` có `dayOffset: -1` — nó thuộc về hôm qua (quy tắc "ngày sở hữu", spec §8 + Phụ lục A). Bảng §5 của spec chưa liệt kê field này.

## Milestones

Xem spec §11. Trạng thái: **M0 đang làm** (schema + seed + migration runner). Deploy (Oracle/HTTPS/24-7) lùi sau khi MVP local chạy.
