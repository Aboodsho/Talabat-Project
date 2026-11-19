# Authentication Stage — Complete Guide
# مرحلة الـ Authentication — الدليل الكامل

Project: E-Commerce backend — ASP.NET Core Web API (.NET 9), Clean Architecture (5 layers) + CQRS (MediatR), EF Core, SQL Server.

المشروع: باك إند e-commerce — ASP.NET Core Web API (.NET 9)، Clean Architecture (5 طبقات) + CQRS (MediatR)، EF Core، SQL Server.

---

## Table of Contents / المحتويات

1. What we built / إيه اللي عملناه
2. How we work / إزاي بنشتغل
3. Architecture: where each piece lives and why / كل حاجة موجودة فين وليه
4. Core concepts / المفاهيم الأساسية
5. Step-by-step build with full code / البناء خطوة بخطوة بالكود كامل
6. Request flows / مسار الطلبات
7. Errors we hit and how we fixed them / الأخطاء اللي قابلتنا وحلولها
8. Testing checklist / قايمة التجربة
9. Security notes / ملاحظات الأمان
10. What is next / الخطوة الجاية

---

## 1. What we built / إيه اللي عملناه

**EN:**
- Register (create account, role `User`).
- Login (returns a signed JWT access token).
- Roles: `Admin` and `User`, seeded at startup.
- Admin account seeded from secrets (not from code).
- `[Authorize(Roles = "Admin")]` on admin controllers.
- Global exception handler (validation errors return 400 instead of 500).
- `ICurrentUserService` (reads the user id from the token).

**AR:**
- Register: بنعمل حساب جديد وبياخد Role اسمه `User`.
- Login: بيرجّع Token (JWT) متوقّع من السيرفر.
- الـ Roles: `Admin` و `User` وبيتعملوا لوحدهم أول ما التطبيق يشتغل.
- حساب الـ Admin بيتعمل من الـ secrets، مش من الكود.
- حطينا `[Authorize(Roles = "Admin")]` على الـ Admin controllers.
- Global exception handler: أخطاء الـ Validation بترجع 400 بدل 500.
- `ICurrentUserService`: بيقرا Id اليوزر من الـ Token.

---

## 2. How we work / إزاي بنشتغل

**EN:**
1. Discuss first, code after. Code is written only when asked ("go", "make it").
2. One small step at a time. Build after every step.
3. Explain WHY, not only WHAT.
4. Challenge weak decisions. Avoid over-engineering.
5. Dependencies point inward only (Dependency Rule).
6. One handler per use case (CQRS). Input = Command/Query, Output = DTO.
7. Expected failures return `Result`, rule violations throw exceptions that the global handler converts to HTTP.
8. Secrets never go into Git.

**AR:**
1. نتناقش الأول وبعدين نكتب كود. الكود بيتكتب لما تقول "go" أو "make it".
2. خطوة صغيرة في كل مرة. وبعد كل خطوة نعمل Build.
3. بنشرح ليه الحاجة بتحصل، مش بس إيه اللي بيحصل.
4. لو قرار ضعيف بنعترض عليه. وبنبعد عن التعقيد الزيادة.
5. الاعتماد بيمشي لجوه بس (Dependency Rule).
6. كل use case ليه Handler لوحده (CQRS). الداخل = Command أو Query، والخارج = DTO.
7. الفشل المتوقع بيرجع `Result`. وكسر قواعد البيزنس بيرمي Exception والـ global handler بيحوله لـ HTTP.
8. الـ Secrets عمرها ما تدخل Git.

---

## 3. Architecture / المعمارية

```
Core  <-  Domain  <-  Service  <-  Infrastructure  <-  API
```

| Layer | Responsibility (EN) | المسؤولية (AR) |
|-------|---------------------|----------------|
| Core | Shared base types: `BaseEntity`, `Result`, `PagedResult`, constants | حاجات مشتركة: `BaseEntity` و `Result` والثوابت |
| Domain | Business entities and rules (no packages) | الـ Entities وقواعد البيزنس (من غير packages) |
| Service | Use cases (Commands/Handlers/Validators), DTOs, Interfaces | الـ use cases والـ DTOs والـ Interfaces |
| Infrastructure | EF Core, Identity, JWT, anything technical | EF Core و Identity و JWT وأي تفصيلة تقنية |
| API | Controllers, Middleware, Program.cs | الـ Controllers والـ Middleware و Program.cs |

### Where authentication pieces live / أجزاء الـ Authentication فين

```
API:             AuthController, GlobalExceptionHandler, CurrentUserService, Program.cs
Service:         IIdentityService, IJwtTokenService, ICurrentUserService,
                 Register/Login (Command + Validator + Handler), DTOs
Infrastructure:  ApplicationUser, IdentityService, JwtTokenService, JwtSettings,
                 IdentitySeeder, AppDbContext (IdentityDbContext)
Core:            Roles (constants)
```

### Why Identity lives in Infrastructure / ليه Identity في Infrastructure

