# مراجعة التصميم + شرح الـ Repositories (بالمصري)

مشروع الـ E-Commerce: Clean Architecture + CQRS · .NET 9 · EF Core · SQL Server · MediatR

> الملف ده بيجمع حاجتين في مكان واحد:
> 1. مراجعة التصميم لشغل النهارده: Cancel، Admin Orders، Reviews، تأكيد الإيميل، Reset Password.
> 2. شرح كود كل الـ Repositories سطر بسطر.
>
> الدياجرامات بالإنجليزي بس. الكود بالإنجليزي.
>
> كل Code Block متعلّم:
> - **[EXACT]** = الكود اللي كتبناه في المشروع.
> - **[REFERENCE]** = تنفيذ مرجعي مطابق للـ Interface والسلوك المتفق عليه. قارنه بملفك، ولو مختلف، ملفك هو الصح.

---

## الفهرس

1. حالة الشغل والاختبارات
2. كل حاجة قاعدة في أنهي طبقة
3. طريقة التفكير: من الـ Requirement للكود
4. تسلسل البيانات (Flows)
5. فهرس كل الـ Commands والـ Queries
6. شرح كود الـ Repositories
7. مين بينده أنهي Method (Matrix)
8. الأمان، الأخطاء الشائعة، ونقاط الضعف
9. افتراضات لازم تتأكد منها

---

# 1. حالة الشغل والاختبارات

| الـ Feature | اتكتب | اتجرب |
|-------------|:----:|-------|
| Cancel Order (يوزر) | ✔ | ✔ |
| Admin Orders | ✔ | ✔ ("good") |
| Enums بالاسم في Swagger | ✔ | ✔ |
| **Reviews** | ✔ | **لسه ما بعتش النتيجة** |
| تأكيد الإيميل + Resend + Login 403 | ✔ | ✔ |
| Forgot / Reset Password | ✔ | ✔ |

---

# 2. كل حاجة قاعدة في أنهي طبقة

```
API              Controllers, Program.cs, GlobalExceptionHandler
  │
Service          Commands / Queries / Handlers / Validators / DTOs
  │              Interfaces (IReviewRepository, IUserLookupService, IEmailService, IIdentityService)
Domain           Entities + قواعد البيزنس + DomainException
Core             Constants (Roles, AuthErrors), BaseEntity, Result, PagedResult
Infrastructure   EF Core, Repositories, Identity, JWT, Email

قاعدة الاعتماد: السهم دايماً لجوه. الطبقة الجوانية متعرفش اللي برا.
```

| الحاجة الجديدة | في أنهي طبقة | ليه |
|----------------|--------------|-----|
| `AddReviewCommand` + Handler | Service | ده Use Case. ترتيب خطوات بس، من غير تفاصيل تقنية |
| قواعد الـ Review (التقييم 1 لـ 5) | Domain + Validator | الـ Domain آخر خط دفاع. الـ Validator بيرجّع 400 بدري |
| `IReviewRepository` | Service (Interface) | الـ Handler يعتمد على Interface |
| `ReviewRepository` | Infrastructure | بيستخدم EF Core |
| `IUserLookupService` | Service (Interface) | الـ Handler عايز أسماء بس مينفعش يعرف `UserManager` |
| `UserLookupService` | Infrastructure | الـ Identity كله هناك |
| `IEmailService` | Service (Interface) | الـ Handler بيقول "ابعت" مش "إزاي" |
| `ConsoleEmailService` | Infrastructure | نسخة Dev. بعدين نبدّلها بـ SMTP من غير ما نلمس أي Handler |
| `AuthErrors` | **Core/Constants** | Infrastructure بتطلّعه والـ API بيقارن بيه. الـ Core ظاهر للاتنين زي `Roles` |
| تشفير الـ Token (Base64Url) | Infrastructure | ده تفصيل Identity. الـ Handlers بتشوفه كـ string وبس |
| `JsonStringEnumConverter` | API | شكل الـ JSON قرار عرض، مش Domain |

**ليه `IUserLookupService` مش في الـ Domain؟**
اليوزر (`ApplicationUser`) في Infrastructure، والـ Domain مبيعرفوش. فبنسأل عن الأسماء من خلال Interface.

---

# 3. طريقة التفكير: من الـ Requirement للكود

الترتيب بتاعي في أي Feature، **من جوه لبرا**:

```
1  Requirement (جملة بيزنس)
2  القواعد والـ Invariants        → Domain
3  الـ Use Cases                   → Commands / Queries
4  البيانات اللي كل Use Case محتاجها → Repository methods
5  الـ DTOs (اللي بيخرج بس)
6  تسلسل الـ Handler
7  الـ Controller وربط الأخطاء (آخر حاجة)
8  Config / Migration
9  جدول اختبار
```

**ليه الـ Domain الأول؟**
لو القاعدة جوه الـ Domain، كل Handler بياخدها لوحده ومحدش ينساها.

**ليه الـ Controller آخر حاجة؟**
لأنه مجرد باب. بيستلم، بيبعت للـ Mediator، وبيحوّل النتيجة لـ HTTP Status.

## 3.1 الـ Review

**الطلب:** "العميل يقيّم منتج استلمه فعلاً، والباقي يقرا التقييمات."

**القواعد:**

| القاعدة | بتتطبق فين |
|---------|-----------|
| التقييم من 1 لـ 5 | `Review` (DomainException) + Validator |
| التعليق اختياري، أقل من 500 حرف | Validator |
| الشراء لازم يكون أوردر **Delivered** | الـ Handler، عن طريق `HasCustomerPurchasedProductAsync` |
| تقييم واحد لكل يوزر لكل منتج | الـ Handler + **Unique Index** `(ProductId, CustomerId)` |
| صاحب التقييم بس هو اللي يعدّل أو يحذف | البحث بـ `(ProductId + userId من التوكن)` |

**ليه قاعدة الشراء في الـ Handler مش في `Review`؟**
لأنها محتاجة بيانات من `Order`. الـ Entity مينفعش يسأل Aggregate تاني.

**الـ Repo methods بتطلع من احتياج الـ Handlers:**

```
AddReview محتاج:    المنتج موجود؟              → Products.GetByIdAsync
                    اشتراه وDelivered؟         → Orders.HasCustomerPurchasedProductAsync
                    قيّمه قبل كده؟             → Reviews.GetByProductAndCustomerAsync
                    احفظ                       → Reviews.AddAsync

Update / Delete:    تقييمي (Tracked)          → Reviews.GetByProductAndCustomerAsync  (نفس الـ Method)
AdminDelete:        تقييم بالـ Id              → Reviews.GetByIdAsync (Generic)

GetProductReviews:  صفحة + الأحدث الأول        → Reviews.GetPagedByProductAsync
                    المتوسط                    → Reviews.GetAverageRatingAsync
                    أسماء الكُتّاب              → IUserLookupService.GetNamesByIdsAsync
```

**الفايدة:** Method واحدة (`GetByProductAndCustomerAsync`) بتخدم 3 Handlers.

**قرار اسم الكاتب:**

