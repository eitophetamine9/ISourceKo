-- ==============================================================================
-- ISourceKO — Academic Source Hub & Student Exchange
-- Database Schema for Supabase (PostgreSQL)
-- Module: Part VI Project Milestone (CIT-U Application Development)
-- ==============================================================================

-- 1. Student Study Notes & Peer Submissions Table
create table if not exists public.student_submissions (
    id bigint generated always as identity primary key,
    title text not null,
    description text not null,
    field int not null default 0,
    type int not null default 0,
    source_url text,
    image_path text,
    student_name text not null default 'Anonymous Student',
    school_or_program text,
    upvotes int not null default 0 check (upvotes >= 0),
    tags text[] not null default '{}',
    deleted_at timestamptz,                              -- Soft Delete storage pattern (Part IV / VI)
    created_at timestamptz not null default now()
);

-- Fast Habit 1: Disciplinary and active-status filtering indexes
create index if not exists idx_student_submissions_field on public.student_submissions (field);
create index if not exists idx_student_submissions_deleted_at on public.student_submissions (deleted_at);

-- Lock 1: Grant permissions to anon role
grant select, insert, update on public.student_submissions to anon;

-- Lock 2: Row Level Security (RLS) policies
alter table public.student_submissions enable row level security;

-- Drop existing policies if re-running
drop policy if exists "read_active_notes" on public.student_submissions;
drop policy if exists "insert_note" on public.student_submissions;
drop policy if exists "update_note" on public.student_submissions;

-- Fast Habit 2: Database-level active record filtering (read only non-deleted rows)
create policy "read_active_notes" on public.student_submissions
    for select to anon using (deleted_at is null);

-- Allow students to submit new study materials
create policy "insert_note" on public.student_submissions
    for insert to anon with check (true);

-- Allow students to upvote and perform soft deletions
create policy "update_note" on public.student_submissions
    for update to anon using (true) with check (true);


-- 2. Student Syllabus Bookmarks Table
create table if not exists public.bookmarks (
    id bigint generated always as identity primary key,
    client_id text not null,
    source_id text not null,
    title text not null,
    author text,
    platform text,
    category text,
    url text,
    notes text,
    bookmarked_at timestamptz not null default now()
);

-- Fast Habit 1: Index on client_id for rapid student syllabus lookups
create index if not exists idx_bookmarks_client_id on public.bookmarks (client_id);

-- Grants & RLS for bookmarks
grant select, insert, update, delete on public.bookmarks to anon;
alter table public.bookmarks enable row level security;

drop policy if exists "bookmarks_all" on public.bookmarks;
create policy "bookmarks_all" on public.bookmarks
    for all to anon using (true) with check (true);


