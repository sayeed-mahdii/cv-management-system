# Web-Based CV Management System

> **Course Final Project**  
> **Tech Stack:** ASP.NET Core Web API (.NET 10) · Entity Framework Core · PostgreSQL (Supabase) · Cloudinary · React (Vite) + Bootstrap

---

## ⚠️ STRICT RULES & GUARDRAILS (Read Before Coding)

### The Strict DON'Ts (Spec §2.2)
1. ❌ **Do NOT perform full database scans** using raw `SELECT *` queries. Always select only required fields or page your data.
2. ❌ **Do NOT upload image files to the web server or store them in the database** (as BLOBs or Base64). Images must go directly to Cloudinary; only URLs and public IDs are stored in the database.
3. ❌ **Do NOT execute database queries inside loops** (the $N+1$ problem). Always use EF Core `.Include()`, projections (`.Select()`), or batch queries.
4. ❌ **Do NOT add action buttons inside table rows**.
5. ❌ **Do NOT use JSON to serialize CVs for storage**. Store attributes in the structured EAV tables and reference them.
6. ❌ **Do NOT generate tables in the database on the fly**.

### Strict UI Constraints (Automatic -20% Grade Penalties) (Spec §5)
* 🚫 **Penalty 1: Buttons in table rows (-20%)**  
  Having `Edit` / `Delete` / `View` buttons directly in table rows will cost 20% of your grade.  
  👉 **Requirement:** Use a **top toolbar** with buttons that activate when table row checkboxes are selected, or use animated context menus.
* 🚫 **Penalty 2: Tile / Gallery views for positions and CVs (-20%)**  
  Representing positions or CVs as card tiles / galleries will cost 20% of your grade.  
  👉 **Requirement:** Positions and CVs must be rendered strictly as **tables**.

---

## 📋 Categorized Feature & Task Breakdown

### 🌟 Killer Features (The Heart of the Project)
* [ ] **Killer Feature #1: Reusable Attribute Library (Spec §4.3)**
  * Global attribute definitions managed by Recruiters.
  * Supported data types: `String`, `Text`, `Image`, `Numeric`, `Date`, `Period`, `Boolean`, `One of many`.
  * Lookup by prefix, recently used (`LastUsedAt`), and category filtering.
  * Unique name validation.
* [ ] **Killer Feature #2: Customizable Position Templates (Spec §4.4)**
  * Recruiter creates positions specifying required attributes from the library.
  * Access rules / filters based on attribute values (e.g. `IELTS > 7.0`, `Remote Work == true`).
  * Project filter tags & maximum project count.
  * Position duplication feature.
* [ ] **Killer Feature #3: Automatically Generated Virtual CVs (Spec §4.5)**
  * Dynamic CV assembly (no static copies stored).
  * Missing attributes highlighted in **red**.
  * In-place editing on the CV updates the user's master profile.
  * "Publish" action unlocked only when all attributes are filled.

### ⚙️ Core Requirements (Must-Haves)
* [ ] **Authentication & Roles (Spec §3)**
  * Social login via Supabase Auth (Google, GitHub/Facebook).
  * JWT Bearer authentication on ASP.NET Core Web API.
  * 3 Roles: `Candidate`, `Recruiter`, `Administrator`.
  * Non-authenticated public access: browse positions read-only, view public statistics.
* [ ] **Personal Profile (Spec §4.2)**
  * Mandatory built-in "Me" attributes (First Name, Last Name, Location, Personal Photo) running on the same EAV engine.
  * User-selected attributes ("Info" tab).
  * Projects list with Markdown description and tags created on first use.
  * Auto-save every 5–10 seconds with **Optimistic Locking** (`Version` concurrency token).
* [ ] **Main Page (Spec §4.1)**
  * Latest positions table.
  * Top 5 most popular positions (by submitted CV count).
  * Interactive Tag Cloud (links to Positions for Candidates, CVs for Recruiters).
  * Real-time statistics counters.
* [ ] **Discussions & Likes (Spec §4.6)**
  * Chronological Markdown discussion posts per position.
  * Updates visible to viewers within 2–5 seconds (polling every 3s).
  * Likes toggle by recruiters on CVs.
* [ ] **General UI/UX (Spec §4.7)**
  * Full-text search header on every page.
  * Internationalization (`i18n`): English + 1 other language.
  * Dark / Light mode switch (persisted).
  * Responsive layout (mobile & desktop).
* [ ] **Administrator Capabilities (Spec §3.4)**
  * User management table: block, unblock, delete, change roles.
  * Admins can relinquish their own admin role.

### 🎁 Optional Bonus Features (Spec §8 - Only after Core is 100% Done)
* [ ] PDF export with dynamic QR code linking to the live CV.
* [ ] Form-based authentication with email confirmation.
* [ ] Gamified badges/achievements system generating downloadable SVG panel.
* [ ] Advanced field tuning (regex validators, min/max range limits).
* [ ] Aggregate CSV/Excel export for position CVs.
