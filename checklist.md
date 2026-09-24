# CV Management System: Detailed Build Checklist

**Stack:** ASP.NET Core Web API (C#) · EF Core · Supabase Postgres · Cloudinary · React (Vite) + Bootstrap

**How to use this:** Work top to bottom. Every phase ends with a "Done when" test. Commit to Git and redeploy at the end of every phase, so you always have a working version.

**Words you will see**

- **API endpoint**: a backend URL like `GET /api/positions` that returns JSON.
- **EF Core (ORM)**: you write C# classes, and EF turns them into tables and SQL queries.
- **Migration**: a versioned script that changes the database structure.
- **DTO**: a small C# class shaped like what the API sends or receives, so you never expose raw database entities.
- **Optimistic locking**: every record has a `Version` number. A save only works if the version you send still matches the database. If someone else saved first, you get a conflict instead of silently overwriting them.
- **EAV**: one table describes attributes, another stores `(user, attribute, value)` rows, so new attributes need no new columns.

---

## Phase 0: Plan (before writing code)

- [x] Copy every feature from the spec into a task list (in README.md).
- [x] Label each task **Core**, **Killer feature**, or **Bonus**.
- [x] Draw the database diagram and document system design (in DATABASE_DESIGN.md).
- [x] Create free accounts: GitHub, Supabase, Cloudinary, Render, Google Cloud Console.
- [x] Put the "Strict DON'Ts" and the two -20% UI penalties at the top of your README as a reminder.

**Done when:** you have a task list, a diagram, and all accounts.

## Phase 1: Set up your tools

- [x] Install the .NET SDK (v10.0), Node.js, Git, and an editor. Check with `dotnet --version` and `node -v`.
- [x] Create one Git repo with `/backend` and `/frontend`. Add a `.gitignore` (`bin/`, `obj/`, `node_modules/`, secret config files).
- [x] Backend: `dotnet new webapi -o backend`. Use controllers (easier to follow than minimal APIs).
- [x] Frontend: `npm create vite@latest frontend -- --template react` (JavaScript).
- [x] Backend packages: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `CloudinaryDotNet`, `Microsoft.AspNetCore.OpenApi` (Swagger).
- [x] Frontend packages: `@supabase/supabase-js`, `bootstrap`, `react-bootstrap`, `react-router-dom`, `@tanstack/react-query`, `@tanstack/react-table`, `react-i18next`, `i18next`, `react-markdown`, `react-dropzone`, `react-select`, `react-tagcloud`, plus a Markdown editor such as `@uiw/react-md-editor`.
- [x] In `vite.config`, proxy `/api` to your local backend (`http://localhost:5190`).
- [x] Keep secrets out of Git: use `dotnet user-secrets` locally and environment variables in production.

**Done when:** `dotnet run` and `npm run dev` both start.

## Phase 2: Walking skeleton and first deployment

*This proves the whole chain works while the app is still tiny.*

- [x] Create a Supabase project. Copy the Postgres connection string. Supabase's direct connection can be IPv6-only, so hosts like Render may need the **pooler** string (check Supabase's current docs). You use Supabase as both your Postgres host and your Auth provider.
- [x] Add `AppDbContext`, register it with `UseNpgsql(...)`, and run `dotnet ef migrations add Init` then `dotnet ef database update`.
- [x] Add `GET /api/health`. Show its result on a React page.
- [x] In `Program.cs`, serve the React build: `app.UseDefaultFiles(); app.UseStaticFiles(); app.MapFallbackToFile("index.html");`
- [x] Also in `Program.cs`, read the `PORT` environment variable, listen on it, and call `db.Database.Migrate()` at startup.
- [x] Write a multi-stage Dockerfile (React build, then .NET publish, then run). Match the .NET version and DLL name to your project:

```dockerfile
FROM node:22-alpine AS web
WORKDIR /web
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY backend/*.csproj ./
RUN dotnet restore
COPY backend/ ./
COPY --from=web /web/dist ./wwwroot
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /out .
ENTRYPOINT ["dotnet", "backend.dll"]
```

- [ ] Create a Render **Web Service** from the repo (Docker). Add the connection string as an environment variable (`ConnectionStrings__DefaultConnection`).
- [x] Add `UseForwardedHeaders` so the app knows it is behind HTTPS (needed for redirects).

