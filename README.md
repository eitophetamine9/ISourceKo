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
  <a href="https://tailwindcss.com/" target="_blank">
    <img src="https://img.shields.io/badge/Tailwind_CSS_v3-06B6D4?style=flat&logo=tailwind-css&logoColor=white" alt="Tailwind CSS" />
  </a>
  <a href="https://developer.mozilla.org/docs/Web/HTML" target="_blank">
    <img src="https://img.shields.io/badge/HTML5-E34F26?style=flat&logo=html5&logoColor=white" alt="HTML5" />
  </a>
  <a href="https://nodejs.org/" target="_blank">
    <img src="https://img.shields.io/badge/Node.js-339933?style=flat&logo=node.js&logoColor=white" alt="Node.js" />
  </a>
</p>

**ISourceKO** is a centralized study and research aggregator designed specifically for university and collegiate students. Instead of searching multiple tabs separately, students enter a topic once and receive consolidated, high-yield results from diverse academic and educational platforms—including **YouTube Video Lectures**, **ScienceDirect Peer-Reviewed Articles**, and **Semantic Scholar Citations**.

In addition, ISourceKO features a dedicated **Peer Exchange Section** (Optional Challenge) where students can upload photos of their own handwritten notes, formula cheat sheets, diagrams, or share direct links to external study resources with real-time upvoting and community feedback.

