# نظام الإيميل: اللي اتعمل + شرح طابور الإرسال في الخلفية

---

## 1. الصورة الكبيرة

```
Register / Forgot / Resend
        |
        v
   Command Handler  (Service layer)
        |
        v
   IEmailService  (interface in Service)
        |
        +--> ConsoleEmailService   (dev: prints to log)
        |
        +--> SmtpEmailService      (real: MailKit -> Gmail)
```

- الـ Handler مبيعرفش هل الإيميل بيتبعت فعلاً ولا بيتطبع في الـ Console.
- ده بسبب إن `IEmailService` Interface في الـ Service، والتنفيذ في الـ Infrastructure.
- نبدّل بين الاتنين بإعداد واحد: `Email:Provider`.

---

## 2. الملفات حسب الطبقة

| الطبقة | الملف | الدور |
|--------|-------|-------|
| Service | `IEmailService` | العقد: `SendEmailConfirmationAsync` و`SendPasswordResetAsync` |
| Service | Commands: Register / ResendConfirmationEmail / ForgotPassword | بتنده على الـ Interface |
| Infrastructure | `ConsoleEmailService` | بيطبع اللينك في الـ Log (للتطوير) |
| Infrastructure | `SmtpEmailService` | بيبعت بـ MailKit |
| Infrastructure | `SmtpSettings` | إعدادات الـ SMTP (`Email:Smtp`) |
| Infrastructure | `EmailTemplates` | قوالب HTML + نص عادي |
| Infrastructure | `FrontendSettings` | `Frontend:BaseUrl` لبناء اللينك |
| Infrastructure | `DependencyInjection` | اختيار الـ Provider |
| API | `RateLimitingExtensions` | حماية `forgot-password` و`resend-confirmation` |

---

## 3. رحلة كل إيميل

### تأكيد الإيميل

```
Register
  -> create user (EmailConfirmed = false)
  -> create confirmation token
  -> send email with link
  -> user opens link -> Frontend calls POST /api/auth/confirm-email
  -> EmailConfirmed = true
```

- الـ Login مبيشتغلش قبل التأكيد (403 `EmailNotConfirmed`).
- `confirm-email` Idempotent: لو اتبعت تاني على حساب متأكد، بيرجع 204.

### نسيت الباسورد

```
forgot-password
  -> always return 200 (same message)
  -> if account exists AND confirmed -> send reset email
reset-password
  -> validate token + new password
  -> SecurityStamp rotates (token dies)
  -> lockout is cleared
```

### إعادة إرسال التأكيد

- بيرجع 200 دايماً.
- بيبعت بس لو الحساب موجود وغير متأكد.

---

## 4. قرارات الأمان

| القرار | ليه |
|--------|-----|
| `forgot-password` و`resend` يرجعوا 200 دايماً | عشان المهاجم ميعرفش مين مسجّل عندنا (User Enumeration) |
| إيميل Reset للحسابات المتأكدة بس | الحساب غير المتأكد معناه إن ملكية الإيميل لسه مش مثبتة |
| `SmtpEmailService` مبيرميش Exception | لو رمى، الرد هيختلف للحساب الموجود، ويكشفه |
| الفشل بيتسجل في الـ Log بس | نشوفه إحنا، العميل لأ |
| الإيميل في الـ Log مقنّع (`s***@gmail.com`) | الـ Logs بتتنقل وبتتشارك |
| مفيش Token ولا لينك في الـ Log (في وضع Smtp) | الـ Token = مفتاح الحساب |
| `HtmlEncode` للاسم واللينك | لو الاسم فيه `<script>`، مينفذش جوه الإيميل |
| الـ Token بيتحرق بعد الاستخدام | تغيير الـ SecurityStamp |
| الأسرار في user-secrets | مش في Git |

---

## 5. الإعدادات

### `appsettings.json` (من غير أسرار)

```json
"Email": {
  "Provider": "Console",
  "Smtp": { "Host": "smtp.gmail.com", "Port": 587, "FromName": "E-Commerce" }
}
```

