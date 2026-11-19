# Repositories — Full Code Explained
# شرح كود كل الـ Repositories

Project: E-Commerce (Clean Architecture + CQRS) · EF Core · SQL Server

> **Read this first / اقرأ الأول**
>
> Every code block is tagged:
> - **[EXACT]** = the code we wrote together in this project.
> - **[REFERENCE]** = a reference implementation matching the interface and the behavior we agreed on. Compare it with your own file. If yours differs, yours wins.
>
> كل Code Block متعلّم:
> - **[EXACT]** = الكود اللي كتبناه في المشروع.
> - **[REFERENCE]** = تنفيذ مرجعي مطابق للـ Interface والسلوك المتفق عليه. قارنه بملفك، ولو مختلف، ملفك هو الصح.

---

## Table of Contents

1. Concepts you need first (DbContext, DbSet, Tracking, Query Filter, Include)
2. `GenericRepository<T>` and `IRepository<T>`
3. `UnitOfWork`
4. `ProductRepository`
5. `OrderRepository`
6. `CartRepository`
7. `WishlistRepository`
8. `CategoryRepository`
8B. `ReviewRepository` (new)
8C. `UserLookupService` (new, used like a repository)
9. Who calls which method (matrix)
10. Common mistakes & performance notes

---

# 1. Concepts you need first

## 1.1 DbContext and DbSet

```
AppDbContext  (one per HTTP request, Scoped)
   │
   ├── ChangeTracker   ← remembers every entity it loaded (tracked)
   ├── DbSet<Product>  ← like a table
   ├── DbSet<Order>
   ├── DbSet<Cart>
   └── ...

Repository = a small class that holds _dbSet and exposes queries with names.
```

**Simple definition:**
`DbSet<Order>` = the Orders table as C# collection.
Repository = a wrapper that gives each query a clear name.

**بالمصري:**
الـ `DbSet<Order>` هو جدول الأوردرات كأنه List في C#.
الـ Repository بيدّي كل Query اسم واضح، عشان الـ Handler ميعرفش EF.

## 1.2 Tracking vs AsNoTracking

```
TRACKED (default)                       AsNoTracking
─────────────────                       ────────────
Load Product                            Load Product
   │                                       │
ChangeTracker keeps a snapshot          Nothing saved
   │                                       │
product.DecreaseStock(2)                product.DecreaseStock(2)  ← EF doesn't know
   │                                       │
SaveChanges → UPDATE generated          SaveChanges → NOTHING happens (silent bug!)
```

| | Tracked | AsNoTracking |
|---|---------|--------------|
| Use when | You will **change** and save | You only **read** |
| Memory / CPU | More | Less (faster) |
| Danger | none | Changing an untracked entity = **silently ignored** |

**Rule we used:** Command handler → tracked. Query handler → AsNoTracking.

**القاعدة:** لو هتعدّل وتحفظ → Tracked. لو هتقرا بس → AsNoTracking.
لو عدّلت حاجة جت AsNoTracking، مفيش Error. التعديل بيضيع بصمت.

## 1.3 Global Query Filter (soft delete)

```
modelBuilder.Entity<Product>().HasQueryFilter(p => p.DeletedAt == null);

Normal query:            SELECT ... FROM Products WHERE DeletedAt IS NULL
.IgnoreQueryFilters():   SELECT ... FROM Products            (deleted included)
```

**WHY:** nobody forgets `WHERE DeletedAt IS NULL`. Hidden by default, visible only when we ask.

**ليه؟** مفيش حد ينسى يفلتر المحذوف. هو مخفي تلقائي، وبنظهره بس لما نطلب.

## 1.4 Include

```
Order ──< OrderItem        (Items)
Order ──1 ShippingAddress

Without Include:  order.Items → EMPTY (EF doesn't load children by itself)
With Include:     order.Items → loaded in the same SQL (JOIN)
```

**بالمصري:** من غير `Include` الـ Items بتيجي فاضية. مش Error، بس البيانات ناقصة.

## 1.5 `Contains` → SQL `IN`

```csharp
var idList = ids.Distinct().ToList();
_dbSet.Where(p => idList.Contains(p.Id))
```

becomes

