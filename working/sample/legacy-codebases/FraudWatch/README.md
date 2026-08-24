# FraudWatch (APP-005)

## ⚠️ DO NOT TOUCH - Already Modernized ⚠️

**This application has already been modernized to .NET 6 and runs on Linux EC2.**

It is excluded from the current migration project scope. No modernization work is required.

## Overview

FraudWatch is a real-time fraud detection and alerting service that processes payment
transactions, evaluates fraud rules, and generates risk scores. It integrates with external
banking APIs for transaction verification and uses Redis for high-performance caching.

## Technology Stack

| Component          | Technology                          |
|--------------------|-------------------------------------|
| Runtime            | .NET 6.0                            |
| Framework          | ASP.NET Core Web API                |
| Hosting            | Linux EC2 (containerized)           |
| Caching            | Redis (StackExchange.Redis)         |
| External APIs      | Banking API (REST/HttpClient)       |
| Logging            | Serilog + ILogger<T>                |
| Testing            | xUnit + Moq                         |
| Container          | Docker (multi-stage build)          |

## Architecture

- **FraudWatch.Api** - ASP.NET Core Web API (minimal hosting, controllers)
- **FraudWatch.Core** - Domain models, interfaces, business services
- **FraudWatch.Infrastructure** - Redis caching, Banking API client, data access
- **FraudWatch.Tests** - Unit tests (xUnit)

## Running Locally

```bash
# Build and run with Docker
docker build -t fraudwatch .
docker run -p 5000:80 fraudwatch

# Or run directly with .NET CLI
dotnet run --project FraudWatch.Api
```

## Key Features

- Real-time transaction fraud scoring
- Configurable fraud detection rules
- Banking API integration for transaction verification
- Redis-backed caching for rule sets and scoring models
- Alert management and notification pipeline

## Deployment

Deployed as a Docker container on Linux EC2 instances behind an ALB.
CI/CD via CodePipeline with automated tests and container image publishing to ECR.

## Status

| Metric              | Value       |
|---------------------|-------------|
| Current .NET        | 6.0         |
| OS                  | Linux       |
| Container           | Yes         |
| Modernization       | Complete    |
| Migration Required  | **No**      |