### user-secrets (على جهازك بس)

```json
"Email": {
  "Provider": "Smtp",
  "Smtp": {
    "Username": "your-account@gmail.com",
    "Password": "<app-password>",
    "FromEmail": "your-account@gmail.com"
  }
}
```

- الـ secrets بتغلب الـ appsettings.
- `ValidateOnStart`: لو قيمة ناقصة، التطبيق مبيشتغلش، وده أحسن من إنك تكتشف وقت الإرسال.
- `Provider` غلط إملائي = Exception واضح عند التشغيل.

---

## 6. تفاصيل الـ SMTP (MailKit)

| النقطة | التفاصيل |
|--------|----------|
| Port 587 | STARTTLS: يبدأ عادي ثم يتشفّر |
| `SecureSocketOptions.Auto` | MailKit يختار الأنسب للـ Port |
| `SmtpClient` جديد لكل إيميل | مش Thread-safe، فمنشاركوش |
| `Timeout = 15s` | عشان طلب ميعلقش للأبد |
| App Password | مش باسورد الحساب، ودي بتتلغى لوحدها |
| حد Gmail | حوالي 500 إيميل في اليوم، كفاية للتعلم بس |

---

## 7. الـ Rate Limiting على الإيميل

| Endpoint | Policy | الحد |
|----------|--------|------|
| `login` | `auth-login` | 5 في الدقيقة |
| `forgot-password`، `resend-confirmation` | `auth-email` | 3 كل 15 دقيقة |
| `register` | `auth-register` | 5 في الساعة |
| كل الـ API | Global | 100 في الدقيقة |

- بيتقسم بالـ IP.
- الرد: 429 + `{"code": "RATE_LIMITED"}` + هيدر `Retry-After`.
- اكتشفنا في الاختبار إن الـ Sliding Window مبيرجعش `Retry-After` لوحده. حليناها بقاموس `PolicyWindows` بيحسب من نافذة الـ Policy (القيمة حد أقصى).
- `UseRateLimiter` بعد `UseCors` عشان الـ 429 يحتفظ بهيدرز الـ CORS.

---

## 8. حالة الاختبارات

| # | الاختبار | الحالة |
|---|----------|--------|
| 1 | Console Provider | تمام |
| 2 | إرسال حقيقي لـ Gmail | تمام |
| 3 | `confirm-email` بالـ Token من الإيميل | تمام (204) |
| 4 | إيميل Reset + `reset-password` | تمام |
| 5 | باسورد SMTP غلط يرجع 200 مش 500 | **لسه** |
| 6 | `Password` ناقص يمنع التشغيل | **لسه** |
| 7 | `<b>Test</b>` يظهر نص حرفي | **لسه** |
| 8 | مفيش Token ولا لينك في الـ Log | **لسه** |
| 10 | 429 على `login` | تمام |
| 12 | 429 على `forgot-password` + `Retry-After: 900` | تمام |
| 13 | `Retry-After` موجود | تمام (بعد الإصلاح) |
| 14 | Log بالـ IP | تمام |

> الزرار في الإيميل بيفتح `localhost:4200` وبيدّي "This site can't be reached"، لأن Angular لسه مش موجود. ده متوقع مش عيب.

---

## 9. نقاط الضعف المعروفة

| # | الضعف | الحل |
|---|-------|------|
| 1 | **Timing Leak**: الحساب الموجود بياخد ثواني (SMTP)، وغير الموجود فوري | طابور الخلفية (القسم 10) |
| 2 | الـ API بيستنى الـ SMTP ثواني قبل ما يرد | نفس الحل |
| 3 | مفيش حد لكل إيميل، بس لكل IP | Cooldown في الـ Handler |
| 4 | الـ JWT يفضل شغال بعد Reset (لحد ما ينتهي في 30 دقيقة) | مقبول للتعلم |
| 5 | الـ Rate Limit في الذاكرة ويتصفّر مع Restart | Redis لو أكتر من سيرفر |
| 6 | الـ IP ورا Proxy هيبقى IP الـ Proxy | `ForwardedHeaders` عند الـ Deploy |
| 7 | Gmail مش للإنتاج | SendGrid / Mailgun / Amazon SES |