**EN:**
- Identity depends on a package (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`) that depends on EF Core. Domain and Service must not depend on it.
- Identity is a technical detail (how we store users and passwords), not a business rule.
- `IdentityUser` carries storage fields (`PasswordHash`, `SecurityStamp`) that must never leak.
- If we replace Identity later, only Infrastructure changes.
- Domain only needs a `CustomerId` (Guid) in Cart/Order/Review.

**AR:**
- Identity محتاج package بيعتمد على EF Core، والـ Domain والـ Service مينفعش يعتمدوا عليه.
- Identity تفصيلة تقنية (إزاي نخزن اليوزر والباسورد)، مش قاعدة بيزنس.
- `IdentityUser` فيه حقول تخزين زي `PasswordHash` ومينفعش تتسرب.
- لو غيرنا Identity بعدين، التغيير في Infrastructure بس.
- الـ Domain محتاج `CustomerId` من نوع Guid بس في الـ Cart والـ Order والـ Review.

### Why the Handler talks to an interface / ليه الـ Handler بيكلم Interface

```
Handler (Service)
    |  calls
    v
IIdentityService  <- interface lives in Service
    ^  implemented by
    |
IdentityService   <- class lives in Infrastructure (uses UserManager)
```

**EN:** Service cannot reference Infrastructure (dependency rule), so Service defines the contract and Infrastructure implements it.

**AR:** الـ Service مينفعش يشوف الـ Infrastructure، فالـ Service بيعرّف العقد (Interface) والـ Infrastructure بينفذه.

---

## 4. Core Concepts / المفاهيم الأساسية

### 4.1 Authentication vs Authorization

| | Authentication | Authorization |
|---|----------------|---------------|
| EN | Who are you? | What are you allowed to do? |
| AR | إنت مين؟ | مسموحلك تعمل إيه؟ |
| Here | Login + JWT validation | `[Authorize(Roles = "Admin")]` |

```
Request -> UseAuthentication() -> identifies the user from the token
        -> UseAuthorization()  -> allows or blocks based on roles
```

**AR:** الـ Authentication بيعرّف اليوزر، والـ Authorization بيقرر يسمحله ولا لأ. لازم `UseAuthentication` قبل `UseAuthorization`.

### 4.2 ASP.NET Core Identity

**EN:** A library that manages users, passwords (hashing), roles, lockout, and tokens for email confirmation. We use it through `UserManager<T>` and `RoleManager<T>`.

**AR:** مكتبة بتدير اليوزرز والباسوردات (Hashing) والـ Roles والـ Lockout. بنستخدمها من خلال `UserManager` و `RoleManager`.

| Tool | Job |
|------|-----|
| `UserManager<ApplicationUser>` | create user, check password, roles, lockout |
| `RoleManager<IdentityRole<Guid>>` | create and check roles |
| `IdentityDbContext` | adds the `AspNet*` tables |

Tables created / الجداول اللي اتعملت: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`.

### 4.3 `AddIdentityCore` vs `AddIdentity`

**EN:** `AddIdentity` also adds cookies and UI helpers (for MVC). We build an API with JWT, so `AddIdentityCore` is lighter and enough.

**AR:** `AddIdentity` بيضيف Cookies وحاجات للـ MVC. إحنا API بـ JWT، فـ `AddIdentityCore` أخف وكفاية.

### 4.4 Password hashing

**EN:** Passwords are never stored. Identity stores a salted hash (`PasswordHash`). Login re-hashes the input and compares.

**AR:** الباسورد عمره ما بيتخزن. Identity بيخزن Hash مع Salt. وفي الـ Login بيعمل Hash للي اتكتب ويقارن.

```
"Abc12345"  --hash+salt-->  "AQAAAAIAAYag..."   (cannot be reversed)
```

### 4.5 JWT (JSON Web Token)

**EN:** A signed string the client sends with every request: `Authorization: Bearer <token>`. The server verifies the signature and trusts the claims inside, with no database lookup.

**AR:** نص موقّع من السيرفر والـ Client بيبعته مع كل طلب. السيرفر بيتأكد من التوقيع ويصدّق اللي جواه من غير ما يروح الداتابيز.

```
header.payload.signature
```

| Part | Content |
|------|---------|
| header | algorithm (HS256) |
| payload | claims: sub, email, jti, role, exp, iss, aud |
| signature | HMAC of header+payload using the SecretKey |

Claims we put / الـ Claims اللي حطيناها:

| Claim | Use / الاستخدام |
|-------|-----------------|
| `sub` | user id (= `CustomerId` in Cart/Order) |
| `email` | user email |
| `jti` | unique token id |
| `role` | for `[Authorize(Roles = ...)]` |
| `exp` | expiry time |
| `iss` / `aud` | who issued it / who it is for |

**EN warning:** the payload is only Base64, anyone can read it. Never put passwords or sensitive data inside. The signature prevents tampering, not reading.

**AR تحذير:** محتوى الـ Token ممكن أي حد يقراه. متحطش فيه باسورد. التوقيع بيمنع التعديل مش القراءة.

### 4.6 SecretKey

**EN:** The key used to sign tokens. If it leaks, anyone can forge tokens. At least 32 characters for HS256. Stored in `user-secrets` (development), environment variables or a vault (production), never in Git.

**AR:** المفتاح اللي بيوقّع الـ Tokens. لو اتسرب أي حد يقدر يزوّر Tokens. لازم 32 حرف على الأقل. بنحطه في `user-secrets` وقت التطوير، ومش في Git.

### 4.7 Options pattern (`JwtSettings`)

**EN:** Bind a config section to a typed class and inject `IOptions<T>`. No magic strings in code.

**AR:** بنربط قسم من الإعدادات بكلاس وبنحقنه بـ `IOptions<T>`. من غير strings مكررة في الكود.

### 4.8 Lockout

**EN:** After 3 wrong passwords the account is locked for 15 minutes. `CheckPasswordAsync` does NOT count failures by itself, so we call `AccessFailedAsync` manually.

**AR:** بعد 3 محاولات غلط الحساب بيتقفل 15 دقيقة. `CheckPasswordAsync` مبيعدّش المحاولات لوحده، فبننادي `AccessFailedAsync` بإيدينا.

### 4.9 Data Protection

**EN:** `AddDefaultTokenProviders` needs `IDataProtectionProvider` to encrypt tokens (email confirmation, password reset). `AddIdentityCore` does not register it, so we call `AddDataProtection()`. In production the keys must be persisted, or old tokens break after restart.

**AR:** `AddDefaultTokenProviders` محتاج `IDataProtectionProvider`. `AddIdentityCore` مش بيسجله، فبنضيف `AddDataProtection()`. في الـ production لازم المفاتيح تتحفظ في مكان ثابت.

### 4.10 `FrameworkReference`

**EN:** A plain class library does not see the ASP.NET Core shared framework. `AddDefaultTokenProviders` lives there, so Infrastructure needs `<FrameworkReference Include="Microsoft.AspNetCore.App" />`. It is part of .NET itself, not an external package.

**AR:** الـ Class Library العادي مش شايف الـ ASP.NET Core framework. فضفنا `FrameworkReference` في Infrastructure.

### 4.11 CQRS flow for auth

```
Client -> Controller -> MediatR Send(Command)
       -> ValidationBehavior (FluentValidation)
       -> Handler -> IIdentityService / IJwtTokenService
       -> Result<T> -> Controller -> HTTP status
```

**EN:** The Command is the input DTO. We do not create separate request DTOs.

**AR:** الـ Command نفسه هو الـ Input. مفيش DTO تاني للإدخال.

### 4.12 DTOs: input vs output

| Type | Example | Direction |
|------|---------|-----------|
| Input | `RegisterCommand`, `LoginCommand` | client to system |
| Output | `UserDto`, `AccessTokenDto`, `AuthResponseDto` | system to client |

**EN:** `UserDto` exists because Service cannot see `ApplicationUser`, and `ApplicationUser` contains `PasswordHash` that must not leak.

**AR:** `UserDto` موجود لأن الـ Service مش شايف `ApplicationUser`، وعشان `PasswordHash` ميتسربش.

### 4.13 Result pattern vs exceptions

| Situation | How |
|-----------|-----|
| Expected failure (wrong password, duplicate email) | `Result.Failure(...)` |
| Invalid input (validator) | `ValidationException` |
| Business rule broken (Domain) | `DomainException` |
| Unexpected bug | any exception, returns 500 |

### 4.14 Why Register returns no token

**EN:** Register creates an account. Login proves identity. Later we add email confirmation, so a user must not get a token before confirming.

**AR:** الـ Register بيعمل حساب، والـ Login بيثبت الهوية. وبعدين هنضيف تأكيد الإيميل، فمينفعش ياخد Token قبل التأكيد.

### 4.15 Single error message for login

**EN:** Wrong email and wrong password return the same message, so attackers cannot discover which emails exist.

**AR:** إيميل غلط وباسورد غلط بيرجعوا نفس الرسالة عشان المهاجم ميعرفش أنهي إيميلات مسجلة.

### 4.16 `UserName = Email`

**EN:** Identity requires a unique `UserName`. We log in by email, which is already unique. `FullName` is not unique and may contain spaces (not allowed by default), so it is a separate display field.

**AR:** Identity لازم `UserName` فريد. الإيميل فريد أصلاً. و`FullName` ممكن يتكرر وفيه مسافات، فهو حقل منفصل للعرض بس.

### 4.17 Why Identity is not inside `UnitOfWork`

**EN:** `UserManager` saves by itself (each call commits). The UoW wraps Domain repositories and one `SaveChanges`. Putting Identity inside would mislead.

**AR:** `UserManager` بيحفظ لوحده. الـ UoW بيلف Repositories الـ Domain. لو حطينا Identity جواه هيبقى مضلل.

### 4.18 DI lifetimes used

| Service | Lifetime | Why |
|---------|----------|-----|
| `IIdentityService` | Scoped | depends on `UserManager` (scoped) |
| `IJwtTokenService` | Singleton | stateless, only reads settings |
| `ICurrentUserService` | Scoped | depends on current `HttpContext` |
| `GlobalExceptionHandler` | Singleton (by `AddExceptionHandler`) | stateless |

### 4.19 Why `CurrentUserId` comes from the token, not the body

**EN:** If the client sends `CustomerId`, a user can read another user's cart/orders. The id must come from the signed token.

**AR:** لو الـ Client بعت `CustomerId` ممكن يشوف Cart حد تاني. الـ Id لازم يجي من الـ Token الموقّع.

### 4.20 Global exception handler

**EN:** One place converts exceptions to consistent `ProblemDetails` responses. Expected errors are logged as warnings, unexpected as errors with stack trace, and the client never sees internals.

**AR:** مكان واحد بيحوّل الـ Exceptions لرد موحد (`ProblemDetails`). الأخطاء المتوقعة Warning، وغير المتوقعة Error مع Stack Trace، والعميل مبيشوفش تفاصيل داخلية.

---

## 5. Step-by-step build with full code / البناء خطوة بخطوة

> Namespaces below follow the project style (no `ECommerce.` prefix). Adjust `using`s to your folders.
> الـ namespaces زي ستايل مشروعك (من غير `ECommerce.`). عدّل الـ `using` حسب الفولدرات عندك.

---

### Step 1 — Packages and project settings / الـ Packages وإعدادات المشروع

**Infrastructure:**

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version "9.*"
dotnet add package System.IdentityModel.Tokens.Jwt --version "8.*"
```

`Infrastructure.csproj` (add):

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

**API:**

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version "9.*"
```

**EN:** Use `9.*` because the project is .NET 9; without a version NuGet may pick 10.x.

**AR:** لازم `9.*` لأن المشروع .NET 9، من غير version ممكن ينزل نسخة 10 وتضرب.

---

### Step 2 — Roles constants / ثوابت الـ Roles

`Core/Constants/Roles.cs`

```csharp
namespace Core.Constants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }
}
```

**EN:** In Core because both Infrastructure (seeding) and API (`[Authorize]`) use it. Constants avoid typos in strings.

**AR:** في Core لأن Infrastructure (للـ Seed) و API (لـ `[Authorize]`) الاتنين بيستخدموه. الثوابت بتمنع الغلط في الكتابة.

---

### Step 3 — `ApplicationUser`

`Infrastructure/Identity/ApplicationUser.cs`

```csharp
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

**EN:**
- Inherits `IdentityUser<Guid>` so the `Id` is `Guid`, matching `CustomerId` everywhere.
- It cannot also inherit `BaseEntity` (C# has single inheritance, and both have `Id`).
- Already provided by Identity: `Email`, `UserName`, `PasswordHash`, `PhoneNumber`, `EmailConfirmed`, `LockoutEnd`.

**AR:**
- بيرث من `IdentityUser<Guid>` عشان الـ `Id` يبقى `Guid` ويطابق `CustomerId`.
- مينفعش يرث `BaseEntity` كمان (C# مفيهاش وراثة متعددة، والاتنين فيهم `Id`).
- Identity بيدينا جاهز: `Email` و `UserName` و `PasswordHash` و `PhoneNumber` و `EmailConfirmed` و `LockoutEnd`.

---

### Step 4 — `AppDbContext`

`Infrastructure/Data/AppDbContext.cs`

```csharp
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class AppDbContext
        : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Wishlist> Wishlists => Set<Wishlist>();
        public DbSet<Review> Reviews => Set<Review>();
        // No DbSet for Users: IdentityDbContext provides it.

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);   // MUST be first

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
```

**EN:**
- The three generics are: user type, role type, key type (`Guid`). The key type is also used by join tables like `AspNetUserRoles`, so all three must agree.
- `base.OnModelCreating` must be first. It defines the Identity tables and keys. Forgetting it causes "requires a primary key" migration errors.

**AR:**
- التلات Generics: نوع اليوزر، نوع الـ Role، نوع الـ Key (`Guid`). الـ Key بتستخدمه الجداول الوسيطة زي `AspNetUserRoles`، فلازم التلاتة يتطابقوا.
- `base.OnModelCreating` لازم أول سطر. هو اللي بيعرّف جداول Identity ومفاتيحها. لو نسيته الـ Migration بيفشل.

---

### Step 5 — Register Identity in DI / تسجيل Identity

`Infrastructure/DependencyInjection.cs`

```csharp
using Core.Constants;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Service.Interfaces;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, IConfiguration configuration)
        {
            // existing: AddDbContext with MigrationsAssembly, UnitOfWork, repositories...

            services.AddDataProtection();

            services.AddIdentityCore<ApplicationUser>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;

                    options.User.RequireUniqueEmail = true;

                    options.Lockout.MaxFailedAccessAttempts = 3;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<JwtSettings>(
                configuration.GetSection(JwtSettings.SectionName));

            services.AddScoped<IIdentityService, IdentityService>();
            services.AddSingleton<IJwtTokenService, JwtTokenService>();

            return services;
        }
    }
}
```

| Line | Job (EN) | الوظيفة (AR) |
|------|----------|--------------|
| `AddDataProtection` | needed by token providers | محتاجينه لـ token providers |
| `AddIdentityCore` | registers `UserManager` and rules | بيسجل `UserManager` والقواعد |
| `AddRoles` | registers `RoleManager` | بيسجل `RoleManager` |
| `AddEntityFrameworkStores` | store users in `AppDbContext` | يخزن في `AppDbContext` |
| `AddDefaultTokenProviders` | tokens for email confirm / reset | توكنز تأكيد الإيميل وإعادة الباسورد |
| `RequireUniqueEmail` | no duplicate emails | مفيش إيميلين متشابهين |
| `MaxFailedAccessAttempts = 3` | lock after 3 failures | قفل بعد 3 محاولات غلط |

---

### Step 6 — Seeder (roles + admin) / الـ Seeder

`Infrastructure/Identity/IdentitySeeder.cs`

```csharp
using Core.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Identity
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            foreach (var roleName in new[] { Roles.Admin, Roles.User })
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = roleName });
                }
            }
        }

        public static async Task SeedAdminAsync(
            IServiceProvider services, IConfiguration configuration)
        {
            var email = configuration["Admin:Email"];
            var password = configuration["Admin:Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return;

            using var scope = services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            if (await userManager.FindByEmailAsync(email) is not null)
                return;

            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FullName = "Administrator",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }
}
```

**EN:**
- `CreateScope`: `RoleManager`/`UserManager` are scoped, so they cannot be taken from the root provider.
- Checks before inserting, so it is safe to run on every startup.
- Admin data comes from configuration (secrets), not code.
- Users cannot make themselves Admin: `RegisterAsync` only assigns `User`.
- It must run after the migration is applied (it talks to the database).

**AR:**
- `CreateScope` لأن `RoleManager` و `UserManager` Scoped ومينفعش ناخدهم من الـ root.
- بيتأكد قبل ما يضيف، فآمن يشتغل كل مرة.
- بيانات الـ Admin من الإعدادات (secrets) مش من الكود.
- اليوزر مينفعش يعمل نفسه Admin لأن `RegisterAsync` بيدي Role `User` بس.
- لازم يشتغل بعد ما الـ Migration يتطبق (لأنه بيكلم الداتابيز).

---

### Step 7 — Migration

Package Manager Console (Default project = `Infrastructure`, Startup project = `API`):

```
Add-Migration AddIdentity
Update-Database
```

**EN:** Review the migration before applying: it must create the `AspNet*` tables and must not touch Products/Orders (no `DropTable`/`DropColumn`).

**AR:** راجع ملف الـ Migration قبل التطبيق: لازم يعمل جداول `AspNet*` ومميلمسش جداول Products و Orders.

Verify:

```sql
SELECT * FROM AspNetRoles
SELECT * FROM AspNetUserRoles
```

---

### Step 8 — DTOs

`Service/DTOs/Auth/UserDto.cs`

```csharp
namespace Service.DTOs.Auth
{
    public sealed record UserDto(
        Guid Id,
        string Email,
        string FullName,
        IReadOnlyList<string> Roles);
}
```

`Service/DTOs/Auth/AccessTokenDto.cs`

```csharp
namespace Service.DTOs.Auth
{
    public sealed record AccessTokenDto(string Token, DateTime ExpiresAtUtc);
}
```

`Service/DTOs/Auth/AuthResponseDto.cs`

```csharp
namespace Service.DTOs.Auth
{
    public sealed record AuthResponseDto(
        string AccessToken,
        DateTime ExpiresAtUtc,
        UserDto User);
}
```

| DTO | Why (EN) | ليه (AR) |
|-----|----------|----------|
| `UserDto` | Service cannot see `ApplicationUser`; hides `PasswordHash` | الـ Service مش شايف `ApplicationUser` وبيخبّي `PasswordHash` |
| `AccessTokenDto` | token and its expiry travel together | الـ Token ووقت انتهائه مع بعض |
| `AuthResponseDto` | final login response | الرد النهائي للـ Login |

---

### Step 9 — Service interfaces

`Service/Interfaces/IIdentityService.cs`

```csharp
using Core.Results;
using Service.DTOs.Auth;

