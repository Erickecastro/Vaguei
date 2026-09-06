# Vaguei

> A smarter way to discover job opportunities that match your profile.

![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet)
![Avalonia](https://img.shields.io/badge/Avalonia-12-8B44AC?style=for-the-badge)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp)
![Tests](https://img.shields.io/badge/tests-passing-success?style=for-the-badge)
![Platform](https://img.shields.io/badge/Desktop-Linux%20%7C%20Windows-blue?style=for-the-badge)
![Android](https://img.shields.io/badge/Android-experimental-3DDC84?style=for-the-badge&logo=android&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-success?style=for-the-badge)

# About

**Vaguei** is a privacy-conscious job-discovery application for desktop, with an experimental Android prototype. Desktop remains the primary development target.

Users can import a resume or search directly by role, technology, or company. Resume content is processed locally to identify professional context and relevant skills; contact details and other unnecessary personal data are discarded. Results keep the original application URL so the candidate always applies on the employer's or recruiting platform's page.

Software development is the initial validation domain because it reflects the first real test profile. The domain model is not restricted to technology and is being expanded to support administration, accounting, design, engineering, healthcare, logistics, sales, and other professions.

> Vaguei is not affiliated with the job platforms or employers listed below. Availability depends on their public endpoints and career pages.

# Current Features

### Desktop experience

* Avalonia desktop interface with light and grayscale dark themes
* Theme preference applied before window creation and persisted locally
* Resume selection and drag-and-drop
* Candidate sidebar with constrained scrolling and an adaptive overlay on narrow or short windows
* Responsive, resizable, movable, and maximizable custom window
* Direct search by role, technology, or company
* Mutually exclusive Brazil-only and international-only scopes
* Publication filters for 24 hours, 3 days, 7 days, 30 days, and 3 months
* Work-model filter for remote, hybrid, and on-site opportunities
* Advanced filters for location, contract type, and seniority, persisted locally between sessions
* Locally persisted favorite jobs with a saved-results filter
* Loading and search-attention states that keep work off the UI thread
* Search refresh that clears stale results immediately and presents an explicit loading state
* Theme-aware four-second startup introduction with a fixed logo and smooth fade sequence
* Keyboard search submission with Enter
* Compact source-health summary without exposing provider exception details
* Dismissible connection warning for confirmed network loss, without exposing provider exceptions
* Original job link for every result
* In-app About area covering purpose, privacy, terms, licenses, and source authorization status
* Fixed compact footer with copyright and MIT license identifier

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
* Source-health diagnostics showing responsive providers and collected vacancy totals
* Shared concurrency limit, per-source timeout, one retry for transient network failures, and a bounded query cache; desktop keeps successful catalogs locally for 30 minutes across restarts
* Brazilian location recognition and national-only filtering
* Publication-date filtering
* Duplicate removal using stable provider identifiers plus canonical employer names, title, location, URL, and description similarity
* Role normalization and controlled search-term expansion, including Portuguese and English internship variants
* Controlled bilingual variants for common technology, data, HR, accounting, healthcare, and logistics roles
* Compatibility based on role and skills, with penalties for missing core or required skills
* Limited, explainable experience-gap adjustment when both the vacancy requirement and dated résumé history are available
* Compatibility is displayed only when a resume has been analyzed
* Ranking by compatibility and recency, with compatibility hidden for direct searches without a resume
* Seniority inferred from explicit direct-search terms such as junior, senior, trainee, internship, and lead

# Job Sources

Vaguei currently reads anonymous, read-only job data from public endpoints or public career pages offered by:

* Arbeitnow
* Ashby
* Greenhouse
* InHire
* Jobicy
* Jooble (optional, with an official API key)
* Lever
* Remotive
* SmartRecruiters
* Workable

The repository contains a curated employer catalog for sources that require a board, tenant, site, company, or account identifier. This includes the public Sidia page on InHire and Brazilian coverage such as CI&T, Wildlife Studios, dLocal, EBANX, QuintoAndar, Wellhub, and others across the supported providers. The shared catalog lives in [`config/job-sources.json`](config/job-sources.json), is copied into Desktop and CLI builds, and falls back to validated built-in defaults when it is missing or malformed.

The current integration does **not** scrape authenticated or protected pages. LinkedIn, Gupy, Catho, and similar platforms will only be integrated through an official API, an approved partnership, an employer-owned public feed, or another method explicitly permitted by their terms. Vaguei does not attempt to bypass authentication, anti-bot protection, rate limits, or access controls.

Jooble support is enabled only when an official credential is available. Request a key through the [Jooble API portal](https://jooble.org/api/about), then provide it at runtime without committing it:

```bash
export JOOBLE_API_KEY="your-key"
dotnet run --project src/Vaguei.Desktop
```

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
├── Vaguei.Mobile          # Shared small-screen Avalonia presentation
├── Vaguei.Android         # Experimental Android entry point and package
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
* No production mobile release; Android is an installed experimental prototype and iOS has not started

These technologies should be added only when a concrete product requirement justifies their operational and privacy cost.

# Getting Started

The current development baseline requires the .NET 10 SDK. Clone the repository and run commands from its root.

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

### Linux

Install the .NET 10 SDK using your distribution or Microsoft's official packages. Restore and start the desktop application:

```bash
dotnet restore Vaguei.slnx
dotnet run --project src/Vaguei.Desktop
```

To create a self-contained per-user installation with desktop entry and icons:

```bash
./packaging/linux/install-user.sh
```

The detailed Linux packaging behavior is documented in [`packaging/linux/README.md`](packaging/linux/README.md).

### Windows

Install the .NET 10 SDK, open PowerShell in the repository, and run:

```powershell
dotnet restore Vaguei.slnx
dotnet run --project src/Vaguei.Desktop
```

To generate a self-contained Windows x64 directory:

```powershell
dotnet publish src/Vaguei.Desktop -c Release -r win-x64 --self-contained true
```

The executable is produced under `src/Vaguei.Desktop/bin/Release/net10.0/win-x64/publish/`. An installer and code signing are not implemented yet.

### Android experimental

Android development additionally requires the .NET Android workload, Android SDK, `adb`, and JDK 21:

```bash
dotnet workload install android
dotnet restore Vaguei.Mobile.slnx
dotnet build src/Vaguei.Android/Vaguei.Android.csproj \
  -p:AndroidSdkDirectory="$ANDROID_SDK_ROOT" \
  -p:JavaSdkDirectory="$JAVA_HOME"
```

With USB debugging enabled and one device visible in `adb devices`, install the signed debug APK:

```bash
adb install -r src/Vaguei.Android/bin/Debug/net10.0-android/com.erickecastro.vaguei-Signed.apk
adb shell monkey -p com.erickecastro.vaguei -c android.intent.category.LAUNCHER 1
```

The Android application is a test build, not a Play Store release. Its current capabilities, limitations and complete device checklist are documented in [`docs/MOBILE_PROTOTYPE.md`](docs/MOBILE_PROTOTYPE.md) and [`docs/ANDROID_TESTING.md`](docs/ANDROID_TESTING.md).

For local diagnostics, run the CLI with a resume path:

```bash
dotnet run --project src/Vaguei.Cli -- "/path/to/resume.pdf"
```

Resume contents are not printed by default. Use `--show-raw` only in a controlled local environment because resumes contain personal data.

# Platform Status

Linux and Windows are the tested desktop development environments. Avalonia also supports macOS, but this repository does not yet provide a validated macOS build. Release installers, signing and automatic updates are not implemented.

The Android prototype is implemented while desktop remains the primary product. Avalonia 12 and .NET 10 allow the domain, application, collectors, ViewModel, and most UI resources to be shared, while Android keeps a separate entry project, native document picker, lifecycle handling, portrait layout, and small-screen navigation.

The Android prototype compiles in the separate [`Vaguei.Mobile.slnx`](Vaguei.Mobile.slnx) solution. It reuses the desktop search ViewModel and provides a four-second mobile introduction, local resume import, direct and profile-based search, compact expandable filters, virtualized results, favorites, compatibility explanations, connectivity feedback, persistent theme, the same five institutional tabs as desktop, original links, and a compact footer. Mobile searches use bounded concurrent access to the nine configured sources, short per-source and overall limits, partial-result preservation, and network-loss cancellation. The Android Activity is locked to portrait and uses a non-resizing keyboard mode for smoother input. Desktop-only window operations are replaced by Android-native behavior. See the [device testing guide](docs/ANDROID_TESTING.md).

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
* Matching considers only explicit experience-year requirements and does not yet model education, language proficiency, compensation, or mandatory location constraints in depth.
* Desktop keeps successful source responses in a bounded local cache for 30 minutes, but there is no long-lived searchable vacancy index.
* Favorites are stored only on the current device and are not synchronized.
* Accessibility, localization, installers, update delivery, and end-to-end UI automation still need production validation.

# Roadmap

### Near term

* Expand authorized public career sources and Brazilian employer coverage
* Broaden role and skill taxonomies beyond software development
* Add accessibility checks and automated desktop UI tests

### Matching evolution

* Refine experience duration with month-level dates and role relevance
* Compare seniority, education, languages, and work-model requirements
* Distinguish mandatory, preferred, and contextual requirements more precisely
* Calibrate scores against reviewed, anonymized examples
* Make every score component visible and auditable to the user

### Distribution

* Produce reproducible Linux, Windows, and macOS release artifacts
* Add installers, application metadata, signing, and update delivery
* Extract reusable presentation components into a shared UI project
* Stabilize and profile the existing Android prototype before considering store distribution
* Begin iOS work only after the Android experiment and desktop workflow are stable

# Privacy and Responsible Access

Vaguei should collect only the information required to search and rank vacancies. Resume analysis is local in the current version, and unnecessary contact or identity data is sanitized before profile analysis.

New providers must use documented APIs, explicitly public employer feeds, licensed aggregators, or written authorization. Credentials must never be committed to the repository, and a provider failure must never expose resume contents in logs or diagnostics.

Jobicy and Remotive results retain their canonical vacancy URLs and source attribution. Their public feeds are cached locally according to provider guidance, so changing filters does not generate unnecessary API traffic.

### License scope

The Vaguei source code is distributed under the permissive MIT License. This choice keeps reuse, contribution, modification, and commercial distribution straightforward while requiring preservation of the copyright and license notice. MIT applies only to code and documentation owned by the project: it does not grant rights over employer names, platform brands, logos, vacancy descriptions, or third-party datasets. Dependency and provider terms remain independently applicable. Apache-2.0 is the principal future alternative if the project needs an explicit contributor patent grant; changing the project license requires confirmation from every relevant copyright holder.

Project governance documents are versioned with the code:

* [Terms of Use](docs/TERMS_OF_USE.md)
* [Privacy](docs/PRIVACY.md)
* [Data Inventory](docs/DATA_INVENTORY.md)
* [LGPD Readiness](docs/LGPD_READINESS.md)
* [Integrations and Partnerships](docs/PARTNERSHIPS.md)
* [Partnership Request Templates](docs/PARTNERSHIP_REQUEST_TEMPLATES.md)
* [Security Policy](SECURITY.md)
* [MIT License](LICENSE)

These documents describe the current experimental application. They require legal review, an official contact channel, and an explicit acceptance/versioning mechanism before commercial distribution or remote personal-data processing.

Before committing changes, run the local credential check:

```bash
./scripts/check-secrets.sh
```

The check complements review and repository secret scanning; it does not make committing credentials safe. If a credential ever reaches Git history, revoke and rotate it immediately.

# Contributing

Contributions, ideas, and corrections are welcome. Before submitting a change:

* Keep domain and application logic independent from the UI.
* Add automated tests for parsers, matching rules, filters, and collectors.
* Preserve source failure isolation and original application URLs.
* Do not add scraping that violates access controls or platform terms.
* Run the complete build and test suite.

# License

This project is licensed under the [MIT License](LICENSE).
