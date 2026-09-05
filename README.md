# Vaguei

> A smarter way to discover job opportunities that match your profile.

![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet)
![Avalonia](https://img.shields.io/badge/Avalonia-12-8B44AC?style=for-the-badge)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp)
![Tests](https://img.shields.io/badge/tests-passing-success?style=for-the-badge)
![Platform](https://img.shields.io/badge/Desktop-Linux%20%7C%20Windows%20%7C%20macOS-blue?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-success?style=for-the-badge)

# About

**Vaguei** is an experimental, privacy-conscious desktop application for discovering and ranking job opportunities from multiple public career sources.

Users can import a resume or search directly by role, technology, or company. Resume content is processed locally to identify professional context and relevant skills; contact details and other unnecessary personal data are discarded. Results keep the original application URL so the candidate always applies on the employer's or recruiting platform's page.

Software development is the initial validation domain because it reflects the first real test profile. The domain model is not restricted to technology and is being expanded to support administration, accounting, design, engineering, healthcare, logistics, sales, and other professions.

> Vaguei is not affiliated with the job platforms or employers listed below. Availability depends on their public endpoints and career pages.

# Current Features

### Desktop experience

* Avalonia desktop interface with light and grayscale dark themes
* Resume selection and drag-and-drop
* Fixed candidate sidebar with adaptive overlay on narrow windows
* Responsive, resizable, movable, and maximizable custom window
* Direct search by role, technology, or company
* Brazil-only and Brazil-plus-international scopes
* Publication filters for 24 hours, 3 days, 7 days, 30 days, and 3 months
* Work-model filter for remote, hybrid, and on-site opportunities
* Advanced filters for location, contract type, and seniority, persisted locally between sessions
* Locally persisted favorite jobs with a saved-results filter
* Loading and search-attention states that keep work off the UI thread
* Non-destructive refresh that keeps previous results visible until an updated search completes
* Theme-aware four-second startup introduction with a fixed logo and smooth fade sequence
* Keyboard search submission with Enter
* Source failure warnings without interrupting successful providers
* Dismissible connection warning with automatic timeout when every source is unreachable
* Original job link for every result

### Resume processing

* ODT, DOCX, PDF, and TXT text extraction
* Candidate name and professional-title identification
* Professional experience, company, role, and employment-period extraction
* Skill aliases, evidence, categories, and relevance levels
* Resume-section classification
* Removal of email addresses, phone numbers, URLs, and empty contact labels before analysis
* Local processing with no resume upload performed by the current application

### Search and matching

* Parallel source orchestration and isolated provider failures
* Per-source result diagnostics for measuring real search coverage
* Shared concurrency limit, per-source timeout, one retry for transient network failures, and a five-minute in-memory query cache limited to 32 entries per source
* Brazilian location recognition and national-only filtering
* Publication-date filtering
* Duplicate removal using stable provider identifiers plus company, title, location, URL, and description similarity
* Role normalization and controlled search-term expansion, including Portuguese and English internship variants
* Controlled bilingual variants for common technology, data, HR, accounting, healthcare, and logistics roles
* Compatibility based on role and skills, with penalties for missing core or required skills
* Compatibility is displayed only when a resume has been analyzed
* Ranking by compatibility and recency, with compatibility hidden for direct searches without a resume

# Job Sources

Vaguei currently reads anonymous, read-only job data from public endpoints or public career pages offered by:

* Arbeitnow
* Ashby
* Greenhouse
* InHire
* Lever
* SmartRecruiters
* Workable

The repository contains a curated employer catalog for sources that require a board, tenant, site, company, or account identifier. This includes the public Sidia page on InHire and a mix of Brazilian and international employers across the other providers. The shared catalog lives in [`config/job-sources.json`](config/job-sources.json), is copied into Desktop and CLI builds, and falls back to validated built-in defaults when it is missing or malformed.

The current integration does **not** scrape authenticated or protected pages. LinkedIn, Gupy, Catho, and similar platforms will only be integrated through an official API, an approved partnership, an employer-owned public feed, or another method explicitly permitted by their terms. Vaguei does not attempt to bypass authentication, anti-bot protection, rate limits, or access controls.

# Architecture

The solution separates domain rules, application services, external collectors, resume parsing, and presentation:

```text
Vaguei
├── Vaguei.Domain          # Entities, value objects, and enums
├── Vaguei.Application     # Analysis, filtering, matching, and orchestration
├── Vaguei.ResumeParser    # ODT, DOCX, PDF, and TXT readers
├── Vaguei.Collectors      # Public job-source adapters
├── Vaguei.Infrastructure  # Reserved for persistence and platform services
├── Vaguei.Desktop         # Avalonia desktop application
├── Vaguei.Cli             # Local diagnostics
└── Vaguei.Tests           # Automated test suite
```

The current application flow is:

```text
Resume or direct query
        |
        v
Resume parsing and sanitization (when provided)
        |
        v
Candidate profile and search terms
        |
        v
Parallel public-source collectors
        |
        v
Geography and publication filters
        |
        v
Cross-source deduplication
        |
        v
Matching and ranking
        |
        v
Desktop results with original application links
```

# Tech Stack

### In use

* C# and .NET 10
* Avalonia 12
* CommunityToolkit.Mvvm
* xUnit
* LINQ, regular expressions, JSON, XML, and HTTP APIs

### Not currently in use

* No cloud backend or REST service
* No database or user account system
* No Docker runtime requirement
* No external generative-AI service
* No telemetry or resume upload
* No production Android or iOS application

These technologies should be added only when a concrete product requirement justifies their operational and privacy cost.

# Getting Started

The current development baseline requires the .NET 10 SDK.

Restore, build, and test the complete solution:

```bash
dotnet restore Vaguei.slnx
dotnet build Vaguei.slnx --no-restore
dotnet test Vaguei.slnx --no-build
```

Run the desktop application:

```bash
dotnet run --project src/Vaguei.Desktop
```

For local diagnostics, run the CLI with a resume path:

```bash
dotnet run --project src/Vaguei.Cli -- "/path/to/resume.pdf"
```

Resume contents are not printed by default. Use `--show-raw` only in a controlled local environment because resumes contain personal data.

# Desktop Packaging

The desktop application targets Linux, Windows, and macOS through Avalonia. Linux and Windows are the currently tested development environments; release installers and code signing are not yet implemented.

For a per-user Linux installation without `sudo`:

```bash
./packaging/linux/install-user.sh
```

This publishes a self-contained build, installs the desktop entry and the required freedesktop icon sizes, and refreshes the available desktop caches when supported. See [`packaging/linux/README.md`](packaging/linux/README.md) for details.

Android and iOS remain a later phase. A faithful mobile port will require responsive navigation, platform file pickers, lifecycle handling, secure local storage, accessibility validation, and dedicated device testing; sharing domain code does not by itself guarantee an identical mobile experience.

# Matching Model

When a resume is available, the current score combines:

* Up to 50% for normalized role similarity
* Up to 50% for skill evidence, weighted by relevance
* Penalties for missing skills classified as core or required

The score is intentionally explainable and deterministic. It is not a hiring prediction and should not be treated as an assessment of candidate quality. Direct searches without a resume do not display a compatibility percentage.

# Current Limitations

* The configured employer catalog does not discover every company automatically.
* Public APIs can change, rate-limit requests, omit publication dates, or become unavailable.
* Search breadth is constrained by the configured public sources and employers.
* The initial skill and role taxonomies are strongest for software development.
* Matching does not yet model education, language proficiency, years of experience, compensation, or mandatory location constraints in depth.
* Results are fetched live and cached only in memory for five minutes; there is no persistent local index yet.
* Favorites are stored only on the current device and are not synchronized.
* Accessibility, localization, installers, update delivery, and end-to-end UI automation still need production validation.

# Roadmap

### Near term

* Add richer per-source diagnostics and an optional persistent cache
* Expand authorized public career sources and Brazilian employer coverage
* Broaden role and skill taxonomies beyond software development
* Improve cross-source deduplication with canonical employer identities
* Add locally persisted search preferences and an optional favorites view
* Add accessibility checks and automated desktop UI tests

### Matching evolution

* Consider years and recency of professional experience
* Compare seniority, education, languages, and work-model requirements
* Distinguish mandatory, preferred, and contextual requirements more precisely
* Calibrate scores against reviewed, anonymized examples
* Make every score component visible and auditable to the user

### Distribution

* Produce reproducible Linux, Windows, and macOS release artifacts
* Add installers, application metadata, signing, and update delivery
* Begin Android and iOS work only after the desktop workflow and shared core are stable

# Privacy and Responsible Access

Vaguei should collect only the information required to search and rank vacancies. Resume analysis is local in the current version, and unnecessary contact or identity data is sanitized before profile analysis.

New providers must use documented APIs, explicitly public employer feeds, licensed aggregators, or written authorization. Credentials must never be committed to the repository, and a provider failure must never expose resume contents in logs or diagnostics.

# Contributing

Contributions, ideas, and corrections are welcome. Before submitting a change:

* Keep domain and application logic independent from the UI.
* Add automated tests for parsers, matching rules, filters, and collectors.
* Preserve source failure isolation and original application URLs.
* Do not add scraping that violates access controls or platform terms.
* Run the complete build and test suite.

# License

This project is licensed under the [MIT License](LICENSE).
