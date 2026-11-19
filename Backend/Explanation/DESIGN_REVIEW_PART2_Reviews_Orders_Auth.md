# Design Review — Part 2
# مراجعة التصميم — الجزء التاني

Scope: Cancel + Admin Orders · **Reviews** · **Email Confirmation** · **Forgot/Reset Password** · User Lookup · Swagger enums
Part 1 (`ARCHITECTURE_DESIGN_REVIEW.md`) covers the base architecture. This part applies it to the new features and adds a **master index of every Command and Query** in the project.

> Diagrams are English only. Explanations: English first, then Egyptian Arabic on separate lines.

---

## Table of Contents

0. What was built, and test status
1. Architecture applied: where each new piece lives
2. Thinking process (Review, Email confirmation, Reset password)
3. Data flow sequences
4. Catalog: new Commands & Queries
5. Master index: ALL Commands & Queries in the project
6. Design decisions & security design
7. Honest review: trade-offs & weaknesses
8. Assumptions to verify

---

# 0. What was built, and test status

| Feature | Built | Tested by you |
|---------|:----:|---------------|
| Cancel Order (user) | ✔ | covered by the 13-case table |
| Admin Orders (list, details, confirm/ship/deliver, cancel) | ✔ | "admin order is good" |
| Swagger enums as names (`JsonStringEnumConverter`) | ✔ | ✔ works |
| Review (add, update, delete, list, admin delete) | ✔ | **result not reported yet** (14-case table pending) |
| User Lookup (`IUserLookupService`) | ✔ | used by Review list |
| Email confirmation (+ resend, login 403) | ✔ | ✔ works (logs confirm 200/403/400/204) |
| Forgot / Reset password | ✔ | ✔ "they all work" |

**بالمصري:** كله اتجرب إلا الـ Review. نتيجته لسه ما وصلتنيش.

---

# 1. Architecture applied: where each new piece lives

```
API              Controllers (Reviews, AdminReviews, Auth additions)
  │
Service          Commands / Queries / Handlers / Validators / DTOs
  │              Interfaces: IUserLookupService, IEmailService, IIdentityService (+methods), IReviewRepository (+methods)
Domain           Review (UpdateRating, UpdateComment), DomainException
Core             Constants: Roles, AuthErrors
Infrastructure   ReviewRepository, UserLookupService, ConsoleEmailService, IdentityService (+methods)
```

| New piece | Layer | WHY there |
|-----------|-------|-----------|
| `AddReviewCommand` + Handler | Service | A use case = orchestration, no tech details |
| Review rules (rating 1..5, comment ≤ 500) | Domain (`Review`) + Validator | Domain = last line of defense; Validator = friendly early 400 |
| `IReviewRepository` | Service (interface) | Handlers depend on abstractions |
| `ReviewRepository` | Infrastructure | Uses EF Core |
| `IUserLookupService` | Service (interface) | Handler needs names but must not know `UserManager` |
| `UserLookupService` | Infrastructure | Uses `UserManager<ApplicationUser>` (Identity lives here) |
| `IEmailService` | Service (interface) | Handlers say "send confirmation", not how |
| `ConsoleEmailService` | Infrastructure | Dev implementation. SMTP replaces it later, handlers untouched |
| `AuthErrors` | **Core/Constants** | Used by Infrastructure (producer) and API (consumer). Core is visible to both, like `Roles` |
| Token encode/decode (Base64Url) | Infrastructure (`IdentityService`) | Token format is an Identity detail. Handlers see an opaque string |
| `JsonStringEnumConverter` | API (`Program.cs`) | JSON shape is a presentation decision, not a Domain one |

**Dependency rule check:** Service references Domain + Core only. Nothing in Service references EF, `UserManager`, or `HttpContext`. ✔

**ليه `IUserLookupService` مش جوه Domain؟**
لأن اليوزر (`ApplicationUser`) في Infrastructure. الـ Domain مبيعرفوش. فبنسأل عن الأسماء من خلال Interface.