-- 3. Seed High-Yield Collegiate Starter Notes (If Table is Empty)
insert into public.student_submissions (title, description, field, type, image_path, student_name, school_or_program, upvotes, tags)
select * from (values
    ('Handwritten Quick Reference: Graph Algorithms & Dijkstra Complexity', 
     'My colored handwritten cheat sheet summarizing Dijkstra, Bellman-Ford, A*, and Floyd-Warshall with Big-O runtime comparisons and edge case notes for CS 201.', 
     1, 0, 'https://images.unsplash.com/photo-1434030216411-0b793f4b4173?w=800&auto=format&fit=crop&q=80', 'Alex Chen', 'BS Computer Science - Year 2', 46, array['Graph Theory', 'Algorithms', 'Handwritten Notes', 'Cheat Sheet']),
    
    ('Feedback Control Systems: Root Locus & Bode Stability Analysis', 
     'Step-by-step procedure for plotting Root Locus diagrams, calculating phase margin and gain margin on Bode plots, and tuning PID controllers for underdamped second-order systems.', 
     2, 0, 'https://images.unsplash.com/photo-1581092160607-ee22621dd758?w=800&auto=format&fit=crop&q=80', 'Devon Vance', 'BS Mechanical Engineering - Year 3', 39, array['Control Systems', 'Root Locus', 'Bode Plot', 'PID Tuning']),
    
    ('Cardiovascular Hemodynamics & 12-Lead ECG Interpretation Matrix', 
     'Comprehensive clinical matrix covering Wiggers diagram pressure curves, cardiac cycle phases, and systematic 12-lead ECG vector analysis for STEMI, NSTEMI, and arrhythmias.', 
     3, 0, 'https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=800&auto=format&fit=crop&q=80', 'Sarah Jenkins', 'Doctor of Medicine - Year 2', 58, array['Cardiology', 'ECG Interpretation', 'Wiggers Diagram', 'Hemodynamics']),

    ('Cellular Respiration & Chemiosmosis Electron Flow Schematic', 
     'Color-annotated pathway map detailing Glycolysis, Pyruvate Oxidation, Citric Acid Cycle, and Complex I-IV electron transport chain proton gradients and ATP synthase stoichiometry.', 
     4, 0, 'https://images.unsplash.com/photo-1530497610245-94d3c16cda28?w=800&auto=format&fit=crop&q=80', 'Maya Patel', 'BS Biology - Year 2', 42, array['Cellular Biology', 'Metabolism', 'Krebs Cycle', 'Electron Transport']),

    ('Multivariable Calculus: Vector Fields, Curl, Divergence & Stokes Theorem', 
     'Hand-derived cheat sheet connecting Greens Theorem, Stokes Theorem, and Divergence Theorem in 2D and 3D with geometric intuition and flux calculation templates.', 
     5, 0, 'https://images.unsplash.com/photo-1509228468518-180dd4864904?w=800&auto=format&fit=crop&q=80', 'Kenji Sato', 'BS Applied Mathematics - Year 2', 51, array['Calculus III', 'Vector Calculus', 'Stokes Theorem', 'Flux']),

    ('Organic Chemistry Reaction Pathways Mindmap & Mechanism Guide', 
     'A complete illustrated reaction tree for SN1, SN2, E1, E2 mechanisms with solvent preferences, stereochemistry outcomes (inversion vs retention), and nucleophile strength rankings.', 
     9, 0, 'https://images.unsplash.com/photo-1532094349884-543bc11b234d?w=800&auto=format&fit=crop&q=80', 'Sarah Jenkins', 'Pre-Med / BS Chemistry - Year 2', 63, array['Organic Chemistry', 'Mindmap', 'Mechanisms', 'Stereochemistry']),

    ('Electromagnetism & Maxwell Equations Formula Sheet (Annotated)', 
     'One-page exam summary connecting Gauss law, Faraday law, and Ampere-Maxwell law in both integral and differential forms with boundary conditions and wave propagation derivations.', 
     10, 0, 'https://images.unsplash.com/photo-1635070041078-e363dbe005cb?w=800&auto=format&fit=crop&q=80', 'Kenji Sato', 'BS Physics - Year 3', 52, array['Electromagnetism', 'Maxwell Equations', 'Formula Sheet', 'Wave Equation']),

    ('Financial Statements & Cash Flow Ratio Quick Guide', 
     'Step-by-step linkage between Income Statement, Balance Sheet, and Statement of Cash Flows plus liquidity, solvency, and profitability ratios with DuPont analysis breakdown.', 
     13, 0, 'https://images.unsplash.com/photo-1554224155-6726b3ff858f?w=800&auto=format&fit=crop&q=80', 'Marco Delgado', 'BS Accountancy - Year 3', 45, array['Financial Accounting', 'Cash Flow', 'Ratios', 'DuPont Analysis']),

    ('Clinical Pharmacology: IV Flow Rates, Dosage Math & High-Alert Meds', 
     'Nursing clinical pocket summary: dimensional analysis dosage formulas, IV drop factor calculations, pediatric weight adjustments, and antidote cheat sheet (naloxone, protamine, vitamin K).', 
     15, 0, 'https://images.unsplash.com/photo-1584515979956-d9f6e5d09982?w=800&auto=format&fit=crop&q=80', 'Priya Sharma', 'BS Nursing - Year 3', 62, array['Nursing', 'Dosage Calculations', 'IV Rates', 'Clinical Pharmacology']),

    ('Deep Learning Architectures: Multi-Head Attention, Transformers & CNNs', 
     'Detailed architectural diagrams breaking down scaled dot-product attention, positional encodings, residual connections, layer normalization, and convolution feature map dimensions.', 
     21, 0, 'https://images.unsplash.com/photo-1555949963-ff9fe0c870eb?w=800&auto=format&fit=crop&q=80', 'Alex Chen', 'MS Data Science / AI - Year 1', 67, array['Deep Learning', 'Transformers', 'Self-Attention', 'Neural Networks']),

    ('OWASP Top 10 Web Vulnerabilities & Applied Defense Cheat Sheet', 
     'Defensive developer summary for SQLi, XSS, CSRF, SSRF, and broken access control with vulnerable code snippets, payload examples, and parameterized query mitigation recipes.', 
     22, 0, 'https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=800&auto=format&fit=crop&q=80', 'Tariq Mansour', 'BS Information Security - Year 3', 54, array['Cybersecurity', 'OWASP', 'Web Security', 'Penetration Testing']),

    ('CRISPR-Cas9 Gene Editing Workflow & sgRNA Design Protocol', 
     'Molecular biology protocol map detailing guide RNA target selection, PAM sequence recognition (NGG), double-strand break repair pathways (NHEJ vs HDR), and off-target screening.', 
     29, 0, 'https://images.unsplash.com/photo-1532187863486-abf9dbad1b69?w=800&auto=format&fit=crop&q=80', 'Amara Okafor', 'BS Biotechnology - Year 3', 53, array['Biotechnology', 'CRISPR', 'Gene Editing', 'Molecular Biology'])
) as v(title, description, field, type, image_path, student_name, school_or_program, upvotes, tags)
where not exists (select 1 from public.student_submissions);