| الحل | القرار |
|------|--------|
| A. `IUserLookupService` | **اخترناه.** الاسم دايماً محدّث ومفيش بيانات متكررة، وهنستخدمه تاني في الأدمن |
| B. نخزّن `AuthorName` جوه الـ Review | الاسم القديم بيفضل لو اتغيّر، ومحتاجين Lookup وقت الإنشاء برضه |
| C. الـ Domain يعرف `ApplicationUser` | **مرفوض.** بيكسر الـ Clean Architecture |

**Result ولا Exception؟**

| الحالة | الآلية | HTTP |
|--------|--------|------|
| المنتج مش موجود | `Result.Failure` | 404 |
| التقييم مش موجود أو مش بتاعي | `Result.Failure` | 404 |
| مشتراش أو الأوردر لسه ما وصلش | `DomainException("review.not_purchased")` | 400 |
| قيّمه قبل كده | `DomainException("review.already_exists")` | 400 |
| Rating برا النطاق | Validator | 400 |

القاعدة: حاجة طبيعية بس مش ممكنة ← `Result`. قاعدة بيزنس اتكسرت ← Exception بكود الـ Frontend يقدر يتعامل معاه.

## 3.2 تأكيد الإيميل: مشكلة اكتشفناها

```
الخطة الساذجة:  Identity option  SignIn.RequireConfirmedEmail = true

الحقيقة:        الإعداد ده بيشتغل مع SignInManager بس.
                إحنا بنسجّل الدخول بـ UserManager (بنبني الـ JWT بإيدينا).
                يعني الإعداد مش بيعمل أي حاجة عندنا.

القرار:         نفحص user.EmailConfirmed بإيدينا جوه LoginAsync.
```

**ترتيب الفحص جوه `LoginAsync`:**

```
1. دوّر على اليوزر                    (مش موجود ← 401 عام)
2. هو مقفول (Lockout)؟               (← 401 عام)
3. الباسورد غلط؟                     (← AccessFailed++ ← 401 عام)
4. الباسورد صح ← صفّر عدّاد الفشل
5. EmailConfirmed == false؟          (← 403)      ← بعد الباسورد
6. ابني الـ JWT
```

**ليه بعد الباسورد؟**
لو قلنا "الإيميل مش مؤكد" قبل ما نتأكد من الباسورد، أي حد يجرّب إيميلات عشوائية ويعرف مين مسجل عندنا.

**ليه الـ Register مبيرجّعش Token؟**
لو رجّع Token، اليوزر يدخل من غير ما يأكد. فبيرجّع `UserDto` بس.

## 3.3 Forgot / Reset Password

| الخطر | القرار |
|-------|--------|
| معرفة مين مسجل | `forgot-password` بيرجّع 200 دايماً بنفس الرسالة |
| حساب غير مؤكد | **مفيش إيميل Reset** (غير كده حد يسجّل بإيميل ضحيته ويلعب بيه) |
| إعادة استخدام الـ Token | `ResetPasswordAsync` بتغيّر الـ SecurityStamp، فالـ Token بيموت بعد أول استخدام |
| صاحب الحساب المقفول | بعد الـ Reset بنفك القفل ونصفّر عدّاد الفشل |
| رسائل الخطأ | Token غلط ← رسالة واحدة عامة. أخطاء سياسة الباسورد ← بتتعرض عشان اليوزر يصلّحها |

---

# 4. تسلسل البيانات (Flows)

## 4.1 AddReview (Command)

```
Client  POST /api/products/{productId}/reviews   Bearer <JWT>   body: { rating, comment }
  ▼
Middleware: Serilog → ExceptionHandler → Authentication → Authorization
  ▼
ReviewsController.Add
  │  command.ProductId = productId        (من الـ Route)
  │  mediator.Send(command)
  ▼
MediatR → ValidationBehavior   (Rating 1..5؟ Comment ≤ 500؟)   فشل ← 400
  ▼
AddReviewCommandHandler
  │  userId   = ICurrentUserService.UserId                         (من التوكن، مش الـ Body)
  │  product  = Products.GetByIdAsync(productId)                   null ← 404
  │  bought   = Orders.HasCustomerPurchasedProductAsync(...)       false ← 400 review.not_purchased
  │  existing = Reviews.GetByProductAndCustomerAsync(...)          موجود ← 400 review.already_exists
  │  review   = new Review(productId, userId, rating, comment)     ← قواعد الـ Domain
  │  Reviews.AddAsync(review)
  │  uow.SaveChangesAsync()
  ▼
201 Created { id }
```

**شبكة الأمان:** الـ Unique Index. لو طلبين وصلوا مع بعض ونجحوا في فحص الـ Handler، الداتابيز بترفض التاني. (دلوقتي ده بيطلع 500 لحد ما نحوّل `DbUpdateException` لـ 409.)

## 4.2 GetProductReviews (Query)

```
GET /api/products/{productId}/reviews?pageNumber=1&pageSize=10     (من غير توكن)
  ▼
Validator: page ≥ 1, size من 1 لـ 50
  ▼
GetProductReviewsQueryHandler
  │  Q1  Products.GetByIdsAsync([productId])             فاضي ← 404
  │  Q2  Reviews.GetPagedByProductAsync(...)             COUNT + صفحة (AsNoTracking، الأحدث الأول)
  │  Q3  Reviews.GetAverageRatingAsync(productId)        AVG في SQL
  │  Q4  IUserLookupService.GetNamesByIdsAsync(ids)      استعلام واحد: WHERE Id IN (...)
  │  Map ← ReviewDto(..., AuthorName = names.GetValueOrDefault(id, "Deleted user"))
  ▼
200 ProductReviewsDto { averageRating, reviews: PagedResult }
```

**نفس خدعة أسماء المنتجات في الأوردرات:** استعلام واحد، `Dictionary`، وبعدين Map في الذاكرة. مفيش N+1.

## 4.3 Update / Delete تقييمي

```
PUT / DELETE /api/products/{productId}/reviews     Bearer
  review = Reviews.GetByProductAndCustomerAsync(productId, userId من التوكن)   (Tracked)
  null ← 404            (تقييم حد تاني = "مش موجود")
  Update: review.UpdateRating(r) + review.UpdateComment(c) ← SaveChanges ← 204
  Delete: Reviews.Remove(review)                           ← SaveChanges ← 204
```

مفيش Id للتقييم في الـ URL، فاليوزر مستحيل يشاور على تقييم حد تاني.

## 4.4 Register ← Confirm ← Login

```
POST /register
  Handler: RegisterAsync (يعمل اليوزر + Role + Rollback لو فشل)
           CreateEmailConfirmationTokenAsync(email)      ← Token من Identity بتشفير Base64Url
           IEmailService.SendEmailConfirmationAsync(...) ← (Dev: بيكتب اللينك في الـ Log)
  ← 200 UserDto  (من غير JWT)

اللينك: {Frontend:BaseUrl}/confirm-email?userId=...&token=...
صفحة الـ Frontend ← POST /confirm-email { userId, token }
  ConfirmEmailAsync:
     اليوزر مش موجود            ← Failure("Invalid or expired confirmation link.")
     مؤكد قبل كده               ← Success   (Idempotent)
     فك التشفير فشل             ← Failure بنفس الرسالة
     UserManager.ConfirmEmailAsync ← الـ SecurityStamp بيتغير ← الـ Token بيموت
  ← 204 / 400

POST /login
  الباسورد صح + EmailConfirmed == false ← 403 { error }
  الباسورد صح + مؤكد                    ← 200 JWT
```

