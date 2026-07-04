# CampusConnect Documentation

This folder is the canonical documentation home for CampusConnect. Outside this folder, only expected entry-point, tooling, agent, and protected requirements files should remain.

## Current Implementation

- [Setup Checklist](product/setup.md)
- [Architecture](product/architecture.md)
- [API Reference](product/api.md)
- [Testing](product/testing.md)
- [Testfallkatalog](product/testfallkatalog.md)
- [Development Demo Data](information/demo-data.md)

## Product And Planning

- [Project Description](product/projektbeschreibung.md)
- [Requirements Status](anforderungsstatus.md)
- [MVP Must-have Critique](mvp-must-have-kritik.md)
- [MVP PRD](../../prd-mvp.md) remains the protected requirements document and must not be changed casually.
- [Market Analysis](information/marktanalyse.md)
- [Presentation Slides](team/presentation-slides.md)
- [Roles And Responsibilities](team/roles.md)

## Concepts

- [Groups Concept](concepts/groups.md)
- [Contact Book Rework](concepts/contact-book-rework.md)
- [Onboarding Concept](concepts/onboarding.md)

## Reports And Media

- [Promotional Video](media/werbevideo.md)

## Source Of Truth

Live code and configuration are authoritative for implemented behavior. `product/api.md`, `product/architecture.md`, and `product/testing.md` should be updated with the same change that alters API contracts, architecture, or validation workflow. Concept documents are planning material unless they explicitly state that a feature is implemented.
