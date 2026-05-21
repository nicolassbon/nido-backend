---
name: dotnet-design-pattern-review
description: 'Review C#/.NET code for design quality, architecture alignment, and correct pattern usage based on project conventions.'
---

# .NET/C# Design & Architecture Review

## Use this skill when

- Reviewing finished or in-progress .NET code for design quality
- Auditing whether an implementation stayed aligned with the chosen architecture
- Looking for overengineering, unclear abstractions, or maintainability issues

## Do not use this skill when

- Writing the first implementation of a feature from scratch
- Defining the initial architecture of the project; use `dotnet-architect`
- Handling framework-specific implementation details where `aspnet-core` or `dotnet-patterns` should lead

Review the C#/.NET code in ${selection} focusing on design quality, architecture alignment, and maintainability.

Do NOT enforce patterns blindly.  
Do NOT assume enterprise architecture unless explicitly required.  
Do NOT suggest unnecessary abstractions.

Your goal is to validate that the implementation is:

- clear
- consistent
- maintainable
- aligned with project conventions and architecture decisions

This skill is review-first. It should act as a critic, not as the default author of routine backend code.

---

## Context Awareness (IMPORTANT)

Before reviewing:

- Identify the architecture style used (layered, simple, clean-ish, etc.)
- Respect existing decisions unless they are clearly harmful
- Prefer consistency with the current codebase over introducing new patterns

---

## Review Checklist

### 1. Architecture Alignment

- Does the code respect the defined architecture?
- Are responsibilities placed in the correct layer?
- Is business logic leaking into controllers or infrastructure?

---

### 2. Simplicity (KISS)

- Is the solution simpler than necessary?
- Is there any overengineering or premature abstraction?
- Are patterns used only when they add real value?

---

### 3. Pattern Usage (Pragmatic)

- Are patterns used correctly where they exist?
- Are patterns overused or unnecessary?
- Would removing a pattern improve clarity?

---

### 4. Dependency Injection

- Are dependencies injected via constructor?
- Are interfaces used where they add value (not by default)?
- Are service lifetimes reasonable?

---

### 5. Data Access (EF Core)

- Is EF Core used correctly?
- Is `AsNoTracking()` used for read-only queries?
- Are queries simple and readable?
- Any signs of N+1 queries?

---

### 6. Async & Performance

- Is async/await used correctly?
- Any `.Result` or `.Wait()` usage?
- Are async flows consistent?

---

### 7. Separation of Concerns

- Are classes focused on a single responsibility?
- Is logic properly distributed across layers?
- Any God classes or fat controllers?

---

### 8. Maintainability

- Is the code easy to understand?
- Are names meaningful and consistent?
- Is duplication avoided?

---

### 9. Testability

- Are dependencies mockable?
- Is the code easy to unit test?
- Any tight coupling blocking testing?

---

### 10. Error Handling

- Is error handling consistent?
- Are exceptions used appropriately?
- Is there a clear strategy?

---

### 11. Security (Basic)

- Input validation present?
- Sensitive data handled safely?
- No obvious injection risks?

---

## Anti-Goals (VERY IMPORTANT)

DO NOT:

- Force Command Pattern, Factory Pattern, or CQRS unless already present
- Suggest microservices or distributed architecture
- Introduce MediatR, AutoMapper, or similar unless justified
- Enforce specific project structures not already in use
- Overcomplicate simple logic

---

## Output Format

Provide:

1. **Quick Summary**
   - Overall quality (Good / Needs Improvement / Risky)

2. **Key Issues**
   - Concrete problems found

3. **Improvement Suggestions**
   - Practical, minimal changes

4. **Optional Enhancements**
   - Only if they add clear value (not required)

---

## Goal

Help improve the code **without breaking simplicity or existing architecture**.
