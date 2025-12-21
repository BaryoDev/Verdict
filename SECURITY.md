# Security Policy

## Supported Versions

The following versions of Upshot are currently supported with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0.0 | :x:                |

## Reporting a Vulnerability

We take the security of Upshot seriously. If you believe you have found a security vulnerability in this project, please report it to us as described below.

**Please do not report security vulnerabilities through public GitHub issues.**

### Reporting Process

1.  **Email**: Send an email to `security@baryo.dev` with the details of the vulnerability.
2.  **Details to Include**:
    *   A description of the vulnerability.
    *   The version(s) affected.
    *   Steps to reproduce the issue.
    *   Potential impact of the vulnerability.
3.  **Encrypted Communication**: If you wish to encrypt your communication, please request our PGP public key in your initial email.

### Response Timeline

*   **Acknowledgment**: You will receive an acknowledgment of your report within 48 hours.
*   **Assessment**: We will perform a preliminary assessment within 5 business days.
*   **Fix**: We aim to provide a fix or mitigation within 30 days, depending on the complexity of the issue.
*   **Advisory**: Once fixed, we will publish a security advisory if appropriate.

### Disclosure Policy

We follow a policy of coordinated vulnerability disclosure. We ask that you give us reasonable time to address the vulnerability before making any public disclosure.

## Security Audit

Upshot is built with a "zero-trust" approach to memory safety and performance. We use `readonly struct` and avoid unnecessary allocations to minimize the attack surface related to memory corruption and DOS attacks.

---
*Last Updated: December 2025*