---

# 2. Thinking process

> Same order as always: Requirement → Domain rules → Use cases → Repo methods → DTOs → Handler sequence → Controller → Config/Migration → Tests.

## 2.1 Review

**Requirement:** "A customer can rate a product he really received. Others can read ratings."

**Step 1 — Rules (Domain first):**

| Rule | Where enforced |
|------|----------------|
| Rating between 1 and 5 | `Review` ctor / `UpdateRating` (DomainException) + Validator |
| Comment optional, ≤ 500 | Validator (+ Domain) |
| Only buyers whose order is **Delivered** | Handler → `Orders.HasCustomerPurchasedProductAsync` (Status == Delivered) |
| One review per user per product | Handler check **+ unique index** `(ProductId, CustomerId)` |
| Only the author edits/deletes his review | Lookup by `(ProductId, userId-from-token)` |

**Why the purchase rule is in a Handler, not in `Review`:**
It needs data from another aggregate (`Order`). A Domain entity should not query other aggregates. So the Handler asks the repository and decides before creating the `Review`.

**ليه قاعدة الشراء في الـ Handler؟**
لأنها محتاجة بيانات من Order. الـ Review entity ميعرفش الأوردرات.

**Step 2 — Use cases:**

| Use case | Type |
|----------|------|
| Add review | Command |
| Update my review | Command |
| Delete my review | Command |
| Admin delete review | Command |
| List product reviews + average | Query |

**Step 3 — Repo methods derived from handlers:**

```
AddReview needs:   product exists?                 → Products.GetByIdAsync (query filter hides deleted)
                   bought + Delivered?             → Orders.HasCustomerPurchasedProductAsync
                   already reviewed? (tracked)     → Reviews.GetByProductAndCustomerAsync
                   save                            → Reviews.AddAsync

Update/Delete needs: my review (tracked)           → Reviews.GetByProductAndCustomerAsync   (same method)
AdminDelete needs:   review by id (tracked)        → Reviews.GetByIdAsync                   (generic)

GetProductReviews needs: one page, newest first    → Reviews.GetPagedByProductAsync
                         average rating            → Reviews.GetAverageRatingAsync
                         author names              → IUserLookupService.GetNamesByIdsAsync
```

**One method (`GetByProductAndCustomerAsync`) serves three handlers** (add check, update, delete). That's the benefit of deriving methods from needs.

**Step 4 — The author-name decision (a real design review moment):**