## 4.5 Forgot ← Reset

```
POST /forgot-password { email }
  CreatePasswordResetTokenAsync(email)  ← null لو اليوزر مش موجود أو مش مؤكد
  مش null ← SendPasswordResetAsync(...)
  ← 200 بنفس الرسالة دايماً

POST /reset-password { userId, token, newPassword }
  Validator: الباسورد من 8 لـ 20
  ResetPasswordAsync:
     فك التشفير فشل / InvalidToken ← Failure عام
     أخطاء السياسة                  ← بتتعرض لليوزر
     نجح ← فك القفل + تصفير عدّاد الفشل
  ← 204 / 400
```

## 4.6 دورة حياة الـ Token

```
GenerateXToken(user) ← مربوط بـ: userId + SecurityStamp + الغرض + المدة (24 ساعة)
      ▼ Base64Url encode   (يبقى آمن في الـ URL)
      ▼ يتبعت في اللينك
      ▼ Base64Url decode
      ▼ ConfirmEmailAsync / ResetPasswordAsync
نجح ← الـ SecurityStamp يتغير ← أي Token أقدم (تأكيد أو Reset) يبوظ
```

## 4.7 ربط الأخطاء بالـ HTTP

| الحالة | الآلية | HTTP |
|--------|--------|------|
| Login غير مؤكد (الباسورد صح) | `Result.Failure(AuthErrors.EmailNotConfirmed)` + مقارنة في الـ Controller | **403** |
| Token تأكيد أو Reset غلط أو منتهي | `Result.Failure` | 400 |
| باسورد ضعيف | `Result.Failure` (رسائل Identity) | 400 |
| كسر قاعدة Review | `DomainException` | 400 + كود |
| Review أو Product مش موجود | `Result.Failure` | 404 |
| Validation | `ValidationException` | 400 |
| `?status=abc` | Model Binding | 400 |

---

# 5. فهرس كل الـ Commands والـ Queries

| المجال | Commands | Queries |
|--------|----------|---------|
| **Auth** | Register, Login, ConfirmEmail, ResendConfirmationEmail, ForgotPassword, ResetPassword | — |
| **Cart** | AddItem, DecreaseItem, RemoveItem, Clear | GetCart |
| **Wishlist** | AddItem, RemoveItem, Clear, MoveToCart | GetWishlist |
| **Orders (يوزر)** | Checkout, CancelMyOrder | GetMyOrders, GetOrderById |
| **Orders (أدمن)** | UpdateOrderStatus (Confirm/Ship/Deliver), AdminCancelOrder | GetAllOrders, AdminGetOrderById |
| **Reviews** | AddReview, UpdateMyReview, DeleteMyReview, AdminDeleteReview | GetProductReviews |
| **Products** | Create, Update, Delete (Soft)، تغيير الستوك والحالة (أدمن) | قائمة عامة بفلتر، بالـ Id، قائمة الأدمن (بالمحذوف) |
| **Categories** | Create, Update, Delete (Soft) (أدمن) | قائمة عامة، مع المنتجات، قائمة الأدمن (بالمحذوف) |

## 5.1 الـ Auth

| الاسم | Input | الشغل | الخرج |
|-------|-------|-------|-------|
| `RegisterCommand` | Email, Password, FullName | يسجّل + يبعت إيميل التأكيد | `UserDto` |
| `LoginCommand` | Email, Password | يفحص الباسورد ثم `EmailConfirmed` | JWT / 401 / 403 |
| `ConfirmEmailCommand` | UserId, Token | §4.4 | 204 / 400 |
| `ResendConfirmationEmailCommand` | Email | يبعت لغير المؤكد، **ويرجّع Success دايماً** | 200 عام |
| `ForgotPasswordCommand` | Email | يبعت للمؤكد بس، **ويرجّع Success دايماً** | 200 عام |
| `ResetPasswordCommand` | UserId, Token, NewPassword | §4.5 | 204 / 400 |

## 5.2 الـ Cart (`api/cart`)

| الاسم | الشغل |
|-------|-------|
| `AddItemCommand` | يجيب/يعمل Cart ← `cart.AddCartItem(productId, qty, stock)` (يفحص الستوك ثم أقصى 10) |
| `DecreaseItemCommand` | `cart.DecreaseItemQuantity` |
| `RemoveItemCommand` | `cart.RemoveItem` |
| `ClearCartCommand` | `cart.Clear()` |
| `GetCartQuery` | الكارت + المنتجات (المحذوف بيختفي) ← `CartDto` |

## 5.3 الـ Wishlist (`api/wishlist`)

| الاسم | الشغل |
|-------|-------|
| `AddItemCommand` | `wishlist.AddItem` (Idempotent، أقصى 50) |
| `RemoveItemCommand` / `ClearWishlistCommand` | `RemoveItem` / `Clear` |
| `MoveToCartCommand` | Wishlist + Cart + Product ← `cart.AddCartItem` ← `wishlist.RemoveItem` ← **SaveChanges واحد** (نقل Atomic) |
| `GetWishlistQuery` | `WishlistDto` |

## 5.4 الـ Orders

| الاسم | النوع | Input | الشغل |
|-------|-------|-------|-------|
| `CheckoutCommand` | Command | عنوان الشحن | Cart ← خصم الستوك ← `new Order` ← `cart.Clear()` ← SaveChanges واحد |
| `CancelMyOrderCommand` | Command | OrderId | ملكية ← `order.Cancel()` ← رجوع الستوك ← SaveChanges |
| `GetMyOrdersQuery` | Query | PageNumber, PageSize | `GetPagedByCustomerAsync` |
| `GetOrderByIdQuery` | Query | OrderId | أوردر + أسماء المنتجات (حتى المحذوفة) |
| `GetAllOrdersQuery` | Query (أدمن) | Status?, Page | `GetPagedAsync` |
| `AdminGetOrderByIdQuery` | Query (أدمن) | OrderId | زي بتاع اليوزر بس **من غير فحص الملكية** |
| `UpdateOrderStatusCommand` | Command (أدمن) | OrderId, Action | `switch` على `Confirm/Ship/Deliver` |
| `AdminCancelOrderCommand` | Command (أدمن) | OrderId | `Cancel()` ← رجوع الستوك |

دورة حياة الأوردر (القواعد في الـ Domain مش في الـ Handlers):

```
Pending ──Confirm──► Confirmed ──Ship──► Shipped ──Deliver──► Delivered
   │                     │
   └─────── Cancel ──────┘      (من Pending أو Confirmed بس، والستوك بيرجع)
```

**ليه `UpdateOrderStatus` واحد والإلغاء Command منفصل؟**
التلات حالات ليهم نفس الخطوات. الإلغاء مختلف لأنه بيرجّع الستوك، واليوزر والأدمن بيختلفوا في فحص الملكية.

## 5.5 الـ Reviews

