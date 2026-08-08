# Daily Tracker

A single-user app: weekly planning + daily self-observation (sleep, mood, productivity, habits). Full spec: [`spec/daily-tracker-spec.md`](spec/daily-tracker-spec.md) — the single source of truth.

## Stack

- **Backend:** .NET 10 · HotChocolate (GraphQL) · MongoDB.Driver — `backend/`
- **Frontend:** Vue 3 + TypeScript · Vite · hand-rolled UI kit · villus — `frontend/`
- **DB:** MongoDB Atlas M0 (free tier)
- **Auth:** secret key in the `X-Secret-Key` header

## Running locally

```bash
# Backend (needs backend/src/Api/.env — see infra/.env.example)
cd backend/src/Api && dotnet run

# Run migrations manually
dotnet run -- migrate

# Frontend
cd frontend && npm install && npm run dev
```

Local Mongo for development (until Atlas is set up) — port **27018** because 27017 is taken by another workspace's Mongo:

```bash
docker run -d --name tracker-mongo -p 27018:27017 mongo:8
```

Backend runs at `http://localhost:5199` (launchSettings), frontend dev server at `http://localhost:5173`. The dev secret key lives in `backend/src/Api/.env` (gitignored).

## Addendum to the spec

- `metric_definitions` has an extra field **`dayOffset`** (default 0): a value entered at day D's check-in is written to the document of day `D + dayOffset`. `screen_time` has `dayOffset: -1` — it belongs to yesterday (the "owning day" rule, spec §8 + Appendix A). The §5 table doesn't list this field.

## Milestones

See spec §11. Status: **M0 + M1 done (local MVP)**, tasks portion of M2 pulled forward. Deploy (Oracle/HTTPS/24-7) deferred until after the local MVP settles.