namespace Service.Interfaces
{
    public interface IIdentityService
    {
        Task<Result<UserDto>> RegisterAsync(
            string email, string password, string fullName,
            CancellationToken cancellationToken = default);

        Task<Result<UserDto>> LoginAsync(
            string email, string password,
            CancellationToken cancellationToken = default);
    }
}
```

`Service/Interfaces/IJwtTokenService.cs`

```csharp
using Service.DTOs.Auth;

namespace Service.Interfaces
{
    public interface IJwtTokenService
    {
        AccessTokenDto GenerateAccessToken(UserDto user);
    }
}
```

`Service/Interfaces/ICurrentUserService.cs`

```csharp
namespace Service.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        bool IsAuthenticated { get; }
    }
}
```

**EN:**
- Login is split in two services: identity verification (needs `UserManager`) and token creation (needs the secret). Separate responsibilities, easier to change and test.
- `GenerateAccessToken` is not async: signing is pure CPU work, no I/O.
- The `CancellationToken` on `IIdentityService` is not used by `UserManager` (it does not accept one), kept for a consistent contract.

**AR:**
- الـ Login اتقسم لخدمتين: التأكد من اليوزر (محتاج `UserManager`) وصناعة الـ Token (محتاج الـ Secret). كل واحدة بمسؤولية واحدة.
- `GenerateAccessToken` مش async لأن التوقيع حساب بس من غير I/O.
- الـ `CancellationToken` في `IIdentityService` مش بيستخدمه `UserManager`، سبناه عشان العقد يبقى ثابت.

---

### Step 10 — `IdentityService`

`Infrastructure/Identity/IdentityService.cs`

```csharp
using Core.Constants;
using Core.Results;
using Microsoft.AspNetCore.Identity;
using Service.DTOs.Auth;
using Service.Interfaces;

