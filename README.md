# reCruitoV &mdash; Dynamic CV &amp; Candidate Management System

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512bd4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React-19.0-61dafb?style=flat-square&logo=react)](https://react.dev/)
[![Vite](https://img.shields.io/badge/Vite-8.0-646cff?style=flat-square&logo=vite)](https://vitejs.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Supabase-336791?style=flat-square&logo=postgresql)](https://supabase.com/)
[![Cloudinary](https://img.shields.io/badge/Media-Cloudinary-3448c5?style=flat-square&logo=cloudinary)](https://cloudinary.com/)
[![Bootstrap 5](https://img.shields.io/badge/UI-Bootstrap%205-7952b3?style=flat-square&logo=bootstrap)](https://getbootstrap.com/)

**reCruitoV** is an enterprise-grade talent acquisition and candidate management platform designed to replace static, out-of-date resumes with dynamic, criteria-driven **Virtual CVs**. 

Built with **ASP.NET Core Web API (.NET 10)** and **React 19**, reCruitoV dynamically tailors candidate applications to job openings by matching technical tags, validating dynamic credentials on the fly, and assembling tailored portfolio representations without duplicating data or serializing static JSON blobs.

---

## 🌟 Key Highlights & Architectural Features

### 1. Dynamic Attribute Library (EAV Engine)
* **Zero Runtime Migrations**: Recruiters can define custom attributes across diverse data types (`String`, `Numeric`, `Date`, `Boolean`, and `OneOfMany` dropdowns) without modifying the relational database schema.
* **Built-in Protection**: Core identity attributes (*First Name*, *Last Name*, *Location*, and *Personal Photo*) run on the same dynamic engine while being shielded against accidental deletion.
* **Instant Prefix Search**: Attributes are indexed and searchable with fast prefix lookups (`ILIKE` / `StartsWith`) and category filtering.

### 2. Tailored Virtual CV Assembly (Killer Feature)
* **On-the-Fly Assembly**: Rather than storing static PDF or JSON copies of CVs, reCruitoV generates a Virtual CV in real time whenever a recruiter reviews an applicant.
* **Contextual Project Filtering**: Candidate projects are automatically filtered to match the position's required technology tags (e.g. `#Python`, `#SQL`) and limited to the recruiter's specified project quota (`MaxProjects`), showing the most relevant work first.
* **Requirements Compliance Checklist**: Evaluates candidate dynamic credentials against job criteria, clearly highlighting satisfied requirements and missing qualifications.

### 3. Signed Direct-to-Cloud Media Pipeline
* **Zero Binary Bloat**: Image binaries are never stored in the database as base64 or BLOBs, nor are they passed through the web server.
* **HMAC-SHA1 Signed Uploads**: The backend issues short-lived cryptographic upload signatures. Browsers stream photos directly to Cloudinary CDN over secure HTTPS, storing only secure CDN URLs and Cloudinary public IDs in PostgreSQL.

### 4. High-Concurrency Optimistic Locking
* **Version-Token Collision Handling**: User profiles, position templates, and portfolio projects carry integer `Version` concurrency tokens.
* **HTTP 409 Conflict Safety**: If two recruiters or candidate sessions submit overlapping updates, the backend immediately rejects the stale write with `409 Conflict`, prompting the user to reload the latest changes and preventing accidental overwrites.

### 5. High-Performance, Query-Optimized Database Access
* **Zero $N+1$ Query Anti-Patterns**: All repository queries use direct EF Core projections (`.Select()`) and compiled subqueries instead of executing SQL in application loops.
* **Strict Cascade Deletions**: Deleting a position or user cascades cleanly through relational tables (`PositionAttributes`, `PositionTags`, `Cvs`, `UserAttributeValues`) with database-level foreign key constraints.

### 6. Strict & Accessible UI Design Standards
* **Toolbar-Driven Actions**: Strictly zero action buttons inside table rows. Bulk operations (View, Edit, Duplicate, Delete, Inspect) are centralized in top toolbars activated by row selection checkboxes.
* **Dark / Light Theme Toggle**: Persistent theme switching using Bootstrap `data-bs-theme` and local storage synchronization.
* **Interactive Tag Cloud & Filters**: Clickable technology tags in tables instantly filter vacancies and portfolios with a single click.

---

## 🛠️ Technology Stack

| Layer | Technologies |
| :--- | :--- |
| **Backend API** | C# ASP.NET Core Web API (.NET 10.0), Entity Framework Core 10 |
| **Database** | PostgreSQL 16 (hosted on Supabase Cloud), Connection Pooling |
| **Authentication** | Supabase GoTrue Auth (OIDC Discovery, ES256 JWT Bearer verification) |
| **Media CDN** | Cloudinary .NET SDK & Cloudinary REST API |
| **Frontend SPA** | React 19, Vite 8, React Router v7, Context API |
| **Styling & Components**| Bootstrap 5.3, Bootstrap Icons, Custom Glassmorphic Dark/Light CSS |
| **Interactive Controls**| `react-select` (Creatable tags), `react-markdown` (Rich descriptions) |
| **Containerization** | Multi-stage Docker build with Debian ASP.NET runtime |

---

## 🚀 Getting Started

### Prerequisites
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
* [Node.js (v20.x or later)](https://nodejs.org/) & `npm`
* A [Supabase](https://supabase.com/) project (PostgreSQL + Auth)
* A [Cloudinary](https://cloudinary.com/) account (for signed media uploads)

---

### Installation & Local Setup

#### 1. Clone Repository
```bash
git clone https://github.com/sayeed-mahdii/cv-management-system.git
cd cv-management-system
```

#### 2. Configure Backend
Create or edit `backend/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<SUPABASE_HOST>;Port=5432;Database=postgres;Username=postgres.<PROJECT_ID>;Password=<PASSWORD>;"
  },
  "Supabase": {
    "Url": "https://<PROJECT_ID>.supabase.co",
    "JwtSecret": "<SUPABASE_JWT_SECRET>"
  },
  "Cloudinary": {
    "CloudName": "<CLOUDINARY_CLOUD_NAME>",
    "ApiKey": "<CLOUDINARY_API_KEY>",
    "ApiSecret": "<CLOUDINARY_API_SECRET>"
  }
}
```

Run database migrations and launch the backend:
```bash
cd backend
dotnet restore
dotnet run
```
*The backend API will start on `http://localhost:5190`.*

#### 3. Configure & Launch Frontend
Create `frontend/.env.local`:
```env
VITE_SUPABASE_URL=https://<PROJECT_ID>.supabase.co
VITE_SUPABASE_ANON_KEY=<SUPABASE_PUBLIC_ANON_KEY>
```

Install dependencies and start the Vite dev server:
```bash
cd ../frontend
npm install
npm run dev
```
*The frontend SPA will be live on `http://localhost:5173`.*

---

## 👥 User Roles & Permissions

* **Candidate**:
  * Build personal virtual profile (*First Name, Last Name, Location, Cloudinary photo*).
  * Select and populate dynamic credentials (*Certifications, Degrees, Test Scores*).
  * Maintain a portfolio of projects with Markdown formatting and auto-created technology tags.
  * Browse open positions and submit 1-click applications.
  * Review and withdraw active applications from the profile dashboard.

* **Recruiter**:
  * Manage the global Attribute Library (*Add, edit choices, categorize*).
  * Author position templates specifying required attributes, technology tags, and max project display limits.
  * 1-Click duplicate position templates.
  * Review applicant rosters per vacancy and inspect candidate Virtual CVs with dynamic project filtering.

* **Administrator**:
  * Complete governance over user accounts, role promotions (`Candidate`, `Recruiter`, `Administrator`), and account suspensions.
  * Unrestricted authoring across all platform positions and library attributes.

---

## 📁 Repository Structure

```text
cv-management-system/
├── backend/
│   ├── Controllers/          # RESTful controllers (Attributes, Positions, Profile, Auth)
│   ├── Data/                 # AppDbContext & EF Core configuration
│   ├── Filters/              # Global filters (BlockedUserFilter)
│   ├── Migrations/           # EF Core PostgreSQL schema migrations
│   ├── Models/
│   │   ├── DTOs/             # Strongly typed request/response transfer models
│   │   ├── Entities/         # Relational entities (EAV, Users, Positions, Projects, CVs)
│   │   └── Enums/            # Domain enums (DataType, CvState, Role)
│   ├── Services/             # CloudinaryService & SupabaseClaimsTransformation
│   ├── Dockerfile            # Production multi-stage Docker build
│   └── Program.cs            # Pipeline configuration & DI registrations
│
├── frontend/
│   ├── src/
│   │   ├── components/       # UI components (Navbar, AuthModal, ApplicantsModal, Inspector)
│   │   ├── context/          # Global AuthContext & theme state
│   │   ├── pages/            # View pages (HomePage, PositionsPage, ProfilePage, AttributesPage)
│   │   ├── services/         # Axios/Fetch API client wrapper
│   │   ├── App.jsx           # Main routing & layout assembly
│   │   └── index.css         # Custom styling & dark/light theme variables
│   ├── package.json          # Frontend dependencies & scripts
│   └── vite.config.js        # Vite bundler & reverse proxy config
│
└── README.md                 # Project documentation
```

---

## 📄 License
This project is open-source and available under the [MIT License](LICENSE).