Repository: **[https://github.com/eitophetamine9/ISourceKo](https://github.com/eitophetamine9/ISourceKo)**

---

## Aesthetic Direction: Modern Collegiate Archive

- **Typography**: Editorial two-font pairing:
  - **Display / Headings / Citations**: `Newsreader` (Variable Optical Serif, Google Fonts)
  - **Interface / Metadata / Controls**: `Plus Jakarta Sans` (Geometric Humanist Sans, Google Fonts)
- **Palette**: Archival Bone (`#FBFBF9`), Deep Oxford Ink (`#0C131F`), Tactile Platform Accents (YouTube Crimson `#DC2626`, ScienceDirect Ochre `#D97706`, Semantic Scholar Slate-Blue `#0284C7`, Student Evergreen `#059669`, Electric Flame `#FF5400`). Strict zero-purple policy.
- **Dynamic Interactions**:
  - **Scrolling**: Seamless floating scrollbar, dynamic top reading progress bar, and sticky blur masthead.
  - **Hovering**: Card physical lift (`-translate-y-1`), image zoom (`img-zoom-frame`), underline draw.
  - **Clicking**: Tactile button press (`active:scale-[0.96]`), checkmark flash on citation copy, burst animation on upvotes.
- **Heterogeneous Content Layout**:
  - **Academic Papers**: High-density citation dossier with DOI link, publication year, and instant citation copy.
  - **Video Lectures**: 16:9 media-first preview with duration pill, educator verification, and direct watch link.
  - **Student Notes**: Notebook index-card styling with full-resolution handwritten preview lightbox.

---

## Architecture & Tech Stack

- **Framework**: **ASP.NET Core Blazor Web App** (Interactive Server mode).
  > **Note**: Uses Blazor Web (Server) as the main project instead of Blazor WebAssembly, ensuring fast initial loads, direct server-side data access, secure API key handling, and zero browser WebAssembly overhead.
- **Styling**: **Tailwind CSS (v3)** + Semantic HTML5.
- **Language**: **C# 13 / .NET 10** (compatible with .NET 8 LTS).
- **Package Management**: NuGet (.NET) + npm (Tailwind CSS CLI).

---

## Project Directory Structure

```text
ISourceKo/
├── Components/                         # Blazor UI Layer
│   ├── App.razor                       # Root HTML shell linking Newsreader & Plus Jakarta Sans
│   ├── Routes.razor                    # Blazor router and route table
│   ├── _Imports.razor                  # Global Razor namespace imports
│   ├── Layout/                         # Layout components
│   │   ├── MainLayout.razor            # Responsive master layout with sticky masthead & footer
│   │   └── NavMenu.razor               # Navigation bar with badges, active links, and mobile drawer
│   ├── Pages/                          # Application pages
│   │   ├── Home.razor                  # Multi-Platform Search Hub & Aggregator stream
│   │   ├── Community.razor             # Student Submissions Study Hall (Peer Notes & Links)
│   │   ├── SubmitSource.razor          # Form for students to deposit notes/diagrams or links
│   │   ├── Bookmarks.razor             # Student's saved syllabus & bibliography exporter
│   │   ├── NotFound.razor              # Tailwind styled 404 page
│   │   └── Error.razor                 # Error boundary handler
│   └── Shared/                         # Reusable UI components
│       ├── SearchBar.razor             # Search desk with segmented corpus toggles & trending chips
│       ├── SourceCard.razor            # Heterogeneous card (Citation dossier for papers, video player for lectures)
│       ├── StudentSourceCard.razor     # Peer card supporting image zoom preview & upvotes
│       ├── PlatformBadge.razor         # Tactile platform badge with icons for each corpus
│       ├── ImageModal.razor            # Darkroom lightbox modal for student handwritten notes
│       ├── LoadingSkeleton.razor       # Shimmer loading skeleton
│       └── EmptyState.razor            # Placeholder state for zero search results
│
├── Models/                             # Domain Entities & DTOs
│   ├── Enums/
│   │   ├── SourcePlatform.cs           # YouTube, ScienceDirect, SemanticScholar, Community
│   │   ├── SubmissionType.cs           # Link, ImageNote, Document
│   │   └── AcademicField.cs            # CS, Medicine, Engineering, Natural Sciences, etc.
│   ├── SourceItem.cs                   # Normalized entity representing a source across platforms
│   ├── StudentSubmission.cs            # Entity for student-submitted notes/photos/links & comments
│   ├── SearchQuery.cs                  # Search filter criteria (query, platform, field, sort)
│   └── SearchResponse.cs               # Search result envelope with metrics and metadata
│
├── Services/                           # Business Logic & External Data Integration
│   ├── Interfaces/
│   │   ├── ISourceAggregatorService.cs # Multi-provider concurrent query coordinator
│   │   ├── ISourceProvider.cs          # Extensible provider contract
│   │   ├── IStudentSubmissionService.cs# Student notes & links CRUD with upvotes
│   │   ├── IFileStorageService.cs      # Handling uploaded pictures of study notes
│   │   └── IBookmarkService.cs         # Managing student saved items
│   ├── Providers/
│   │   ├── YouTubeSourceProvider.cs    # Video tutorials & lecture playlists provider
│   │   ├── ScienceDirectSourceProvider.cs # Elsevier peer-reviewed journal papers provider
│   │   ├── SemanticScholarSourceProvider.cs # Open-access academic citations provider
│   │   └── CommunitySourceProvider.cs  # Searches within peer-uploaded study notes
│   ├── SourceAggregatorService.cs      # Executes all providers concurrently via Task.WhenAll
│   ├── StudentSubmissionService.cs     # Manages submissions (with high-yield seed data)
│   ├── LocalFileStorageService.cs      # Stores student photos in wwwroot/uploads/
│   └── BookmarkService.cs              # Personal study bookmarks service
│
├── Styles/                             # CSS Source Directory
│   └── tailwind.css                    # Tailwind base directives & micro-interaction components
│
├── wwwroot/                            # Static Web Assets
│   ├── css/
│   │   └── tailwind.min.css            # Minified compiled Tailwind CSS bundle
│   └── uploads/
│       └── student-submissions/        # Storage folder for student-uploaded note photos
│
├── appsettings.json                    # Configuration for API keys and file upload quotas
├── package.json                        # Node dependencies (tailwindcss, postcss, autoprefixer)
├── tailwind.config.js                  # Tailwind configuration matching Razor files
├── postcss.config.js                   # PostCSS plugins
├── Program.cs                          # ASP.NET Core DI container and request pipeline
└── ISourceKO.csproj                    # .NET project specification
```

---

## Getting Started

### 1. Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (Version 8.0, 9.0, or 10.0)
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
Then navigate to **`http://localhost:5000`** in your browser.
