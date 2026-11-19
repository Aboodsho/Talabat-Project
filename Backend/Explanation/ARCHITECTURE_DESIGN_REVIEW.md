# E-Commerce Backend — Architectural Design Review
# مراجعة التصميم المعماري — مشروع الـ E-Commerce

Stack: ASP.NET Core Web API (.NET 9) · EF Core · SQL Server · MediatR · FluentValidation · Serilog

> Note / ملحوظة:
> Diagrams are English only. Explanations are English then Egyptian Arabic on separate lines.
> Namespaces and some names (e.g. `IProdcutRepository`) follow your project as-is.

---

## Table of Contents

1. Architecture & Methodology
2. Thinking Process (requirement → code)
3. Data Flow Sequences (Command / Query / Error)
4. Catalog: all Commands & Queries
5. Catalog: all Repositories
6. Design Decisions: file organization
7. Honest review: trade-offs & weaknesses
8. Assumptions to verify

---

# 1. Architecture & Methodology

## 1.1 Clean Architecture (5 layers)

```
 API ──────────► Service ◄────────── Infrastructure
  │                │  │                    │
  │                ▼  ▼                    │
  │             Domain ◄── Core             │
  └────────────────────────────────────────┘
   (API also references Infrastructure only to wire DI)

Dependency rule: arrows point INWARD. Inner layers know nothing about outer layers.
```

| Layer | Contains | Knows about | Must NOT contain |
|-------|----------|-------------|------------------|
| Core | Constants (`Roles`), shared primitives | nothing | business logic |
| Domain | Entities, business rules, `DomainException` | Core | EF Core, Identity, HTTP |
| Service | Commands, Queries, Handlers, DTOs, Validators, **interfaces** (repos, `IIdentityService`, `ICurrentUserService`) | Domain, Core | EF Core, DB code |
| Infrastructure | EF Core `AppDbContext`, repositories, Identity, JWT | Service, Domain | HTTP concerns |
| API | Controllers, `Program.cs`, exception handler | Service | business rules |

**WHY:**
- Business rules (Domain) survive any change of DB / framework.
- Handlers depend on **interfaces**, so they can be tested with fakes.

**ليه؟**
قواعد الشغل (Domain) مبتتأثرش لو غيرت الداتابيز أو الـ Framework.
الـ Handlers بتعتمد على Interfaces بس، فسهل نختبرها.

**Rule we applied:** interface lives in Service, implementation in Infrastructure.
Exception: `CurrentUserService` lives in API because it needs `HttpContext`.

**القاعدة:**
الـ Interface في Service، والتنفيذ في Infrastructure.
الاستثناء: `CurrentUserService` في API لأنه محتاج `HttpContext`.

## 1.2 CQRS (Command Query Responsibility Segregation)

| | Command | Query |
|---|---------|-------|
| Purpose | Change state | Read state |
| Returns | `Result` / `Result<Guid>` (small) | DTO / `PagedResult<DTO>` |
| EF tracking | **Tracked** (needs change detection) | **AsNoTracking** (read-only, faster) |
| Touches Domain methods | Yes (`order.Cancel()`) | No (maps to DTO) |
| Calls `SaveChanges` | Yes (once) | Never |

**Benefit in our project:**
1. Each use case = one small class set (Command + Validator + Handler). No giant `OrderService` with 20 methods.
2. Writes go through the Domain (rules enforced). Reads skip the Domain and project straight to DTOs (fast).
3. Cross-cutting (validation) is done once in the MediatR pipeline, not repeated in every method.
4. Adding a feature = adding files, not editing existing ones (Open/Closed).

**الفايدة عندنا:**
كل Use Case ليه ملفات صغيرة لوحده (Command + Validator + Handler).
مفيش Service ضخمة فيها 20 method.
الكتابة بتعدي على الـ Domain (القواعد تتطبق). القراءة بتروح للـ DTO على طول (أسرع).
إضافة Feature جديدة = ملفات جديدة، مش تعديل في القديم.

## 1.3 Supporting patterns

| Pattern | Where | Why |
|---------|-------|-----|
| Repository + Unit of Work | `IOrderRepository`, `IUnitOfWork` | Handlers don't know EF. One `SaveChanges` = one transaction across aggregates |
| Result pattern | `Result` / `Result<T>` | **Expected** failures (cart empty, not found) without exceptions |
| Exceptions | `DomainException`, `ValidationException`, `DbUpdateConcurrencyException` | **Unexpected / rule-breaking** cases → `GlobalExceptionHandler` → ProblemDetails |
| Aggregate root | `Order`, `Cart`, `Wishlist` | Children (`OrderItem`, `CartItem`) changed only through the root |
| Optimistic concurrency | `Product.RowVersion` | Two users buying the last item → one gets 409 |
| Soft delete + global query filter | Product, Category | Deleted data hidden by default, `IgnoreQueryFilters` when needed |

