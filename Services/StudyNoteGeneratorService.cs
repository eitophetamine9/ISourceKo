using System.Text;
using ISourceKO.Models;
using ISourceKO.Models.Enums;

namespace ISourceKO.Services;

/// <summary>
/// Generates structured, discipline-specific study notes for system-premade starter entries
/// and provides reusable study note templates for peer submissions.
/// </summary>
public static class StudyNoteGeneratorService
{
    public record NoteTemplate(string Name, string Description, string Content);

    private static readonly HashSet<string> KnownStarterTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Handwritten Quick Reference: Graph Algorithms & Dijkstra Complexity",
        "Feedback Control Systems: Root Locus & Bode Stability Analysis",
        "Cardiovascular Hemodynamics & 12-Lead ECG Interpretation Matrix",
        "Cellular Respiration & Chemiosmosis Electron Flow Schematic",
        "Multivariable Calculus: Vector Fields, Curl, Divergence & Stokes Theorem",
        "Qualitative Research Methodologies & Thematic Analysis Framework",
        "Microeconomics: Market Structures, Game Theory & Elasticity Reference",
        "Western Art History Movements & Stylistic Chronology Guide",
        "Organic Chemistry Reaction Pathways Mindmap & Mechanism Guide",
        "Electromagnetism & Maxwell Equations Formula Sheet (Annotated)",
        "Cognitive Biases, Memory Models & Neurotransmitter Study Matrix",
        "Constitutional Law: Bill of Rights, Judicial Review & Tiered Scrutiny",
        "Financial Statements & Cash Flow Ratio Quick Guide",
        "Constructivist Learning Theories & Bloom's Revised Taxonomy Matrix",
        "Clinical Pharmacology: IV Flow Rates, Dosage Math & High-Alert Meds",
        "Architectural Structural Systems & Gravity/Lateral Load Path Guide",
        "Mass Communication Theories: Framing, Agenda-Setting & Cultivation",
        "Historiographical Schools & Primary Source Internal/External Criticism",
        "Literary Critical Theories & Close Reading Analysis Matrix",
        "Biogeochemical Cycles & Anthropogenic Climate Forcing Pathways",
        "Deep Learning Architectures: Multi-Head Attention, Transformers & CNNs",
        "OWASP Top 10 Web Vulnerabilities & Applied Defense Cheat Sheet",
        "Structural Analysis: Shear Force & Bending Moment Diagrams (SFD & BMD)",
        "Operational Amplifier Configurations & Frequency Compensation Guide",
        "Fluid Dynamics: Navier-Stokes Simplifications & Boundary Layer Analysis",
        "Binary Phase Diagrams & Gibbs Phase Rule Quick Reference",
        "Epidemiological Measures: Incidence, Prevalence, Odds Ratios & R0",
        "Antibiotic Classes, Mechanisms of Action & Resistance Pathways",
        "CRISPR-Cas9 Gene Editing Workflow & sgRNA Design Protocol",
        "Econometric Modeling: Gauss-Markov OLS Assumptions & Diagnostic Tests",
        "Strategic Management Frameworks: Porter's Five Forces, VRIO & SWOT Matrix",
        "Classical Social Theory: Marx, Weber, Durkheim & Bourdieu Matrix",
        "Public Policy Cycle & Comparative Governance Models Study Guide",
        "Music Theory: Four-Part Voice Leading, Roman Numeral Analysis & Cadences",
        "Philippine Post-Colonial Historiography & Agrarian Reform Timeline",
        "Normative Ethics Comparison: Deontology, Utilitarianism & Virtue Ethics",
        "Sustainable Destination Management & Eco-Tourism Carrying Capacity"
    };

    public static bool IsKnownStarterSubmission(string? id, string title)
    {
        if (!string.IsNullOrWhiteSpace(id) &&
            id.StartsWith("sub-", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(title) && KnownStarterTitles.Contains(title.Trim());
    }

    public static void EnsureHydrated(StudentSubmission submission)
    {
        if (!submission.IsSystemGenerated && IsKnownStarterSubmission(submission.Id, submission.Title))
        {
            submission.IsSystemGenerated = true;
        }

        if (string.IsNullOrWhiteSpace(submission.NoteContent) && submission.IsSystemGenerated)
        {
            submission.NoteContent = GenerateStructuredNote(
                submission.Title,
                submission.Description,
                submission.Field,
                submission.Tags);
        }
    }

    public static string GenerateStructuredNote(
        string title,
        string description,
        AcademicField field,
        IEnumerable<string>? tags = null)
    {
        var tagList = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Take(6).ToList() ?? new List<string>();
        var disciplineName = field.GetDisplayName();
        var (referenceBlock, pitfalls, reviewPrompts) = GetDisciplineModule(field, title);

        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine($"Discipline: {disciplineName} | Source: System-Generated Starter Study Sheet");
        if (tagList.Count > 0)
        {
            sb.AppendLine($"Topics: {string.Join(", ", tagList)}");
        }
        sb.AppendLine();
        sb.AppendLine("## 1. Core Summary & Objective");
        sb.AppendLine(description.Trim());
        sb.AppendLine();
        sb.AppendLine("## 2. High-Yield Reference & Key Formulas");
        sb.AppendLine(referenceBlock.Trim());
        sb.AppendLine();
        sb.AppendLine("## 3. Common Exam & Lab Pitfalls");
        foreach (var pitfall in pitfalls)
        {
            sb.AppendLine($"- {pitfall}");
        }
        sb.AppendLine();
        sb.AppendLine("## 4. Active Recall Checkpoints");
        for (int i = 0; i < reviewPrompts.Length; i++)
        {
            sb.AppendLine($"{i + 1}. {reviewPrompts[i]}");
        }

        return sb.ToString().Trim();
    }

    public static IReadOnlyList<NoteTemplate> GetSubmissionTemplates() => new List<NoteTemplate>
    {
        new(
            "Formula & Equation Sheet",
            "Structured layout for definitions, equations, boundary conditions, and units.",
            """
            ## 1. Core Topic Overview
            State the central theorem, system, or phenomenon covered in this sheet.

            ## 2. Essential Equations & Variables
            - Equation 1: [Expression] — where [variable] represents [unit/meaning]
            - Equation 2: [Expression] — valid when [boundary condition]

            ## 3. Worked Problem Pattern
            1. Identify knowns and assumptions.
            2. Select governing rule or formula.
            3. Verify dimensional consistency and edge cases.

            ## 4. Common Exam Traps
            - Watch out for sign conventions or unit conversions.
            """),
        new(
            "Concept Comparison Matrix",
            "Side-by-side breakdown contrasting competing models, theories, or algorithms.",
            """
            ## 1. Scope of Comparison
            Summarize what concepts or frameworks are being contrasted.

            ## 2. Side-by-Side Breakdown
            | Criterion | Concept A | Concept B |
            | --- | --- | --- |
            | Core Mechanism | ... | ... |
            | Best Used When | ... | ... |
            | Key Limitation | ... | ... |

            ## 3. Key Takeaways
            - Distinguish Concept A vs. Concept B by checking [primary indicator].
            """),
        new(
            "Lab / Clinical Protocol",
            "Step-by-step procedural checklist with safety checks and diagnostic indicators.",
            """
            ## 1. Purpose & Clinical/Lab Indication
            Describe the objective of the procedure or experimental workflow.

            ## 2. Step-by-Step Procedure
            1. Preparation & baseline calibration / patient assessment.
            2. Primary intervention or assay execution.
            3. Verification metrics & expected normal ranges.

            ## 3. Critical Red Flags & Mitigations
            - If [abnormal reading], verify [control / corrective action].
            """)
    };

    private static (string ReferenceBlock, string[] Pitfalls, string[] ReviewPrompts) GetDisciplineModule(AcademicField field, string title) => field switch
    {
        AcademicField.ComputerScience => (
            """
            - Dijkstra's Algorithm (Binary Heap): O((V + E) log V) time; greedy relaxation fails if negative edge weights exist.
            - Bellman-Ford Algorithm: O(V * E) time; handles negative edges and detects negative-weight cycles on the V-th pass.
            - A* Search: f(n) = g(n) + h(n); optimal when heuristic h(n) is admissible (never overestimates) and consistent.
            - Floyd-Warshall (All-Pairs): O(V^3) time, O(V^2) space via dp[k][i][j] = min(dp[k-1][i][j], dp[k-1][i][k] + dp[k-1][k][j]).
            """,
            new[]
            {
                "Running Dijkstra on graphs with negative edges produces silently wrong shortest paths.",
                "Forgetting to skip stale priority queue entries (dist > d[u]) degrades performance or corrupts parent pointers."
            },
            new[]
            {
                "Why does Dijkstra require non-negative edge weights to preserve its greedy invariant?",
                "How do you reconstruct the shortest path and detect a negative cycle using Bellman-Ford?"
            }),

        AcademicField.Engineering => (
            """
            - Root Locus Angles: Asymptote angles = (2k + 1) * 180 deg / (n - m); centroid = (sum(poles) - sum(zeros)) / (n - m).
            - Gain Margin (GM): 1 / |G(j*omega_pc)| in dB at phase crossover (-180 deg).
            - Phase Margin (PM): 180 deg + angle(G(j*omega_gc)) at gain crossover (|G| = 1 / 0 dB).
            - PID Transfer Function: C(s) = Kp + Ki/s + Kd*s (proportional reduces rise time, integral eliminates steady-state error, derivative adds damping).
            """,
            new[]
            {
                "Confusing phase crossover frequency (where phase = -180 deg) with gain crossover frequency (where magnitude = 0 dB).",
                "Forgetting non-minimum phase zeros in the right-half plane reverse stability predictions on standard Bode plots."
            },
            new[]
            {
                "How does increasing derivative gain Kd affect overshoot and settling time on the Root Locus?",
                "If Phase Margin is negative, where do the closed-loop poles lie in the s-plane?"
            }),

        AcademicField.MedicineAndHealth => (
            """
            - Cardiac Cycle Heart Sounds: S1 (mitral/tricuspid closure, onset of isovolumetric contraction); S2 (aortic/pulmonic closure, onset of diastole).
            - Mean Arterial Pressure: MAP = DBP + 1/3(SBP - DBP) = CO * TPR.
            - ECG Lead Territory Mapping: Inferior (II, III, aVF -> RCA); Anterior (V1-V4 -> LAD); Lateral (I, aVL, V5-V6 -> LCx).
            - STEMI Criteria: >= 1 mm ST elevation in >= 2 contiguous limb leads (or >= 2 mm in V2-V3) with reciprocal ST depressions.
            """,
            new[]
            {
                "Overlooking right-sided (V4R) or posterior (V7-V9) leads in inferior wall MI presentations.",
                "Confusing normal S3 in young athletes with pathologic volume-overload S3 in heart failure."
            },
            new[]
            {
                "During which Wiggers diagram phase is aortic pressure rising while ventricular pressure exceeds it?",
                "Which coronary artery is implicated if ST elevations appear in leads II, III, and aVF with ST depression in aVL?"
            }),

        AcademicField.NaturalSciences => (
            """
            - Glycolysis (Cytosol): Glucose (6C) -> 2 Pyruvate (3C); net yield = 2 ATP + 2 NADH (rate-limiting enzyme: PFK-1).
            - Pyruvate Dehydrogenase & Citric Acid Cycle (Mitochondrial Matrix): 2 Pyruvate -> 6 CO2 + 8 NADH + 2 FADH2 + 2 GTP.
            - Oxidative Phosphorylation (Inner Mitochondrial Membrane): Complex I, III, IV pump H+ into intermembrane space.
            - Net ATP Stoichiometry: ~30-32 ATP per glucose (~2.5 ATP/NADH, ~1.5 ATP/FADH2 via ATP synthase F0F1 rotor).
            """,
            new[]
            {
                "Confusing substrate-level phosphorylation (Glycolysis, Krebs) with oxidative phosphorylation (ETC + chemiosmosis).",
                "Forgetting that Complex II (succinate dehydrogenase) accepts electrons from FADH2 without pumping protons."
            },
            new[]
            {
                "Why do NADH electrons yield more ATP than FADH2 electrons across the electron transport chain?",
                "How does an uncoupler like DNP affect oxygen consumption, proton gradient, and ATP synthesis?"
            }),

        AcademicField.Mathematics => (
            """
            - Divergence (div F = nabla . F): Measures outward flux density per unit volume.
            - Curl (curl F = nabla x F): Measures local circulation/rotation density around an axis.
            - Green's Theorem (2D): closed line integral (P dx + Q dy) = double integral_D (dQ/dx - dP/dy) dA.
            - Stokes' Theorem (3D): closed line integral_C F . dr = surface integral_S (nabla x F) . dS.
            - Divergence Theorem: closed surface integral_S F . dS = triple integral_E (nabla . F) dV.
            """,
            new[]
            {
                "Reversing the right-hand rule boundary orientation between C and surface normal n in Stokes' Theorem.",
                "Applying Green's Theorem across a singularity (such as origin in (-y/(x^2+y^2), x/(x^2+y^2))) without excising it."
            },
            new[]
            {
                "Under what domain condition does curl F = 0 guarantee F is conservative?",
                "How do you choose an outward vs. upward unit normal vector when converting a flux integral?"
            }),

        AcademicField.Chemistry => (
            """
            - SN2: Bimolecular (Rate = k[Sub][Nuc]), 1 step, backside attack -> inversion of stereochemistry; favored by 1 deg > 2 deg, strong nucleophile, polar aprotic solvent (DMSO, acetone).
            - SN1: Unimolecular (Rate = k[Sub]), 2 steps via carbocation intermediate -> racemization; favored by 3 deg > 2 deg, weak nucleophile, polar protic solvent (H2O, EtOH).
            - E2: Concerted anti-periplanar beta-H abstraction; Zaitsev (more substituted alkene) with unhindered base, Hofmann with bulky base (t-BuOK).
            - E1: Carbocation intermediate competing with SN1 at elevated temperatures.
            """,
            new[]
            {
                "Attempting SN2 on a tertiary alkyl halide (steric hindrance blocks backside attack completely).",
                "Missing carbocation hydride or methyl shifts that rearrange SN1/E1 intermediates."
            },
            new[]
            {
                "Why do polar protic solvents slow SN2 nucleophiles while stabilizing SN1 leaving groups?",
                "When does a secondary substrate with NaOEt favor E2 over SN2?"
            }),

        AcademicField.PhysicsAndAstronomy => (
            """
            - Gauss's Law (Electric): div E = rho / epsilon_0  |  closed integral E . dA = Q_enc / epsilon_0.
            - Gauss's Law (Magnetism): div B = 0  |  closed integral B . dA = 0 (no magnetic monopoles).
            - Faraday's Law: curl E = -dB/dt  |  emf = -dPhi_B/dt (Lenz's law opposition).
            - Ampere-Maxwell Law: curl B = mu_0 J + mu_0 epsilon_0 dE/dt (displacement current term yields EM wave speed c = 1/sqrt(mu_0 epsilon_0)).
            """,
            new[]
            {
                "Omitting Maxwell's displacement current term inside charging capacitors where conduction current J = 0.",
                "Applying symmetric Gaussian surfaces when charge density lacks spherical, cylindrical, or planar symmetry."
            },
            new[]
            {
                "How do Faraday's Law and the Ampere-Maxwell Law decouple into the 3D electromagnetic wave equation in vacuum?",
                "What boundary conditions govern perpendicular E and parallel B across a dielectric interface?"
            }),

        AcademicField.AccountingAndFinance => (
            """
            - Core Accounting Identity: Assets = Liabilities + Shareholders' Equity; Operating Cash Flow = Net Income + Non-Cash Expenses - Delta Working Capital.
            - Liquidity Ratios: Current Ratio = Current Assets / Current Liabilities; Quick Ratio = (Cash + Marketable Sec + AR) / Current Liabilities.
            - Solvency & Leverage: Debt-to-Equity = Total Debt / Total Equity; Interest Coverage = EBIT / Interest Expense.
            - DuPont ROE Identity: ROE = (Net Income / Sales) * (Sales / Avg Assets) * (Avg Assets / Avg Equity).
            """,
            new[]
            {
                "Classifying interest paid or dividends received inconsistently between US GAAP and IFRS on the Cash Flow Statement.",
                "Including inventory in the Quick Ratio numerator."
            },
            new[]
            {
                "Which three operational levers drive Return on Equity under the DuPont decomposition?",
                "Why can a profitable company with rising Net Income still experience negative Operating Cash Flow?"
            }),

        AcademicField.NursingAndAlliedHealth => (
            """
            - Oral / IV Bolus Dose Formula: Dose Required = (Desired / Have) * Vehicle Volume.
            - IV Gravity Flow Rate: Drops/min (gtt/min) = (Total Volume mL * Drop Factor gtt/mL) / Time in minutes.
            - Pediatric Weight-Based Dosing: Verify safe dose range in mg/kg/day before dividing per q6h/q8h/q12h administration.
            - Critical Antidotes: Opioids -> Naloxone; Heparin -> Protamine Sulfate; Warfarin -> Vitamin K; Acetaminophen -> N-Acetylcysteine; Benzodiazepines -> Flumazenil.
            """,
            new[]
            {
                "Rounding IV pump rates or pediatric doses prematurely before completing dimensional analysis.",
                "Confusing microdrip tubing (60 gtt/mL) with macrodrip tubing (10, 15, or 20 gtt/mL)."
            },
            new[]
            {
                "Calculate gtt/min for 1,000 mL D5W infused over 8 hours using 15 gtt/mL macrodrip tubing.",
                "What safety verification must precede every high-alert anticoagulant or insulin administration?"
            }),

        AcademicField.DataScienceAndAI => (
            """
            - Scaled Dot-Product Attention: Attention(Q, K, V) = softmax((Q * K^T) / sqrt(d_k)) * V.
            - Multi-Head Attention: Projects Q, K, V across h subspaces in parallel to attend to syntactic and semantic relations simultaneously.
            - CNN Output Dimension: floor((W - K + 2P) / S) + 1 for input width W, kernel K, padding P, stride S.
            - Residual & LayerNorm: Output = LayerNorm(x + Sublayer(x)) stabilizes gradient flow across deep transformer blocks.
            """,
            new[]
            {
                "Omitting the 1/sqrt(d_k) scaling factor pushes softmax into regions with vanishingly small gradients.",
                "Using BatchNorm instead of LayerNorm on variable-length sequence tokens causes batch-size instability."
            },
            new[]
            {
                "Why do transformers inject positional encodings alongside token embeddings?",
                "How does causal masking in a decoder self-attention block prevent future token leakage?"
            }),

        AcademicField.Cybersecurity => (
            """
            - Broken Access Control (A01): Enforce server-side object-level authorization checks on every endpoint; deny by default.
            - Injection / SQLi (A03): Replace string-concatenated queries with parameterized prepared statements and strict allowlists.
            - Cross-Site Scripting (XSS): Context-aware output encoding, HttpOnly/SameSite cookies, and strict Content-Security-Policy (CSP).
            - SSRF (A10): Block private RFC 1918 / link-local IP ranges (169.254.169.254), disable HTTP redirects, validate destination hostnames after DNS resolution.
            """,
            new[]
            {
                "Relying on client-side validation or hidden form fields for authorization or price integrity.",
                "Filtering SSRF URLs by regex on the string host before resolving DNS (vulnerable to DNS rebinding and decimal IP encoding)."
            },
            new[]
            {
                "Explain how parameterized queries eliminate SQL injection regardless of user input characters.",
                "What is the difference between Stored XSS, Reflected XSS, and DOM-based XSS?"
            }),

        AcademicField.Biotechnology => (
            """
            - CRISPR-Cas9 Target Requirements: ~20 nt protospacer guide RNA (sgRNA) immediately upstream of a 5'-NGG-3' PAM sequence (for SpCas9).
            - Double-Strand Break (DSB) Cleavage: Cas9 HNH and RuvC domains cleave 3 bp upstream of the PAM site.
            - Repair Outcome 1 — NHEJ (Non-Homologous End Joining): Active throughout cell cycle; error-prone indels -> gene knockout / frameshift.
            - Repair Outcome 2 — HDR (Homology-Directed Repair): Active in S/G2 phase with exogenous donor template -> precise knock-in or point mutation.
            """,
            new[]
            {
                "Designing sgRNA without confirming the 5'-NGG-3' PAM lies on the target DNA (not inside the sgRNA itself).",
                "Assuming HDR dominates in post-mitotic cells without suppressing NHEJ or optimizing homology arm length."
            },
            new[]
            {
                "Why is the PAM motif essential for SpCas9 self-vs-non-self discrimination?",
                "When would you prefer base editing or prime editing over canonical DSB-mediated HDR?"
            }),

        _ => (
            $"""
            - Primary Framework ({field.GetDisplayName()}): Identify the foundational axioms, definitions, and classification criteria emphasized in {title}.
            - Analytical Synthesis: Connect cause-and-effect relationships, boundary conditions, and empirical evidence supporting the topic.
            - Comparative Lenses: Contrast standard textbook models with edge cases or applied domain case studies.
            - Verification Metric: Confirm assumptions, methodology constraints, and primary source attribution before drawing conclusions.
            """,
            new[]
            {
                "Memorizing isolated terms without linking them to underlying mechanisms or decision criteria.",
                "Overgeneralizing conclusions outside the stated assumptions or experimental/historical scope."
            },
            new[]
            {
                $"How would you summarize the core thesis of '{title}' in three structured points without looking at your notes?",
                "What boundary condition or counterexample would invalidate the primary framework?"
            })
    };
}
