# تقرير التنضيف: اللي اتعمل وليه (بالمصري)

مشروع الـ E-Commerce · مرحلة التنضيف بعد الـ Review وتأكيد الإيميل وReset Password

> الدياجرامات والكود بالإنجليزي. الشرح بالمصري.
> كل بند فيه: المشكلة، الحل، الكود، وإزاي اتأكدنا منه.

---

## الفهرس

1. الملخص في جدول
2. `DbUpdateException` ← 409
3. CORS (ومشكلة الأخطاء اللي اتكشفت)
4. `UserId` في الـ Logs
5. قصة اختبار الـ CORS: إيه اللي اتعلمناه
6. تضارب الحد الأدنى للباسورد
7. اللي لسه ناقص
8. النسخة النهائية من الكود

---

# 1. الملخص

| # | البند | الحالة | اتجرّب؟ |
|---|-------|--------|---------|
| أ | `DbUpdateException` (Unique violation) ← 409 | الكود اتضاف | **لسه** |
| ب | CORS لـ `http://localhost:4200` | ✔ شغال | ✔ اتأكدنا من الهيدر |
| ب2 | هيدر CORS على ردود الأخطاء (400/409/500) | ✔ اتصلّح | ✔ |
| ج | `UserId` في Request Logs | ✔ شغال | ✔ (`by anonymous` ظاهرة) |
| د | إعادة التسمية (`Prodcut` ← `Product` وغيرها) | لسه | — |
| هـ | تضارب Min Password بين Register و Reset | اتكشف | محتاج قرار |

---

# 2. `DbUpdateException` ← 409

## المشكلة

الـ Review عندنا فيه Unique Index على `(ProductId, CustomerId)`. الـ Handler بيفحص التكرار الأول، بس لو **طلبين وصلوا في نفس اللحظة**، الاتنين بيعدّوا الفحص، والداتابيز بترفض التاني.

الـ `GlobalExceptionHandler` ما كانش بيعرف الـ `DbUpdateException`، فالنتيجة كانت **500** ورسالة عامة. وده غلط: اللي حصل ده تعارض (Conflict) مش خطأ في السيرفر.

## الحل

```csharp
// SQL Server: 2601 = duplicate in unique index, 2627 = unique constraint violation
case DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } ex:
    status = StatusCodes.Status409Conflict;
    title = "Conflict";
    details = "A record with the same unique value already exists.";
    code = "DUPLICATE_RESOURCE";
    _logger.LogWarning(ex, "Unique constraint violation");
    break;
```

## قرارات مهمة

| القرار | السبب |
|--------|-------|
| الـ `case` ده **بعد** `DbUpdateConcurrencyException` | `DbUpdateConcurrencyException` بيورّث من `DbUpdateException`. لو الترتيب اتعكس، الكومبايلر بيدّي Error (`CS8120`) |
| بنحوّل **أرقام الـ Unique بس** (2601 و 2627) | أي `DbUpdateException` تانية (FK مثلاً) تفضل 500. مش بنخمّن أخطاء مش فاهمينها |
| الرسالة للعميل **عامة** | رسالة SQL الأصلية فيها أسماء Indexes وجداول. بتتسجّل في الـ Log بس |

## الاختبار (لسه ما اتعملش)

1. علّق مؤقتاً الفحص `existing is not null` في `AddReviewCommandHandler`.
2. `POST` تقييم على نفس المنتج مرتين.
3. الثانية لازم ترجع **409** و`"code": "DUPLICATE_RESOURCE"`.
4. رجّع الفحص.

---

# 3. CORS

## المشكلة

لما الـ Angular (على `http://localhost:4200`) يكلّم الـ API (على بورت تاني)، المتصفح بيعتبرهم Origins مختلفة وبيمنع الطلب إلا لو السيرفر قال صراحة إنه مسموح.

## الحل

**`Program.cs`:**

```csharp
const string CorsPolicy = "Frontend";

builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

// ... after UseExceptionHandler()
app.UseCors(CorsPolicy);
```

**`appsettings.json`:**