**Result vs Exception — the rule we used:**
- "The user did something normal but not possible" (empty cart, order not found) → `Result.Failure`.
- "A rule was broken / bad input / race" (invalid state transition, validation, concurrency) → Exception.

**قاعدة Result مقابل Exception:**
حاجة طبيعية بس مش ممكنة (الكارت فاضي، الأوردر مش موجود) → `Result.Failure`.
كسر قاعدة أو input غلط أو تعارض (حالة غلط، Validation، Concurrency) → Exception.

---

# 2. Thinking Process (requirement → code)

> This is the order I follow for every feature. Example: **Checkout**.

```
Step 1  Requirement (business sentence)
Step 2  Rules & invariants       → Domain
Step 3  Use cases (Commands / Queries)
Step 4  Data each use case needs → Repository methods
Step 5  DTOs (what leaves the system)
Step 6  Handler sequence (orchestration)
Step 7  Endpoint + error mapping (Controller last)
Step 8  Migration / config
Step 9  Test table
```

**ترتيب التفكير: من جوه لبرا. الـ Domain الأول، الـ Controller آخر حاجة.**

## Step 1 — Requirement
"A user turns his cart into an order. Stock must be reduced. Prices must be frozen."

## Step 2 — Domain first (rules, no technology)
Questions I ask:
- What can never be true? (order with zero items, negative quantity, cancel a shipped order)
- Who owns the rule? (`Order` owns status transitions; `Cart` owns max quantity; `Product` owns stock)

