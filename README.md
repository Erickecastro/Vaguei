# Vaguei

> A smarter way to find job opportunities that match your profile.

![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge\&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge\&logo=c-sharp)
![xUnit](https://img.shields.io/badge/xUnit-Testing-512BD4?style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-Cross--Platform-blue?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-success?style=for-the-badge)

# About

**Vaguei** is a job discovery and matching application designed for professionals from any field. Its goal is to make the search for relevant opportunities simpler, smarter, and more transparent.

The application analyzes a candidate's resume, identifies professional skills, experience, education, and career context, and uses this information together with the user's preferences to search, organize, and rank opportunities from multiple job sources.

Software development is the initial validation domain because it reflects the first real test profile. The architecture and matching concepts must remain generic enough to support administration, accounting, design, engineering, healthcare, logistics, sales, and other professional fields.


# Features

* Resume text extraction
* Candidate profile analysis
* Skill and technology detection
* Technology alias recognition
* Professional experience extraction
* Company and job title identification
* Employment period detection
* Structured candidate profiles
* Skill relevance and evidence tracking
* Job freshness filtering
* Explainable job matching and ranking
* Job requirement classification
* Job source orchestration and failure isolation
* Duplicate job removal
* Public Ashby, Greenhouse, InHire, and Lever job board integrations
* Brazilian job location recognition
* Cross-platform execution

# Tech Stack

### Core

* C#
* .NET 10
* LINQ
* Regular Expressions
* XML processing

### Testing

* xUnit

### Planned

* Additional authorized job source integrations
* Broader profession and skill taxonomies
* REST APIs
* Docker
* Artificial Intelligence

# Architecture

The solution is divided into independent projects with clearly defined responsibilities.

```text
Vaguei
├── Vaguei.Domain
├── Vaguei.Application
├── Vaguei.ResumeParser
├── Vaguei.Collectors
├── Vaguei.Infrastructure
├── Vaguei.Cli
└── Vaguei.Tests
```

The current application flow follows this structure:

```text
Resume
   |
   v
Resume Parser
   |
   v
Resume Analyzer
   |
   v
Candidate Profile
   |
   v
Job Search
   |
   v
Source Orchestrator
   |
   v
Freshness and Deduplication
   |
   v
Matching Engine
   |
   v
Compatible Jobs
```

# Supported Resume Formats

* [x] ODT
* [x] DOCX
* [x] PDF
* [x] TXT

# Platform Direction

The core currently targets .NET 10 and is kept independent from the user interface.

The graphical interface uses Avalonia and currently targets:

* Linux desktop
* Windows desktop
* macOS desktop

Android and iOS are part of the long-term direction, after the desktop workflow is stable. The current CLI is a development and diagnostic interface, not the intended final product.

The graphical workflow should allow the user to select or drag a resume into the application, review the extracted profile, configure search preferences, and receive ranked opportunities with clear compatibility explanations.

An initial Avalonia desktop interface is available in `src/Vaguei.Desktop`. Run it with:

```bash
dotnet run --project src/Vaguei.Desktop
```

The current collector searches Arbeitnow and a curated catalog of public Ashby,
Greenhouse, InHire, and Lever career pages. This includes the official public
Sidia feed on InHire. The catalog can be extended without scraping protected job
platforms, and results always preserve the original application URL.

The window and Windows executable embed the Vaguei icon. Linux packages should
also install the files under `packaging/linux`; see
`packaging/linux/README.md` for the desktop-entry and icon locations required by
GNOME and other freedesktop environments.

For local diagnostics, the CLI accepts a resume path:

```bash
dotnet run --project src/Vaguei.Cli -- "/path/to/resume.pdf"
```

Resume contents are not printed by default. The optional `--show-raw` flag should only be used in a controlled local environment because resumes contain personal data.

# Goals

* Automate part of the job search process
* Support professionals from different fields
* Identify professional skills and context from resumes
* Build structured candidate profiles
* Search jobs from multiple sources
* Rank opportunities by profile compatibility
* Reduce irrelevant job recommendations
* Provide explainable compatibility scores
* Identify potential skill gaps
* Provide a simple, modern, cross-platform graphical experience

# Matching

The goal of the matching system is to compare the candidate profile with each job opportunity using factors such as:

* Skills
* Technologies
* Job title
* Professional experience
* Seniority
* Location
* Work model
* Candidate preferences

Each opportunity will eventually receive a compatibility score to help prioritize the most relevant jobs.

# Screenshots

> Coming soon.

# Contributing

Contributions, ideas, and suggestions are welcome.

If you find an issue or have an idea that could improve the project, feel free to open an Issue or submit a Pull Request.

# License

This project is licensed under the MIT License.
