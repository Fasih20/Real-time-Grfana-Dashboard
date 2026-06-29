# A Custom Grafana-Based Monitoring and Control Console 🚀

An industrial-grade, real-time IoT observability and control platform designed to replace rigid legacy SCADA systems. Developed in collaboration with **SUPARCO**, this system utilizes a decoupled microservices architecture to provide sub-200ms telemetry visualization, bidirectional hardware control, and automated alerting.

## ✨ Key Features

*   **Bidirectional Control (CQRS):** Strict separation of concerns. A dedicated C# Worker Service continuously polls telemetry (Query), while a secured `.NET 8 Web API` executes state-change commands like Start/Stop (Command) without interrupting read cycles.
*   **Native EtherNet/IP Integration:** Communicates natively with Allen-Bradley ControlLogix PLCs using the `libplctag.NET` library.
*   **The Digital Twin (Physics Engine):** A built-in `--demo` flag bypasses network execution, utilizing a mathematical physics engine to simulate oscillating LOX tank levels and pump temperatures for development without physical hardware.
*   **Hybrid Next.js Dashboard:** Breaks the rigid constraints of native Grafana iframes. Utilizes `react-grid-layout` to seamlessly embed isolated Grafana telemetry panels alongside custom Ant Design React control widgets.
*   **Active Alerting Microservice:** An independent C# background worker that queries InfluxDB via Flux, evaluates mutable threshold states, and dispatches automated SMTP email alerts with programmatic anti-spam cooldowns.
*   **Enterprise Security:** Secured via JSON Web Tokens (JWT) and strict Role-Based Access Control (RBAC) to differentiate between 'Admin' (Write) and 'Operator' (Read-Only) privileges.

## 🛠️ Technology Stack

*   **Frontend:** Next.js, React, Tailwind CSS, Ant Design, `react-grid-layout`
*   **Backend & Middleware:** C# .NET 8 (ASP.NET Core Web API & Worker Services)
*   **Time-Series Database:** InfluxDB 2.7
*   **Visualization Engine:** Grafana OSS
*   **Protocols:** EtherNet/IP (`libplctag.NET`)
*   **Containerization:** Docker & Docker Compose

## 📂 Repository Structure

```text
/Real-time-Grafana-Dashboard
├── docker-compose.yml              # Master orchestrator for all services
├── /Frontend                       # Next.js Hybrid UI source code
│    ├── /client-app
│    └── Dockerfile
├── /SUPARCO_API                    # C# .NET 8 Web API (Process Controller & Alerts)
│    ├── /WebApplication1
│    └── Dockerfile
├── /SuparcoDataSimulator           # C# Worker Service (Telemetry Polling & Digital Twin)
│    ├── /WorkerService1
│    └── Dockerfile
└── /grafana-config                 # Custom Grafana configuration (CORS & Auth overrides)
     └── custom.ini                 

```

## 🚀 Getting Started (Deployment)

This project is fully containerized for a seamless, 1-click deployment using Docker.

### Prerequisites

* [Docker Desktop](https://www.docker.com/products/docker-desktop/) or Docker Engine installed on the host machine.
* [Docker Compose](https://docs.docker.com/compose/install/) (Included with Docker Desktop).

### Installation & Execution

1. **Clone the repository:**
```bash
git clone [https://github.com/Fasih20/Real-time-Grfana-Dashboard.git](https://github.com/Fasih20/Real-time-Grfana-Dashboard.git)
cd Real-time-Grfana-Dashboard

```


2. **Spin up the environment:**
From the root directory (where the `docker-compose.yml` file is located), run:
```bash
docker-compose up -d --build

```


*This command will automatically pull the InfluxDB and Grafana images, build the custom C# and Next.js Dockerfiles, and launch the entire ecosystem in isolated, networked containers.*
3. **Access the Console:**
* **Main Next.js Dashboard:** `http://localhost:3001`
* **Grafana Backend:** `http://localhost:3000`
* **Backend API Swagger UI:** `http://localhost:5255/swagger`
* **InfluxDB UI:** `http://localhost:8086`



### Stopping the System

To safely spin down the containers without losing persistent database storage:

```bash
docker-compose down

```

## ⚙️ Configuration Notes

* **Hardware vs. Simulation Mode:** The backend defaults to physical PLC communication. To run the system in simulation mode (Digital Twin), ensure the `--demo` flag environment variable is passed to the C# worker service container.
* **Grafana Iframe Security:** The `custom.ini` file mounts directly into the Grafana container to disable `cookie_secure` and enable `auth.anonymous`. This is required for the Next.js frontend to securely render the panels across local HTTP ports without browser CORS blocking.

## 👥 Project Team

* **Muhammad Fasih Zaheer** (Backend, API, Digital Twin, EtherNet/IP Integration)
* **Muhammad Ammar** (Frontend, Next.js Hybrid Grid, Security, React Widgets)

**Institution:** Salim Habib University, Department of Computer Science (FYP 2025-2026)
