# Daily Tracker — Spec v3.2

*(English translation of the Vietnamese original — see git history)*

The main body is for lookup while coding. The reasoning behind each decision lives in Appendix A.

**v3.2 decisions:** cut v1 scope (Month/Year tier, Settings screen, 10-best/10-worst block → v2; keep a single read-only year-goal line) · sleep belongs to the day it affects · `dayType` defaults from the calendar · sick days only ask about recovery · data stays editable until the day closes · ISO 8601 week codes.

---

## 1. What this app is

A tool for a single user, made of two halves bolted together:

- **Planning** — year goals broken down into months, months into weeks, weeks into days.
- **Self-observation** — every day, record a few numbers about sleep, mood, productivity, and what got done.

After a few months, look back over everything to figure out for yourself which schedule makes you live better. The app doesn't draw conclusions for you; it lays the data out densely and honestly enough for the user to interpret on their own.

**Not doing:**
- Task manager for company work (work items show up so plans stay realistic, but they never enter analysis)
- Project management: dependencies, sub-tasks, priority matrices
- Multi-user, sharing, social
- Automatic causal conclusions

---

## 2. What a day with the app looks like

**7am.** Open the app on the phone. Three questions: what time did you sleep last night, from when to when; how many hours of phone use yesterday (don't remember yet — tap defer); how do you feel this morning. Done — yesterday closes, and the app drops you onto the Today screen.

**The Today screen** splits into two zones. On top is Life: a grid of habit icons, then a few quick tasks. Below is the Work block, gray background, collapsible — it's only there so you know how much room is left today; it counts toward no metric.

**During the day.** Tap a habit cell to tick or enter hours. Finish a task, tick it. Touched an in-progress ticket? Tap "worked on it today".

**11pm.** Three screens: three scales for productivity, mood, physical state; pick at most two options for where your attention mainly went; a text box if you feel like writing. On days off, the first screen gets two extra scales: did you actually recover, and was the free time spent meaningfully.

**Sunday.** Open the Plan tab, Week tier: set targets for next week, throw into the backlog the things you know must happen but don't yet know which day.

**End of month, on the laptop.** Open the Analysis tab. A grid: one row per day, one column per metric, each cell colored by value. A whole month on one screen. Stare a while and it starts to show: the dark days tend to follow the late-sleep nights.

**A day you forget to open the app.** Nothing happens. No notification, no broken streak, no scolding. The day is marked missing and excluded from analysis. There is no way to backfill it.

---

## 3. Original requirements

Frozen — translated from the Vietnamese originals (2026-08); the Vietnamese text in git history remains authoritative for R1–R27. To change intent, add a new row referencing the one it replaces — **never edit old rows**.

| # | |
|---|---|
| R1 | To-do list to plan day/week/month work, covering things I want to do and things I have to do |
| R2 | Compare across days: when I get these things done, are mood and physical state actually better |
| R3 | Plan in the morning · tick through the day · review at night + plan for tomorrow |
| R4 | Spot patterns myself. The app only surfaces concrete correlations, nothing fancy |
| R5 | End goal: find the schedule that fits each phase of life |
| R6 | Quick self-rated mood at end of day + optional text box |
| R7 | Planning one day takes under 10 minutes. No hard cap on entry time, but it must be convenient |
| R8 | One user. Web-first + mobile. Sync between laptop and phone |
| R9 | Fast, but on the right stack (.NET + Vue) to learn along the way |
| R10 | Company work is mandatory → doesn't count toward measuring personal growth |
| R11 | Instead, measure how productive the workday was |
| R12 | Big items (study abroad) → year goal, a single reminder line is enough |
| R13 | Quick tasks are tick-and-forget, but a day that doesn't finish them all still matters |
| R14 | Baseline state is the thing I care about most |
| R15 | Baseline variables: sleep time, wake time, screen time, what attention went to |
| R16 | Habits: gym, reading, tech study, language study |
| R17 | Productivity on a point scale, self-estimated |
| R18 | A miss is a miss — no backfilling |
| R19 | ~~Attention only needs a light touch (pick one)~~ → replaced by R24 |
| R20 | Standardize on a 1–10 scale |
| R21 | Tasks can carry to the next day, multi-day tickets are normal, there's an `in_progress` |
| R22 | Reading/study measured in hours; gym ticked per day + rate the session's quality |
| R23 | Days off measured separately: recovery quality and whether free time was meaningful |
| R24 | Attention allows multiple selections — **replaces R19** |
| R25 | Mobile screens must not be dense with information |
| R26 | Life items and company work must be separated on screen |
| R27 | The Today screen includes a look back at yesterday (read-only) and planning tasks for tomorrow — added in v3.2 when M2's task portion was pulled up to M1 |

---

## 4. Principles

| # | | Consequence |
|---|---|---|
| 1 | Low friction beats fast | Open and enter immediately; the main flow needs no keyboard |
| 2 | No backfilling subjective ratings | Objective data may be deferred within its window (§5) |
| 3 | Never lose data that was genuinely recorded | Day missing fields → `partial`. A stray tap never deletes data |
| 4 | Phone and laptop always see the same data | Server is the single source of truth, no local DB |
| 5 | Must open on the phone at 11pm | Backend runs 24/7 from M1 |
| 6 | Adding/removing/changing tracked variables requires no code change | Config-driven (§5) |
| 7 | `no_data` differs from `not_done` differs from `0` | No booleans for habits |
| 8 | **Nothing is streak-accumulating** | Ban streaks, consecutive-day counts, running totals, badges. **Allowed**: cycle-resetting counts — 2 tasks left today, 2/3 sessions this week |
| 9 | Old data stays readable after config changes | Never change the meaning of a `key` that has data; targets carry an effective date |
| 10 | Each mobile screen does one thing | When entering, fold away what isn't used; when viewing, compress into a grid |

Principle 8 separates two things that are easily confused. The streak-accumulating kind creates pressure to **keep the streak** — ticking dishonestly to avoid losing 47 consecutive days. The cycle-resetting kind only answers "how much is left", and every week starts back at zero, so there is nothing to lose.

---

## 5. Core mechanism: config-driven

No form is hand-written. Field definitions live in the DB; the frontend reads the definitions and builds the screens itself. Adding a new tracked variable = insert one document — no code, no migration, no deploy.

A single `MetricField` component, switching on `type` to pick the widget. Check-in screens are built by: fetch definitions, filter by `phase` and `visibleWhen`, sort by `order`, loop and render.

### What a variable definition contains

| Field | Meaning |
|---|---|
| `key` | identifier, never changes |
| `label` | display name |
| `type` | decides the widget — table below |
| `phase` | morning · evening · anytime |
| `order` | position within the screen |
| `visibleWhen` | visibility condition, empty means always visible |
| `deferrable` | how many days it may be deferred |
| `polarity` | is higher good or higher bad |
| `validation` | min, max, required or not |
| `options`, `maxSelect` | for choice types |
| `active` | deactivate a variable, never delete |

### Supported `type` values

| type | widget | notes |
|---|---|---|
| `scale` | swipe-scale | strip of 10 colored cells, horizontal swipe, large number above |
| `number` | stepper | configurable step, 0.5 for hours |
| `time` | time picker | remembers last value as default |
| `enum` | single-select chips | |
| `multi_enum` | multi-select chips | `maxSelect` is mandatory |
| `text` | textarea | optional, unlimited length |

### `polarity`

Accepts `higher_better` or `higher_worse`. Decides the direction of the color scale on the Analysis screen. Without it, the screen time column (higher is bad) gets colored the same direction as the mood column (higher is good), and the whole table reads wrong.

### `visibleWhen`

Just a simple value match: a field name and a list of accepted values. Example: `recovery` only shows when `dayType` is in the days-off group. **Do not build an expression language** — building a DSL burns a month on a parser instead of collecting data.

### `deferrable`

Declares how many days a value may still be recorded after the day it belongs to. The value **belongs to day X and is written into day X's document**, but stays writable through the end of day X+n.

In the check-in flow, such a field gets a "defer" button and never blocks progress; it drops into a list on the Today screen tagged with the day it belongs to. Past the window it becomes `no_data`.

**Only set this for objective data** — numbers that already exist somewhere else; the app merely copies them. No 1–10 scale is ever `deferrable`.

A day's `status` is decided solely by the morning/evening check-ins; an unfilled `deferrable` field never leaves the day in limbo.

### Versioning rule (mandatory)

Never change the meaning of a `key` that has data. Want to move `productivity` from a 1–10 scale to 1–5: deactivate the old one, create `productivity_v2`. Changing `label` is fine — only the meaning and the scale are immutable.

Violating this makes before-and-after data incomparable, and **no error will ever tell you**.

---

## 6. Data model

Six collections.

### `metric_definitions`
Per §5.

### `habits`

| Field | Notes |
|---|---|
| `label` | full name, used in Settings and Analysis |
| `shortLabel` | max ~8 characters, used only in grid cells |
| `icon` | icon name from the bundled set |
| `measure` | `binary` or `duration` |
| `hasQuality` | whether a 1–10 rating is attached |
| `qualityLabel` | the question asked when rating |
| `active`, `order` | |

### `habit_targets`

| Field | Notes |
|---|---|
| `habitId` | |
| `period` | only accepts `week` |
| `target`, `unit` | number of sessions or hours |
| `effectiveFrom` | first week this target applies |

Changing a target = create a new record with `effectiveFrom` set to next week, **never edit the old record**. Past weeks are always scored against the target that was in effect at the time (principle 9).

Week and month codes follow ISO 8601, weeks start on Monday — e.g. `2026-W32`, `2026-08`. Shared by `effectiveFrom` and task `scopeKey`.

Targets exist only at the week tier. Setting both a week and a month target for the same habit produces two conflicting numbers the user has to reconcile by hand.

### `daily_entries`
One document per day, `date` is the unique key.

| Field | Notes |
|---|---|
| `date` | year-month-day string in local time |
| `status` | `open` · `closed` · `partial` · `missed` |
| `dayType` | `workday` · `weekend` · `dayoff` · `sick` — defaults: Sat/Sun are `weekend`, everything else `workday`; change by tapping the Today screen header |
| `values` | key-value bag, keys come from `metric_definitions` |
| `habits` | one entry per habit: `state`, `hours`, `quality` |
| `quickPlanned` | denominator — hard-locked, see below |
| `quickDone` | numerator |
| `quickAddedLater` | tasks added after the denominator was locked |
| `ongoingTouched` | count of multi-day tasks touched |
| timestamps | morning check-in, evening check-in, day closed, updated |

**Habit `state`** always has three values `done` · `not_done` · `no_data`, even for duration habits. `hours` of 0 is real data, entirely different from `no_data`. `quality` only exists when the habit has a rating and `state` is `done`.

**Denominator-locking rule:** `quickPlanned` locks the moment the morning check-in finishes and never grows again. Tasks added afterwards go into `quickAddedLater`; completing them increments `quickDone` but not the denominator. The ratio can therefore exceed 1 — deliberate, not a bug.

Without the lock, adding a task at night and doing it immediately yields 1/1 and the day looks perfect — while R13 wants to catch the opposite: days that were over-planned and left unfinished. A day with no morning check-in has `quickPlanned` as `no_data` and is excluded from ratio comparisons.

### `tasks`

| Field | Notes |
|---|---|
| `title` | |
| `category` | `personal` or `work` — `work` never enters analysis |
| `kind` | `quick` or `ongoing` |
| `scope` | `day` · `week` — `month` deferred to v2, alongside the Month tier |
| `scopeKey` | a specific date, or a week code, or a month code |
| `plannedDate` | only present when `scope` is `day` |
| `status` | `todo` · `in_progress` · `done` · `dropped` |
| `originalDate`, `carryCount`, `touchedDates`, `doneAt` | |

A task in the week backlog has `scope` of `week` and no `plannedDate` yet. Tapping "assign day" sets `plannedDate` and switches `scope` to `day`. If the week ends unassigned, it **does not auto-roll into next week** — it stays in the old week as unfinished work.

| | `quick` | `ongoing` |
|---|---|---|
| Examples | photograph documents, laundry | a ticket spanning 5 days |
| Counts in numerator/denominator? | yes | no |
| Signal | done / not done | touched today or not |

### `goals`

| Field | Notes |
|---|---|
| `title` | |
| `scope` | `year` or `month` |
| `targetDate` | optional |
| `parentId` | a month goal hangs under a year goal |
| `status`, `active` | |

Year goals don't require a deadline — some are measured indirectly through habits rather than milestones.

**v1:** the collection is still created at M0 but seeded with exactly one document (the year goal — study abroad), displayed read-only at the top of the Plan tab (R12: a single reminder line). CRUD and month goals go to v2.

---

## 7. Life cycle of a day

Day D's morning check-in does two things: closes day D-1's books, and opens day D's. During the day, tick habits and tasks. At night, do the evening check-in. Next morning, the books close again.

| Situation | status |
|---|---|
| Has evening check-in + next morning's check-in | `closed` |
| Partially missing | `partial` — existing data kept, missing fields are `no_data` |
| App never opened all day | `missed` |

Day D closes when D+1's morning check-in happens, or once D+1 has fully passed. Midnight is not the cutoff, because sleep usually comes after it.

**Editing data:** while a day is not yet closed, every value is editable — reopen a check-in to edit, tap habit cells per the rules in §9.2. Once closed, a day is locked forever; the sole exception is a `deferrable` field still within its window (§5). Fixing a mistake is not backfilling: backfilling means filling in a day that has passed; fixing means correcting a value just recorded within the day.

**No book-closing cron:** `missed` and `partial` are settled lazily — computed at the next app open or at query time. A day with no document means `missed`.

**Analysis:** every comparison uses only days that have all the fields it needs, and always shows how many usable days it had. Splits by `dayType` by default; never mixes workdays with days off.

---

## 8. Seed data

### Morning variables
| key | label | type | deferrable |
|---|---|---|---|
| `sleep_start` | Bedtime last night | time | — |
| `sleep_end` | Wake time | time | — |
| `screen_time` | Screen time (hours) | number | 1 day |
| `mood_morning` | Morning mood | scale | — |

Hours slept is computed automatically from the two timestamps (handles crossing midnight: 23:30 → 07:00 is 7.5 hours), not a manually entered variable. `screen_time` has higher-is-bad polarity.

**Owning day:** the night of D-1 → morning of D is written into day D's document — the day it affects, so the comparison "slept late → worse next day" reads straight off a single row. `screen_time` belongs to D-1 and is written into D-1's document (the `deferrable` rule, §5).

### Evening variables
| key | label | type | visible when |
|---|---|---|---|
| `productivity` | Productivity | scale | |
| `mood_evening` | End-of-day mood | scale | |
| `physical` | Physical state | scale | |
| `attention_main` | Where did your attention mainly go | multi_enum, max 2 | |
| `recovery` | Did you actually recover | scale | `weekend` · `dayoff` · `sick` |
| `time_meaningful` | Was free time spent meaningfully | scale | `weekend` · `dayoff` |
| `note` | Note | text | |

Every scale is 1–10.

Sick days ask about recovery but not "was free time meaningful" — when you're flat on your back that question isn't fair, and a sick day's low score would pollute the data for genuine days off.

Options for `attention_main`: work · learning & growth · phone & entertainment · social & other people · empty

### Habits
| label | shortLabel | icon | measure | rating |
|---|---|---|---|---|
| Gym / exercise | gym | barbell | binary | ✅ "Was the workout good?" |
| Reading | read | book | duration | — |
| Tech learning (off-hours) | tech | code | duration | — |
| RP practice | RP | microphone | duration | — |
| Go outside / meet people | out | door-exit | binary | — |

Convention: "tech learning" only counts time outside official work hours. `productivity` is rated for the whole day.

---

## 9. Screens

Four-item nav: **Today · Plan · Analysis · Settings**. On mobile, the Analysis item still shows but leads to a short notice suggesting the desktop.

### 9.1 Morning check-in — 3 steps
Full-screen, one question per screen, thumb-optimized, a three-notch progress bar at the top.

1. Sleep — two time pickers, showing the auto-computed total hours
2. Yesterday's screen time — stepper, with a "defer" button
3. Morning mood — swipe-scale

Finishing drops you onto the Today screen, and `quickPlanned` locks right here. **There is no separate planning step** — the Today screen already does that job.

### 9.2 Today
Header: date and `dayType` in the top right corner, tap to change. Defaults: Sat/Sun are `weekend`, everything else `workday`.

**Life zone**
- Habit icon grid, 4 columns. Cells always show the icon and `shortLabel`. Values appear in the top-right corner as a small badge, **never replacing the habit name**.
- Below it, the day's personal tasks, one line each, plus an "Add task" line.
- Unfilled deferred fields appear as regular lines, clearly labeled with the day they belong to.

**Three time zones (R27, added v3.2):** at the top, a read-only "Yesterday" block (book status, tasks done/undone, habits ticked — not editable, R18 stands); at the bottom before the evening CTA, a "Tomorrow" block for planning tasks ahead (R3: plan for tomorrow at night). Tasks added for tomorrow enter tomorrow's denominator at the next morning's check-in.

**Work zone**
- Its own block on a light gray background, text one shade fainter.
- Collapsible and expandable. Collapsed, it shows the count of unfinished tasks today; expanded, it adds the line "not counted toward measurement".
- Remembers the last collapsed/expanded state. **Never guesses based on time of day.**

**Habit cell interaction rules**

| Kind | Tap on empty cell | Tap on filled cell |
|---|---|---|
| binary, no rating | tick immediately, no tray | untick |
| binary with rating | tick then open the rating tray | open the tray to edit, with an untick button |
| duration | open the hours tray | open the tray to edit |

The input tray opens **below the grid**; the grid never shifts. Only one tray open at a time. Tapping a cell that has data **never deletes that data** — you must press a button inside the tray.

### 9.3 Evening check-in — 3 steps
1. **Three scales on one screen**: productivity, mood, physical state. Days off make it five scales; sick days four (no `time_meaningful`).
2. Where did your attention mainly go — chips, max 2
3. Note — textarea, with a skip button

### 9.4 Plan
**v1 has only the Week tier**, plus one read-only year-goal line at the top of the tab (pre-seeded — R12). The full Month and Year tiers go to v2; the descriptions are kept below to avoid redesigning them.

**Week** — shows the current week and days remaining.
- Habit targets: each habit with a target is one line, formatted "2 / 3 sessions" with a progress bar. The target used is the one in effect for that week.
- This week's unassigned tasks: each line has an "assign day" button

**Month (v2)** — month goals, each hanging under a year goal.

**Year (v2)** — each goal is a card, listing inside it the month goals that belong to it with status and expected month.

### 9.5 Analysis — desktop
**Unusable on mobile** and no attempt is made to make it usable: 31 days times 9 columns on a 390px screen leaves each cell 20px.

The grid: rows are days, columns are metrics, cells colored by value, color-scale direction per `polarity`. `no_data` cells get a gray hatched background, instantly recognizable, never confusable with a low value. Clicking a day opens its details and note. Filter by `dayType`.

**(v2)** The "10 best vs 10 worst days" block by end-of-day mood: side by side, listing the metrics that diverge sharply between the two groups, with all notes from those days. Needs about 2 months of data to mean anything, so it's postponed.

### 9.6 Settings (v2)
v1 has no such screen: seed and adjust config via migrations or direct DB inserts — being config-driven, the forms update themselves, no deploy needed.

Two lists: **Habits** and **Tracked variables**. Each list has an "Add" line.

Deactivating a variable marks it discontinued, **never deletes it** — old data must stay readable.

---

## 10. Infrastructure

**Stack:** Vue 3 + TypeScript / .NET / MongoDB / GraphQL.

**DB:** MongoDB Atlas M0 — free forever, 512 MB. The app generates about 365 documents a year, roughly 1KB each.

**Backend:** Oracle Cloud Always Free, ARM Ampere A1 machine (2 OCPU / 12 GB after the June 2026 quota reduction).
- Requires a credit card for verification; never charged while inside the quota
- Regions often report out-of-capacity when creating ARM machines — fall back to the AMD micro machine, still sufficient
- Long-idle machines can be reclaimed; a light cron takes care of it
- Runs ARM64, so the Dockerfile must build for the right target
- **Fallback** if the account gets blocked: a machine at home plus Cloudflare Tunnel

**Ruled out:** Render free tier (sleeps without traffic, roughly 50-second cold start on every open). Railway and Fly.io (by 2026 both moved to trials or usage-based pricing).

**Sync:** the server is the single source of truth; both clients call the same API. Update timestamps are per-field, last write wins.

**GraphQL with dynamic data:** a list of key–value pairs, each pair carrying a slot per data type. The client has to fetch definitions to build forms anyway.

**Type safety:** one constants file on the client for the keys the Analysis screen references by name.

**Timezone:** dates stored as year-month-day strings in local time, no UTC timestamps.

**Auth:** a secret key in a header, or Cloudflare Access.

**Visual system:** use an off-the-shelf Vue component library, changing only the accent color and font. Do not build from scratch.

---

## 11. Backlog

| M | Contents |
|---|---|
| **M0** | Atlas cluster · schema + seed · migration runner |
| **M1** | Config-driven form renderer · morning + evening check-in · **minimal habit ticking** · 24/7 deploy · reachable from the phone |
| **M2** | Full Today screen: icon grid, two zones, collapsible work block |
| **M3** | Plan: Week tier (targets + backlog) · read-only year-goal line |
| **M4** | Analysis: the color grid on desktop |
| **M5** | CSV export |
| **M6** | PWA, offline queue, install to home screen |

Pushed to v2 (decided in v3.2): full Month/Year tiers · Settings screen · 10-best/10-worst block — see Appendix C.

M1 is the only milestone with a real deadline: the data cannot be recovered; every week it isn't done is a week lost forever.

**Why M1 must include habit ticking.** Habits are not tracked variables, so they don't go through the form renderer; the place to tick them is the Today screen in M2. If M1 runs for a few weeks before M2 lands, there will be sleep, mood, and productivity data with the **gym, reading, and tech columns completely empty** — empty in exactly the half that matters to the original question. An ugly checkbox list is fine, as long as it exists.

### M0 is done when
- [ ] Atlas cluster running, connection string in env
- [ ] All 6 collections created, date in `daily_entries` is the unique key
- [ ] The 11 variables and 5 habits from §8 plus 1 year goal seed successfully via the migration runner
- [ ] Running the migration a second time does not duplicate data

### M1 is done when
- [ ] Query returns definitions filtered by `phase` and `dayType`
- [ ] `MetricField` renders all 6 types
- [ ] Morning check-in saves, closes the previous day with the correct status, locks the denominator
- [ ] The "defer" button works; deferred fields show up with the correct day
- [ ] Evening check-in saves; visibility conditions work (days off get 2 extra scales)
- [ ] The max-2 selection limit enforces correctly
- [ ] **All 5 habits tickable, all three states distinguishable, hours enterable**
- [ ] Backend runs 24/7, has HTTPS, opens over 4G on the street
- [ ] Enter on the phone, open the laptop, see it immediately
- [ ] Adding a new variable by inserting a document grows the form a new field, no redeploy

---

## 12. Open questions

- [x] **Cut v1 scope** — decided in v3.2: cut all three, keep a single read-only year-goal line true to R12. See §11 and Appendix C
- [ ] Empty states not yet designed: the first day, the Analysis grid with only 3 days, the Today screen with no tasks
- [ ] Does the weekly-target progress bar create pressure to tick dishonestly — use it for real for a few weeks, then decide; drop the bar and keep just the number if it does
- [ ] The 10-cell strip on narrow screens: each cell about 24px, narrower than the recommended touch target. Swiping is fine; direct taps may slip. If it grates, shrink to 1–5, at the cost of adding a row replacing R20
- [ ] Should the Work block disappear entirely after the morning
- [ ] Will the Oracle account get approved — known within the first day

---
---

# Appendix A — Why

**Morning mood score.** The central hypothesis is that high productivity leads to good mood. But the reverse is just as plausible: waking up feeling good is why a lot got done and the evening score was high. With only an evening score, those two stories produce identical numbers. With a morning score, the question becomes: among days that started out feeling the same, did the days with more done end better?

**Recovery and meaning split in two.** They pull against each other — lying around watching movies all day is recovery 8, meaning 2; grinding a side project for 10 hours is meaning 9, recovery 2. Merging them into one score erases exactly the most valuable information. Hypothesis worth testing: burnout weeks are weeks whose days off score high on meaning but low on recovery.

**Always split by day type in analysis.** Productivity means different things on workdays and days off. A fully rested Sunday scored productivity 2 — the number isn't wrong, but mixed in with workdays, the analysis reads it as a bad day.

**The `partial` status.** "A miss is a miss" is right in that there's no backfilling. But wiping the ticks made during the day would be destroying real data.

**Allowing deferral for objective data.** At 7am nobody remembers how many hours of phone use yesterday had — forcing immediate entry creates pointless friction and yields made-up numbers. But screen time differs from mood in one way: the phone already recorded it, the number reads the same whenever you look, there's nothing to bypass.

**Write into the day that owns the value.** The previous version stored screen time in today's document meaning "yesterday's". That forces every analysis to remember a one-day offset — sooner or later it's forgotten, and that kind of error raises no alarm. Sleep defines "ownership" by direction of effect: the night of D-1 → morning of D belongs to day D, because the question is "how did I sleep → how is today", not which day the eyes closed.

**Measure hours instead of binary thresholds.** A binary threshold hard-locks the decision at collection time — tick "read over 20 minutes" and a 25-minute day and a 3-hour day land in the same bucket, unaskable three months later. Record hours and the threshold moves to analysis time.

**Zero differs from no data.** A day that deliberately records "read 0 hours" is real, valuable data. A day of forgotten check-ins is not a 0.

**Separate single-day tasks from multi-day tasks.** A ticket spanning 5 days, if shoved into the numerator/denominator, counts as unfinished for 4 straight days, dragging those 4 days' scores down despite good work.

**Store numerator and denominator, not the ratio.** A good day: planned 8, did 5, that's 0.62; a tired day: planned 2, did 2, that's 1.0. Store only the ratio and the app concludes backwards.

**Lock the denominator at the morning check-in.** Without the lock, adding a task at night and doing it immediately yields 1/1 and the day looks perfect — while the original intent was to catch days that were over-planned and left unfinished.

**Config-driven instead of hardcoded columns.** The first two months will see the tracked variables change a dozen times. Hardcoded, every change touches five places: the C# class, the GraphQL type, the resolver, the TS type, the Vue form.

**Take the config-driven pattern from CoverGo, drop the excess.** CoverGo is config-driven because it's a multi-tenant platform — a business reason. This app has one user, so drop multi-tenancy, versioning UIs, config migration tooling, and the expression language.

**Targets carry an effective date.** Changing "gym 3 sessions" to 4 by editing in place rescores every past week against the new target. Same bug as changing the meaning of a variable that already has data.

**Attention capped at 2 selections.** With free choice, reality is 3–4 items ticked every night, and all discriminating power vanishes. The original question is where attention **mainly** went; keep the word "mainly" and the signal survives.

**Icon grid instead of a line list.** The dominant convention in this app category: a grid fits 8–12 items in the space a list fits 4, and the eye scans a grid faster than lines. The habit name is never replaced by its value; otherwise, three months later the grid is unreadable.

**Three scales merged into one evening screen.** Keeping "one question per screen" makes the evening 5 screens, days off 7 — too long for a nightly ritual.

**Dropping the planning step from the morning check-in.** It fully duplicates the Today screen: pick habits, add tasks, confirm the day type.

**The color scale goes light-to-dark only, no red-yellow-green.** Red conventionally means wrong. Using it for mood means the app is judging the user for feeling bad today. Light-to-dark conveys the same information without the judgment.

**Company work one visual step down.** Not just a separate group — it's a different class because it isn't measured. Wedged in between, the eye assumes it's being scored too.

**Remember collapsed/expanded state instead of guessing by hour.** When an app guesses the user's intent, nobody notices when it's right, and it's irritating when it's wrong. For something opened twice a day, being predictable matters more than being smart.

**Tapping a filled cell never deletes data.** Fat-fingering a 60px cell on a phone is routine; it must never silently erase a recorded workout.

**Targets only at the week tier.** Setting both week and month targets for the same habit yields two conflicting numbers the user has to reconcile by hand — which is exactly the job the app should be doing for them.

**Week backlog tasks never auto-roll to next week.** Auto-rolling turns the backlog into a junk pile within two months, and destroys the signal "that week was over-planned".

**Distinguish streak accumulation from cycle counting.** Streaks create pressure to keep the chain, leading to dishonest ticks to avoid losing 47 straight days — data corrupted right at the source. Counts that reset every day or week only answer "how much is left" and have nothing to lose. The previous version mistakenly banned both, then violated its own ban in the screens section.

**No correlation coefficients in v1.** With small n, comparing the two extremes is more honest, and it pushes interpretation back onto the user.

---

# Appendix B — Risks

| Risk | Handling |
|---|---|
| Reverse causality | Morning score as a control variable |
| Lag — last night's sleep affects today | The color grid lets the eye catch it; lag analysis goes to v2 |
| Small n — 30 days, 15 variables, spurious patterns come easily | No correlation coefficients; always show the usable-day count |
| Workout ratings have a tiny n, about 12 points a month | Show day counts separately for conditional variables |
| Mixing days off with workdays | Every comparison splits by day type |
| Multi-day tasks distorting the ratio | Separate single-day and multi-day tasks |
| Denominator inflating through the day | Locked at the morning check-in |
| "Higher is bad" columns colored the same direction as "higher is good" | The polarity field |
| Attention losing discriminating power | Capped at 2 selections |
| Scale changes making old data useless | Versioning rule in §5 |
| Target changes rescoring past weeks | Effective dates on `habit_targets` |
| Abandonment after 2 weeks | Optimize friction, no streaks |
| Feature creep: attention becoming time tracking | Locked as quick-pick chips, max 2 |
| Scope drifting into a task manager | Company work never enters analysis |
| **The spec growing faster than the code** | §12 has a dedicated line for scope cutting. Re-check on every new screen |
| Weekly targets becoming streaks in disguise | Reset every week, no chains. Tracked in §12 |
| Oracle rejecting the account or out of capacity | Cloudflare Tunnel fallback, known within the first day |

---

# Appendix C — v2

- **Full Month + Year Plan tiers** — goal CRUD, months hanging under years, task scope `month` (cut from v1 in v3.2)
- **Settings screen** — UI to add/edit habits and tracked variables; direct DB inserts for now
- **10-best/10-worst block** — needs ~2 months of data to mean anything
- **Lag analysis** — shift a data column by n days and compare again. Needs 2–3 months of data to be meaningful.
- **Gentle reminder** — one fixed-time notification, no repeats; ignore it and that's that.
- **Carry count as a tracked variable** — a task pushed across many consecutive days is an avoidance signal, possibly correlated with mood.
- **Plan-versus-reality comparison** — in weeks where targets exceeded capacity, how did the week end mood-wise.