| الاسم | النوع | Input | الخرج | الأخطاء |
|-------|-------|-------|-------|---------|
| `AddReviewCommand` | Command | ProductId(route), Rating, Comment? | `Guid` ← 201 | 404 / 400 |
| `UpdateMyReviewCommand` | Command | ProductId(route), Rating, Comment? | 204 | 404 |
| `DeleteMyReviewCommand` | Command | ProductId | 204 | 404 |
| `AdminDeleteReviewCommand` | Command | ReviewId | 204 | 404 |
| `GetProductReviewsQuery` | Query | ProductId(route), PageNumber, PageSize | `ProductReviewsDto` | 404 |

---

# 6. شرح كود الـ Repositories

## 6.1 مفاهيم لازم تتفهم الأول

### الـ DbContext والـ DbSet

```
AppDbContext  (واحد لكل HTTP request، Scoped)
   ├── ChangeTracker   ← بيفتكر كل Entity حمّلها (Tracked)
   ├── DbSet<Product>  ← زي الجدول
   ├── DbSet<Order>
   └── ...

الـ Repository = كلاس صغير شايل _dbSet وبيدّي كل Query اسم واضح.
```

الـ `DbSet<Order>` هو جدول الأوردرات كأنه List في C#. والـ Repository بيخلّي الـ Handler ميعرفش EF.

### Tracked ولا AsNoTracking

```
TRACKED (الافتراضي)                    AsNoTracking
────────────────────                   ─────────────
حمّل Product                           حمّل Product
EF بيحتفظ بنسخة                         مفيش حفظ
product.DecreaseStock(2)               product.DecreaseStock(2)  ← EF مش عارف
SaveChanges ← UPDATE بيتولد             SaveChanges ← مفيش حاجة (Bug صامت!)
```

| | Tracked | AsNoTracking |
|---|---------|--------------|
| متى | هتعدّل وتحفظ | قراءة بس |
| التكلفة | أكتر | أقل (أسرع) |
| الخطر | مفيش | لو عدّلت حاجة جت AsNoTracking، التعديل **بيضيع من غير أي Error** |

**قاعدتنا:** الـ Command بياخد Tracked. الـ Query بياخد AsNoTracking.

### الـ Global Query Filter (Soft Delete)

```
HasQueryFilter(p => p.DeletedAt == null)

Query عادي:                SELECT ... WHERE DeletedAt IS NULL
.IgnoreQueryFilters():     SELECT ...          (المحذوف كمان)
```

مفيش حد ينسى يفلتر المحذوف. هو مخفي تلقائي، وبنظهره بس لما نطلب.

### `Include`

من غير `Include`، الـ `order.Items` بتيجي فاضية (مش Error، بس بيانات ناقصة). معاه، الـ Items بتتحمل في نفس الـ SQL.

### `Contains` بيتحول لـ `IN`

```csharp
var idList = ids.Distinct().ToList();
_dbSet.Where(p => idList.Contains(p.Id))
// SELECT ... FROM Products WHERE Id IN (@p0, @p1, @p2)
```

استعلام واحد لكل المنتجات بدل ما نسأل الداتابيز مرة لكل منتج (مشكلة N+1).

## 6.2 `IRepository<T>` و `GenericRepository<T>`

**الـ Interface (في Core)** **[EXACT]**:

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

**التنفيذ** **[REFERENCE]**:

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
        => await _dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);   // Tracked

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    public virtual void Update(T entity) => _dbSet.Update(entity);

    public virtual void Remove(T entity) => _dbSet.Remove(entity);
}
```

| السطر | معناه |
|-------|-------|
| `where T : BaseEntity` | بس الـ Entities اللي ليها `Id`. عشان كده `BaseEntity` في Core |
| `protected` fields | الـ Repos اللي بتورّث بتستخدم `_dbSet` و `_context` |
| `GetByIdAsync` Tracked | أغلب الاستخدام "حمّل وعدّل" |
| `AddAsync` | بيعلّم الـ Entity إنها `Added` بس. **لسه مفيش حاجة راحت للداتابيز** |
| `Update` / `Remove` | نفس الفكرة: بيعلّم الحالة بس |
| مفيش `SaveChanges` | الحفظ شغل الـ Unit of Work لوحده |

## 6.3 `UnitOfWork` **[REFERENCE]**

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    private IProdcutRepository?   _products;
    private IOrderRepository?     _orders;
    private ICartRepository?      _carts;
    private IWishListRepository?  _wishlists;
    private IReviewRepository?    _reviews;

    public UnitOfWork(AppDbContext context) => _context = context;

    public IProdcutRepository  Products  => _products  ??= new ProductRepository(_context);
    public IOrderRepository    Orders    => _orders    ??= new OrderRepository(_context);
    public ICartRepository     Carts     => _carts     ??= new CartRepository(_context);
    public IWishListRepository Wishlists => _wishlists ??= new WishlistRepository(_context);
    public IReviewRepository   Reviews   => _reviews   ??= new ReviewRepository(_context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
```

```
Handler
  │ uow.Products ──┐
  │ uow.Orders   ──┼──► كلهم على نفس AppDbContext
  │ uow.Carts    ──┘
  ▼
uow.SaveChangesAsync()  ──► Transaction واحدة لكل اللي اتغيّر
```

- `??=` (Lazy): الـ Repo بيتعمل لما نحتاجه بس.
- `_context` واحد مشترك: ده شرط الـ Transaction الواحدة.
- **مثال Checkout:** خصم الستوك + أوردر جديد + تفريغ الكارت، كله في `SaveChanges` واحد. لو حاجة فشلت، **مفيش حاجة بتتحفظ**.

## 6.4 `ProductRepository`

الـ Interface **[EXACT]**:

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

### `GetByIdsAsync`: للعرض **[EXACT]**

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

| السطر | معناه |
|-------|-------|
| `ids.Distinct().ToList()` | شيل المكرر وحوّلها List (EF مش بيترجم `IEnumerable` Lazy بشكل موثوق) |
| `AsNoTracking()` | قراءة بس ← أسرع |
| `Include(Images)` | الكارت والـ Wishlist محتاجين الصورة الرئيسية |
| `Contains` | `WHERE Id IN (...)` استعلام واحد |
| من غير `IgnoreQueryFilters` | المنتج المحذوف **بيختفي** من الكارت (قرارك) |

بينادي عليها: `GetCartQuery`، `GetWishlistQuery`، `MoveToCart`، `GetProductReviews` (فحص وجود المنتج).

### `GetByIdsTrackedAsync`: لتعديل الستوك **[EXACT]**

```csharp
public async Task<IReadOnlyList<Product>> GetByIdsTrackedAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
{
    var idList = ids.Distinct().ToList();

    return await _dbSet
        .Where(p => idList.Contains(p.Id))
        .ToListAsync(ct);
}
```

| الفرق عن اللي فوق | ليه |
|-------------------|-----|
| من غير `AsNoTracking` | هننده `DecreaseStock` / `IncreaseStock` ومحتاجين `UPDATE` |
| من غير `Include(Images)` | مش محتاجينها، فالاستعلام أخف |
| المحذوف لسه مخفي | مينفعش تشتري منتج محذوف |

**شكل الذاكرة في Checkout:**

```
ChangeTracker
  ├─ Product A (Stock 10) ◄── راجع من الـ Repo، Tracked
  │      DecreaseStock(2) → Stock 8 → الحالة Modified
  ├─ Order (جديد)           → Added
  └─ Cart                   → CartItems اتشالت → Deleted
SaveChanges → UPDATE Products ... WHERE Id=@id AND RowVersion=@old   (فحص التعارض)
              INSERT Orders / OrderItems
              DELETE CartItems
```

