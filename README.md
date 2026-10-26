# Bakery POS

![CI](https://github.com/iClinton97/bakery-pos/actions/workflows/ci.yml/badge.svg)

A point-of-sale system for a working bakery: a cashier screen for the counter, card payments through a Tyro EFTPOS terminal, and a live stock list that shows which products need refilling.

> **Status: early development.** The UI is designed (see [docs/mockups](docs/mockups)) and the project foundation is in place. The .NET solution is next. See the [roadmap](#roadmap).

## Why this exists

I run a bakery in Melbourne. Off-the-shelf POS systems don't tell the kitchen what to bring out as items sell. This project ties each approved sale to a live stock count so refills happen before the shelf is empty.

## Planned features

- Touch-friendly cashier screen with product categories (Pies, Hot food, Cakes, Savoury), popular items first, and a Miscellaneous manual-price entry
- Integrated card payments via Tyro, with stock deducted only on an **approved** payment
- Cash sales, voids and refunds
- Live stock board with per-product refill thresholds, separating what is in the pie booth from what is in the fridge
- Stock page served on the shop network, viewable from any browser (no hosted service, no data leaves the shop)
- Receipt printing and cash drawer control

## Tech stack

| Area | Choice |
| --- | --- |
| Language / runtime | C# on .NET |
| UI | [Avalonia](https://avaloniaui.net/) (cross-platform; runs on the Windows till and on macOS for development) |
| Storage | SQLite (planned) |
| Tests | xUnit |
| CI | GitHub Actions |

## Architecture

The business logic lives in `BakeryPos.Core`, with no UI or hardware dependencies. Anything that touches hardware or an external service sits behind an interface, for example `IPaymentTerminal`. A mock terminal lets the whole app be developed, tested and demonstrated without a physical EFTPOS machine.

See [docs/architecture.md](docs/architecture.md) for the diagram and key decisions.

## Repository layout

```
src/
  BakeryPos.Core/        Business logic and interfaces (no UI)
  BakeryPos.App/         Avalonia desktop application
tests/
  BakeryPos.Core.Tests/  Unit tests for the core library
docs/                    Architecture, design decisions and UI mockups
```

## Getting started

Prerequisite: the [.NET SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/iClinton97/bakery-pos.git
cd bakery-pos
dotnet build
dotnet test
dotnet run --project src/BakeryPos.App
```

## Roadmap

- [x] **Stage 0:** UI mockups, licence, CI workflow, payment abstraction with mock terminal
- [ ] **Stage 1:** .NET solution skeleton (core library, Avalonia app, test project)
- [ ] **Stage 2:** product catalogue and cashier screen
- [ ] **Stage 3:** cart, totals and sales recording
- [ ] **Stage 4:** live stock tracking and refill board
- [ ] **Stage 5:** cash drawer and receipt printing
- [ ] **Stage 6:** Tyro terminal integration

## Payments and Tyro

Production card payments require the integration to be certified by Tyro. Until then the app runs against a mock terminal. This repository contains no merchant credentials and does not include Tyro's SDK files.

## Security and data

- No secrets or credentials are committed. Local settings live in git-ignored files.
- All products, prices and sales in this repository are sample data.

## Licence

[MIT](LICENSE)