```json
"Cors": { "AllowedOrigins": [ "http://localhost:4200" ] }
```

| القرار | السبب |
|--------|-------|
| الـ Origins من الإعدادات | الـ Production له Origin تاني، فبنغيّره من غير ما نلمس الكود |
| مفيش `AllowCredentials` | الـ JWT بيتبعت في `Authorization` Header مش Cookie |
| `WithOrigins` مش `AllowAnyOrigin` | أي موقع يقدر يكلّم الـ API لو فتحناها للكل |

## ترتيب الـ Pipeline

```
UseSerilogRequestLogging
UseExceptionHandler
UseCors                    ← الجديد
UseSwagger / UseSwaggerUI
UseHttpsRedirection
UseAuthentication
UseAuthorization
MapControllers
```

## ب2. المشكلة المخفية: الأخطاء بتفقد هيدر CORS

```
Request ──► CORS middleware (بيضيف Access-Control-Allow-Origin)
              │
              ▼
          Controller ──► Validation fails ──► throws ValidationException
                                                  │
        ExceptionHandlerMiddleware ◄──────────────┘
              │  1. يمسح الـ Response بالكامل (والهيدرز معاه، منها CORS)
              │  2. يستدعي GlobalExceptionHandler
              ▼
        الرد النهائي: 400 من غير Access-Control-Allow-Origin
              │
              ▼
        المتصفح: "CORS error"   ← مش شايف رسالة الـ 400
```

**النتيجة العملية:** كل أخطاء الـ Validation والـ Domain والـ 409 عندنا بتتعمل بـ Exceptions. يعني Angular كان هيشوف "CORS error" بدل رسالة "Password too short".

**الحل: نعيد تطبيق CORS جوه الـ Handler:**

```csharp
using Microsoft.AspNetCore.Cors.Infrastructure;

// ... inside TryHandleAsync, before writing the response
var corsService = httpContext.RequestServices.GetRequiredService<ICorsService>();
var corsProvider = httpContext.RequestServices.GetRequiredService<ICorsPolicyProvider>();
var corsPolicy = await corsProvider.GetPolicyAsync(httpContext, "Frontend");

if (corsPolicy is not null)
{
    var corsResult = corsService.EvaluatePolicy(httpContext, corsPolicy);
    corsService.ApplyResult(corsResult, httpContext.Response);
}
```

**ليه ده آمن؟**
- `EvaluatePolicy` بيشوف الـ `Origin` الفعلي. لو غريب، مفيش هيدر. يعني مفيش فتح لحد جديد.
- لو الهيدر موجود أصلاً، `ApplyResult` بيكتب فوقه.

## الدليل إنه اشتغل

```
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8
Access-Control-Allow-Origin: http://localhost:5124
Cache-Control: no-cache,no-store
Vary: Origin
```

(الـ `Vary: Origin` ظاهر مرتين. تكرار مش مؤثر.)

---

# 4. `UserId` في الـ Logs

## المشكلة

الـ Request Logging كان بيطلّع `HTTP POST /api/... responded 200 in 33 ms` من غير ما نعرف **مين** عمل الطلب.

## الحل

```csharp
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms by {UserId}";

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        // MapInboundClaims = false, so the claim type is "sub"
        var userId = httpContext.User.FindFirst("sub")?.Value ?? "anonymous";
        diagnosticContext.Set("UserId", userId);
    };
});
```

**ليه شغال مع إن `UseAuthentication` جاي بعده؟**
الـ `EnrichDiagnosticContext` بيتنفذ في **آخر** الطلب، بعد ما الـ Pipeline كله خلص واليوزر اتعرف.

**ليه `"sub"` مش `ClaimTypes.NameIdentifier`؟**
عندنا `MapInboundClaims = false` في الـ JWT Bearer، فأسماء الـ Claims بتفضل زي ما في التوكن.

وكمان في الـ `default` بتاع الـ Handler (أخطاء 500):

```csharp
_logger.LogError(exception, "Unhandled exception for user {UserId}",
    httpContext.User.FindFirst("sub")?.Value ?? "anonymous");
```

