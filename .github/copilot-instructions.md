# Copilot Instructions (SkyTeam Bot)

Aim: Generate clean, maintainable .NET 10 / C# 14 code aligned with Domain-Driven Design (DDD), high readability, and robust tests.

## Core Principles
- Prefer short, readable, composable methods (aim < ~20 lines; extract intentful helpers).
- Return early to avoid deep nesting (guard clauses first; fail fast).
- Strive for explicitness over magic; no hidden side effects.
- DDD where applicable: Entities, Value Objects (immutable), Aggregates as transactional boundaries, Domain Services for domain logic that does not belong to an entity.
- Separate domain from infrastructure concerns. Keep domain pure (no I/O, no framework types, no logging inside entities/value objects).
- Favor immutability; use `record` / `readonly record struct` / `init` setters for value objects.
- Single Responsibility: one reason to change per class/file.

## C# / .NET Usage
- Use file-scoped namespaces, `using` directives kept minimal (no unused usings).
- Enable nullable reference types; never ignore warnings—address root cause.
- Prefer expression-bodied members when they enhance clarity (not for complex logic).
- Pattern matching for clarity (e.g., `switch` expressions) when replacing sprawling `if/else`.
- Use primary constructors (when available) to reduce boilerplate; validate invariants immediately.
- Prefer `async Task`/`ValueTask` methods; avoid async void (except event handlers).
- Cancellation tokens for all externally-invoked async operations.
- Avoid premature allocation; prefer spans/`ReadOnlySpan<char>` in hot paths if performance critical.
- Use collection expressions (when available) for concise initialization in tests or static config.

## Error Handling & Guard Clauses
- Validate inputs at boundaries; throw precise exceptions (`ArgumentNullException.ThrowIfNull`, `ArgumentOutOfRangeException`).
- Domain invariants: throw domain-specific exception types (create minimal, meaningful ones) or return Result types if adopting functional style—pick one pattern consistently.
- Never swallow exceptions; log or propagate.

## Naming & Structure
- Clear, intention-revealing names; avoid abbreviations.
- Methods: verb; Classes: noun; Interfaces: leading `I` only when required (framework conventions).
- One public type per file (unless strongly cohesive tiny types).

## Domain Modeling (Examples)
- Value Objects: immutable, equality based on all components; add factory or validation in constructor.
- Entities: identity + behaviors; avoid anemic models—push logic inside.
- Aggregates: ensure invariants; expose behavior methods rather than setters.
- Keep `Game` logic cohesive; avoid leaking internal state—expose queries instead.

## Coding Style Checklist (Pre-Commit)
- No TODOs left unresolved (or tagged with owner & date).
- No magic numbers/strings—extract constants.
- Public API docs where non-trivial.
- Remove unused members/usings; build warning-free.

## Tests
- Framework: xUnit (implicit), Assertions: FluentAssertions.
- Pattern: AAA (Arrange, Act, Assert) with blank line separation.
- Test names: `MethodOrBehavior_ShouldExpectedResult_WhenCondition`.
- Prefer one logical assertion per test (FluentAssertions chains count as one); additional assertions only for closely related invariants.
- Edge cases: null/empty, min/max boundaries, invalid inputs, concurrency (if applicable), exceptions thrown, idempotency.
- Use data-driven tests for boundary ranges where helpful.
- Avoid mocking value objects; mock only external dependencies/infrastructure.
- Keep tests fast, deterministic; no sleeps—use time abstraction if needed.
- Use collection expression / object initializer for brevity in test setup.

## Performance & Safety
- Only optimize after measuring. Add benchmarks separately if performance-critical.
- Avoid global state and static mutability.
- Thread safety: document assumptions; use immutable data or proper synchronization primitives.

## PR / Review Guidance
Before accepting:
1. Does code express domain language (ubiquitous language) clearly?
2. Are methods small with early returns?
3. Are invariants enforced centrally?
4. Are tests comprehensive (happy + edge + failure cases) & readable?
5. Any unnecessary complexity or duplication?

## AI Suggestion Preferences
- Favor minimal diffs; if a large refactor is needed, outline plan first.
- When implementing new domain logic: propose Value Object/Entity shape before coding heavy logic.
- For tests: auto-suggest FluentAssertions patterns (e.g., `result.Should().Be(expected)`; exceptions: `invoking.Should().Throw<DomainException>()`).
- Offer guard clause templates automatically for public methods.

## Anti-Patterns To Reject
- Deep nesting > 3 levels; replace with early returns or pattern matching.
- Static mutable state; global singletons (except pure stateless helpers).
- God classes / anemic domain models.
- Overuse of `#pragma` or suppression of warnings.
- Large methods doing multiple responsibilities.

## Small Examples
Guard clause:
```
public void AddAltitude(Altitude altitude)
{
    ArgumentNullException.ThrowIfNull(altitude);
    if (!IsValid(altitude)) return; // early exit when no-op
    _altitudes.Add(altitude);
}
```
AAA + FluentAssertions:
```
// Arrange
var die = new Die(6);
// Act
var roll = die.Roll();
// Assert
roll.Should().BeInRange(1, 6);
```

## Keep It Concise
If unsure between clever and clear: choose clear. Optimize for maintainability first.

