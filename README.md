# ISourceKO - Student Academic Source Hub

**ISourceKO** is a centralized study and research aggregator designed specifically for students. Instead of searching multiple tabs separately, students enter a topic once and receive consolidated, high-yield results from diverse academic and educational platforms—including **YouTube Video Lectures**, **ScienceDirect Peer-Reviewed Articles**, and **Semantic Scholar Citations**.

In addition, ISourceKO features a dedicated **Peer Contributions Section** (Optional Challenge) where students can upload pictures of their own handwritten notes, formula cheat sheets, diagrams, or share direct links to external study resources with real-time upvoting and community feedback.

---

## 🛠️ Architecture & Tech Stack

- **Framework**: **ASP.NET Core Blazor Web App** (Interactive Server mode).
  > **Note**: Uses Blazor Web (Server) as the main project instead of Blazor WebAssembly, ensuring fast initial loads, direct server-side data access, secure API key handling, and zero browser WebAssembly overhead.
- **Styling**: **Tailwind CSS (v3)** + Semantic HTML5.
- **Language**: **C# 13 / .NET 10** (also compatible with .NET 8 LTS).
- **Package Management**: NuGet (.NET) + npm (Tailwind CSS CLI).

---

## 📂 Project Directory Structure

```text
ISourceKo/
├── Components/                         # Blazor UI Layer
│   ├── App.razor                       # Root HTML shell linking Tailwind CSS & Google Fonts
│   ├── Routes.razor                    # Blazor router and route table
│   ├── _Imports.razor                  # Global Razor namespace imports
│   ├── Layout/                         # Layout components
│   │   ├── MainLayout.razor            # Responsive master layout with sticky navbar & footer
│   │   └── NavMenu.razor               # Navigation bar with badges, active links, and mobile menu
│   ├── Pages/                          # Application pages
│   │   ├── Home.razor                  # Multi-Platform Search Hub & Aggregator stream
│   │   ├── Community.razor             # Student Submissions hub (Peer Notes & Links)
│   │   ├── SubmitSource.razor          # Form for students to upload notes/diagrams or links
│   │   ├── Bookmarks.razor             # Student's saved sources collection
│   │   ├── NotFound.razor              # Tailwind styled 404 page
│   │   └── Error.razor                 # Error boundary handler
│   └── Shared/                         # Reusable UI components
│       ├── SearchBar.razor             # Search bar with platform toggles & trending chips
│       ├── SourceCard.razor            # Aggregated source card (YouTube / ScienceDirect / etc.)
│       ├── StudentSourceCard.razor     # Peer card supporting image previews & upvotes
│       ├── PlatformBadge.razor         # Color-coded badge with icons for each platform
│       ├── ImageModal.razor            # Lightbox modal for student handwritten notes
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
│   └── tailwind.css                    # Tailwind base directives & custom components
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

## 🚀 Getting Started

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
dotnet run
```
Then navigate to `http://localhost:5000` (or the HTTPS URL displayed in your console).

---

## 💡 Key Highlights

1. **True Blazor Web App (Interactive Server)**:
   Avoids the massive download size and slow startup of Blazor WebAssembly, giving students immediate sub-second page loads.
2. **Unified Search Aggregator**:
   Queries YouTube, ScienceDirect, Semantic Scholar, and Student Notes simultaneously via `Task.WhenAll`.
3. **Peer Submissions (Challenge)**:
   Students can upload photos of handwritten notes (with full-size modal viewer) or share interactive links, complete with real-time upvotes.
4. **Tailwind CSS Design System**:
   Zero Bootstrap dependencies. Clean, modern, responsive interface with custom badges and smooth animations.