الـ `RowVersion` بيخلّي SQL يضيف `AND RowVersion = @old`. لو مشتري تاني غيّر نفس المنتج الأول، مفيش صفوف اتعدّلت ← `DbUpdateConcurrencyException` ← **409**.

### `GetByIdsIncludingDeletedAsync`: للأوردرات القديمة **[EXACT]**

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

أوردر من السنة اللي فاتت لازم لسه يعرض "iPhone 15" حتى لو المنتج اتحذف بعدها. الـ `IgnoreQueryFilters()` بيشيل `DeletedAt IS NULL`.

### ملخص التلات Methods

| Method | Tracked | المحذوف | الصور | بينده |
|--------|:------:|:------:|:----:|-------|
| `GetByIdsAsync` | ✗ | مخفي | ✔ | Cart، Wishlist |
| `GetByIdsTrackedAsync` | ✔ | مخفي | ✗ | Checkout، Cancel (الستوك) |
| `GetByIdsIncludingDeletedAsync` | ✗ | **ظاهر** | ✗ | عرض الأوردرات |

**ليه 3 Methods مش واحدة بـ Flags؟**
`GetByIds(ids, tracked: true, includeDeleted: false)` بتخبّي النية وبتسهّل الغلط. الاسم نفسه هو التوثيق.

### باقي دوال الـ Product **[REFERENCE]**

```csharp
public async Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default)
    => await _dbSet.AsNoTracking()
                   .Include(p => p.Images)
                   .Where(p => p.CategoryId == categoryId)
                   .ToListAsync(ct);

public async Task<Product?> GetWithImagesAsync(Guid id, CancellationToken ct = default)
    => await _dbSet.Include(p => p.Images)
                   .Include(p => p.Category)
                   .FirstOrDefaultAsync(p => p.Id == id, ct);          // Tracked: بيتستخدم قبل التعديل كمان

public async Task<PagedResult<Product>> GetFilteredAsync(ProdcutFilter filter, CancellationToken ct = default)
{
    var query = _dbSet.AsNoTracking().AsQueryable();

    // كل فلتر بيتطبق لو اتبعت بس ← SQL ديناميكي واحد
    if (filter.CategoryId.HasValue) query = query.Where(p => p.CategoryId == filter.CategoryId);
    if (!string.IsNullOrWhiteSpace(filter.Search)) query = query.Where(p => p.Name.Contains(filter.Search));
    if (filter.MinPrice.HasValue) query = query.Where(p => p.Price >= filter.MinPrice);
    if (filter.MaxPrice.HasValue) query = query.Where(p => p.Price <= filter.MaxPrice);

    var total = await query.CountAsync(ct);                              // العدّ بعد الفلاتر

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

> أسماء خصائص الـ Filter تقريبية. طابقها مع `ProdcutFilter` بتاعك. وبنفس الطريقة `GetPagedAsunc` بدون الفلاتر.

**الـ LINQ بيتنفذ متأخر (Lazy):**

```
_dbSet.AsNoTracking()              ← مفيش SQL لسه
  .Where(...)                      ← مفيش SQL (بيبني تعبير بس)
await .CountAsync()                ← SQL #1: SELECT COUNT(*) ... WHERE ...
await .Skip().Take().ToListAsync() ← SQL #2: SELECT ... ORDER BY ... OFFSET/FETCH
```

## 6.5 `OrderRepository`

الـ Interface **[EXACT]**:

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

### `GetByIdWithDetailsAsync` **[EXACT]**

```csharp
public async Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
{
    return await _dbSet
        .Include(o => o.Items)
        .Include(o => o.ShippingAddress)
        .FirstOrDefaultAsync(o => o.Id == id, ct);
}
```

| السطر | معناه |
|-------|-------|
| من غير `AsNoTracking` | **Tracked بقصد.** Cancel / Confirm / Ship / Deliver بينادوا Domain methods ويحفظوا |
| `Include(Items)` | `TotalAmount` محسوبة من الـ Items، والـ Mapping محتاجها، والـ Cancel محتاجها للستوك |
| `Include(ShippingAddress)` | الـ DTO بيعرض العنوان |
| `FirstOrDefaultAsync` | صف واحد أو `null` ← الـ Handler يرجّع "مش موجود" |

**فحص الملكية مش في الـ Repo.** الـ Repo بيرجّع أي أوردر بالـ Id. الـ **Handler** بيقارن `order.CustomerId` بـ userId من التوكن (يوزر)، أو بيتخطى الفحص (أدمن). عشان كده نفس الـ Method بتخدم الاتنين.

```
Handler اليوزر:  order == null || order.CustomerId != userId  → 404
Handler الأدمن:  order == null → 404
```

بينده عليها: `GetOrderById`، `CancelMyOrder`، `AdminCancelOrder`، `UpdateOrderStatus`، `AdminGetOrderById`.

### `GetPagedByCustomerAsync` **[EXACT]**

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

| # | السطر | بيحصل إيه | ليه |
|---|-------|-----------|-----|
| 1 | `AsNoTracking().Where(CustomerId == ...)` | بيبني الـ Query، **لسه مفيش SQL** | قراءة بس. والفلتر **في SQL** (مش بنحمّل كل الأوردرات ونفلتر في C#) |
| 2 | `CountAsync` | `SELECT COUNT(*) ... WHERE CustomerId=@id` | الـ Frontend محتاج عدد الصفحات. بنعدّ **قبل** Skip/Take |
| 3 | `Include(Items)` | JOIN على OrderItems | `TotalAmount` و `ItemsCount` محتاجين الـ Items |
| 4 | `OrderByDescending(CreatedAt)` | الأحدث الأول | **Skip/Take من غير ORDER BY ترتيبها غير مضمون** (صفحات ممكن تتكرر أو تفوّت صفوف) |
| 5 | `Skip((p-1)*size).Take(size)` | `OFFSET ... FETCH NEXT ...` | صفحة واحدة بس هي اللي بتخرج من الداتابيز |
| 6 | `new PagedResult<Order>(...)` | تغليف | نفس الشكل لكل الـ Queries المقسّمة |

```
pageNumber = 3, pageSize = 10
Skip = (3 - 1) * 10 = 20     ← الصفوف من 21 لـ 30
```

الـ Index المستخدم: `IX_Orders_CustomerId`.

### `GetPagedAsync` (أدمن) **[EXACT]**

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

- `OrderStatus? status`: `null` يعني كل الأوردرات. قيمة يعني الحالة دي بس.
- `.AsQueryable()`: عشان نقدر نعيد تعيين `query` بعد `Where`.
- `if (status.HasValue)`: الفلتر ديناميكي، الـ `WHERE Status = ...` بيتحط لما يتطلب بس. والـ Index: `IX_Orders_Status`.
- الفرق عن اللي قبله: **مفيش فلتر `CustomerId`** (الأدمن بيشوف الكل).

### `GetByCustomerIdAsync` و `HasCustomerPurchasedProductAsync` **[REFERENCE]**

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
                               && o.Status == OrderStatus.Delivered           // القرار النهائي: Delivered بس
                               && o.Items.Any(i => i.ProductId == productId), ct);
```

