# CampusConnect Documentation

This folder is the canonical documentation home for CampusConnect. Outside this folder, only expected entry-point, tooling, agent, and protected requirements files should remain.

## Current Implementation

- [Project Overview](project-overview.md)
- [Architecture](architecture.md)
- [API Reference](api.md)
- [Testing](testing.md)
- [QA Evidence](qa-nachweis.md)
- [Development Demo Data](demo-data.md)
- [Demo Checklist](demo-checkliste.md)
- [Delivery And Handover](abgabe-und-uebergabe.md)
- [Contributing](contributing.md)
- [Frontend Notes](frontend.md)

## Product And Planning

- [Project Description](product/projektbeschreibung.md)
- [Requirements Status](anforderungsstatus.md)
- [MVP PRD](../../prd-mvp.md) remains the protected requirements document and must not be changed casually.
- [Market Analysis](marktanalyse.md)
- [Presentation Slides](presentation-slides.md)
- [Roles And Responsibilities](roles.md)

## Concepts

- [Groups Concept](concepts/groups.md)
- [Contact Book Rework](concepts/contact-book-rework.md)
- [Onboarding Concept](onboarding.md)

## Reports And Media

- [Group Feature Test Report 2026-06-12](testbericht-gruppenfunktion-2026-06-12.md)
- [Historical Code Review Findings](code-review.md)
- [Promotional Video](media/werbevideo.md)

## Source Of Truth

Live code and configuration are authoritative for implemented behavior. `api.md`, `architecture.md`, and `testing.md` should be updated with the same change that alters API contracts, architecture, or validation workflow. Concept documents are planning material unless they explicitly state that a feature is implemented.