---

## 10. طابور الإرسال في الخلفية (Background Email Queue)

### 10.1 المشكلة

```
Without queue:

Request --> Handler --> SMTP (2-5 seconds) --> Response
                              ^
                    the user waits here
```

- **الأول: بطء.** المستخدم بيستنى الـ SMTP جوه الـ Request.
- **التاني: Timing Leak.**

```
forgot-password(existing email)      = ~3000 ms  (SMTP call)
forgot-password(non-existing email)  = ~5 ms     (nothing to send)
```

- الرد نفسه (200 + نفس الرسالة)، بس المهاجم بيقيس الوقت.
- لو الرد أبطأ، يبقى الإيميل **مسجّل عندنا**.
- ده بيكسر كل اللي عملناه عشان نخفي وجود الحسابات.

### 10.2 الفكرة

```
With queue:

Request --> Handler --> put job in queue --> Response (fast)
                              |
                              v
                       Background worker --> SMTP (slow, nobody waits)
```

- الـ Handler يحط "شغلانة" في طابور ويرجع فوراً.
- شغّال تاني (Worker) شغّال في الخلفية بياخد الشغلانات ويبعتها.
- الـ Request مبقاش مرتبط بسرعة الـ SMTP.

### 10.3 المكونات

| المكوّن | الدور | الـ Lifetime |
|---------|-------|--------------|
| `EmailJob` (record) | بيانات الشغلانة: النوع، الإيميل، الاسم، userId، token | بيانات بس |
| `Channel<EmailJob>` | الطابور نفسه، آمن للـ Threads | **Singleton** |
| `QueuedEmailService : IEmailService` | بيكتب في الـ Channel بس | Scoped |
| `EmailSenderWorker : BackgroundService` | بيقرا من الـ Channel ويبعت | Singleton (Hosted) |
| `SmtpEmailService` | الإرسال الفعلي (زي ما هو) | Scoped |

### 10.4 الشكل بعد التعديل

```
Handler
  |
  v
IEmailService  = QueuedEmailService
  |  writes EmailJob
  v
Channel<EmailJob>   (Singleton, in memory)
  |
  v
EmailSenderWorker (BackgroundService)
  |  create scope per job
  v
SmtpEmailService --> Gmail
```

- **الـ Handlers متتغيرش خالص.** لسه بينده على `IEmailService`.
- ده بيثبت قيمة الـ Interface اللي عملناه من الأول.
- بنبدّل `IEmailService` من `SmtpEmailService` لـ `QueuedEmailService`، و`SmtpEmailService` بيبقى جوه الـ Worker.

### 10.5 ليه `Channel` مش `List` أو `Queue`

| الخيار | المشكلة |
|--------|---------|
| `List` / `Queue` عادي | مش Thread-safe، يحتاج Locks |
| `ConcurrentQueue` | آمن، بس الـ Worker لازم يعمل Polling (Loop + Delay) |
| **`Channel<T>`** | آمن، والـ Worker بينتظر **Async** من غير ما يستهلك CPU، ويصحى لما فيه شغلانة |

### 10.6 Bounded ولا Unbounded

| النوع | المعنى | المخاطرة |
|-------|--------|----------|
| Unbounded | طابور بلا حد | لو SMTP وقع، الذاكرة تتملي |
| **Bounded (مثلاً 100)** | حد أقصى | لازم تقرر: تستنى، أو ترفض، أو ترمي الأقدم |

- توصيتي: **Bounded** بـ `FullMode = DropWrite`، ونسجل Warning.
- ده نفس مبدأ `QueueLimit = 0` في الـ Rate Limiter: بنرفض بدل ما نكدّس.

### 10.7 مشكلة الـ Scope (فخ شائع)

```
Worker  = Singleton
SmtpEmailService = Scoped      <-- cannot inject Scoped into Singleton
```