- `AnyAsync` بيتحول لـ `SELECT CASE WHEN EXISTS (...)`. بيقف عند أول تطابق ومبيحمّلش صفوف. ده أرخص طريقة تسأل "اشتراه؟".
- **لو ملفك لسه `!= Cancelled`، غيّره لـ `Delivered`.** وإلا حد يقيّم منتج لسه ما وصلوش.

## 6.6 `CartRepository` **[EXACT]**

```csharp
public async Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
{
    return await _dbSet.Include(c => c.CartItems)
                       .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
}
```

- **Tracked:** الكارت دايماً بيتعدّل بعد ما يتحمّل.
- **`Include(CartItems)`:** لازم الـ Aggregate يتحمّل كامل، عشان قواعد الـ Domain (أقصى 10، منتج مش مكرر) تشوف كل العناصر.
- **`FirstOrDefault`:** كارت واحد لكل عميل. `null` ← الـ Handler بيعمل `new Cart(userId)` و `AddAsync`.

**ليه `ValueGeneratedNever` مهم هنا (Bug قابلناه):**

```
cart.AddCartItem(...)  →  new CartItem { Id = Guid.NewGuid() }   (الـ Id اتعمل في الكود)

الافتراضي في EF لمفتاح Guid = "الداتابيز بتولّده"
  → المفتاح فيه قيمة بالفعل → EF يفتكر "الصف ده موجود" → Modified → UPDATE
  → UPDATE بيأثر على 0 صفوف → DbUpdateConcurrencyException / 500

الحل: builder.Property(x => x.Id).ValueGeneratedNever();
  → EF يعرف إن المفتاح جاي من الكود → Added → INSERT ✔
```

والـ Unique Index `(CartId, ProductId)` بيمنع التكرار حتى لو طلبين اتسابقوا.

## 6.7 `WishlistRepository` **[REFERENCE]**

```csharp
public async Task<Wishlist?> GetByCustomerId(Guid customerId, CancellationToken ct = default)
{
    return await _dbSet.Include(w => w.Items)
                       .FirstOrDefaultAsync(w => w.CustomerId == customerId, ct);
}
```

نفس منطق الكارت: Tracked + Aggregate كامل (أقصى 50، الإضافة Idempotent). اسم خاصية العناصر (`Items` أو `WishListItems`) لازم يطابق الـ Entity عندك.

> كل الـ Methods التانية آخرها `Async` إلا دي. غيّرها لـ `GetByCustomerIdAsync` بـ Refactor واحد في الـ IDE.

**`MoveToCart` بيستخدم 3 Repos في Transaction واحدة:**

```
wishlist = Wishlists.GetByCustomerId(userId)         Tracked
cart     = Carts.GetByCustomerIdAsync(userId)        Tracked
product  = Products.GetByIdsAsync([productId])       قراءة (فحص الستوك)
cart.AddCartItem(...)       → CartItem Added
wishlist.RemoveItem(...)    → WishListItem Deleted
uow.SaveChangesAsync()      → الاتنين في Transaction واحدة (نقل Atomic)
```

## 6.8 `CategoryRepository` **[REFERENCE]**

```csharp
public async Task<IReadOnlyList<Category>> GetAllIncludingDeletedAsync(CancellationToken ct = default)
    => await _dbSet.IgnoreQueryFilters()          // الأدمن بيشوف المحذوف كمان
                   .AsNoTracking()
                   .ToListAsync(ct);
```

`GetWithProductsAsync` بترجّع Tuple (الكاتيجوري + منتجاتها النشطة) بـ Query تاني للمنتجات. والـ Pagination للكاتيجوري **في الذاكرة** حالياً (مناسب للداتا الصغيرة). لو الكاتيجوريز كترت، انقلها لـ `Skip/Take` في SQL. وتأكد من الـ Signature الحقيقي في ملفك.

## 6.9 `ReviewRepository` (جديد)

```csharp
Task<IReadOnlyList<Review>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);   // قديمة، من غير Paging

Task<Review?> GetByProductAndCustomerAsync(Guid productId, Guid customerId, CancellationToken cancellationToken = default);

Task<PagedResult<Review>> GetPagedByProductAsync(
    Guid productId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

Task<double> GetAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default);
```

> `GetByProductIdAsync` (من غير Paging) مبقتش مستخدمة. بترجّع **كل** تقييمات المنتج، فهي فخ على منتج مشهور. امسحها.

### `GetByProductAndCustomerAsync` **[EXACT]**

```csharp
// Tracked: بتتستخدم لفحص التكرار، والتعديل، والحذف
public async Task<Review?> GetByProductAndCustomerAsync(Guid productId, Guid customerId, CancellationToken ct = default)
{
    return await _dbSet.FirstOrDefaultAsync(r => r.ProductId == productId && r.CustomerId == customerId, ct);
}
```

- **Tracked:** التعديل بينده `UpdateRating/UpdateComment`، والحذف بينده `Remove`. الاتنين محتاجين Tracking.
- **المفتاح `(ProductId, CustomerId)`:** مطابق للـ Unique Index، فصف واحد على الأكتر وبحث سريع.
- **`CustomerId` من التوكن:** اليوزر مستحيل يشاور على تقييم حد تاني.
- **Method واحدة بـ 3 مستخدمين:** فحص التكرار في `AddReview`، و `UpdateMyReview`، و `DeleteMyReview`.

### `GetPagedByProductAsync` **[EXACT]**

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

نفس وصفة `GetPagedByCustomerAsync`: فلتر في SQL ← عدّ الأول ← `OrderByDescending` ← `Skip/Take` ← `PagedResult`.
مفيش `Include` لأن التقييم ملوش أولاد بنعرضهم. أسماء الكُتّاب جاية من `IUserLookupService` مش من Join.

### `GetAverageRatingAsync` **[EXACT]**

```csharp
public async Task<double> GetAverageRatingAsync(Guid productId, CancellationToken ct = default)
{
    // (double?) عشان المجموعة الفاضية ترجّع null بدل ما ترمي Exception
    var avg = await _dbSet.AsNoTracking()
        .Where(r => r.ProductId == productId)
        .AverageAsync(r => (double?)r.Rating, ct);

    return avg.HasValue ? Math.Round(avg.Value, 1) : 0;
}
```

| السطر | معناه |
|-------|-------|
| `(double?)r.Rating` | `AverageAsync` على مجموعة **فاضية** من `int` بترمي `InvalidOperationException`. الـ Nullable بيرجّع `null` |
| SQL | `SELECT AVG(CAST(Rating AS float)) FROM Reviews WHERE ProductId = @id` |
| `Math.Round(..., 1)` | 4.3333 ← 4.3 للعرض |
| مفيش صفوف ← `0` | الـ Frontend يعرض "لسه مفيش تقييمات" |

**ليه نحسبه ومنخزّنوش في Product؟**
التخزين معناه نعدّل Product مع كل إضافة/تعديل/حذف تقييم، وده بيزوّد تعارض الـ Concurrency مع `Product.RowVersion` (الستوك). الـ `AVG` على عمود عليه Index رخيص.

