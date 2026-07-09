# Contributing to ApexDebloater

Thank you for your interest in contributing to ApexDebloater. Contributions from the open-source community help improve system performance and security optimization for everyone.

---

## Code of Conduct

By participating in this project, you agree to maintain a professional, respectful, and collaborative environment.

---

## How to Contribute

### Reporting Bugs
If you find a bug, please submit an issue on the GitHub repository containing:
1. A clear and descriptive title.
2. Steps to reproduce the issue.
3. Your Windows version (Windows 10 or Windows 11) and build number.
4. Expected vs. actual behavior.

### Suggesting Enhancements
If you have ideas for new features or registry optimizations:
1. Open a feature request issue.
2. Explain the benefits of the optimization and provide registry keys or command arguments if applicable.
3. Discuss the safety level (Safe or Caution) of the tweak.

### Pull Requests
If you want to contribute code updates:
1. Fork the repository.
2. Create a new branch for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. Implement your changes. Ensure the code conforms to standard C# coding guidelines and uses safe Win32 API calls.
4. Verify that the project compiles with zero warnings or errors:
   ```bash
   dotnet build
   ```
5. Submit a pull request to the main repository, including a clear description of the modifications made.

---

## Development Guidelines

### Tweak Structuring
All tweaks should be registered inside the InitializeTweaks method in DebloatEngine.cs using the TweakItem model:
* Id: Unique string identifier.
* Name: Concise display name.
* Description: Detailed explanation of what the tweak alters.
* Category: System, Services, Privacy, Performance, Customization, or Gaming.
* Risk: Safe or Caution.
* ApplyAction: Action to apply registry settings or disable services.
* UndoAction: Action to revert registry settings or restore services.
* CheckAppliedAction: Function to verify if the tweak is active.

### Safety Priorities
* Never implement tweaks that break Windows update structures permanently. All actions must be fully reversible via the UndoAction.
* Always check for registry key existence before trying to read or delete keys to prevent NullReferenceExceptions.
