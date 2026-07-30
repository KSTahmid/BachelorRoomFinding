# Contributing to Bachelor Room Finding

Thank you for your interest in contributing! 🎉

## How to Contribute

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Make your changes with clear, descriptive commits
4. Push and open a Pull Request

## Coding Standards

- Follow existing naming conventions (PascalCase for C# classes/methods)
- Add XML comments to all public methods and services
- Keep controllers thin — business logic belongs in Services
- Use ViewModels for passing data to Views, not raw Entities

## Commit Message Format

```
type: short description

Examples:
feat: add Nagad payment gateway integration
fix: resolve duplicate payment key constraint
docs: update README with setup instructions
refactor: extract OTP logic into IOtpService
```

## Branching Strategy

| Branch | Purpose |
|--------|---------|
| `main` | Production-ready code |
| `feature/*` | New features |
| `fix/*` | Bug fixes |
| `docs/*` | Documentation only |

## Contact

- **KSTahmid** (Lead Dev) — Backend, Architecture
- **raktim3050** (Co-Dev) — Payment, Mess Board, UI
