# Agent Instructions

## General Principles

- Follow the existing architecture, conventions and patterns of the repository.
- Prefer simple, readable and maintainable code over clever or overly abstract solutions.
- Before implementing changes, inspect nearby code to understand existing conventions.
- Avoid introducing unnecessary dependencies, abstractions or frameworks.
- Keep changes focused on the requested task. Avoid unrelated refactoring.
- Do not introduce backwards-compatibility code unless it is actually required.

## C# Coding Style

- Prefer `var` for local variable declarations where the type is apparent.
- Prefer early returns and guard clauses over deeply nested conditionals.
- Always use braces for loops and conditional statements.
- Prefer file-scoped namespaces for new files. Avoid converting unrelated existing code solely for style.
- Prefer simple `using` declarations over `using` blocks where appropriate.
- Aim for one operation per line and use whitespace to separate logical blocks.
- Prefer assigning a result to a local variable before returning it when doing so improves readability.
- Follow existing naming conventions and formatting.

## Documentation

- Add XML documentation comments to public methods and classes where useful.
- Document non-obvious behaviour and design decisions rather than restating what the code does.
- Keep README files accurate when changing functionality, configuration or setup procedures.
- Keep `example.env` updated with required configuration settings, without including secrets or unnecessary defaults.

## Testing

- Add or update tests for meaningful algorithmic blocks, complex behaviour, or regressions where a test provides lasting value. Do not add tests by default for every change.
- Most simple methods, straightforward delegation, configuration properties and routine CRUD operations do not need dedicated tests unless they carry a specific correctness risk.
- Prefer a small number of impactful tests over large suites of trivial or repetitive assertions that obscure important coverage.
- Validate intended behaviour, including relevant edge cases and failure conditions. Avoid tests that simply reproduce the implementation's logic.
- Use focused integration checks when database behaviour or external interactions cannot be meaningfully verified in isolation.
- Do not modify existing tests merely to make an incorrect implementation pass.
- Run relevant existing tests and checks after changes. Choose verification proportional to the change; documentation-only changes do not require application tests.
- Report failures and any tests that could not be executed or behaviour that could not be verified.

## Change Discipline

- Do not silently swallow exceptions.
- Preserve existing error-handling and logging conventions.
- Avoid changing public APIs or database schemas unless required.
- Explain significant design decisions or trade-offs.
