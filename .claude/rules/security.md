---
paths:
    - "**/*.cs"
---

# C# Security Guidelines

- Always validate input, especially for public APIs.
- Never log sensitive information such as passwords or API keys.
- Use secure cryptographic algorithms and libraries.
- Keep dependencies up to date to avoid known vulnerabilities.
- Follow the principle of least privilege when assigning permissions.
- Use EF Core's built-in protections against SQL injection by using parameterized queries and LINQ.
- For web applications, ensure proper authentication and authorization mechanisms are in place.
- Regularly review and audit code for security vulnerabilities. OWASP ZAP and SonarQube can be used for automated security scanning.