**Done when:** a public URL shows your React page with data from the database. Free hosts sleep when idle, so the first request is slow.

## Phase 3: Database design with EF Core

- [ ] Create these entities (C# classes) and relationships:
  - **AppUser**: `Id` (string/Guid matching Supabase Auth `sub`), `Email`, `Role` (Candidate, Recruiter, Administrator), `IsBlocked`, `Version`, `CreatedAt`. Note: Profile details are NOT stored as fixed columns here; all profile data lives in the EAV engine.
  - **Category**, **AttributeDefinition** (unique `Name`, description, data type, category, `IsBuiltIn`, `LastUsedAt`, `Version`), **AttributeOption** (choices for "One of many").
  - **UserAttributeValue** (`UserId`, `AttributeId`, `Value`). Unique index on `(UserId, AttributeId)`. **All user profile attributes—both built-in "Me" attributes and user-selected library attributes—are stored here.**
  - **Project** (`UserId`, name, start date, end date, Markdown description), **Tag** (unique `Name`), **ProjectTag**. Tags are created in DB on first use.
  - **Position** (title, short description, is-public, max projects, timestamps, `Version`), **PositionAttribute**, **PositionTag**, **PositionAccessRule** (attribute, operator, value).
  - **Cv** (`UserId`, `PositionId`, state, timestamps, `Version`) with a unique index on the pair. **Do not store CV content here.**
  - **DiscussionPost** (position, author, Markdown text, timestamp) and **Like** (recruiter, CV, unique on the pair).
- [ ] Use enums for data types (`String`, `Text`, `Image`, `Numeric`, `Date`, `Period`, `Boolean`, `OneOfMany`), CV states, and rule operators.
- [ ] Configure **cascade delete** on every relationship with `.OnDelete(DeleteBehavior.Cascade)`. Deleting a position then removes its posts and CVs with no loops.
- [ ] Add **indexes** on columns you filter by (`Name`, `PositionId`, `UserId`, `CreatedAt`).
- [ ] Optimistic locking, entity side: `public int Version {get;set;}` and `builder.Property(x => x.Version).IsConcurrencyToken();` on `AppUser`, `AttributeDefinition`, `Position`, and `Cv`.
- [ ] Seed the categories and the four built-in "Me" attributes: First Name, Last Name, Location, Personal Photo, all marked `IsBuiltIn = true` so they can never be deleted from the library.
- [ ] Run a migration and check the tables in Supabase.

**Done when:** the tables exist in Supabase and seed data is visible.

## Phase 4: Authentication and roles (Supabase Auth + JWT Bearer)

- [ ] In Supabase Dashboard, configure Social Providers (Google, Facebook/GitHub).
- [ ] In React, use `@supabase/supabase-js` for sign-in (`supabase.auth.signInWithOAuth({ provider: 'google' })`) and sign-out.
- [ ] In .NET `Program.cs`, add `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` validating the Supabase JWT authority and audience.
- [ ] Create a user sync middleware or endpoint (`GET /api/me`):
  - Extracts the Supabase `sub` claim.
  - If user does not exist in backend `AppUsers` table, insert them with default role `Candidate`.
  - Checks if `IsBlocked == true`. If blocked, return `403 Forbidden` immediately on all protected endpoints.
  - Injects backend role (`Candidate`, `Recruiter`, or `Administrator`) into user claims so `[Authorize(Roles="Recruiter,Administrator")]` works.
- [ ] Create initial Administrator account by manually setting role = `Administrator` in the DB for your user ID.
- [ ] Add ownership checks (`isOwner || isAdmin`) on anything personal.
- [ ] React: a `useAuth` context, protected routes, and a navbar that changes by role.
- [ ] Unauthenticated users can only sign in/register, browse positions read-only, and see public statistics.

**Done when:** you can log in with a social provider via Supabase, backend validates the JWT, and each role sees different menus.

## Phase 5: Attribute Library

- [ ] Endpoints: create, update (with version), delete, list, and search by:
  - Name prefix (`StartsWith` or `ILIKE`)
  - Category filter
  - Recently used (`LastUsedAt` timestamp updated when attributes are selected in profiles or positions)
- [ ] Enforce a globally unique name (unique index, and return a clear error).
- [ ] Support all types: String, Text, Image, Numeric, Date, Period (Start & End dates), Boolean, One-of-many. Editing options must be possible for One-of-many.
- [ ] Frontend: a table with a checkbox column and a **toolbar** above it (Delete, Edit). Toolbar actions enable when rows are selected. **No buttons inside rows.**
- [ ] Built-in attributes (`IsBuiltIn == true`) cannot be edited or deleted by recruiters.
- [ ] Create and edit forms in a modal or side panel. Show a conflict message on a stale version ("Someone changed this. Reload?").
- [ ] Deleting an attribute cascades to values and position links. Confirm with the user first.
- [ ] Use paging and a debounced search box. Do not load everything.

**Done when:** a recruiter can create the "CAP" drop-down from the Acme scenario and find it by typing "CA".

## Phase 6: Personal profile

- [ ] Route `/profile/:id`. Only the owner and admins can edit. Recruiters see a read-only CV view.
- [ ] **Me tab**: renders the 4 mandatory built-in attributes (First Name, Last Name, Location, Personal Photo) fetched from `UserAttributeValues` where `IsBuiltIn == true`.
- [ ] **Photo upload**: the browser uploads straight to Cloudinary using a **signed upload** (the backend creates the signature, so your secret never reaches the browser). Then send the returned URL to the backend. Save the URL to the database only after the upload succeeds. When deleting, delete the DB record first, then the Cloudinary file.
- [ ] **Info tab**: pick attributes from the library (with search), and fill values with a matching input per type (checkbox, date picker, period range, numeric, select, and so on). Allow removing an attribute.
- [ ] **Projects tab**: table plus a form with name, date range (period), Markdown editor, and a **tag input with autocomplete** (`react-select` creatable). Tags are created in the database on first use. Render Markdown with `react-markdown`.
- [ ] **CVs tab**: table of the user's CVs with position, state, and likes.
- [ ] **Auto-save**: keep changes in React state, and save with a timer every 5–10 seconds (not per keystroke) only if something changed.
- [ ] **Optimistic locking, save side**:

```csharp
var user = await db.Users.FindAsync(id);
db.Entry(user).Property(x => x.Version).OriginalValue = dto.Version; // version the client saw
user.Version++;
try { await db.SaveChangesAsync(); return Ok(user.Version); }
catch (DbUpdateConcurrencyException) { return Conflict(); }
```

- [ ] On the client, store the returned version for the next save. On a 409, show "This profile changed elsewhere" and offer to reload.
- [ ] When saving many values, load existing rows in one query and update in memory. **No queries inside loops.**

**Done when:** editing in two tabs produces a graceful conflict message, and auto-save works.

## Phase 7: Positions

- [ ] Endpoints: create, edit (with version), delete, **duplicate**, list (paged), and details. Any recruiter can edit any position.
- [ ] Position form: title, short description, attribute picker (from the library), project tags, max projects.
- [ ] **Access rules**: public, or a list of rules like `IELTS > 7.0` or `Remote Work is checked`.
  - Operators depend on type: numbers use `= ≠ > ≥ < ≤`, text uses `equals/contains`, Boolean uses `is checked`, dates use `before/after`, and One-of-many uses `is`.
  - Show only the operators valid for the chosen attribute.
- [ ] Checking access for one candidate: load that user's values in **one query**, load positions and rules in **another**, and evaluate in C#. This is simple and avoids loops of queries.
- [ ] "Positions available to me" endpoint uses that check.
- [ ] Positions table: checkbox column, toolbar for Edit, Duplicate, Delete. Include a CV-count column and a link to the position's CV table.
- [ ] Update the timestamp when a position is edited (used by "Latest Positions").

**Done when:** you can build "Junior Data Engineer @ Acme Corp" from the spec scenario, and a restricted position is hidden from a candidate who fails the rule.

## Phase 8: CV generation

- [ ] "Create CV" for an accessible position creates only a small `Cv` row (user, position, Draft). One CV per position is enforced by the unique index.
- [ ] **The CV is virtual.** To show it, query the position's attributes joined with the user's values, plus the user's projects filtered by the position's tags (recent first, limited to max projects). Never copy values into the CV.
- [ ] Empty values appear **red**. Clicking one lets the user fill it in place. That saves to the user's own attribute value, so it updates everywhere.
- [ ] If a required attribute isn't in the profile yet, it appears automatically as an empty field.
- [ ] **Publish** button: enabled only when all attributes have values. **Validate again on the server** before changing the state to Published.
- [ ] Recruiters see only Published CVs, read-only.
- [ ] If the candidate no longer passes the position's access rules, hide the CV (check at read time).
- [ ] The CVs table for a position: checkbox column, likes count, toolbar actions, and no row buttons.

**Done when:** the Acme scenario works end to end, including the auto-added CAP field and Publish.

## Phase 9: Main page, search, discussions, and likes

- [ ] **Main page queries** (one query each):
  - latest positions by created/updated time
  - top 5 by count of Published CVs (`GroupBy`)
  - tag cloud (tags with counts) using `react-tagcloud`. Clicking goes to CVs for recruiters and positions for candidates.
  - statistics: CVs created in the last 24 hours, and totals for positions, candidates, recruiters, and submitted CVs.
- [ ] **Full-text search** in the header on every page. Use Postgres full-text search through EF Core (`tsvector` generated columns with a GIN index, queried with `EF.Functions.ToTsVector(...).Matches(...)`). Search positions, projects, and profile values. Show results in a table.
- [ ] **Discussions**: a tab per position, Markdown posts in time order, author name linking to their profile. Load new posts by polling every 3 seconds (`refetchInterval: 3000` in React Query). SignalR is an optional upgrade.
- [ ] Only authenticated users can post.
- [ ] **Likes**: recruiters toggle one like per CV (unique index prevents doubles). Show the total in lists and search results, calculated in the same query.

**Done when:** a post typed in one browser appears in another within about 5 seconds.

## Phase 10: Administrator features, UI, and language

- [ ] Admin users table with checkboxes and a toolbar: Block, Unblock, Delete, Add role, Remove role. Admins can remove their own admin role.
- [ ] Admin can view and edit any profile, CV, and position.
- [ ] **i18n**: use `react-i18next` with English plus one more language. Translate only UI text, not user-entered content. Save the language choice.
- [ ] **Themes**: set `data-bs-theme="light|dark"` on `<html>`. Save the choice (localStorage, or your user record).
- [ ] **Responsive**: use Bootstrap's grid and `table-responsive`. Test at phone width.
- [ ] Loading and error states everywhere.
- [ ] Positions and CVs appear as **tables only**, never tiles or galleries.

**Done when:** you can switch language and theme, and every page is usable on a phone screen.

## Phase 11: Quality check, final deployment, and defense prep

- [ ] Go through the Strict DON'Ts one by one:
  - no raw `SELECT *` or full-table scans
  - no database queries inside loops (use `Include`, projections, or batch queries)
  - no image files on your server or DB
  - no buttons in table rows, and no tiles
  - no JSON-serialized CVs
  - no tables created on the fly
- [ ] Test each role: anonymous, candidate, recruiter, admin.
- [ ] Test version conflicts with two browser tabs.
- [ ] Test the full Acme scenario on the **deployed** site, not only on localhost.
- [ ] Check production settings: OAuth redirect URIs, Cloudinary keys, Facebook app set to Live, and migrations applied.
- [ ] Wake the free host before your demo (open the site a few minutes early).
- [ ] Write a README covering the architecture, the DB diagram, how to run, and the environment variables.
- [ ] **Defense preparation**: be able to open any file and explain every line. Practice small live changes such as adding a field, changing a filter, or adding an operator.

**Done when:** a friend can log in on your live URL and finish the scenario.

## Phase 12: Bonus (only if the core is 100% done)

- [ ] PDF export with a QR code linking back to the CV.
- [ ] Email confirmation for form-based login.
- [ ] Badges or achievements, downloadable as an SVG panel.
- [ ] Field tuning: length limits, regex validators, value ranges.
- [ ] Export a position's CVs to CSV or Excel.

---

### Beginner rules of thumb

1. One vertical slice at a time: database, then API, then React screen. Don't build all the backend first.
2. Use libraries for anything a library already does (Markdown, uploads, tables, tags).
3. Commit small and often, and redeploy after each phase.
4. If you can't explain a piece of code, rewrite it in a simpler way you can explain.