```sql
SELECT ... FROM Products WHERE Id IN (@p0, @p1, @p2)
```

**One query for N products.** Without it we would loop and call the DB N times (N+1 problem).

**استعلام واحد لكل المنتجات** بدل ما نسأل الداتابيز مرة لكل منتج.

---

# 2. `IRepository<T>` and `GenericRepository<T>`

## Interface (Core) **[EXACT contract]**

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}
```

## Implementation (Infrastructure) **[REFERENCE]**

```csharp
public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);   // tracked

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    public virtual void Update(T entity) => _dbSet.Update(entity);

    public virtual void Remove(T entity) => _dbSet.Remove(entity);
}
```

**Line by line:**

| Line | Meaning |
|------|---------|
| `where T : BaseEntity` | Only entities with `Id` can use it. This is why `BaseEntity` lives in **Core** |
| `protected` fields | Child repos (`OrderRepository`) reuse `_dbSet` and `_context` |
| `GetByIdAsync` tracked | Most uses are "load then change", so tracked is the safe default |
| `AddAsync` | Only marks the entity as `Added`. **Nothing goes to the DB yet** |
| `Update` / `Remove` | Same: only mark state. DB changes at `SaveChanges` |
| No `SaveChanges` anywhere | Commit belongs to Unit of Work only |

**الخلاصة:**
الـ Repository مبيحفظش في الداتابيز. هو بس بيجهّز التغييرات. الحفظ في `SaveChangesAsync` بتاع الـ UoW.

---

# 3. `UnitOfWork` **[REFERENCE]**

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    private IProdcutRepository? _products;
    private IOrderRepository?   _orders;
    private ICartRepository?    _carts;
    private IWishListRepository? _wishlists;

    public UnitOfWork(AppDbContext context) => _context = context;

    public IProdcutRepository Products   => _products  ??= new ProductRepository(_context);
    public IOrderRepository   Orders     => _orders    ??= new OrderRepository(_context);
    public ICartRepository    Carts      => _carts     ??= new CartRepository(_context);
    public IWishListRepository Wishlists => _wishlists ??= new WishlistRepository(_context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
```

```
Handler
  │ uow.Products ──┐
  │ uow.Orders   ──┼──► all share the SAME AppDbContext instance
  │ uow.Carts    ──┘
  ▼
uow.SaveChangesAsync()  ──► ONE SQL transaction for everything changed
```

| Piece | Why |
|-------|-----|
| `??=` (lazy) | Repo created only when used |
| One shared `_context` | Required for one transaction across repos |
| `SaveChangesAsync` | EF wraps all pending INSERT/UPDATE/DELETE in **one transaction** |

**Example (Checkout):** stock decrease (Product) + new Order + cart clear (Cart) → one `SaveChanges`. If one fails, **nothing** is saved.

**بالمصري:** الـ UoW بيخلّي كل الـ Repos يشتغلوا على نفس الـ DbContext، فالحفظ مرة واحدة = Transaction واحدة. يا كله يتحفظ يا ولا حاجة.

---

# 4. `ProductRepository`

Interface **[EXACT]**:

```csharp
public interface IProdcutRepository : IRepository<Product>
{
    Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Product?> GetWithImagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Product>> GetPagedAsunc(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<Product>> GetFilteredAsync(ProdcutFilter filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByIdsTrackedAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByIdsIncludingDeletedAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
```

## 4.1 `GetByIdsAsync` — for display **[EXACT]**

```csharp
public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
{
    var idList = ids.Distinct().ToList();

    return await _dbSet
        .AsNoTracking()
        .Include(p => p.Images)
        .Where(p => idList.Contains(p.Id))
        .ToListAsync(ct);
}
```

