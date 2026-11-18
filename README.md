# ISourceKO — Academic Source Hub & Student Exchange

<p align="center">
  <a href="https://dotnet.microsoft.com/" target="_blank">
    <img src="https://img.shields.io/badge/.NET_10-512BD4?style=flat&logo=dotnet&logoColor=white" alt=".NET 10" />
  </a>
  <a href="https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor" target="_blank">
    <img src="https://img.shields.io/badge/Blazor_Web_App-512BD4?style=flat&logo=blazor&logoColor=white" alt="Blazor Web" />
  </a>
  <a href="https://learn.microsoft.com/dotnet/csharp/" target="_blank">
    <img src="https://img.shields.io/badge/C%23_13-239120?style=flat&logo=c-sharp&logoColor=white" alt="C#" />
  </a>
  <a href="https://learn.microsoft.com/ef/core/" target="_blank">
    <img src="https://img.shields.io/badge/EF_Core_10-512BD4?style=flat&logo=dotnet&logoColor=white" alt="Entity Framework Core" />
  </a>
  <a href="https://www.sqlite.org/" target="_blank">
    <img src="https://img.shields.io/badge/SQLite-003B57?style=flat&logo=sqlite&logoColor=white" alt="SQLite" />
  </a>
  <a href="https://tailwindcss.com/" target="_blank">
    <img src="https://img.shields.io/badge/Tailwind_CSS_v3-06B6D4?style=flat&logo=tailwind-css&logoColor=white" alt="Tailwind CSS" />
  </a>
  <a href="https://openalex.org/" target="_blank">
    <img src="https://img.shields.io/badge/OpenAlex_API-0F766E?style=flat&logoColor=white" alt="OpenAlex API" />
  </a>
  <a href="https://developer.mozilla.org/docs/Web/HTML" target="_blank">
    <img src="https://img.shields.io/badge/HTML5-E34F26?style=flat&logo=html5&logoColor=white" alt="HTML5" />
  </a>
  <a href="https://nodejs.org/" target="_blank">
    <img src="https://img.shields.io/badge/Node.js-339933?style=flat&logo=node.js&logoColor=white" alt="Node.js" />
  </a>
  <a href="https://isourceko-web-production.up.railway.app" target="_blank">
    <img src="https://img.shields.io/badge/Live_Demo-Railway-0B0D0E?style=flat&logo=railway&logoColor=white" alt="Live Demo on Railway" />
  </a>
</p>

**ISourceKO** is a centralized study and research aggregator designed specifically for university and collegiate students. Instead of searching multiple tabs separately, students enter a topic once and receive consolidated, high-yield results from diverse academic and educational platforms—including **Live OpenAlex Open-Access Works**, **Semantic Scholar Citations**, **ScienceDirect Peer-Reviewed Articles**, and **YouTube Video Lectures**.

In addition, ISourceKO features a SQLite-persisted **Peer Exchange Archive** where students deposit handwritten notes, formula cheat sheets, diagrams, or study links with zoomable darkroom inspection, peer discussion comments, and device-guarded upvoting.