## الدليل

```
20:41:43.275 [INF] HTTP POST /api/auth/register responded 400 in 178.6174 ms by anonymous
```

---

# 5. قصة اختبار الـ CORS: إيه اللي اتعلمناه

الاختبار خد وقت لأن فيه 4 فخاخ. مهم تعرفهم لأنهم هيتكرروا في أي اختبار CORS.

| # | الفخ | الدرس |
|---|------|-------|
| 1 | `curl` في PowerShell = `Invoke-WebRequest` | استخدم `curl.exe` (بـ `.exe`) |
| 2 | الاختبار من **Swagger** | Swagger على نفس عنوان الـ API، فالـ Origin بتاعه هو `http://localhost:5124`، مش `4200`. من غير ما نضيفه في `AllowedOrigins`، السيرفر **صح** إنه ما يرجّعش هيدر |
| 3 | `Content-Type: application/problem+json` | ده 400 من **Model Binding** بتاع `[ApiController]` (Body ناقص). مبيعديش على الـ Exception Handler خالص، فمش بيختبر الإصلاح. الـ Handler بتاعنا بيرجّع `application/json; charset=utf-8` مع `Cache-Control: no-store` |
| 4 | الـ Hot Reload | بيتجاهل تعديلات كتير. لازم Stop كامل ثم F5 |

**الطلب الصح للاختبار:** لازم يعدّي الـ Binding ويفشل في الـ **Validator**:

```json
{ "email": "test@example.com", "password": "1", "fullName": "Test" }
```

## تقنية الـ Debug اللي حسمت الموضوع

بدل ما نخمّن، ضفنا سطر Log مؤقت:

```csharp
_logger.LogWarning("CORS debug: policyFound=True origin={Origin} allowed={Allowed} allowedOrigin={AllowedOrigin}",
    httpContext.Request.Headers.Origin.ToString(), corsResult.IsOriginAllowed, corsResult.AllowedOrigin);
```

الناتج:

```
CORS debug: policyFound=True origin=http://localhost:5124 allowed=True allowedOrigin=http://localhost:5124
```

ده أثبت إن: الـ Policy اتلقت، والـ Origin مقبول، والكود شغال. وبعدها الهيدر ظهر في الـ Response.

**الدرس:** لما الاختبار الخارجي بيدّي نتيجة غامضة، سطر Log مؤقت جوه الكود بيفصل بين الاحتمالات في تجربة واحدة. وبعدها بنشيله.

---

# 6. تضارب الحد الأدنى للباسورد

## اللي اتكشف

رسالة الـ Validation في الـ Log:

```
The length of 'Password' must be at least 6 characters.
```

يعني الـ Register بيقبل من **6** حروف. لكن `ResetPasswordCommandValidator` (اللي أنا كاتبه) فيه `MinimumLength(8)`.

**النتيجة:** يوزر يسجّل بباسورد 6 حروف، وبعدين مش يقدر يرجّع لنفس الطول في الـ Reset. وجدول اختباراتي القديم كان بيفترض 8 غلط.

## الخيارات

| الخيار | التعديل |
|--------|---------|
| أ. نوحّد على 6 | `ResetPasswordCommandValidator`: `MinimumLength(6)` |
| **ب. نشدّد على 8 (الأنصح)** | Register Validator: `MinimumLength(8)`، و`options.Password.RequiredLength = 8` في Identity |

**ليه 8؟** معيار الحد الأدنى الشائع، ومفيش تكلفة على مشروع تعلّم. والمهم إن **الـ Validator وإعدادات Identity** يكونوا نفس الرقم، وإلا Identity هيرفض بعد ما الـ Validator يقبل.

**القرار لسه ماخدتوش.**

---

# 7. اللي لسه ناقص

