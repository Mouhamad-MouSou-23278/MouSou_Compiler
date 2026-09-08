# ADR-003: Hybrid Authentication Architecture with HTTP-Only Refresh Cookies & Short-Lived JWT Access Tokens

## Context
Storing long-lived JWT access tokens in browser `localStorage` or `sessionStorage` leaves the system vulnerable to Cross-Site Scripting (XSS) attacks, as any injected JavaScript script can read tokens and impersonate the user. Conversely, storing authentication solely in traditional stateful session cookies creates scaling bottlenecks for mobile/API clients and introduces Cross-Site Request Forgery (CSRF) vulnerabilities.

## Decision
We implemented a **hybrid enterprise authentication pattern**:

1. **Short-Lived Access Tokens (JWT)**:
   - Lifespan: 15 to 60 minutes.
   - Signed with HMAC-SHA256 using a high-entropy secret (`Jwt:Secret`).
   - Delivered in the HTTP response JSON body upon login and refresh.
   - Maintained in client memory / volatile storage for API requests (`Authorization: Bearer <token>`).
   - Accepted by SignalR WebSocket connections via the `access_token` query parameter during handshake.

2. **Long-Lived Refresh Tokens in HTTP-Only Cookies**:
   - Stored in an `HttpOnly`, `SameSite=Lax` (or `Strict`), and `Secure` cookie named `RefreshToken`.
   - Inaccessible to JavaScript `document.cookie`, neutralizing XSS exfiltration.
   - Hashed using **BCrypt** with work factor 12 before persisting in PostgreSQL.
   - Lifespan: 14 days.

3. **Refresh Token Rotation & Revocation**:
   - Every time `/api/auth/refresh` is invoked, the previous refresh token is invalidated and a cryptographically generated 64-byte token is issued.
   - Upon `/api/auth/logout`, the active refresh token hash is wiped from the database and the cookie is expired immediately.

## Consequences
### Positive
- Maximum security posture against XSS token theft.
- High scalability; protected API endpoints validate JWT signatures statelessly without hitting the database on every HTTP request.
- Seamless automatic re-authentication handled transparently by Axios response interceptors.

### Negative
- Requires CORS configuration with `AllowCredentials()` and explicit origin whitelisting (`http://localhost:3000`).