namespace Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private const string InvalidCredentials = "Invalid email or password.";
        private readonly UserManager<ApplicationUser> _userManager;

        public IdentityService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<UserDto>> RegisterAsync(
            string email, string password, string fullName,
            CancellationToken cancellationToken = default)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FullName = fullName
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
                return Result<UserDto>.Failure(
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));

            var roleResult = await _userManager.AddToRoleAsync(user, Roles.User);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);   // rollback
                return Result<UserDto>.Failure("Failed to assign role.");
            }

            return Result<UserDto>.Success(ToDto(user, new[] { Roles.User }));
        }

        public async Task<Result<UserDto>> LoginAsync(
            string email, string password,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                return Result<UserDto>.Failure(InvalidCredentials);

            if (await _userManager.IsLockedOutAsync(user))
                return Result<UserDto>.Failure("Account is locked. Try again later.");

            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                await _userManager.AccessFailedAsync(user);   // counts toward lockout
                return Result<UserDto>.Failure(InvalidCredentials);
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            return Result<UserDto>.Success(ToDto(user, roles.ToList()));
        }

        private static UserDto ToDto(ApplicationUser user, IReadOnlyList<string> roles)
            => new(user.Id, user.Email!, user.FullName, roles);
    }
}
```

**EN walk-through:**
1. Register: create the user (Identity validates and hashes the password), then add role `User`. If the role fails we delete the user so no user exists without a role.
2. Login: find by email, check lockout, check password, count failures manually, reset the counter on success, load roles, return `UserDto`.
3. The same message is used for wrong email and wrong password.

**AR شرح:**
1. Register: بنعمل اليوزر (Identity بيتحقق من الباسورد ويعمل Hash)، وبعدين بنضيف Role `User`. لو الـ Role فشل بنمسح اليوزر عشان مفيش يوزر من غير Role.
2. Login: ندوّر بالإيميل، نشوف الـ Lockout، نتأكد من الباسورد، نعدّ المحاولات الغلط بإيدينا، نصفّر العداد لما ينجح، نجيب الـ Roles، ونرجع `UserDto`.
3. نفس الرسالة للإيميل الغلط والباسورد الغلط.

---

### Step 11 — JWT settings and token service

`Infrastructure/Identity/JwtSettings.cs`

```csharp
namespace Infrastructure.Identity
{
    public sealed class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public string SecretKey { get; init; } = string.Empty;
        public int AccessTokenMinutes { get; init; } = 30;
    }
}
```

`Infrastructure/Identity/JwtTokenService.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Service.DTOs.Auth;
using Service.Interfaces;