| Option | Verdict |
|--------|---------|
| A. Lookup service (`IUserLookupService`) | **Chosen.** Always fresh, no duplicated data, reused later for admin orders |
| B. Snapshot `AuthorName` inside Review | Stale if name changes; needs a lookup at creation anyway (name isn't in the JWT) |
| C. Domain references `ApplicationUser` | Rejected: breaks Clean Architecture |

**Step 5 — Result vs Exception for Review:**

| Situation | Mechanism | HTTP |
|-----------|-----------|------|
| Product not found | `Result.Failure` | 404 |
| Review not found / not mine | `Result.Failure` | 404 |
| Not purchased / not delivered | `DomainException("review.not_purchased")` | 400 |
| Already reviewed | `DomainException("review.already_exists")` | 400 |
| Rating out of range | Validator → `ValidationException` | 400 |

Rule used: "normal but impossible" → Result. "A business rule was broken" → Exception with a code the frontend can switch on.

## 2.2 Email confirmation — the constraint we discovered

```
Naive plan:  Identity option  SignIn.RequireConfirmedEmail = true   → done?

Reality:     that option is checked by SignInManager only.
             We log in with UserManager (we build the JWT ourselves).
             → option does NOTHING for us.

Decision:    check user.EmailConfirmed manually inside LoginAsync.
```

**Placement of the check matters:**

```
LoginAsync:
  1. find user                       (not found → generic 401)
  2. locked out?                     (→ generic 401)
  3. password wrong?                 (→ AccessFailed++ → generic 401)
  4. password OK → reset fail count
  5. EmailConfirmed == false?        (→ Failure(AuthErrors.EmailNotConfirmed) → 403)   ← AFTER the password
  6. build JWT
```

**WHY after the password:** if the "email not confirmed" answer came before the password check, anyone could learn "this email is registered here" by trying random emails.

**ليه بعد الباسورد؟**
لو قلنا "الإيميل مش مؤكد" قبل ما نتأكد من الباسورد، أي حد يعرف إن الإيميل ده مسجل عندنا.

**Why `Register` returns no token:** a token would let the user in without confirming. Register returns `UserDto` only.

## 2.3 Forgot / Reset password

Same pattern as confirmation, with two security additions:

| Concern | Decision |
|---------|----------|
| Account enumeration | `forgot-password` returns 200 always |
| Unconfirmed accounts | No reset email (otherwise someone registers with a victim's email and plays with it) |
| Token reuse | `ResetPasswordAsync` rotates the SecurityStamp → the token dies after one use |
| Locked-out real owner | After reset: `SetLockoutEndDateAsync(null)` + `ResetAccessFailedCountAsync` |
| Error messages | Wrong/expired token → one generic message. Password-policy errors → shown (user must fix them) |

---

# 3. Data flow sequences

## 3.1 AddReview (Command)

```
Client  POST /api/products/{productId}/reviews   Bearer <JWT>   body: { rating, comment }
  ▼
Middleware: Serilog → ExceptionHandler → Authentication → Authorization([Authorize])
  ▼
ReviewsController.Add
  │  command.ProductId = productId      (from route; [JsonIgnore] on the property)
  │  mediator.Send(command)
  ▼
MediatR pipeline: ValidationBehavior → Rating 1..5? Comment ≤ 500? ProductId not empty?
  │                                      fail → ValidationException → 400
  ▼
AddReviewCommandHandler
  │  userId   = ICurrentUserService.UserId                         (token, never body)
  │  product  = Products.GetByIdAsync(productId)                   null → Result.Failure → 404
  │  bought   = Orders.HasCustomerPurchasedProductAsync(userId, productId)   (Delivered only)
  │             false → DomainException review.not_purchased → 400
  │  existing = Reviews.GetByProductAndCustomerAsync(productId, userId)
  │             not null → DomainException review.already_exists → 400
  │  review   = new Review(productId, userId, rating, comment)     ← Domain rules
  │  Reviews.AddAsync(review)
  │  uow.SaveChangesAsync()
  ▼
Result<Guid>.Success(review.Id)  →  201 Created { id }
```

**Safety net:** unique index `(ProductId, CustomerId)`. Two simultaneous requests can both pass the handler check; the DB rejects the second. (Currently that surfaces as 500 until we map `DbUpdateException` → 409.)

## 3.2 GetProductReviews (Query)

```
Client  GET /api/products/{productId}/reviews?pageNumber=1&pageSize=10     (no token needed)
  ▼
ReviewsController.GetAll → query.ProductId = productId → mediator.Send
  ▼
Validator: page ≥ 1, size 1..50
  ▼
GetProductReviewsQueryHandler
  │  Q1  Products.GetByIdsAsync([productId])               empty → 404
  │  Q2  Reviews.GetPagedByProductAsync(...)               COUNT + page (AsNoTracking, newest first)
  │  Q3  Reviews.GetAverageRatingAsync(productId)          AVG in SQL, (double?) → 0 when no reviews
  │  Q4  IUserLookupService.GetNamesByIdsAsync(ids)        ONE query: WHERE Id IN (...)
  │  map → ReviewDto(Id, Rating, Comment, CreatedAt, AuthorName = names.GetValueOrDefault(id, "Deleted user"))
  ▼
Result<ProductReviewsDto>(AverageRating, PagedResult<ReviewDto>)  →  200
```

**Same trick as order product names:** build a `Dictionary` with one query, then map in memory. No N+1.

## 3.3 Update / Delete my review

```
PUT/DELETE /api/products/{productId}/reviews     Bearer
  Handler: review = Reviews.GetByProductAndCustomerAsync(productId, userId-from-token)   (tracked)
           null → 404          (someone else's review is simply "not found")
           Update: review.UpdateRating(r); review.UpdateComment(c)     → SaveChanges → 204
           Delete: Reviews.Remove(review)                              → SaveChanges → 204
```

There is **no review id in the URL** for user operations, so a user cannot even address another user's review.

## 3.4 Register → Confirm → Login

```
POST /register
  Handler: RegisterAsync (create user, add role, rollback on failure)
           CreateEmailConfirmationTokenAsync(email)     ← Identity token, Base64Url-encoded
           IEmailService.SendEmailConfirmationAsync(...)    (dev: logs the link)
  → 200 UserDto (no JWT)

Email link:  {Frontend:BaseUrl}/confirm-email?userId=...&token=...
Frontend page → POST /confirm-email { userId, token }
  Handler → ConfirmEmailAsync
              user not found                → Failure("Invalid or expired confirmation link.")
              already confirmed             → Success   (idempotent)
              Base64Url decode fails        → Failure(same message)
              UserManager.ConfirmEmailAsync → SecurityStamp rotates → token dead
  → 204 / 400

POST /login
  password OK + EmailConfirmed == false  → 403 { error }
  password OK + confirmed                → 200 JWT
```

## 3.5 Forgot → Reset

```
POST /forgot-password { email }
  Handler: info = CreatePasswordResetTokenAsync(email)   null if user missing OR unconfirmed
           info != null → IEmailService.SendPasswordResetAsync(...)
  → 200 same message ALWAYS

POST /reset-password { userId, token, newPassword }
  Validator: password 8..20
  Handler → ResetPasswordAsync
              decode token (bad → generic failure)
              UserManager.ResetPasswordAsync
                 InvalidToken      → generic failure
                 policy errors     → shown to user
              success → unlock account, reset fail count
  → 204 / 400
```

## 3.6 Token lifecycle

```
GenerateXToken(user)  ← bound to: userId + SecurityStamp + purpose + expiry (24h)
        │
        ▼ Base64Url encode (URL-safe)       → link in email
        │
        ▼ Base64Url decode                  → ConfirmEmailAsync / ResetPasswordAsync
        │
success ──► SecurityStamp changes ──► every older token (confirm AND reset) becomes invalid
```

## 3.7 Error mapping added today

| Situation | Mechanism | HTTP |
|-----------|-----------|------|
| Unconfirmed login (password OK) | `Result.Failure(AuthErrors.EmailNotConfirmed)` + controller compare | **403** |
| Bad / expired confirm or reset token | `Result.Failure` | 400 |
| Weak new password | `Result.Failure` (Identity messages) | 400 |
| Review rule broken | `DomainException` | 400 (+ code) |
| Review/Product not found | `Result.Failure` | 404 |
| Validation | `ValidationException` | 400 |
| `?status=abc` (enum binding) | ASP.NET model binding | 400 (ProblemDetails) |

---

# 4. Catalog: new Commands & Queries

## 4.1 Reviews (`api/products/{productId}/reviews`)

| Name | Type | Input | Handler logic | Output | Errors |
|------|------|-------|---------------|--------|--------|
| `AddReviewCommand` | Command | ProductId(route), Rating, Comment? | product exists → bought & Delivered → not duplicate → `new Review` → save | `Guid` → 201 | 404 / 400 |
| `UpdateMyReviewCommand` | Command | ProductId(route), Rating, Comment? | my review → `UpdateRating` + `UpdateComment` → save | 204 | 404 |
| `DeleteMyReviewCommand` | Command | ProductId | my review → `Remove` → save | 204 | 404 |
| `AdminDeleteReviewCommand` | Command | ReviewId (`api/admin/reviews/{id}`) | `GetByIdAsync` → `Remove` → save | 204 | 404 |
| `GetProductReviewsQuery` | Query | ProductId(route), PageNumber, PageSize | see §3.2 | `ProductReviewsDto` | 404 |

DTOs: `ReviewDto(Id, Rating, Comment, CreatedAt, AuthorName)`, `ProductReviewsDto(AverageRating, PagedResult<ReviewDto>)`.

## 4.2 Auth additions

| Name | Type | Input | Logic | Output |
|------|------|-------|-------|--------|
| `ConfirmEmailCommand` | Command | UserId, Token | §3.4 | 204 / 400 |
| `ResendConfirmationEmailCommand` | Command | Email | token for unconfirmed user → send; **always success** | 200 generic |
| `ForgotPasswordCommand` | Command | Email | token for confirmed user → send; **always success** | 200 generic |
| `ResetPasswordCommand` | Command | UserId, Token, NewPassword | §3.5 | 204 / 400 |
| `RegisterCommand` (changed) | Command | Email, Password, FullName | register + send confirmation email | `UserDto` |
| `LoginCommand` (changed) | Command | Email, Password | + `EmailConfirmed` check after password | JWT / 401 / 403 |

## 4.3 Orders additions (recap)

`CancelMyOrderCommand`, `AdminCancelOrderCommand`, `UpdateOrderStatusCommand(OrderId, Action)`, `GetAllOrdersQuery`, `AdminGetOrderByIdQuery`. Details are in Part 1 §4.5.

---

# 5. Master index: ALL Commands & Queries

| Area | Commands | Queries |
|------|----------|---------|
| **Auth** | Register, Login, ConfirmEmail, ResendConfirmationEmail, ForgotPassword, ResetPassword | — |
| **Cart** | AddItem, DecreaseItem, RemoveItem, Clear | GetCart |
| **Wishlist** | AddItem, RemoveItem, Clear, MoveToCart | GetWishlist |
| **Orders (user)** | Checkout, CancelMyOrder | GetMyOrders, GetOrderById |
| **Orders (admin)** | UpdateOrderStatus (Confirm/Ship/Deliver), AdminCancelOrder | GetAllOrders, AdminGetOrderById |
| **Reviews** | AddReview, UpdateMyReview, DeleteMyReview, AdminDeleteReview | GetProductReviews |
| **Products** | Create, Update, Delete (soft), stock/status changes (admin) | Public list/filter, by id, admin list (incl. deleted) |
| **Categories** | Create, Update, Delete (soft) (admin) | Public list, with products, admin list (incl. deleted) |

Counts: Auth 6 · Cart 5 · Wishlist 5 · Orders 8 · Reviews 5 · plus Products and Categories from the earlier stages.

---

# 6. Design decisions & security design

## 6.1 Decisions

| Decision | Benefit |
|----------|---------|
| One Command per use case | Small, testable classes; adding Reviews touched no existing handler (except Register/Login for email) |
| `[JsonIgnore] ProductId` on body commands, set from route | Id can't be spoofed in the body; Swagger doesn't ask for it twice |
| User operations address resources by `(ProductId, token userId)` | No way to touch another user's review |
| `IUserLookupService` returns `Dictionary<Guid,string>` | One query per page, reusable (admin orders customer names next) |
| `IEmailService` abstraction | SMTP later = one class swap |
| Token encoding inside Identity layer | Handlers/Controllers never see raw Identity tokens |
| Names only (never email) in public review list | Privacy by default |
| `AuthErrors` constant in Core | Compared by Infrastructure and API without a magic string living in two places |

## 6.2 Security checklist (what each feature defends)

| Threat | Defense |
|--------|---------|
| Review as someone else | `CustomerId` from the JWT only |
| Edit someone else's review | Lookup by `(ProductId, userId)` → 404 |
| Fake review without buying | Purchase check on **Delivered** orders |
| Duplicate reviews / race | Handler check + unique index |
| Account enumeration (resend / forgot) | Always 200, identical message |
| Account enumeration (login) | Unconfirmed status revealed only after a correct password |
| Reusing links | SecurityStamp rotation kills old tokens |
| Malformed token → 500 | `FormatException` caught → 400 |
| Locked-out owner after reset | Lockout cleared on successful reset |
| Spam of emails | **Not yet** protected (see weaknesses) |

---

# 7. Honest review: trade-offs & weaknesses

| # | Issue | Impact | Suggestion |
|---|-------|--------|------------|
| 1 | `DbUpdateException` (unique violation) is not mapped | Concurrent duplicate review → 500 | Map to 409 in `GlobalExceptionHandler` |
| 2 | No rate limiting on `forgot-password` / `resend-confirmation` / `login` | Email spam, brute force (lockout only protects login per account) | `AddRateLimiter` (per IP) |
| 3 | JWT stays valid after password reset until it expires | Stolen token survives a reset | Short expiry (15–30 min); Refresh Tokens if needed later |
| 4 | Reset/confirm links are logged with tokens | Fine for dev, a leak in production | Remove with SMTP |
| 5 | Email sending failure after `Register` → 500 although the user exists | Confusing | User can press resend; later queue the email (outbox) |
| 6 | `GetProductReviews` = 4 queries | Slightly chatty | Acceptable. Optimize (single projection) only if measured |
| 7 | Average computed on every read | Cost grows with reviews | Index on `ProductId`; cache or denormalize later if needed |
| 8 | Old `IReviewRepository.GetByProductIdAsync` (unpaged) is now unused | Dead code, an easy source of "load all reviews" mistakes | Remove it |
| 9 | Inactive (not deleted) products can still be reviewed | Business choice | Block if you prefer |
| 10 | `Result.Value` (your code) vs `Result.Data` (some of my samples) | Compile errors when pasting | Use `.Value` consistently |
| 11 | Name taken from `UserName` | Fine if `UserName = FullName` | Switch to `FullName` if you add it |
| 12 | Still no automated tests | Regressions undetected | Next stage: Domain unit tests, then handler tests |
| 13 | Review hard delete | No audit trail | Acceptable for learning |

---

# 8. Assumptions to verify

| # | Assumption | If wrong |
|---|-----------|----------|
| 1 | `Review` has `ProductId`, `CustomerId`, `Rating`, `Comment`, `CreatedAt`, ctor `(productId, customerId, rating, comment)`, `UpdateRating(int)` | Adjust names / add method |
| 2 | `uow.Reviews` exists (you confirmed) | ✔ |
| 3 | `HasCustomerPurchasedProductAsync` filters `Status == Delivered` | **Must be changed** if still `!= Cancelled` |
| 4 | Unique index `(ProductId, CustomerId)` migration applied | Duplicate guard missing |
| 5 | `ReviewDto.CreatedAt` is `DateTime` | Adjust type |
| 6 | `DataProtectionTokenProviderOptions` lifespan set to 24h | Default is also 24h |
| 7 | `Result.Value` is the success payload property | Match your `Result<T>` |

---

# Appendix: one-page cheat sheet (today's features)

```
Review:   Controller → Validator → Handler
          product? → bought&Delivered? → duplicate? → new Review → Save
          list: page + avg + names(1 query) → ReviewDto

Email confirm:  Register → token(Identity, Base64Url) → IEmailService → link
                confirm-email → decode → ConfirmEmailAsync → SecurityStamp rotates
                Login: password first, THEN EmailConfirmed → 403

Reset:    forgot (always 200, confirmed users only) → link
          reset → decode → ResetPasswordAsync → unlock → 204
```

**ملخص بالمصري:**
الـ Review: كل عملية بتتحدد بـ (المنتج + اليوزر من التوكن)، فمستحيل تلمس تقييم حد تاني.
الأسماء بتيجي بـ Query واحد من الـ Identity عن طريق Interface.
الإيميل: بنفحص التأكيد بإيدينا بعد الباسورد، لأننا مش بنستخدم SignInManager.
أي Token بيموت بعد أول استخدام لأن الـ SecurityStamp بيتغير.