Live Deployment: **[https://isourceko-web-production.up.railway.app](https://isourceko-web-production.up.railway.app)**  
Repository: **[https://github.com/eitophetamine9/ISourceKo](https://github.com/eitophetamine9/ISourceKo)**

---

## Core Capabilities (Increment 2)

- **Live Open-Access Academic Search & Resilient Fallback**:
  - Queries **OpenAlex** (250M+ scholarly works, open-access PDFs, reconstructed abstracts, DOIs), **Semantic Scholar Graph API**, **YouTube Educational Search / Data API v3**, and **Elsevier ScienceDirect** concurrently via `Task.WhenAll`.
  - Automatic timeout and rate-limit fallback ensures uninterrupted study sessions.
- **SQLite Persistence via Entity Framework Core**:
  - Auto-creates and seeds `isourceko.db` on startup using `IDbContextFactory<AppDbContext>`.
  - Persists student submissions, peer discussion comments, upvotes, and saved reading list bookmarks across sessions.
- **In-App YouTube Lecture Theater**:
  - Plays educational videos inside a distraction-free embedded theater modal (`youtube-nocookie.com`) alongside a locally persisted **Lecture Scratchpad** for timestamps and exam takeaways.
- **Multi-Style Academic Citation Suite**:
  - Formats and copies citations in **APA 7th**, **MLA 9th**, **Chicago 17th**, and **BibTeX** (including one-click `.bib` file export for individual sources or an entire saved syllabus).
- **Zoomable Note Inspector & Upvote Guard**:
  - Inspect handwritten diagrams at 50%–300% zoom with click-and-drag panning and 90-degree rotation.
  - Lightweight `localStorage` upvote de-duplication prevents vote inflation while allowing clean upvote toggling.

---

## Aesthetic Direction: Modern Collegiate Archive

- **Typography**: Editorial two-font pairing:
  - **Display / Headings / Citations**: `Newsreader` (Variable Optical Serif, Google Fonts)
  - **Interface / Metadata / Controls**: `Plus Jakarta Sans` (Geometric Humanist Sans, Google Fonts)
- **Palette**: Archival Bone (`#FBFBF9`), Deep Oxford Ink (`#0C131F`), Tactile Platform Accents (YouTube Crimson `#DC2626`, OpenAlex Teal `#0F766E`, ScienceDirect Ochre `#D97706`, Semantic Scholar Slate-Blue `#0284C7`, Student Evergreen `#059669`, Electric Flame `#FF5400`). Strict zero-purple policy.
- **Dynamic Interactions**:
  - **Scrolling**: Lenis smooth scrolling, GSAP ScrollTrigger staggered masked hero reveals, scroll-velocity kinetic marquee, sticky split-screen telemetry, and top reading progress bar.
  - **Hovering & Clicking**: Magnetic action buttons, card lift, smooth image zoom, checkmark flash on citation copy, and burst animation on upvotes.

---

## Architecture & Tech Stack

- **Framework**: **ASP.NET Core Blazor Web App** (Interactive Server mode).
- **Persistence**: **Entity Framework Core 10** + **SQLite** (`isourceko.db`).
- **Styling**: **Tailwind CSS (v3)** + Semantic HTML5 (zero Bootstrap).
- **Motion**: **Lenis** + **GSAP / ScrollTrigger** (GPU-composited properties only).
- **Language**: **C# 13 / .NET 10**.

---

## Project Directory Structure

```text
ISourceKo/
├── Components/                             # Blazor UI Layer
│   ├── App.razor                           # Root HTML shell linking fonts, motion engine & storage helpers
│   ├── Routes.razor                        # Blazor router and route table
│   ├── _Imports.razor                      # Global Razor namespace imports
│   ├── Layout/                             # Layout components
│   │   ├── MainLayout.razor                # Responsive master layout with sticky masthead & footer
│   │   └── NavMenu.razor                   # Navigation bar with persisted bookmark badge & mobile drawer
│   ├── Pages/                              # Application pages
│   │   ├── Home.razor                      # Multi-Platform Search Hub, Video Theater & Citation Suite
│   │   ├── Community.razor                 # Student Submissions Study Hall (Peer Notes & Discussions)
│   │   ├── SubmitSource.razor              # Form for students to deposit notes/diagrams or links
│   │   ├── Bookmarks.razor                 # SQLite-persisted syllabus & multi-format bibliography exporter
│   │   ├── NotFound.razor                  # Tailwind styled 404 page
│   │   └── Error.razor                     # Error boundary handler
│   └── Shared/                             # Reusable UI components
│       ├── SearchBar.razor                 # Search desk with segmented corpus toggles & trending chips
│       ├── SourceCard.razor                # Heterogeneous card (Academic dossier + Video theater trigger)
│       ├── StudentSourceCard.razor         # Peer card with zoom preview, comments thread & upvote guard
│       ├── VideoPlayerModal.razor          # Distraction-free YouTube theater modal with lecture scratchpad
│       ├── CitationModal.razor             # APA 7th / MLA 9th / Chicago 17th / BibTeX generator & .bib export
│       ├── ImageModal.razor                # Zoomable, pannable & rotatable note darkroom inspector
│       ├── PlatformBadge.razor             # Tactile platform badge for each corpus
│       ├── LoadingSkeleton.razor           # Shimmer loading skeleton
│       └── EmptyState.razor                # Placeholder state for zero search results
│
├── Data/                                   # Persistence Layer (EF Core + SQLite)
│   ├── Entities/
│   │   ├── StudentSubmissionEntity.cs      # Student note & link table entity
│   │   ├── StudentCommentEntity.cs         # Peer discussion comments entity
│   │   └── BookmarkEntity.cs               # Saved study syllabus entity
│   ├── AppDbContext.cs                     # Entity Framework Core DbContext & index configurations
│   └── DatabaseInitializer.cs              # Automatic schema initialization & seed data loader
│
├── Models/                                 # Domain Entities & DTOs
│   ├── Enums/
│   │   ├── SourcePlatform.cs               # YouTube, OpenAlex, ScienceDirect, SemanticScholar, Community
│   │   ├── SubmissionType.cs               # Link, ImageNote, Document
│   │   └── AcademicField.cs                # CS, Medicine, Engineering, Natural Sciences, etc.
│   ├── SourceItem.cs                       # Normalized entity representing a source across platforms
│   ├── StudentSubmission.cs                # Student-submitted notes/photos/links & comments model
│   ├── SearchQuery.cs                      # Search filter criteria (query, platform, field, sort)
│   └── SearchResponse.cs                   # Search result envelope with metrics and metadata
│
├── Services/                               # Business Logic & External Data Integration
│   ├── Interfaces/
│   │   ├── ISourceAggregatorService.cs     # Multi-provider concurrent query coordinator
│   │   ├── ISourceProvider.cs              # Extensible provider contract
│   │   ├── IStudentSubmissionService.cs    # SQLite-backed student notes, upvotes & comments service
│   │   ├── IFileStorageService.cs          # Handling uploaded pictures of study notes
│   │   └── IBookmarkService.cs             # SQLite-backed study bookmarks service
│   ├── Providers/
│   │   ├── OpenAlexSourceProvider.cs       # Live OpenAlex open-access scholarly works connector
│   │   ├── SemanticScholarSourceProvider.cs# Live Semantic Scholar citation graph connector
│   │   ├── YouTubeSourceProvider.cs        # YouTube Data API v3 & educational video parser
│   │   ├── ScienceDirectSourceProvider.cs  # Elsevier peer-reviewed journal connector
│   │   └── CommunitySourceProvider.cs      # Queries SQLite peer-uploaded study notes
│   ├── CitationFormatter.cs                # Pure formatter for APA 7th, MLA 9th, Chicago 17th & BibTeX
│   ├── SourceAggregatorService.cs          # Concurrent fan-out & balanced provider interleaving
│   ├── StudentSubmissionService.cs         # EF Core SQLite repository for student archive
│   ├── LocalFileStorageService.cs          # Stores student photos in wwwroot/uploads/
│   └── BookmarkService.cs                  # Write-through cached SQLite bookmark service
│
├── Styles/                                 # CSS Source Directory
│   └── tailwind.css                        # Tailwind base directives & micro-interaction components
│
├── wwwroot/                                # Static Web Assets
│   ├── css/tailwind.min.css                # Minified compiled Tailwind CSS bundle
│   ├── js/                                 # Lenis, GSAP, animations & localStorage helpers
│   └── uploads/student-submissions/        # Storage folder for student-uploaded note photos
│
├── appsettings.json                        # Connection string, API keys & file upload quotas
├── package.json                            # Node dependencies (tailwindcss, postcss, autoprefixer)
├── tailwind.config.js                      # Tailwind configuration matching Razor files
├── Program.cs                              # ASP.NET Core DI container, SQLite init & pipeline
└── ISourceKO.csproj                        # .NET project specification
```

---

## Getting Started

### 1. Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (Version 10.0)
- [Node.js](https://nodejs.org/) (Version 18+)

### 2. Compile Tailwind CSS
To build the Tailwind stylesheet:
```bash
npm run build:css
```
Or for live watching during component UI development:
```bash
npm run watch:css
```

### 3. Run the Blazor Web Application
```bash
dotnet run --launch-profile http
```
On first launch, ISourceKO automatically initializes `isourceko.db` with seeded study notes. Navigate to **`http://localhost:5000`** in your browser.