namespace Infrastructure.Identity
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtSettings _settings;

        public JwtTokenService(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public AccessTokenDto GenerateAccessToken(UserDto user)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return new AccessTokenDto(
                new JwtSecurityTokenHandler().WriteToken(token),
                expiresAt);
        }
    }
}
```

**EN:** Build claims, sign with HMAC-SHA256 using the secret, set expiry, serialize. Use only one `using` for `JwtRegisteredClaimNames` (it exists in two namespaces; we keep `System.IdentityModel.Tokens.Jwt`).

**AR:** بنبني الـ Claims، نوقّع بـ HMAC-SHA256 بالـ Secret، نحدد الانتهاء، ونكتب الـ Token. استخدم `using` واحد بس لـ `JwtRegisteredClaimNames` لأنه موجود في namespace-ين.

Configuration / الإعدادات:

`API/appsettings.json` (no secret here):

```json
"Jwt": {
  "Issuer": "ECommerce.Api",
  "Audience": "ECommerce.Client",
  "AccessTokenMinutes": 30
}
```

`secrets.json` (right-click API project, Manage User Secrets):

```json
{
  "Jwt:SecretKey": "at-least-32-characters-long-random-text-here",
  "Admin:Email": "admin@shop.com",
  "Admin:Password": "Admin12345"
}
```

**EN:** The file must be valid JSON. A broken `secrets.json` crashes the app at startup (`Failed to load configuration from file`).

**AR:** لازم الملف JSON سليم. لو فيه خطأ التطبيق بيقع وقت التشغيل.

---

### Step 12 — Register feature (CQRS)

`Service/Features/Auth/Commands/Register/RegisterCommand.cs`

```csharp
using Core.Results;
using MediatR;
using Service.DTOs.Auth;