الـ Unique Index `(ProductId, CustomerId)` بيبدأ بـ `ProductId`، فبيخدم كل الـ Queries اللي `WHERE ProductId = ...` هنا.

### الـ Generic Methods المستخدمة

| الـ Method | بينده |
|------------|-------|
| `AddAsync` | `AddReviewCommandHandler` |
| `GetByIdAsync` (Tracked) | `AdminDeleteReviewCommandHandler` |
| `Remove` | `DeleteMyReview`، `AdminDeleteReview` |

## 6.10 `UserLookupService` (جديد، بيتستخدم زي Repository)

مش Repository لأن اليوزرز كيانات Identity مش Aggregates بتاعتنا، بس دوره نفس الدور: وصول للبيانات ورا Interface.

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
            .Select(u => new { u.Id, Name = u.UserName! })     // بدّلها بـ u.FullName لو عندك
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);
    }
}
```

| السطر | معناه |
|-------|-------|
| `userManager.Users` | الـ `IQueryable<ApplicationUser>` اللي Identity بيكشفه. EF بيترجمه لـ SQL |
| `Distinct()` + فحص الفاضي | مفيش استعلام ملوش لازمة لو الصفحة من غير تقييمات |
| `Where(ids.Contains)` | `WHERE Id IN (...)` استعلام واحد للصفحة كلها |
| `Select(new { Id, Name })` | بيحمّل **عمودين بس**. مش الإيميل ولا الـ Hash |
| `ToDictionaryAsync` | الـ Handler بيعمل `GetValueOrDefault(id, "Deleted user")` |

```
صفحة تقييمات: [R1(userA), R2(userB), R3(userA)]
ids (من غير تكرار): [userA, userB]
SQL        : SELECT Id, UserName FROM AspNetUsers WHERE Id IN (userA, userB)
Dictionary : { userA: "Ahmed", userB: "Sara" }
Map        : R1 ← "Ahmed", R2 ← "Sara", R3 ← "Ahmed"
```

نفس نمط أسماء المنتجات في الأوردرات: **استعلام واحد + Dictionary + Map في الذاكرة**. وهنستخدمه بعدين لعرض أسماء العملاء في أوردرات الأدمن.

---

# 7. مين بينده أنهي Method (Matrix)

| الـ Repo method | Tracked | الـ Command / Query |
|-----------------|:------:|---------------------|
| `Products.GetByIdsAsync` | ✗ | AddCartItem، GetCart، AddWishlistItem، GetWishlist، MoveToCart، GetProductReviews |
| `Products.GetByIdsTrackedAsync` | ✔ | Checkout، CancelMyOrder، AdminCancelOrder (عن طريق `OrderStockRestorer`) |
| `Products.GetByIdsIncludingDeletedAsync` | ✗ | GetOrderById، AdminGetOrderById |
| `Products.GetFilteredAsync` / `GetPagedAsunc` | ✗ | قوائم المنتجات |
| `Products.GetWithImagesAsync` | ✔ | تفاصيل المنتج / تعديل الأدمن |
| `Products.GetByIdAsync` (Generic) | ✔ | AddReview (فحص وجود المنتج) |
| `Carts.GetByCustomerIdAsync` | ✔ | AddItem، DecreaseItem، RemoveItem، Clear، GetCart، Checkout، MoveToCart |
| `Wishlists.GetByCustomerId` | ✔ | AddItem، RemoveItem، Clear، MoveToCart، GetWishlist |
| `Orders.AddAsync` | — | Checkout |
| `Orders.GetByIdWithDetailsAsync` | ✔ | GetOrderById، CancelMyOrder، AdminCancelOrder، UpdateOrderStatus، AdminGetOrderById |
| `Orders.GetPagedByCustomerAsync` | ✗ | GetMyOrders |
| `Orders.GetPagedAsync` | ✗ | GetAllOrders (أدمن) |
| `Orders.HasCustomerPurchasedProductAsync` | ✗ | AddReview |
| `Reviews.GetByProductAndCustomerAsync` | ✔ | AddReview (فحص التكرار)، UpdateMyReview، DeleteMyReview |
| `Reviews.GetPagedByProductAsync` | ✗ | GetProductReviews |
| `Reviews.GetAverageRatingAsync` | ✗ | GetProductReviews |
| `Reviews.GetByIdAsync` (Generic) | ✔ | AdminDeleteReview |
| `IUserLookupService.GetNamesByIdsAsync` | ✗ | GetProductReviews (وبعدين: أوردرات الأدمن) |
| `uow.SaveChangesAsync` | — | كل Command، مرة واحدة |

---

# 8. الأمان، الأخطاء الشائعة، ونقاط الضعف

## 8.1 قرارات التصميم

| القرار | الفايدة |
|--------|---------|
| Command واحد لكل Use Case | كلاسات صغيرة تتختبر بسهولة. الـ Reviews ما لمسش أي Handler قديم (غير Register و Login للإيميل) |
| `[JsonIgnore] ProductId` على الـ Commands، ويتحط من الـ Route | الـ Id مينفعش يتزوّر في الـ Body |
| عمليات اليوزر بتحدد المورد بـ `(ProductId + userId من التوكن)` | مفيش طريقة تلمس تقييم حد تاني |
| `IUserLookupService` بيرجّع `Dictionary<Guid,string>` | استعلام واحد للصفحة، وبيتعاد استخدامه |
| `IEmailService` كـ Abstraction | SMTP بعدين = تبديل كلاس واحد |
| تشفير الـ Token جوه طبقة الـ Identity | الـ Handlers والـ Controllers مبيشوفوش الـ Token الخام |
| أسماء بس (مش إيميلات) في قائمة التقييمات العامة | الخصوصية افتراضياً |
| `AuthErrors` في Core | مش نص سحري متكرر في مكانين |

## 8.2 الأمان: إيه بيدافع عن إيه

| التهديد | الدفاع |
|---------|--------|
| تقييم باسم حد تاني | `CustomerId` من الـ JWT بس |
| تعديل تقييم حد تاني | البحث بـ `(ProductId, userId)` ← 404 |
| تقييم من غير شراء | فحص الشراء على أوردرات **Delivered** |
| تقييمات مكررة / سباق | فحص الـ Handler + Unique Index |
| معرفة مين مسجل (Resend / Forgot) | دايماً 200 بنفس الرسالة |
| معرفة مين مسجل (Login) | حالة "مش مؤكد" بتظهر بعد الباسورد الصح بس |
| إعادة استخدام اللينكات | تغيير الـ SecurityStamp بيقتل الـ Token |
| Token بايظ ← 500 | `FormatException` متمسكة ← 400 |
| صاحب الحساب المقفول بعد Reset | فك القفل عند نجاح الـ Reset |
| سبام الإيميلات | **لسه مش محمي** (نقطة ضعف رقم 2) |

## 8.3 الأخطاء الشائعة في الـ Repositories

| # | الغلطة | النتيجة | قاعدتنا |
|---|--------|---------|---------|
| 1 | تعديل Entity جاية بـ `AsNoTracking` | التعديل بيضيع بصمت | الـ Commands بتستخدم Methods Tracked |
| 2 | نسيان `Include` | الأولاد فاضية، و `TotalAmount = 0` | اسم الـ Method بيقول `WithDetails` |
| 3 | `Skip/Take` من غير `OrderBy` | صفوف مكررة أو ناقصة بين الصفحات | دايماً `OrderByDescending(CreatedAt)` |
| 4 | فلتر بعد `ToList()` | بيحمّل الجدول كله | فلتر على `IQueryable` قبل `ToListAsync` |
| 5 | `GetById` جوه Loop | N استعلام (N+1) | `GetByIds` بـ `Contains` |
| 6 | `SaveChanges` جوه الـ Repo | Transaction مكسورة | `uow.SaveChangesAsync` بس |
| 7 | العدّ بعد `Skip/Take` | إجمالي غلط | عدّ الأول على نفس الـ Query المفلتر |
| 8 | `pageSize` من غير حد | حمل على الداتابيز | Validator من 1 لـ 50 |
| 9 | Guid Key من الكود من غير `ValueGeneratedNever` | UPDATE بدل INSERT ← 500 | متظبطة على Cart / CartItem / Wishlist / WishListItem |
| 10 | نسيان الـ Soft-delete في Query يدوي | بيانات محذوفة بتظهر | الـ Global Filter + `IgnoreQueryFilters` بس لما نحتاج |
| 11 | `AverageAsync` على مجموعة فاضية بعمود غير Nullable | `InvalidOperationException` ← 500 | Projection لـ `(double?)` |
| 12 | البحث عن اسم كاتب كل تقييم في Loop | N+1 | `GetNamesByIdsAsync` مرة لكل صفحة |
| 13 | `GetByProductIdAsync` من غير Paging على منتج مشهور | يحمّل آلاف التقييمات | استخدم `GetPagedByProductAsync` وامسح القديمة |

**الأداء:**
- Indexes: `Orders(CustomerId)`، `Orders(Status)`، `CartItems(CartId, ProductId) UNIQUE`، `Reviews(ProductId, CustomerId) UNIQUE`.
- القراءة: `AsNoTracking` + استعلام واحد لكل احتياج.
- الـ Paging في SQL، وأقصى حجم صفحة 50.
- `Include(Items)` على قائمة أوردرات (الصفحة ≤ 50) مقبول. لو الـ Items كترت جداً، اعمل Projection لـ DTO بـ `Select` + `Count()`.

## 8.4 نقاط الضعف (بصراحة)

| # | المشكلة | الأثر | الاقتراح |
|---|---------|-------|----------|
| 1 | `DbUpdateException` (Unique violation) مش متحوّلة | تقييمين متزامنين ← 500 | حوّلها لـ 409 في `GlobalExceptionHandler` |
| 2 | مفيش Rate Limiting على `forgot-password` / `resend-confirmation` / `login` | سبام إيميلات وهجوم Brute force | `AddRateLimiter` (حسب الـ IP) |
| 3 | الـ JWT القديم شغال بعد الـ Reset لحد ما يخلص | توكن مسروق بيعدّي الـ Reset | مدة قصيرة (15 لـ 30 دقيقة)، وRefresh Tokens لو احتجنا |
| 4 | لينكات Reset/Confirm بتتكتب في الـ Log بالـ Token | مقبول Dev، تسريب في Production | تتشال مع SMTP |
| 5 | فشل الإرسال بعد `Register` ← 500 رغم إن اليوزر اتعمل | مربك | اليوزر يضغط Resend. وبعدين Outbox |
| 6 | `GetProductReviews` = 4 استعلامات | شوية كلام كتير | مقبول. حسّن لو قست مشكلة |
| 7 | المتوسط بيتحسب في كل قراءة | التكلفة بتزيد مع التقييمات | Index على `ProductId`، وCache لو احتجنا |
| 8 | `GetByProductIdAsync` القديمة مش مستخدمة | كود ميت وفخ | امسحها |
| 9 | المنتج غير النشط (مش المحذوف) لسه يتقيّم | قرار بيزنس | امنعه لو عايز |
| 10 | `Result.Value` عندك و `Result.Data` في بعض أمثلتي | Compile errors عند اللصق | استخدم `.Value` |
| 11 | الاسم من `UserName` | تمام لو `UserName = FullName` | بدّله بـ `FullName` لو أضفته |
| 12 | مفيش Automated Tests | أي Regression مش بيتكشف | الخطوة الجاية: Domain Unit Tests |
| 13 | الـ Review بيتمسح نهائي (Hard delete) | مفيش سجل | مقبول للتعلم |

---

# 9. افتراضات لازم تتأكد منها

| # | الافتراض | لو غلط |
|---|----------|--------|
| 1 | `IUnitOfWork` فيه `Carts`, `Products`, `Wishlists`, `Orders`, `Reviews`, `SaveChangesAsync(ct)` | عدّل |
| 2 | `Review` فيه `ProductId`, `CustomerId`, `Rating`, `Comment`, `CreatedAt`، والـ ctor `(productId, customerId, rating, comment)`، و `UpdateRating(int)` | عدّل الأسماء أو ضيف الـ Method |
| 3 | `Product.IncreaseStock(int)` موجودة | ضيفها في الـ Domain |
| 4 | `HasCustomerPurchasedProductAsync` فيها `Status == Delivered` | **لازم تتغيّر** لو لسه `!= Cancelled` |
| 5 | الـ Unique Index `(ProductId, CustomerId)` على Review اتعمله Migration | الحماية من التكرار ناقصة |
| 6 | `GetPagedAsync` بترجّع `PagedResult<Order>` | طابقها مع `GetMyOrdersQueryHandler` |
| 7 | `Order.Items` هي الـ Collection العامة | غيّر الاسم |
| 8 | الاسم في `UserName` | بدّله بـ `FullName` |
| 9 | الأكواد المعلّمة **[REFERENCE]** (Generic، UnitOfWork، Wishlist، Category، باقي Product) | قارنها بملفاتك. ملفك هو الصح |

---

# ملخص في صفحة

```
Requirement
   ▼