- الحل: الـ Worker بياخد `IServiceScopeFactory`، ولكل شغلانة بيعمل `CreateScope()` ويجيب منه الخدمة.
- أو الأبسط: نخلي `SmtpEmailService` نفسه Singleton، لأنه مبيعتمدش على DbContext ولا حاجة Scoped (بس Options و Logger).

### 10.8 الفشل وإعادة المحاولة

| السيناريو | السلوك المقترح |
|-----------|---------------|
| فشل SMTP مؤقت (شبكة) | إعادة محاولة 2-3 مرات بتأخير متزايد (Backoff) |
| فشل دائم (باسورد غلط) | Log Error وتجاهل الشغلانة |
| Exception غير متوقع | الـ Worker **لازم** يمسكه ويكمل، مينهارش |
| التطبيق وقف وفيه شغلانات في الطابور | **بتضيع** (دي حدود الذاكرة) |
| إيقاف نظيف (Shutdown) | الـ Worker يخلّص اللي في إيده، يحترم `CancellationToken` |

### 10.9 الحدود الصريحة

| النقطة | الواقع |
|--------|--------|
| الطابور في الذاكرة | Restart = ضياع الإيميلات اللي لسه ماتبعتتش |
| مفيش تأكيد توصيل للمستخدم | لو فشل، هو مش هيعرف، يطلب تاني |
| Timing Leak اتقفل **تقريباً** | لسه فيه فرق ملي ثانية: الـ Handler بيعمل بحث وبيولّد Token للحساب الموجود فقط. ده فرق ضئيل جداً قدام ثواني الـ SMTP، لكنه مش صفر |
| مفيش ترتيب مضمون مع أكتر من Worker | مش مشكلة هنا |

### 10.10 البديل للإنتاج

| الأداة | الفايدة |
|--------|---------|
| **Outbox Pattern** | الشغلانة تتحفظ في جدول DB جوه نفس الـ Transaction، فمتضيعش مع Restart |
| Hangfire / Quartz | Retry و Dashboard و Persistence جاهزين |
| RabbitMQ / Azure Service Bus | طابور حقيقي بين خدمات |
| مزوّد جاهز (SendGrid...) | بيعمل الـ Retry والتتبع عنده |

**رأيي كمهندس:**
- للتعلم: الـ `Channel` + `BackgroundService` ممتاز، بيعلّمك Hosted Services و Threading و Lifetimes.
- للإنتاج الحقيقي: Outbox أو Hangfire.
- مفيش Over-engineering هنا، لأن الـ Timing Leak مشكلة أمنية حقيقية، مش تجميل.

### 10.11 خطة التنفيذ (لما تقول "make code")

1. `EmailJob` + نوع الإيميل (`enum EmailKind`).
2. `EmailQueue` (غلاف حوالين `Channel` Bounded) كـ Singleton.
3. `QueuedEmailService : IEmailService`.
4. `EmailSenderWorker : BackgroundService` مع Retry و Try/Catch.
5. تعديل الـ DI: `IEmailService` ← `QueuedEmailService` لما `Provider = Smtp`.
6. Console Provider يفضل زي ما هو (مباشر، للتطوير).
7. اختبار: قياس زمن `forgot-password` لحساب موجود وغير موجود قبل وبعد.

### 10.12 الاختبار المتوقع

| # | السيناريو | المتوقع |
|---|-----------|---------|
| 1 | `forgot-password` لحساب موجود | رد في ملي ثواني، والإيميل يوصل بعدها بثواني |
| 2 | نفس الطلب لحساب غير موجود | نفس الزمن تقريباً |
| 3 | باسورد SMTP غلط | 200 فوراً، و Error في الـ Log من الـ Worker |
| 4 | إيقاف التطبيق بعد طلب | شغلانات الطابور بتضيع (متوقع) |
| 5 | 150 طلب بسرعة (مع إيقاف الـ Rate Limit مؤقتاً) | الزيادة عن حد الطابور بتتسجّل Warning ومفيش Crash |