Result:
- `Order` ctor takes items with `UnitPrice` → price is **frozen** at order time (later price change doesn't touch old orders).
- `Order.Cancel()` allowed only from Pending/Confirmed.
- `Product.DecreaseStock` throws if stock is insufficient.

**WHY Domain first:** if the rule is in the Domain, every handler gets it for free and can't forget it.

**ليه Domain الأول؟**
لو القاعدة في الـ Domain، كل Handler بياخدها تلقائي ومحدش ينساها.

## Step 3 — Use cases
Checkout changes state → **Command**. "My orders" reads → **Query**.
Each use case = one request class. Name = verb + noun (`CheckoutCommand`, `GetOrderByIdQuery`).

## Step 4 — Repository methods are *derived* from handlers
I don't design repositories first. I write the handler sequence on paper, then every data need becomes a method:

```
Checkout needs: cart with items (tracked)        → ICartRepository.GetByCustomerIdAsync
                products by ids (tracked, active) → IProdcutRepository.GetByIdsTrackedAsync
                add the new order                 → IOrderRepository.AddAsync

GetOrderById needs: order + items + address       → IOrderRepository.GetByIdWithDetailsAsync
                    product names (even deleted)  → IProdcutRepository.GetByIdsIncludingDeletedAsync
```

**WHY:** no unused methods. Every repo method has a named caller.

**ليه؟**
مفيش method في الـ Repo من غير ما يكون ليها مستخدم واضح.

## Step 5 — DTOs
Rule: **Command/Query = input. DTO = output.**
- Never return entities (leaks internals, lazy-load/serialization problems).
- `UserId` is **never** in the input → always from token (`ICurrentUserService`).
  Prevents user A sending user B's id.

## Step 6 — Handler sequence
Order always: **load → check → change → save → return**.

## Step 7 — Controller last
Controller = thin. It only: binds input, `Send()`, maps `Result` → HTTP status.

| Result | HTTP |
|--------|------|
| Cart / Wishlist / Order-by-id failure | 404 |
| Checkout failure | 400 |
| Validation exception | 400 |
| `DomainException` | 400 (+ code) |
| Concurrency | 409 |
| Anything else | 500 (generic message) |

---

# 3. Data Flow Sequences

## 3.1 Command flow (state change)

```
Client
  │  POST /api/orders/checkout   (Authorization: Bearer <JWT>)
  ▼
Kestrel → Serilog request logging → ExceptionHandler → Authentication → Authorization
  │       (JWT validated, [Authorize] checked, claims → HttpContext.User)
  ▼
OrdersController.Checkout()
  │  mediator.Send(new CheckoutCommand(...))
  ▼
MediatR
  │  1. finds the single IRequestHandler<CheckoutCommand, Result<Guid>>
  │  2. runs pipeline behaviors (FluentValidation → throws ValidationException on bad input)
  ▼
CheckoutCommandHandler.Handle()
  │  userId  = ICurrentUserService.UserId           (from token)
  │  cart    = Carts.GetByCustomerIdAsync(userId)   (tracked)
  │  products= Products.GetByIdsTrackedAsync(ids)   (tracked)
  │  for each item: check active + stock → product.DecreaseStock(qty)   ← Domain rule
  │  order   = new Order(...)                       ← Domain rule (frozen UnitPrice)
  │  Orders.AddAsync(order)
  │  cart.Clear()                                   ← Domain rule
  │  uow.SaveChangesAsync()                         ← ONE transaction
  ▼
Result<Guid>.Success(order.Id)
  ▼
Controller maps → 201 Created (or 400 on Failure)
```

**Key point:** the Handler *orchestrates* (who to load, in what order). The *rules* live in the Domain. The Handler never does `if (order.Status == ...)` itself.

**النقطة المهمة:**
الـ Handler بيرتب الخطوات بس. القواعد نفسها جوه الـ Domain.
الـ Handler مبيعملش `if (order.Status == ...)` بنفسه.

**What "modifying state" really means in EF:**

```
Load tracked entity ──► call Domain method ──► EF ChangeTracker sees the changes
                                              ──► SaveChanges generates UPDATE/INSERT
                                              ──► one DB transaction (all or nothing)
```

Checkout touches 3 aggregates (Product stock, Order insert, Cart clear) → atomic because of one `SaveChanges`.

## 3.2 Query flow (read)

```
Client  GET /api/orders/{id}
  ▼
(same middleware) → OrdersController.GetById
  ▼
MediatR → GetOrderByIdQueryHandler
  │  userId = ICurrentUserService.UserId
  │  order  = Orders.GetByIdWithDetailsAsync(id)       (Include Items + ShippingAddress)
  │  order == null OR order.CustomerId != userId  →  Result.Failure   (→ 404, not 403)
  │  names  = Products.GetByIdsIncludingDeletedAsync(ids)  → Dictionary<Guid,string>
  │  dto    = order.ToDto(names)                        (OrderMapping)
  ▼
Result<OrderDto>.Success(dto) → 200
```

**Why 404 not 403 for someone else's order:** don't reveal that the id exists.

**ليه 404 مش 403؟**
مش عايزين نقول لليوزر إن الأوردر ده موجود أصلاً.

**Paged query (GetMyOrders / GetAllOrders):**

```
validate (page ≥ 1, size 1..50)
  → repo: Count first → OrderByDescending(CreatedAt) → Skip((p-1)*size).Take(size) → AsNoTracking
  → map each Order → OrderSummaryDto (Id, Status string, CreatedAt, ItemsCount, TotalAmount)
  → PagedResult<T>(items, pageNumber, pageSize, totalCount)
```

Why max page size 50: protects the DB from `pageSize=1000000`.

## 3.3 Error flow

```
Validator fails            → ValidationException        ┐
Domain rule broken         → DomainException(code,msg)  ├→ GlobalExceptionHandler → ProblemDetails 400
Two writers, same Product  → DbUpdateConcurrencyException→ 409
Unexpected bug             → logged by Serilog          → 500 (generic text, no internals)
Expected failure           → Result.Failure             → Controller decides 400/404
```

## 3.4 Cancel flow (new)

```
POST /api/orders/{id}/cancel
  Handler: load order (tracked, with items)
           not found OR not mine → Failure (404)
           order.Cancel()                  ← throws if Shipped/Delivered/Cancelled (BEFORE stock changes)
           OrderStockRestorer.RestoreAsync ← products tracked → product.IncreaseStock(qty)
           SaveChanges                     ← status + stock together
```

Order matters: `Cancel()` first → a second cancel throws → stock can't be restored twice.

---

# 4. Catalog: all Commands & Queries

> Conventions: userId always from token. DTO = output only.

## 4.1 Auth

| Name | Type | Input | Handler logic | Output | Errors |
|------|------|-------|---------------|--------|--------|
| `RegisterCommand` | Command | Email, Password, FullName | validate → `IIdentityService.RegisterAsync` (create user, add role User, rollback if role fails) → issue JWT | `AuthResponseDto` | 400 (validation / duplicate) |
| `LoginCommand` | Command | Email, Password | find user → lockout check → password check → fail count / reset → JWT | `AuthResponseDto` | 401 with ONE generic message |

Security notes: same message for wrong email / wrong password (no user enumeration), lockout after 3 failures for 15 min, JWT HS256 with `jti`, `ClockSkew = Zero`.

## 4.2 Cart (`api/cart`)

| Name | Type | Input | Handler logic | Output |
|------|------|-------|---------------|--------|
| `AddItemCommand` | Command | ProductId, Quantity | get/create cart → load product → `cart.AddCartItem(productId, qty, stock)` (checks stock, then max 10) → save | updated cart |
| `DecreaseItemCommand` | Command | ProductId (route), Amount=1 | cart → `cart.DecreaseItemQuantity` → save | updated cart |
| `RemoveItemCommand` | Command | ProductId | cart → `cart.RemoveItem` → save | updated cart |
| `ClearCartCommand` | Command | — | cart → `cart.Clear()` → save | 204 |
| `GetCartQuery` | Query | — | cart + products; deleted/inactive products **disappear** | `CartDto` |

## 4.3 Wishlist (`api/wishlist`)

| Name | Type | Handler logic |
|------|------|---------------|
| `AddItemCommand` | Command | `wishlist.AddItem` (idempotent, max 50) |
| `RemoveItemCommand` | Command | `wishlist.RemoveItem` |
| `ClearWishlistCommand` | Command | `wishlist.Clear` |
| `MoveToCartCommand` | Command | load wishlist + cart + product → `cart.AddCartItem` → `wishlist.RemoveItem` → **one** save (atomic move) |
| `GetWishlistQuery` | Query | wishlist + product info → `WishlistDto` |

## 4.4 Orders — customer

| Name | Type | Input | Logic | Output | Failure → HTTP |
|------|------|-------|-------|--------|----------------|
| `CheckoutCommand` | Command | ShippingAddress | see §3.1 | `Guid` (order id) | 400 |
| `CancelMyOrderCommand` | Command | OrderId | see §3.4 | — | 404 (+400 by domain) |
| `GetMyOrdersQuery` | Query | PageNumber, PageSize | `GetPagedByCustomerAsync` → summary DTOs | `PagedResult<OrderSummaryDto>` | 400 validation |
| `GetOrderByIdQuery` | Query | OrderId | see §3.2 | `OrderDto` | 404 |

## 4.5 Orders — admin (`api/admin/orders`, `[Authorize(Roles = Admin)]`)

| Name | Type | Input | Logic | Output |
|------|------|-------|-------|--------|
| `GetAllOrdersQuery` | Query | Status?, PageNumber, PageSize | `GetPagedAsync(status, ...)` | `PagedResult<OrderSummaryDto>` |
| `AdminGetOrderByIdQuery` | Query | OrderId | same as customer query, **no ownership check** | `OrderDto` |
| `UpdateOrderStatusCommand` | Command | OrderId, `Action` (Confirm/Ship/Deliver) | load → `switch` on action → domain method → save | 204 |
| `AdminCancelOrderCommand` | Command | OrderId | load → `Cancel()` → restore stock → save | 204 |

Order lifecycle (enforced in Domain, not in handlers):

```
Pending ──Confirm──► Confirmed ──Ship──► Shipped ──Deliver──► Delivered
   │                     │
   └──────── Cancel ─────┘   (only from Pending / Confirmed; stock returns)
```

Why ONE `UpdateOrderStatusCommand` but SEPARATE cancel commands:
- Confirm/Ship/Deliver have identical shape (load → transition → save).
- Cancel has an extra side effect (stock restore), and user vs admin differ in ownership check.

**ليه Command واحد للـ 3 حالات؟**
الـ 3 بيعملوا نفس الخطوات. الإلغاء مختلف لأنه بيرجّع الستوك.

## 4.6 Products & Categories (built earlier, same pattern)

Admin: create / update / delete (soft) / change stock / change status / list including deleted.
Public: list (filter + paging via `GetPagedAsunc`, `ProdcutFilter`), get by id.
Restore (undo soft delete) is postponed.

---

# 5. Catalog: all Repositories

## 5.1 `IProdcutRepository`

| Method | Tracking | Deleted products | Used by | Why this shape |
|--------|----------|------------------|---------|----------------|
| `GetByIdsAsync(ids)` | No | hidden | `GetCartQuery`, `GetWishlistQuery` | Read-only; `Include(Images)` for the main image; **one query for all ids** (no N+1) |
| `GetByIdsTrackedAsync(ids)` | **Yes** | hidden | `CheckoutHandler`, cancel (stock restore) | We change `StockQuantity` → EF must track |
| `GetByIdsIncludingDeletedAsync(ids)` | No | **visible** (`IgnoreQueryFilters`) | `GetOrderById`, `AdminGetOrderById` | Old orders must still show the product name even if the product was deleted later |
| `GetPagedAsunc(filter, ...)` | No | hidden | Product list | Filter + paging |

**Decision:** three "by ids" methods instead of one with flags. Each name tells exactly what you get (tracking + deleted behavior). Flags like `bool tracked, bool includeDeleted` hide mistakes.

**قرار:**
3 methods بدل method واحدة بـ flags. الاسم بيقول بالظبط هترجع إيه.

## 5.2 `IOrderRepository`

| Method | Tracking | Includes | Used by | Why |
|--------|----------|----------|---------|-----|
| `AddAsync(order)` | — | — | Checkout | |
| `GetByCustomerIdAsync` | No | — | (lookups) | |
| `GetByIdWithDetailsAsync(id)` | **Yes** | Items + ShippingAddress | Cancel, UpdateStatus, GetById, AdminGetById | One method for read and write (tracked is fine for reads, tiny cost). Avoids duplicate methods |
| `GetPagedByCustomerAsync` | No | Items (for count/total) | `GetMyOrdersQuery` | Filter by `CustomerId` **inside SQL**; Count → Order → Skip/Take |
| `GetPagedAsync(status?, ...)` | No | Items | `GetAllOrdersQuery` | Admin: optional status filter; index on `Status` |
| `HasCustomerPurchasedProductAsync` | No | — | Review (next) | `Any()` query, returns bool only |

Indexes supporting them: `IX_Orders_CustomerId`, `IX_Orders_Status`.

## 5.3 `ICartRepository`

| Method | Tracking | Includes | Notes |
|--------|----------|----------|-------|
| `GetByCustomerIdAsync` | Yes | CartItems | Unique index `(CartId, ProductId)` guarantees one row per product. `Id`s are client-generated Guids → `ValueGeneratedNever()` so EF treats new children as INSERT, not UPDATE |

## 5.4 `IWishListRepository`

| Method | Tracking | Notes |
|--------|----------|-------|
| `GetByCustomerId` (your naming, no `Async`) | Yes | Same rules as cart |

## 5.5 `IUnitOfWork`

| Member | Purpose |
|--------|---------|
| `Carts`, `Products`, `Wishlists`, `Orders` | Access repos sharing ONE `DbContext` |
| `SaveChangesAsync(ct)` | The only commit point |

**WHY a Unit of Work:** all repos share the same `DbContext` → one `SaveChanges` = one transaction across aggregates. Repositories never call `SaveChanges` themselves.

## 5.6 Not repositories (on purpose)

| Interface | Why separate |
|-----------|--------------|
| `IIdentityService` | Wraps `UserManager`/`RoleManager` (ASP.NET Identity). Not our entity, different API |
| `IJwtTokenService` | Token creation is not persistence |
| `ICurrentUserService` | Reads the HTTP token; lives in API |

---

# 6. Design Decisions: file organization

```
Core/
  Constants/Roles.cs
Domain/
  Entities/ (Product, Category, Cart, CartItem, Wishlist, WishListItem, Order, OrderItem, OrderShippingAddress)
  Exceptions/DomainException.cs
Service/
  DTOs/{Auth,Carts,Wishlists,Orders}/
  Interfaces/ (IUnitOfWork, repos, IIdentityService, IJwtTokenService, ICurrentUserService)
  Features/
    Auth/Commands/{Register,Login}/
    Carts/{Commands,Queries}/...
    Wishlists/{Commands,Queries}/...
    Orders/
      Commands/{Checkout,CancelMyOrder,AdminCancelOrder,UpdateOrderStatus}/
      Queries/{GetMyOrders,GetOrderById,GetAllOrders,AdminGetOrderById}/
      OrderMapping.cs
      OrderStockRestorer.cs
Infrastructure/
  Persistence/ (AppDbContext, Configurations/, Repositories/, UnitOfWork)
  Identity/ (ApplicationUser, IdentityService, IdentitySeeder, JwtTokenService)
API/
  Controllers/ (Auth, Cart, Wishlist, Orders, AdminOrders, Products, Categories)
  Middleware/GlobalExceptionHandler.cs
  Program.cs
```

| Decision | Benefit |
|----------|---------|
| **Feature folders** (by use case, not by type) | Everything about "Checkout" is in one folder. Adding/removing a feature touches one place |
| Command + Validator + Handler together | You see input, rules, and logic in one glance |
| Plural folder names (`Carts`, `Orders`) | Avoids namespace clash with entity names (`Cart`, `Order`) |
| Interfaces in Service | Handlers never reference EF → testable, DB replaceable |
| `OrderMapping` as static extension | Mapping logic in one place; no AutoMapper magic to debug |
| `OrderStockRestorer` shared helper | User-cancel and admin-cancel share restore logic without sharing ownership logic |
| One `GlobalExceptionHandler` | Handlers contain zero try/catch; consistent error format |
| Entity configurations in `Configurations/` | Domain stays free of EF attributes (that's why config is in Infrastructure) |

**Scalability:**
- New feature (Reviews) = new folder + one repo + one controller. No existing file edited except DI/DbContext.
- Read side can later move to a read replica / Dapper without touching commands (CQRS separation already exists).
- Paged queries + `AsNoTracking` + indexes keep reads cheap as data grows.

**Maintainability:**
- A bug in "cancel" is found in `CancelMyOrder` + `Order.Cancel()` only.
- Domain rules are unit-testable with zero infrastructure.

---

# 7. Honest review: trade-offs & weaknesses

| # | Issue | Impact | Suggestion |
|---|-------|--------|------------|
| 1 | MediatR + 5 layers is **heavy for this size** | More files than a simple service layer | Accepted: the goal is learning. In a small production app, 3 layers + services is enough |
| 2 | Repositories on top of EF Core (already a repo + UoW) | Extra abstraction | Kept for layering practice and testability. Know that EF alone is a valid choice |
| 3 | Product stock restore skips soft-deleted products | Stock of deleted product not returned | Acceptable (not sellable). Document it |
| 4 | Admin order view has only `CustomerId` | Admin can't see name/email | Later: `IIdentityService.GetUsersByIds` |
| 5 | Wishlist repo name `GetByCustomerId` has no `Async`; repo `IProdcutRepository` typo | Inconsistent naming | Rename in one commit (IDE refactor) |
| 6 | Concurrency only on `Product` | Two admins updating same order status at once = last write wins | Add `RowVersion` to `Order` if admin count grows |
| 7 | No tests yet | Regressions undetected | Domain unit tests first (cheapest, highest value), then handler tests with fakes |
| 8 | No payment / no order-number | Not production-complete | Out of scope for learning |
| 9 | JWT only, no refresh token | User re-logins after expiry | Deliberately skipped |

---

# 8. Assumptions to verify

| # | Assumption | If wrong |
|---|-----------|----------|
| 1 | `IUnitOfWork` exposes `Carts`, `Products`, `Wishlists`, `Orders`, `SaveChangesAsync(ct)` | Adjust handlers |
| 2 | `Product.IncreaseStock(int)` exists | Add it to Domain (validate `quantity > 0`) |
| 3 | `Result` has `Success()` / `Failure(string)`, `Result<T>` has `Data`, `Error`, `IsSuccess` | Adapt calls |
| 4 | `GetPagedAsync` returns `PagedResult<Order>` | Mirror `GetMyOrdersQueryHandler` |
| 5 | `Order.Items` is the public collection | Rename in helper |
| 6 | `Order.Cancel()` throws `DomainException` on invalid state | Verify (tests #2, #4, #10, #12) |

---

# Appendix: one-page cheat sheet

```
Requirement
   ▼
Domain rule?  ──► put in entity method (throws DomainException)
   ▼
Changes state? ──► Command (+Validator) ──► Handler: load tracked → domain call → SaveChanges once
Reads only?    ──► Query   (+Validator) ──► Handler: AsNoTracking repo call → map to DTO
   ▼
Repo method needed? ──► add ONLY what a handler calls; name states tracking/deleted behavior
   ▼
Controller: [Authorize] → Send → map Result to HTTP code
   ▼
Test table (happy path, not-found, not-mine, wrong state, validation, concurrency)
```

**ملخص بالمصري:**
فكر من جوه لبرا: القاعدة في الـ Domain، بعدين الـ Command أو Query، بعدين الـ Repo اللي الـ Handler محتاجه، وآخر حاجة الـ Controller.
الـ Handler بيرتب الخطوات: حمّل ← غيّر عن طريق الـ Domain ← احفظ مرة واحدة ← رجّع النتيجة.