| # | البند | ملاحظة |
|---|-------|--------|
| 1 | شيل سطور `TEMP DEBUG` من `GlobalExceptionHandler` | الـ `LogWarning` جوه الـ `if` والـ `else` بتاعه |
| 2 | شيل `http://localhost:5124` من `appsettings.json` | كان للاختبار بس |
| 3 | قرار الباسورد (6 ولا 8) | |
| 4 | اختبار الـ 409 (بتعليق فحص التكرار مؤقتاً) | |
| 5 | **د:** إعادة التسمية بالـ IDE | `IProdcutRepository`، `ProdcutFilter`، `GetPagedAsunc`، `GetByCustomerId`، ومسح `GetByProductIdAsync` |
| 6 | اختبار الـ Review (14 حالة) | لسه ما وصلنيش |
| 7 | بعدها: Domain Unit Tests | |

---

# 8. النسخة النهائية من `GlobalExceptionHandler` (بعد شيل الـ Debug)

```csharp
using Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace API.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            int status;
            string title;
            string details;
            string? code = null;

            switch (exception)
            {
                case ValidationException ex:
                    status = StatusCodes.Status400BadRequest;
                    title = "Validation failed";
                    details = ex.Message;
                    code = "VALIDATION_ERROR";
                    _logger.LogWarning("Validation failed: {Message}", ex.Message);
                    break;

                case DomainException ex:
                    status = StatusCodes.Status400BadRequest;
                    title = "Business rule violated";
                    details = ex.Message;
                    code = ex.Code;
                    _logger.LogWarning("Domain rule violated [{Code}]: {Message}", ex.Code, ex.Message);
                    break;

                // MUST stay before the general DbUpdateException case below
                case DbUpdateConcurrencyException ex:
                    status = StatusCodes.Status409Conflict;
                    title = "Conflict";
                    details = "The data was changed by another request. Please retry.";
                    code = "CONCURRENCY_CONFLICT";
                    _logger.LogWarning(ex, "Concurrency conflict");
                    break;

                // SQL Server: 2601 = duplicate in unique index, 2627 = unique constraint violation
                case DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } ex:
                    status = StatusCodes.Status409Conflict;
                    title = "Conflict";
                    details = "A record with the same unique value already exists.";
                    code = "DUPLICATE_RESOURCE";
                    _logger.LogWarning(ex, "Unique constraint violation");
                    break;

                default:
                    status = StatusCodes.Status500InternalServerError;
                    title = "Server error";
                    details = "An unexpected error occurred.";
                    _logger.LogError(exception, "Unhandled exception for user {UserId}",
                        httpContext.User.FindFirst("sub")?.Value ?? "anonymous");
                    break;
            }

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = details,
                Instance = httpContext.Request.Path,
            };
            if (code is not null)
            {
                problem.Extensions["code"] = code;
            }

            // The exception middleware clears response headers (including CORS): re-apply them
            var corsService = httpContext.RequestServices.GetRequiredService<ICorsService>();
            var corsProvider = httpContext.RequestServices.GetRequiredService<ICorsPolicyProvider>();
            var corsPolicy = await corsProvider.GetPolicyAsync(httpContext, "Frontend");

            if (corsPolicy is not null)
            {
                var corsResult = corsService.EvaluatePolicy(httpContext, corsPolicy);
                corsService.ApplyResult(corsResult, httpContext.Response);
            }

            httpContext.Response.StatusCode = status;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }
    }
}
```

**تنبيه صغير:** اسم الـ Policy `"Frontend"` مكتوب هنا كنص ثابت. لو غيّرته في `Program.cs`، غيّره هنا كمان (أو حطه في Constant في Core).

---

# ملخص في سطور

- **409:** تعارض الـ Unique بقى 409 بكود `DUPLICATE_RESOURCE` بدل 500. لسه ما اتجرّبش.
- **CORS:** اشتغل، واكتشفنا إن الـ Exception Middleware بيمسح هيدراته، فأعدنا تطبيقه جوه الـ Handler. اتأكدنا بـ Log مؤقت وبالهيدر الفعلي.
- **UserId:** كل سطر Request Log بيقول مين عمل الطلب (`anonymous` لو مفيش توكن).
- **الباسورد:** لقينا تضارب 6 مقابل 8 بين Register و Reset، ومحتاج قرارك.
- **الجاي:** إعادة التسمية، بعدين Domain Unit Tests.