قاعدة بيزنس؟ ──► في الـ Entity (بترمي DomainException)
   ▼
بتغيّر حالة؟ ──► Command (+Validator) ──► Handler: حمّل Tracked → نادي الـ Domain → SaveChanges مرة
بتقرا بس؟   ──► Query   (+Validator) ──► Handler: Repo بـ AsNoTracking → Map لـ DTO
   ▼
محتاج Repo method؟ ──► ضيف اللي Handler بينده عليه بس، والاسم يقول (Tracked؟ محذوف؟)
   ▼
Controller: [Authorize] → Send → حوّل الـ Result لـ HTTP Status
```

**اسأل نفسك 3 أسئلة قبل أي Repo Method:**
1. هعدّل ولا هقرا بس؟ (Tracked / AsNoTracking)
2. محتاج الأولاد؟ (Include)
3. المحذوف يظهر ولا لأ؟ (IgnoreQueryFilters)

وبعدين سمّي الـ Method باسم يقول الإجابات دي.

**الـ Review:** كل عملية بتتحدد بـ (المنتج + اليوزر من التوكن). الأسماء بتيجي بـ Query واحد من الـ Identity عن طريق Interface.
**الإيميل:** بنفحص التأكيد بإيدينا بعد الباسورد لأننا مش بنستخدم `SignInManager`. وأي Token بيموت بعد أول استخدام لأن الـ SecurityStamp بيتغير.