namespace Service.Features.Auth.Commands.Register
{
    public sealed record RegisterCommand(
        string Email,
        string Password,
        string FullName) : IRequest<Result<UserDto>>;
}
```

`RegisterCommandValidator.cs`

```csharp
using FluentValidation;

namespace Service.Features.Auth.Commands.Register
{
    public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(256);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(20);

            RuleFor(x => x.FullName)
                .NotEmpty()
                .MaximumLength(50);
        }
    }
}
```

`RegisterCommandHandler.cs`

```csharp
using Core.Results;
using MediatR;
using Service.DTOs.Auth;
using Service.Interfaces;

namespace Service.Features.Auth.Commands.Register
{
    public sealed class RegisterCommandHandler
        : IRequestHandler<RegisterCommand, Result<UserDto>>
    {
        private readonly IIdentityService _identityService;

        public RegisterCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public Task<Result<UserDto>> Handle(
            RegisterCommand request, CancellationToken cancellationToken)
        {
            return _identityService.RegisterAsync(
                request.Email.Trim(),
                request.Password,
                request.FullName.Trim(),
                cancellationToken);
        }
    }
}
```

**EN:**
- The validator checks shape only (format, length). Complexity rules (uppercase, digit) belong to Identity and are not duplicated.
- Password max length protects against huge inputs (hashing is expensive). We used 20; a more common limit is 64 so passphrases are allowed.
- `Trim()` avoids accidental spaces creating duplicates.
- The handler is thin: the real work is in `IdentityService`.
- No `IUnitOfWork`: `UserManager` saves by itself.

**AR:**
- الـ Validator بيتحقق من الشكل بس (صيغة وطول). قواعد التعقيد (حرف كبير/رقم) عند Identity ومش بنكررها.
- الحد الأقصى للباسورد بيحمي من المدخلات الضخمة (الـ Hashing تقيل). استخدمنا 20، والمعتاد 64 عشان الجمل الطويلة.
- `Trim()` بيمنع المسافات اللي بتعمل تكرار بالغلط.
- الـ Handler خفيف: الشغل الحقيقي في `IdentityService`.
- مفيش `IUnitOfWork` لأن `UserManager` بيحفظ لوحده.

---

### Step 13 — Login feature (CQRS)

`Service/Features/Auth/Commands/Login/LoginCommand.cs`

```csharp
using Core.Results;
using MediatR;
using Service.DTOs.Auth;

namespace Service.Features.Auth.Commands.Login
{
    public sealed record LoginCommand(string Email, string Password)
        : IRequest<Result<AuthResponseDto>>;
}
```

`LoginCommandValidator.cs`

```csharp
using FluentValidation;

namespace Service.Features.Auth.Commands.Login
{
    public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }
}
```

`LoginCommandHandler.cs`

```csharp
using Core.Results;
using MediatR;
using Service.DTOs.Auth;
using Service.Interfaces;

namespace Service.Features.Auth.Commands.Login
{
    public sealed class LoginCommandHandler
        : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
    {
        private readonly IIdentityService _identityService;
        private readonly IJwtTokenService _jwtTokenService;

        public LoginCommandHandler(
            IIdentityService identityService,
            IJwtTokenService jwtTokenService)
        {
            _identityService = identityService;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<Result<AuthResponseDto>> Handle(
            LoginCommand request, CancellationToken cancellationToken)
        {
            var loginResult = await _identityService.LoginAsync(
                request.Email.Trim(), request.Password, cancellationToken);

            if (!loginResult.IsSuccess)
                return Result<AuthResponseDto>.Failure(loginResult.Error!);

            var user = loginResult.Data!;
            var token = _jwtTokenService.GenerateAccessToken(user);

            return Result<AuthResponseDto>.Success(
                new AuthResponseDto(token.Token, token.ExpiresAtUtc, user));
        }
    }
}
```

**EN:** The login validator has no `MinimumLength` on the password: login must not reveal password rules. The handler only orchestrates: verify user, then create the token.

**AR:** الـ Validator بتاع الـ Login مفيهوش حد أدنى للباسورد عشان منكشفش قواعد الباسورد. الـ Handler بينسق بس: يتأكد من اليوزر وبعدين يعمل الـ Token.

---

### Step 14 — `ICurrentUserService` implementation

`API/Services/CurrentUserService.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using Service.Interfaces;

namespace API.Services
{
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?
                    .User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                return Guid.TryParse(value, out var id) ? id : null;
            }
        }

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }
}
```

**EN:** Implemented in API because it depends on `HttpContext` (an HTTP detail). `Guid?` because an anonymous visitor has no id. It reads `sub`, which keeps its name because we set `MapInboundClaims = false`.

**AR:** متنفذ في API لأنه بيعتمد على `HttpContext` (تفصيلة HTTP). `Guid?` لأن الزائر مفيش له Id. بيقرا `sub` واسمه ثابت لأننا ضبطنا `MapInboundClaims = false`.

---

### Step 15 — Global exception handler

`API/Middleware/GlobalExceptionHandler.cs`

```csharp
using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.Middleware
{
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            int status;
            string title;
            string detail;
            string? code = null;

            switch (exception)
            {
                case FluentValidation.ValidationException ex:
                    status = StatusCodes.Status400BadRequest;
                    title = "Validation failed";
                    detail = ex.Message;
                    code = "VALIDATION_ERROR";
                    _logger.LogWarning("Validation failed: {Message}", ex.Message);
                    break;

                case DomainException ex:
                    status = StatusCodes.Status400BadRequest;
                    title = "Business rule violated";
                    detail = ex.Message;
                    code = ex.Code;
                    _logger.LogWarning("Domain rule violated [{Code}]: {Message}", ex.Code, ex.Message);
                    break;

                default:
                    status = StatusCodes.Status500InternalServerError;
                    title = "Server error";
                    detail = "An unexpected error occurred.";
                    _logger.LogError(exception, "Unhandled exception");
                    break;
            }

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            };
            if (code is not null)
                problem.Extensions["code"] = code;

            httpContext.Response.StatusCode = status;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }
    }
}
```

**EN:**
- Before this, `ValidationBehavior` threw an exception nobody caught, so the API returned 500.
- Use the fully qualified `FluentValidation.ValidationException` to avoid the ambiguity with `System.ComponentModel.DataAnnotations.ValidationException`.
- Never send `exception.Message` / type names for unexpected errors (information disclosure).

**AR:**
- قبل كده الـ `ValidationBehavior` كان بيرمي Exception محدش بيمسكه فالـ API كان بيرجع 500.
- اكتب `FluentValidation.ValidationException` كامل عشان متتلخبطش مع `System.ComponentModel.DataAnnotations.ValidationException`.
- متبعتش `exception.Message` أو اسم النوع للعميل في الأخطاء غير المتوقعة (تسريب معلومات).

---

### Step 16 — `AuthController`

`API/Controllers/AuthController.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Service.Features.Auth.Commands.Login;
using Service.Features.Auth.Commands.Register;