| Line | Meaning |
|------|---------|
| `ids.Distinct().ToList()` | Remove duplicates. Materialize once (EF can't translate a lazy `IEnumerable` reliably) |
| `AsNoTracking()` | Read only → faster |
| `Include(p => p.Images)` | Cart/Wishlist need the main image |
| `Where(... Contains ...)` | `WHERE Id IN (...)` — one query |
| No `IgnoreQueryFilters` | Deleted products **are hidden** → they disappear from Cart (your decision) |

Used by: `GetCartQuery`, `GetWishlistQuery`, `MoveToCart`, `AddItem` (stock check read).

## 4.2 `GetByIdsTrackedAsync` — for changing stock **[EXACT]**

```csharp
public async Task<IReadOnlyList<Product>> GetByIdsTrackedAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
{
    var idList = ids.Distinct().ToList();

    return await _dbSet
        .Where(p => idList.Contains(p.Id))
        .ToListAsync(ct);
}
```

| Difference from 4.1 | Why |
|---------------------|-----|
| No `AsNoTracking` | We call `product.DecreaseStock()` / `IncreaseStock()` and need `UPDATE` at save |
| No `Include(Images)` | Not needed, lighter query |
| Deleted still hidden | A deleted product can't be bought |

**Memory view (Checkout):**

```
ChangeTracker
  ├─ Product A  (Stock 10)  ◄── returned by repo, tracked
  │      DecreaseStock(2) → Stock 8  → state = Modified
  ├─ Order (new)            → state = Added
  └─ Cart                   → CartItems removed → Deleted
SaveChanges → UPDATE Products ... WHERE Id=@id AND RowVersion=@old   (concurrency check)
              INSERT Orders / OrderItems
              DELETE CartItems
```

`RowVersion` (shadow property) makes SQL add `AND RowVersion = @old`. If another buyer changed the same product first → 0 rows updated → `DbUpdateConcurrencyException` → 409.

## 4.3 `GetByIdsIncludingDeletedAsync` — old orders **[EXACT]**

```csharp
public async Task<IReadOnlyList<Product>> GetByIdsIncludingDeletedAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
{
    var idList = ids.Distinct().ToList();

    return await _dbSet
        .IgnoreQueryFilters()
        .AsNoTracking()
        .Where(p => idList.Contains(p.Id))
        .ToListAsync(ct);
}
```

**WHY:** an order from last year must still show "iPhone 15" even if the product was deleted later. `IgnoreQueryFilters()` removes `DeletedAt IS NULL`.

Used by: `GetOrderByIdQuery`, `AdminGetOrderByIdQuery`. In the mapping: name missing → `"Unknown product"`.

## 4.4 Three methods, one purpose each

| Method | Tracked | Deleted | Images | Caller |
|--------|:------:|:------:|:-----:|--------|
| `GetByIdsAsync` | ✗ | hidden | ✔ | Cart, Wishlist |
| `GetByIdsTrackedAsync` | ✔ | hidden | ✗ | Checkout, Cancel (stock) |
| `GetByIdsIncludingDeletedAsync` | ✗ | **visible** | ✗ | Order display |

**Why not one method with flags?** `GetByIds(ids, tracked: true, includeDeleted: false)` hides intent and invites mistakes. Three names = the name is the documentation.

## 4.5 The other Product methods **[REFERENCE]**

```csharp
public async Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default)
    => await _dbSet.AsNoTracking()
                   .Include(p => p.Images)
                   .Where(p => p.CategoryId == categoryId)
                   .ToListAsync(ct);

public async Task<Product?> GetWithImagesAsync(Guid id, CancellationToken ct = default)
    => await _dbSet.Include(p => p.Images)
                   .Include(p => p.Category)
                   .FirstOrDefaultAsync(p => p.Id == id, ct);          // tracked: also used before update

public async Task<PagedResult<Product>> GetPagedAsunc(int pageNumber, int pageSize, CancellationToken ct = default)
{
    var query = _dbSet.AsNoTracking();

    var total = await query.CountAsync(ct);

    var items = await query
        .Include(p => p.Images)
        .Include(p => p.Category)
        .OrderByDescending(p => p.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return new PagedResult<Product>(items, pageNumber, pageSize, total);
}

public async Task<PagedResult<Product>> GetFilteredAsync(ProdcutFilter filter, CancellationToken ct = default)
{
    var query = _dbSet.AsNoTracking().AsQueryable();

    // Each filter is applied only if provided → one dynamic SQL, not many methods
    if (filter.CategoryId.HasValue) query = query.Where(p => p.CategoryId == filter.CategoryId);
    if (!string.IsNullOrWhiteSpace(filter.Search)) query = query.Where(p => p.Name.Contains(filter.Search));
    if (filter.MinPrice.HasValue)  query = query.Where(p => p.Price >= filter.MinPrice);
    if (filter.MaxPrice.HasValue)  query = query.Where(p => p.Price <= filter.MaxPrice);
    if (filter.IsActive.HasValue)  query = query.Where(p => p.IsActive == filter.IsActive);

    var total = await query.CountAsync(ct);                              // count AFTER filters

    var items = await query
        .Include(p => p.Images)
        .Include(p => p.Category)
        .OrderByDescending(p => p.CreatedAt)
        .Skip((filter.PageNumber - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ToListAsync(ct);

    return new PagedResult<Product>(items, filter.PageNumber, filter.PageSize, total);
}
```

> The filter property names above are the typical ones. Match them to your `ProdcutFilter`.

**Query building = LINQ is lazy:**

```
_dbSet.AsNoTracking()           → no SQL yet
  .Where(...)                   → no SQL yet   (just builds an expression)
  .Where(...)                   → no SQL yet
await .CountAsync()             → SQL #1:  SELECT COUNT(*) ... WHERE ...
await .Skip().Take().ToListAsync() → SQL #2:  SELECT ... WHERE ... ORDER BY ... OFFSET/FETCH
```

---

# 5. `OrderRepository`

Interface **[EXACT]**:

```csharp
public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> HasCustomerPurchasedProductAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Order>> GetPagedByCustomerAsync(Guid customerId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<Order>> GetPagedAsync(OrderStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
```

## 5.1 `GetByIdWithDetailsAsync` **[EXACT]**

```csharp
public async Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
{
    return await _dbSet
        .Include(o => o.Items)
        .Include(o => o.ShippingAddress)
        .FirstOrDefaultAsync(o => o.Id == id, ct);
}
```

| Line | Meaning |
|------|---------|
| No `AsNoTracking` | **Tracked on purpose.** Cancel / Confirm / Ship / Deliver call domain methods and save |
| `Include(Items)` | `TotalAmount` is computed from items; mapping needs them; cancel needs them for stock |
| `Include(ShippingAddress)` | The order DTO shows the address |
| `FirstOrDefaultAsync` | One row or `null` → handler returns "not found" |

**Ownership is NOT in the repo.** The repo returns any order by id. The **Handler** compares `order.CustomerId` with the token userId (user) or skips it (admin). That's why the same method serves both.

```
User handler:  order = repo.GetByIdWithDetailsAsync(id)
               order == null || order.CustomerId != userId  → 404
Admin handler: order = repo.GetByIdWithDetailsAsync(id)
               order == null → 404
```

Used by: `GetOrderById`, `CancelMyOrder`, `AdminCancelOrder`, `UpdateOrderStatus`, `AdminGetOrderById`.

## 5.2 `GetPagedByCustomerAsync` **[EXACT]**

```csharp
public async Task<PagedResult<Order>> GetPagedByCustomerAsync(
    Guid customerId, int pageNumber, int pageSize, CancellationToken ct = default)
{
    var query = _dbSet.AsNoTracking().Where(o => o.CustomerId == customerId);

    var totalCount = await query.CountAsync(ct);

    var items = await query
        .Include(o => o.Items)
        .OrderByDescending(o => o.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return new PagedResult<Order>(items, pageNumber, pageSize, totalCount);
}
```

**Step by step:**

| # | Line | What happens | WHY |
|---|------|--------------|-----|
| 1 | `AsNoTracking().Where(CustomerId == ...)` | Builds query, **no SQL yet** | Read only. Filter **in SQL** (never load all orders then filter in C#) |
| 2 | `CountAsync` | `SELECT COUNT(*) FROM Orders WHERE CustomerId=@id` | Client needs total pages. Count **before** Skip/Take |
| 3 | `Include(Items)` | JOIN OrderItems | `TotalAmount` and `ItemsCount` need items |
| 4 | `OrderByDescending(CreatedAt)` | Newest first | **Skip/Take without ORDER BY is non-deterministic** (pages may repeat/miss rows) |
| 5 | `Skip((p-1)*size).Take(size)` | `OFFSET ... FETCH NEXT ...` | Only one page leaves the DB |
| 6 | `new PagedResult<Order>(...)` | Wrap | Same contract for all paged queries |

**Page math:**

```
pageNumber = 3, pageSize = 10
Skip = (3 - 1) * 10 = 20      → rows 21..30
```

Index used: `IX_Orders_CustomerId`.

## 5.3 `GetPagedAsync` (admin) **[EXACT]**

```csharp
public async Task<PagedResult<Order>> GetPagedAsync(
    OrderStatus? status, int pageNumber, int pageSize, CancellationToken ct = default)
{
    var query = _dbSet.AsNoTracking().AsQueryable();

    if (status.HasValue)
        query = query.Where(o => o.Status == status.Value);

    var totalCount = await query.CountAsync(ct);

    var items = await query
        .Include(o => o.Items)
        .OrderByDescending(o => o.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return new PagedResult<Order>(items, pageNumber, pageSize, totalCount);
}
```

| Piece | Meaning |
|-------|---------|
| `OrderStatus? status` | `null` = all orders. Value = only that status |
| `.AsQueryable()` | Needed so `query` can be reassigned after `Where` (type stays `IQueryable<Order>`) |
| `if (status.HasValue)` | Dynamic filter: SQL has `WHERE Status = ...` only when requested |
| Enum stored as string | Config uses enum → string conversion. Index `IX_Orders_Status` |

Difference from 5.2: **no CustomerId filter** (admin sees everyone) + optional status.

## 5.4 `GetByCustomerIdAsync` and `HasCustomerPurchasedProductAsync` **[REFERENCE]**

```csharp
public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    => await _dbSet.AsNoTracking()
                   .Include(o => o.Items)
                   .Where(o => o.CustomerId == customerId)
                   .OrderByDescending(o => o.CreatedAt)
                   .ToListAsync(ct);

public async Task<bool> HasCustomerPurchasedProductAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    => await _dbSet.AsNoTracking()
                   .AnyAsync(o => o.CustomerId == customerId
                               && o.Status == OrderStatus.Delivered           // final decision: Delivered only
                               && o.Items.Any(i => i.ProductId == productId), ct);
```

`AnyAsync` → `SELECT CASE WHEN EXISTS (...) THEN 1 ELSE 0 END`. It stops at the first match and returns a bool, never loads rows. That is the cheapest way to ask "did he buy it?".

Used by: `AddReviewCommandHandler`. **Final decision:** `Delivered` only (you can't review what you haven't received). If your file still says `!= Cancelled`, change it.

---

# 6. `CartRepository`

Interface:

```csharp
public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
}
```

## `GetByCustomerIdAsync` **[EXACT]**

```csharp
public async Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
{
    return await _dbSet.Include(c => c.CartItems)
                       .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
}
```

| Piece | Meaning |
|-------|---------|
| Tracked | Cart is always changed after load (add/decrease/remove/clear) |
| `Include(CartItems)` | The aggregate must be loaded **complete** so domain rules (max 10, no duplicate product) see all items |
| `FirstOrDefault` | One cart per customer. `null` → handler creates `new Cart(userId)` and `AddAsync` |

**Why `ValueGeneratedNever` matters here (bug we hit):**

```
cart.AddCartItem(...)  →  new CartItem { Id = Guid.NewGuid() }   (key set in code)

EF default for Guid key = "generated by DB"
  → key already has a value → EF thinks "this row EXISTS" → state = Modified → UPDATE
  → UPDATE affects 0 rows → DbUpdateConcurrencyException / 500

Fix: builder.Property(x => x.Id).ValueGeneratedNever();
  → EF knows the key comes from code → state = Added → INSERT ✔
```

**بالمصري:** الـ Id بنعمله إحنا في الكود، فلازم نقول لـ EF "ده Id جاهز، متولدوش من الداتابيز". من غيرها بيعمل UPDATE بدل INSERT وبيفشل.

Also: unique index `(CartId, ProductId)` guards against duplicates even if two requests race.

---

# 7. `WishlistRepository` **[REFERENCE]**

Interface (your naming, no `Async` suffix):

```csharp
public interface IWishListRepository : IRepository<Wishlist>
{
    Task<Wishlist?> GetByCustomerId(Guid customerId, CancellationToken cancellationToken = default);
}
```

```csharp
public async Task<Wishlist?> GetByCustomerId(Guid customerId, CancellationToken ct = default)
{
    return await _dbSet.Include(w => w.Items)
                       .FirstOrDefaultAsync(w => w.CustomerId == customerId, ct);
}
```

Same reasoning as Cart: tracked + full aggregate (max 50 items, idempotent add).
The property name for items (`Items` / `WishListItems`) must match your entity.

> Naming note: all other methods end with `Async`. Rename to `GetByCustomerIdAsync` in one IDE refactor for consistency.

**`MoveToCart` uses 3 repos in one transaction:**

```
wishlist = Wishlists.GetByCustomerId(userId)         tracked
cart     = Carts.GetByCustomerIdAsync(userId)        tracked
product  = Products.GetByIdsAsync([productId])       read (stock check)
cart.AddCartItem(...)       → CartItem Added
wishlist.RemoveItem(...)    → WishListItem Deleted
uow.SaveChangesAsync()      → both in ONE transaction (atomic move)
```

---

# 8. `CategoryRepository` **[REFERENCE]**

```csharp
public interface ICategoryRepository : IRepository<Category>
{
    Task<(Category? Category, IReadOnlyList<Product> Products)> GetWithProductsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetAllIncludingDeletedAsync(CancellationToken ct = default);
}
```

```csharp
public async Task<IReadOnlyList<Category>> GetAllIncludingDeletedAsync(CancellationToken ct = default)
    => await _dbSet.IgnoreQueryFilters()          // admin sees soft-deleted categories too
                   .AsNoTracking()
                   .ToListAsync(ct);
```

`GetWithProductsAsync` returns a tuple (category + its active products) — products loaded with a second query. Category-level pagination is done **in memory** for now (fine for small data; move to SQL `Skip/Take` if categories grow).

> The exact signature of `GetWithProductsAsync` is from your file. Check it.

---

# 8B. `ReviewRepository` (new)

Interface (`IReviewRepository : IRepository<Review>`):

```csharp
Task<IReadOnlyList<Review>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);   // existing, unpaged

Task<Review?> GetByProductAndCustomerAsync(Guid productId, Guid customerId, CancellationToken cancellationToken = default);

Task<PagedResult<Review>> GetPagedByProductAsync(
    Guid productId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

Task<double> GetAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default);
```

> `GetByProductIdAsync` (unpaged) is now unused. It returns ALL reviews of a product, so it is a trap on a popular product. Remove it.

## 8B.1 `GetByProductAndCustomerAsync` **[EXACT, from this session]**

```csharp
// Tracked: used for the duplicate check, update, and delete
public async Task<Review?> GetByProductAndCustomerAsync(Guid productId, Guid customerId, CancellationToken ct = default)
{
    return await _dbSet.FirstOrDefaultAsync(r => r.ProductId == productId && r.CustomerId == customerId, ct);
}
```

| Piece | Meaning |
|-------|---------|
| Tracked | Update calls `UpdateRating/UpdateComment`; Delete calls `Remove`. Both need tracking |
| Key = `(ProductId, CustomerId)` | Matches the unique index → at most one row, fast lookup |
| `CustomerId` comes from the token (in the handler) | A user cannot address another user's review |

**One method, three callers:** duplicate check in `AddReview`, `UpdateMyReview`, `DeleteMyReview`.

## 8B.2 `GetPagedByProductAsync` **[EXACT]**

```csharp
public async Task<PagedResult<Review>> GetPagedByProductAsync(
    Guid productId, int pageNumber, int pageSize, CancellationToken ct = default)
{
    var query = _dbSet.AsNoTracking().Where(r => r.ProductId == productId);

    var totalCount = await query.CountAsync(ct);

    var items = await query
        .OrderByDescending(r => r.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return new PagedResult<Review>(items, pageNumber, pageSize, totalCount);
}
```

Same recipe as `GetPagedByCustomerAsync`: filter in SQL → count first → `OrderByDescending` (deterministic pages) → `Skip/Take` → wrap in `PagedResult`.
No `Include`: a review has no children we show. Author names come from `IUserLookupService`, not from a join.

## 8B.3 `GetAverageRatingAsync` **[EXACT]**

```csharp
public async Task<double> GetAverageRatingAsync(Guid productId, CancellationToken ct = default)
{
    // (double?) so an empty set returns null instead of throwing
    var avg = await _dbSet.AsNoTracking()
        .Where(r => r.ProductId == productId)
        .AverageAsync(r => (double?)r.Rating, ct);

    return avg.HasValue ? Math.Round(avg.Value, 1) : 0;
}
```

| Line | Meaning |
|------|---------|
| `(double?)r.Rating` | `AverageAsync` over an **empty** `int` set throws `InvalidOperationException`. A nullable projection returns `null` instead |
| SQL | `SELECT AVG(CAST(Rating AS float)) FROM Reviews WHERE ProductId = @id` |
| `Math.Round(..., 1)` | 4.3333 → 4.3 for display |
| No rows → `0` | Frontend shows "no ratings yet" |

**Why compute instead of storing `AverageRating` on Product?** Storing it means updating Product on every review add/edit/delete, which adds concurrency conflicts with `Product.RowVersion` (stock changes). A single `AVG` over an indexed column is cheap.

**Index:** unique index `(ProductId, CustomerId)` starts with `ProductId`, so it also serves all `WHERE ProductId = ...` queries here.

## 8B.4 Generic methods reused

| Generic method | Used by |
|----------------|---------|
| `AddAsync` | `AddReviewCommandHandler` |
| `GetByIdAsync` (tracked) | `AdminDeleteReviewCommandHandler` |
| `Remove` | `DeleteMyReview`, `AdminDeleteReview` |

---

# 8C. `UserLookupService` (new, used like a repository)

It is **not** a repository: users are Identity entities, not our aggregates. But it plays the same role (data access behind an interface).

```csharp
// Service/Interfaces
public interface IUserLookupService
{
    Task<IReadOnlyDictionary<Guid, string>> GetNamesByIdsAsync(
        IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
}
```

```csharp
// Infrastructure/Identity
public sealed class UserLookupService(UserManager<ApplicationUser> userManager) : IUserLookupService
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetNamesByIdsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await userManager.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.UserName! })     // swap to u.FullName if you have it
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);
    }
}
```

| Line | Meaning |
|------|---------|
| `userManager.Users` | The `IQueryable<ApplicationUser>` Identity exposes. EF translates it to SQL |
| `Distinct()` + empty guard | No pointless query when the page has no reviews |
| `Where(ids.Contains)` | `WHERE Id IN (...)`: one query for the whole page |
| `Select(new { Id, Name })` | Loads **two columns only**. Never the email, hash, or stamp |
| `ToDictionaryAsync` | The handler maps with `GetValueOrDefault(id, "Deleted user")` |

**Memory view:**

```
Page of reviews: [R1(userA), R2(userB), R3(userA)]
ids (distinct) : [userA, userB]
SQL            : SELECT Id, UserName FROM AspNetUsers WHERE Id IN (userA, userB)
Dictionary     : { userA: "Ahmed", userB: "Sara" }
Map            : R1 → "Ahmed", R2 → "Sara", R3 → "Ahmed"
```

Same pattern as order product names: **one query + Dictionary + in-memory map**, no N+1.

**Next use:** customer names in admin orders (just call the same service).

---

# 9. Who calls which method (matrix)

| Repo method | Tracked | Command / Query using it |
|-------------|:------:|--------------------------|
| `Products.GetByIdsAsync` | ✗ | AddCartItem, GetCart, AddWishlistItem, GetWishlist, MoveToCart |
| `Products.GetByIdsTrackedAsync` | ✔ | Checkout, CancelMyOrder, AdminCancelOrder (via `OrderStockRestorer`) |
| `Products.GetByIdsIncludingDeletedAsync` | ✗ | GetOrderById, AdminGetOrderById |
| `Products.GetFilteredAsync` / `GetPagedAsunc` | ✗ | Product list queries |
| `Products.GetWithImagesAsync` | ✔ | Product details / admin update |
| `Carts.GetByCustomerIdAsync` | ✔ | AddItem, DecreaseItem, RemoveItem, Clear, GetCart, Checkout, MoveToCart |
| `Wishlists.GetByCustomerId` | ✔ | AddItem, RemoveItem, Clear, MoveToCart, GetWishlist |
| `Orders.AddAsync` | — | Checkout |
| `Orders.GetByIdWithDetailsAsync` | ✔ | GetOrderById, CancelMyOrder, AdminCancelOrder, UpdateOrderStatus, AdminGetOrderById |
| `Orders.GetPagedByCustomerAsync` | ✗ | GetMyOrders |
| `Orders.GetPagedAsync` | ✗ | GetAllOrders (admin) |
| `Orders.HasCustomerPurchasedProductAsync` | ✗ | Review (next) |
| `Reviews.GetByProductAndCustomerAsync` | ✔ | AddReview (duplicate check), UpdateMyReview, DeleteMyReview |
| `Reviews.GetPagedByProductAsync` | ✗ | GetProductReviews |
| `Reviews.GetAverageRatingAsync` | ✗ | GetProductReviews |
| `Reviews.GetByIdAsync` (generic) | ✔ | AdminDeleteReview |
| `IUserLookupService.GetNamesByIdsAsync` | ✗ | GetProductReviews (later: admin orders) |
| `uow.SaveChangesAsync` | — | Every Command, once |

> Queries that only read could use `GetByIdWithDetailsAsync` (tracked) at a tiny cost. We reuse one method on purpose to avoid duplicates. If it becomes a hot path, add an `AsNoTracking` variant.

---

# 10. Common mistakes & performance notes

| # | Mistake | Result | Our rule |
|---|---------|--------|----------|
| 1 | Modify entity loaded with `AsNoTracking` | Change silently ignored | Commands use tracked methods |
| 2 | Forget `Include` | Children empty, `TotalAmount = 0` | Repo method name says `WithDetails` / `Include` |
| 3 | `Skip/Take` without `OrderBy` | Duplicate/missing rows across pages | Always `OrderByDescending(CreatedAt)` |
| 4 | Filter after `ToList()` | Loads whole table | Filter on `IQueryable` before `ToListAsync` |
| 5 | Loop `GetById` per item (N+1) | N queries | `GetByIds` with `Contains` |
| 6 | `SaveChanges` inside repo | Broken transaction | Only `uow.SaveChangesAsync` |
| 7 | Count after Skip/Take | Wrong total | Count first, on the same filtered query |
| 8 | Unbounded `pageSize` | DB overload | Validator `1..50` |
| 9 | Guid key set in code without `ValueGeneratedNever` | UPDATE instead of INSERT → 500 | Configured on Cart/CartItem/Wishlist/WishListItem |
| 10 | Soft-delete forgotten in manual queries | Deleted data leaks | Global filter + explicit `IgnoreQueryFilters` only where needed |
| 11 | `AverageAsync` on an empty set with a non-nullable column | `InvalidOperationException` → 500 | Project to `(double?)` |
| 12 | Looking up each review author in a loop | N+1 queries | `GetNamesByIdsAsync` once per page |
| 13 | Unpaged `GetByProductIdAsync` on a popular product | Loads thousands of reviews | Use `GetPagedByProductAsync`; delete the old method |

**Performance summary:**
- Indexes: `Orders(CustomerId)`, `Orders(Status)`, `CartItems(CartId, ProductId) UNIQUE`.
- Reads: `AsNoTracking` + one query per need.
- Pagination in SQL, max page size 50.
- `Include(Items)` on a paged order list is fine (page ≤ 50 orders). For huge item counts, project to a DTO with `Select` + `Count()` instead of loading items.

**ملخص بالمصري:**
اسأل نفسك 3 أسئلة قبل أي Repo method:
1. هعدّل ولا هقرا بس؟ (Tracked / AsNoTracking)
2. محتاج الأولاد؟ (Include)
3. المحذوف يظهر ولا لأ؟ (IgnoreQueryFilters)
وبعدها سمّي الـ Method باسم يقول الإجابات دي.
