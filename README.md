# Vaguei

> A smarter way to find job opportunities that match your profile.

![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge\&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge\&logo=c-sharp)
![xUnit](https://img.shields.io/badge/xUnit-Testing-512BD4?style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-Cross--Platform-blue?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-success?style=for-the-badge)

# About

**Vaguei** is a personal project designed to make the job search process in the technology industry simpler, smarter, and more relevant.

The idea is to analyze a candidate's resume, identify skills, technologies, professional experience, and career profile, and use this information to search, organize, and rank job opportunities according to their compatibility.

The project is also a practical environment for exploring software architecture, data processing, automation, APIs, testing, and eventually artificial intelligence using C# and the .NET ecosystem.


# Features

* Resume text extraction
* Candidate profile analysis
* Skill and technology detection
* Technology alias recognition
* Professional experience extraction
* Company and job title identification
* Employment period detection
* Structured candidate profiles
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

* DOCX parsing
* PDF parsing
* Job source integrations
* Job matching engine
* REST APIs
* Avalonia UI
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

The main resume processing flow follows this structure:

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

# Supported Platforms

* Linux
* Windows
* macOS

# Goals

* Automate part of the job search process
* Identify technologies and skills from resumes
* Build structured candidate profiles
* Search jobs from multiple sources
* Rank opportunities by profile compatibility
* Reduce irrelevant job recommendations
* Provide explainable compatibility scores
* Identify potential skill gaps

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
