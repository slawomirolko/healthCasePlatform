# EU Health Regulatory Case Platform

Training / architecture practice project inspired by enterprise systems used in public-sector and regulatory environments.

## Purpose

This repository is a learning and architecture practice project.
It is not an official European Commission, DG SANTE, European Dynamics, or public-sector system.

The goal of this project is to practice designing and implementing an enterprise-grade case management platform using .NET, React, workflow automation, Microsoft 365 integration patterns, messaging, audit logging, and architecture documentation.

The business domain is inspired by health and food safety regulatory processes, such as incident reporting, risk assessment, scientific review, document approval, and regulatory decision tracking.

## Business Scenario

Large regulatory organizations need to manage complex cases involving multiple stakeholders, documents, approvals, deadlines, audit trails, and integrations with existing Microsoft 365 tools.

Example case types:

- food safety incident
- public health alert
- medical device regulatory case
- inspection follow-up
- scientific review request
- member state notification

The platform helps case officers, reviewers, managers, auditors, and administrators manage these processes in a structured and auditable way.

## Main Learning Goals

This project is designed to practice:

- .NET application architecture
- React frontend architecture
- REST API design
- Clean Architecture
- Vertical Slice Architecture
- Modular Monolith
- CQRS
- MediatR
- Entity Framework Core
- SQL Server
- MongoDB audit logging
- RabbitMQ messaging
- Outbox Pattern
- authentication and authorization
- workflow modeling
- SharePoint Online integration patterns
- MS Graph integration patterns
- SPFx concepts
- observability
- Docker-based development
- C4 architecture diagrams
- Architecture Decision Records

## Architecture Lab Approach

This project is intentionally structured as an architecture laboratory.
Some mechanisms are implemented in more than one way in order to compare trade-offs.

Examples:

- Controller + Service vs CQRS vs Vertical Slice
- role-based vs policy-based vs resource-based authorization
- hardcoded workflow vs state pattern vs configurable workflow
- direct service call vs domain events vs RabbitMQ vs Outbox Pattern
- SQL audit vs MongoDB audit vs event sourcing light
- local file storage vs SharePoint storage abstraction
- monolith vs modular monolith vs service extraction

The purpose is not only to build working software, but also to understand why a specific architectural choice may or may not be appropriate in an enterprise environment.

## Technology Stack

Backend:

- .NET / ASP.NET Core
- Entity Framework Core
- SQL Server
- MongoDB
- RabbitMQ
- Redis
- MediatR
- FluentValidation
- Serilog
- OpenTelemetry

Frontend:

- React
- TypeScript
- React Query
- Routing
- Feature-based structure

Integration concepts:

- Microsoft Graph
- SharePoint Online
- SPFx
- Workflow Manager / Nintex-style workflow concepts

Infrastructure:

- Docker Compose
- GitHub Actions
- Health checks
- Observability stack

## Main Business Concepts

Core entities:

- Case
- Case Type
- Case Document
- Task
- Workflow
- Workflow Step
- Decision
- Comment
- Attachment
- Audit Log
- Notification
- User
- Role

Example roles:

- Case Officer
- Scientific Reviewer
- Legal Reviewer
- Team Leader
- Compliance Officer
- Auditor
- System Administrator

## Example Workflow

A food safety incident may follow this process:

- Member State submits a case.
- Case Officer validates the submission.
- Scientific Reviewer performs risk assessment.
- Legal Reviewer reviews the decision.
- Team Leader approves the final decision.
- Member States are notified.
- The case is archived with a full audit trail.
