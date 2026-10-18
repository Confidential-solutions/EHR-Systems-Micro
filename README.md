<div align="center">

# 🏥 Modern EHR Platform

**Electronic Health Records system — microservices backend + Angular frontend.**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)
[![Angular](https://img.shields.io/badge/Angular-18-red.svg)](https://angular.io)
[![.NET](https://img.shields.io/badge/.NET-8-purple.svg)](https://dotnet.microsoft.com)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue.svg)](./EHR-System/)

[📖 Docs](#-documentation) · [🚀 Quick Start](#-quick-start) · [🏗 Architecture](#-architecture) · [🔐 Security](#-security--compliance)

</div>

---

## ✨ Overview

Modern EHR Platform is a microservices-oriented Electronic Health Records system with an Angular 18 frontend and ASP.NET Core 8 services. It targets the clinical workflow — patient registration, scheduling, records, coding, billing, and analytics.

**This repository is under active hardening.** Treat security and compliance claims as **targets**, not guarantees, until secrets, TLS, CORS, and audit controls are verified in your environment.

**Key capabilities (in progress):**

| Area | Details |
|:--- |:--- |
| 🧑‍⚕️ Patient Management | Demographics, medical history, timelines |
| 📅 Appointments | Scheduling, reminders, calendar integration |
| 📋 Medical Records | SOAP notes, vitals, ICD-10 diagnoses, CPT procedures |
| 💊 Prescriptions | eRx, medication history |
| 🧪 Labs & Imaging | Results, trends |
| 💰 Billing & Insurance | Claims, payments |
| 📊 Analytics | Dashboards |
| 🌍 i18n & RTL | English + Arabic |

---

## 🏗 Architecture

```
EHR-Systems-Micro/
├── frontend/                 # Angular 18 SPA
├── EHR-System/               # ASP.NET Core 8 microservices + building blocks
│   ├── services/             # Patient, …
│   ├── building-blocks/
│   └── deployment/           # Docker / K8s assets (expanding)
├── docs/                     # Project documentation
├── docker-compose.yml        # Root local orchestration (see Quick Start)
└── .env.example              # Template for local secrets (copy → .env.development)
```

**Full architecture detail** → [`docs/Architecture/`](./docs/Architecture/) and [`docs/Backend/`](./docs/Backend/)

### Microservices

| Service | Responsibility |
|:--- |:--- |
| `Patient` | Patient records, demographics |
| (others) | Appointment, MedicalRecord, Prescription, Billing, Auth — see `EHR-System/services/` |

---

## 🚀 Quick Start

### Prerequisites

- **Node.js** 18+ & npm 9+
- **Docker** & Docker Compose
- **.NET 8 SDK** (for service-level development)

### Configure secrets (required)

```bash
git clone https://github.com/Confidential-solutions/EHR-Systems-Micro.git
cd EHR-Systems-Micro
cp .env.example .env.development
# Edit .env.development — set strong unique passwords and JWT material
```

`.env.development` is **gitignored**. Never commit real credentials.

### Run locally

```bash
# Infrastructure + services (adjust compose as your stack grows)
docker compose --env-file .env.development up -d

# Frontend
cd frontend && npm install && npm start
# → http://localhost:4200

# Patient API health (example)
curl http://localhost:5000/health   # or the mapped Patient port
```

Optional Makefile targets (if present):

```bash
make up      # docker compose up -d
make down    # docker compose down
```

### Run tests

```bash
# Angular unit tests (Jasmine / Karma)
cd frontend && npm test

# .NET backend tests (per service)
cd EHR-System/services/Patient && dotnet test
```

> **E2E:** Cypress/Playwright are not wired yet. The previous `ng e2e` script was removed until a runner is added (#31).

---

## 📦 Tech Stack

### Frontend
| | |
|:--- |:--- |
| Framework | Angular 18 (standalone components, Signals) |
| State | NgRx Signals |
| Styling | Tailwind CSS |
| Testing | Jasmine · Karma |
| Calendar | FullCalendar Angular packages |

### Backend
| | |
|:--- |:--- |
| Framework | ASP.NET Core 8 |
| Architecture | Clean Architecture · CQRS (MediatR) |
| Database | SQL Server / PostgreSQL · EF Core |
| Messaging | Apache Kafka (where configured) |
| Auth | JWT (RSA preferred; HS256 via env secret) |
| Logging | Serilog + OpenTelemetry (where configured) |
| Testing | xUnit · Moq |

### DevOps
| | |
|:--- |:--- |
| Containers | Docker · Kubernetes |
| CI/CD | GitHub Actions |
| Secrets | Env / User Secrets / Key Vault (do not commit) |

---

## 🔐 Security & Compliance

| Requirement | Status |
|:--- |:--- |
| HIPAA / HITECH readiness | 🔄 Target — not certified; see hardening issues |
| JWT Authentication | 🔄 Configured; requires key/secret in non-Dev |
| Role-Based Access Control | 🔄 Partial |
| PHI Encryption | 🔄 Keys via env only; rotate any previously committed values |
| Audit Logging | 🔄 Feature-flagged |
| CORS | ✅ Config-driven allowlist (no AllowAnyOrigin) |
| HTTPS / SQL TLS | 🔄 Enforce outside Development; `Encrypt=true` preferred |

→ Details: [`docs/SECURITY.md`](./docs/SECURITY.md)

**Ops note after secret cleanup:** Any password/key that ever appeared in git history should be treated as compromised and rotated if used outside throwaway local Docker. Purge history with `git filter-repo` / BFG if the repo was public or widely shared.

---

## 📖 Documentation

| Document | Purpose |
|:--- |:--- |
| [`docs/SECURITY.md`](./docs/SECURITY.md) | Security & secret loading |
| [`docs/CONTRIBUTING.md`](./docs/CONTRIBUTING.md) | Git workflow, standards |
| [`docs/Backend/`](./docs/Backend/) | ASP.NET, architecture notes |
| [`docs/Angular/`](./docs/Angular/) | Frontend patterns |
| [`docs/DevOps/`](./docs/DevOps/) | Docker, CI/CD |

---

## 🤝 Contributing

Please read [`docs/CONTRIBUTING.md`](./docs/CONTRIBUTING.md) before opening a PR.

---

## 📄 License

MIT — see [`LICENSE`](./LICENSE) if present.

---

<div align="center">
Built for healthcare · Repo: Confidential-solutions/EHR-Systems-Micro
</div>