namespace API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);

            return result.IsSuccess
                ? Ok(result.Data)
                : BadRequest(new { error = result.Error });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);

            return result.IsSuccess
                ? Ok(result.Data)
                : Unauthorized(new { error = result.Error });
        }
    }
}
```

**EN:** No logic in the controller. Register failure is a client error (400). Login failure is 401. No `[Authorize]` here because both endpoints must be public.

**AR:** مفيش منطق في الـ Controller. فشل الـ Register = 400، وفشل الـ Login = 401. مفيش `[Authorize]` لأن الاتنين لازم يكونوا مفتوحين.

Protect admin controllers / حماية الـ Admin controllers:

```csharp
using Core.Constants;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = Roles.Admin)]
public class AdminProductsController : ControllerBase { /* ... */ }

[Authorize(Roles = Roles.Admin)]
public class AdminCategoriesController : ControllerBase { /* ... */ }
```

Public controllers (`ProductsController`, `CategoriesController`, `AuthController`) stay open. / الـ Controllers العامة بتفضل مفتوحة.

---

### Step 17 — `Program.cs`

```csharp
using System.Security.Claims;
using System.Text;
using API.Middleware;
using API.Services;
using Infrastructure;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Service;
using Service.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT token only (no 'Bearer ' prefix)."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddService();

var jwt = builder.Configuration
    .GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();            // must be first

await IdentitySeeder.SeedRolesAsync(app.Services);
await IdentitySeeder.SeedAdminAsync(app.Services, app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();              // who are you
app.UseAuthorization();               // what can you do

app.MapControllers();
app.Run();
```

| Setting | Why (EN) | ليه (AR) |
|---------|----------|----------|
| `MapInboundClaims = false` | keep claim names (`sub`, `email`) | الـ Claims تفضل بأسمائها |
| `ClockSkew = Zero` | token expires exactly on time (default adds 5 min) | الـ Token ينتهي في وقته بالظبط |
| 4 validations | issuer, audience, signature, lifetime | الأربعة لازم يشتغلوا |
| `UseExceptionHandler` first | catches errors from everything after it | يمسك أخطاء كل اللي بعده |
| Authentication before Authorization | identify first, then decide | الأول نعرف مين، وبعدين نقرر |
| Top-level statements | `await` works directly; no `Main` | الـ `await` شغال من غير `Main` |

---

## 6. Request flows / مسار الطلبات

### Register

```
POST /api/auth/register { email, password, fullName }
  -> AuthController.Register
  -> MediatR -> ValidationBehavior -> RegisterCommandValidator
       fail -> ValidationException -> GlobalExceptionHandler -> 400
  -> RegisterCommandHandler
  -> IdentityService.RegisterAsync
       UserManager.CreateAsync (hash password, INSERT AspNetUsers)
       UserManager.AddToRoleAsync (User)
  -> Result<UserDto> -> 200 + UserDto  (or 400 + error)
```

### Login

```
POST /api/auth/login { email, password }
  -> LoginCommandValidator
  -> LoginCommandHandler
       IdentityService.LoginAsync -> UserDto (or failure -> 401)
       JwtTokenService.GenerateAccessToken -> AccessTokenDto
  -> 200 + AuthResponseDto { accessToken, expiresAtUtc, user }
```

### Authorized request

```
GET /api/admin/... with  Authorization: Bearer <token>
  -> UseAuthentication: validate signature, issuer, audience, expiry -> User + claims
  -> UseAuthorization: [Authorize(Roles = "Admin")]
       no token        -> 401
       role is User    -> 403
       role is Admin   -> 200
```

| Status | Meaning (EN) | المعنى (AR) |
|--------|--------------|-------------|
| 401 | not authenticated | مش مسجل دخول / Token غلط |
| 403 | authenticated but not allowed | مسجل بس مش مسموحله |

---

## 7. Errors we hit and fixes / الأخطاء وحلولها

| # | Error | Cause | Fix |
|---|-------|-------|-----|
| 1 | `async void Main` — no suitable entry point | `async void` is not a valid entry point | Use top-level statements, or `async Task Main` |
| 2 | `AddDefaultTokenProviders` not found | class library does not see the ASP.NET Core framework | add `FrameworkReference` to Infrastructure |
| 3 | Host failed: cannot resolve `IDataProtectionProvider` | `AddIdentityCore` does not register it | `services.AddDataProtection()` |
| 4 | `Unable to create a DbContext` in Add-Migration | consequence of error 3 (host failed to build) | fix error 3 first |
| 5 | ambiguous `JwtRegisteredClaimNames` | two namespaces contain it | keep only `System.IdentityModel.Tokens.Jwt` |
| 6 | cannot inherit `IdentityUser` and `BaseEntity` | single inheritance, both have `Id` | inherit `IdentityUser<Guid>` only |
| 7 | `Failed to load configuration ... secrets.json` | invalid JSON in secrets file | fix JSON (quotes, braces, commas) |
| 8 | validation returned 500 | `ValidationException` not handled | global exception handler |
| 9 | ambiguous `ValidationException` | `DataAnnotations` and `FluentValidation` | write `FluentValidation.ValidationException` |
| 10 | "unable to connect to webserver" | app crashed at startup | read the exception (Output window or `dotnet run`) |

**AR ملخص:**
1. `async void` مينفعش كـ entry point.
2. الـ Class Library مش شايف الـ framework: ضفنا `FrameworkReference`.
3. Identity محتاج `AddDataProtection`.
4. خطأ الـ DbContext كان نتيجة لخطأ 3.
5. و 9: اسمين متكررين في namespaces مختلفة، اكتب الاسم كامل أو شيل الـ using.
6. مفيش وراثة من كلاسين.
7. ملف الـ secrets لازم JSON سليم.
8. الـ Validation كان بيرمي Exception محدش بيمسكه.
10. الرسالة معناها التطبيق وقع، شوف الـ Exception.

---

## 8. Testing checklist / قايمة التجربة

| # | Test | Expected |
|---|------|----------|
| 1 | Register valid | 200, `roles: ["User"]` |
| 2 | Register same email | 400 (`DuplicateEmail`) |
| 3 | Register bad email | 400 `VALIDATION_ERROR` |
| 4 | Login valid | 200, `accessToken` |
| 5 | Login wrong password | 401 |
| 6 | Login wrong 3 times | account locked |
| 7 | Decode token on jwt.io | `sub`, `email`, `jti`, `role`, `exp`, `iss`, `aud`; no password |
| 8 | Admin endpoint, no token | 401 |
| 9 | Admin endpoint, User token | 403 |
| 10 | Admin endpoint, Admin token | 200 |
| 11 | `GET /api/products` no token | 200 |
| 12 | Temporary `me` endpoint | `userId` equals `user.id` from login |

SQL checks:

```sql
SELECT Id, UserName, Email, FullName, PasswordHash FROM AspNetUsers
SELECT * FROM AspNetRoles
SELECT * FROM AspNetUserRoles
```

---

## 9. Security notes / ملاحظات الأمان

**EN:**
- Passwords are hashed with salt by Identity; never stored or logged.
- `SecretKey` and admin credentials live in user-secrets (dev) or environment variables / vault (production).
- Same message for wrong email and wrong password.
- Lockout after 3 failures for 15 minutes.
- Token lifetime is short (30 minutes). A refresh token flow can be added later.
- Unexpected errors never expose internals; only logs have the stack trace.
- The user id always comes from the signed token, never from the request body.
- Users cannot register as Admin.
- Production: persist DataProtection keys, enforce HTTPS, add rate limiting on login.

**AR:**
- الباسوردات بتتعمل لها Hash مع Salt من Identity، ومبتتخزنش ولا بتتسجل في اللوج.
- الـ `SecretKey` وبيانات الـ Admin في user-secrets (التطوير) أو Environment Variables / Vault (production).
- نفس الرسالة للإيميل الغلط والباسورد الغلط.
- Lockout بعد 3 محاولات غلط لمدة 15 دقيقة.
- عمر الـ Token قصير (30 دقيقة). ممكن نضيف Refresh Token بعدين.
- الأخطاء غير المتوقعة مبتكشفش تفاصيل داخلية، الـ Stack Trace في اللوج بس.
- الـ Id بيجي من الـ Token الموقّع، مش من الـ Body.
- اليوزر مينفعش يسجل نفسه Admin.
- في الـ Production: احفظ مفاتيح DataProtection، فعّل HTTPS، وحط Rate Limiting على الـ Login.

---

## 10. What is next / الخطوة الجاية

| # | Task | المهمة |
|---|------|--------|
| 1 | Serilog (Console + daily file) | Serilog (Console + ملف يومي) |
| 2 | Cart (uses `ICurrentUserService`) | الـ Cart (بيستخدم `ICurrentUserService`) |
| 3 | Wishlist | الـ Wishlist |
| 4 | Order (checkout transaction, stock, price snapshot) | الـ Order (Transaction للـ Checkout والستوك وسعر وقت الشراء) |
| 5 | Review (only after purchase) | الـ Review (بعد الشراء بس) |
| 6 | Restore for Product and Category | Restore للمنتج والكاتيجوري |
| 7 | Email confirmation and password reset (fake console `IEmailSender`) | تأكيد الإيميل وإعادة الباسورد |
| 8 | Tests | الاختبارات |

**EN:** Concepts to remember: Dependency Rule, interface in Service / implementation in Infrastructure, Result for expected failures, exceptions for broken rules, user id from the token.

**AR:** افتكر: الـ Dependency Rule، الـ Interface في Service والتنفيذ في Infrastructure، `Result` للفشل المتوقع، والـ Exceptions لكسر القواعد، والـ Id من الـ Token.
