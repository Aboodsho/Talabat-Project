using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data;

public static class StoreCatalogSeeder
{
    private sealed record CategorySeed(string Name, string Description, string ImageUrl);
    private sealed record ProductSeed(string Name, string Description, decimal Price, int Stock, string Category);

    private static readonly CategorySeed[] Categories =
    [
        new("Health & Beauty", "Personal care and wellness essentials.", "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=900&auto=format&fit=crop&q=80"),
        new("مكملات غذائية", "منتجات غذائية لدعم نمط حياة نشط.", "https://images.unsplash.com/photo-1593095948071-474c5cc2989d?w=900&auto=format&fit=crop&q=80"),
        new("Automotive", "Practical accessories for travel and everyday driving.", "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=900&auto=format&fit=crop&q=80"),
        new("Electronics", "Useful electronics for training, travel, and daily life.", "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=900&auto=format&fit=crop&q=80"),
        new("Sports & Outdoors", "Gear for outdoor activity, sport, and recreation.", "https://images.unsplash.com/photo-1530549387789-4c1017266635?w=900&auto=format&fit=crop&q=80"),
        new("إكسسوارات رياضية", "إكسسوارات عملية للتمرين اليومي.", "https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=900&auto=format&fit=crop&q=80"),
        new("ملابس رياضية", "ملابس مريحة للتمرين والجري.", "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=900&auto=format&fit=crop&q=80"),
        new("Clothing & Apparel", "Comfortable everyday clothing and activewear.", "https://images.unsplash.com/photo-1552902865-b72c031ac5ea?w=900&auto=format&fit=crop&q=80"),
        new("Toys & Games", "Playful indoor and outdoor games for the family.", "https://images.unsplash.com/photo-1596461404969-9ae70f2830c1?w=900&auto=format&fit=crop&q=80"),
        new("معدات التمرين", "معدات أساسية للتمرين في المنزل أو النادي.", "https://images.unsplash.com/photo-1583454110551-21f2fa2afe61?w=900&auto=format&fit=crop&q=80")
    ];

    private static readonly List<ProductSeed> Products = BuildProducts();

    private static List<ProductSeed> BuildProducts()
    {
        var products = new List<ProductSeed>();
        Add(products, "Health & Beauty",
            ("Cooling Sports Towel", "Quick-drying towel for training and recovery.", 180m, 45),
            ("Sensitive Skin Body Wash", "Gentle body wash for daily use after exercise.", 260m, 32),
            ("Sweat-Resistant Sunscreen", "Lightweight sun protection for outdoor workouts.", 390m, 28),
            ("Refreshing Shampoo", "Everyday shampoo with a clean, fresh finish.", 240m, 34),
            ("Muscle Massage Balm", "Warming balm for post-workout massage.", 310m, 25),
            ("Daily Face Moisturizer", "Light moisturizer for daily skin care.", 330m, 24),
            ("Travel Grooming Kit", "Compact personal-care kit for your gym bag.", 420m, 18),
            ("Sport Deodorant", "Long-lasting freshness for busy training days.", 195m, 40),
            ("Hand Care Cream", "Moisturizing hand cream for everyday use.", 160m, 30),
            ("Reusable Cleansing Wipes", "Soft reusable wipes for quick cleanups.", 145m, 38));

        Add(products, "مكملات غذائية",
            ("شيكر بروتين", "شيكر محكم الإغلاق سهل التنظيف.", 210m, 50),
            ("عبوة بروتين تجريبية", "عبوة تجريبية لإضافتها إلى كتالوج المتجر.", 1250m, 15),
            ("Whey Protein Blend", "Whey protein blend for post-workout shakes.", 1490m, 22),
            ("Creatine Monohydrate", "Unflavored creatine powder for training routines.", 890m, 26),
            ("Electrolyte Drink Mix", "Single-serve hydration mix for active days.", 360m, 40),
            ("Pre-Workout Formula", "Pre-workout powder with a citrus flavor.", 780m, 20),
            ("Protein Bar Box", "Box of individually wrapped protein bars.", 520m, 30),
            ("BCAA Powder", "Fruit-flavored amino acid drink mix.", 690m, 18),
            ("Mass Gainer Blend", "Calorie-dense shake mix for meal planning.", 1680m, 12),
            ("Daily Vitamin Pack", "Daily vitamin supplement pack.", 430m, 24));

        Add(products, "Automotive",
            ("Car Phone Mount", "Adjustable phone mount for the dashboard.", 340m, 35),
            ("Trunk Organizer", "Foldable organizer for keeping gear in the car.", 490m, 22),
            ("Portable Car Vacuum", "Compact vacuum for quick interior cleanups.", 1250m, 12),
            ("Seat Gap Organizer", "Keeps small essentials within easy reach.", 280m, 30),
            ("Microfiber Car Towel Set", "Soft microfiber towels for vehicle care.", 230m, 40),
            ("Steering Wheel Cover", "Textured cover for a comfortable grip.", 390m, 18),
            ("USB Car Charger", "Dual-port charger for compatible devices.", 260m, 35),
            ("Car Air Freshener", "Compact freshener for everyday driving.", 120m, 50),
            ("Waterproof Seat Protector", "Protects a seat after outdoor activities.", 550m, 16),
            ("Reflective Safety Vest", "High-visibility vest for roadside emergencies.", 320m, 20));

        Add(products, "Electronics",
            ("Wireless Earbuds", "Wireless earbuds for music during workouts.", 1190m, 20),
            ("Fitness Smart Watch", "Smart watch with activity and workout tracking.", 2450m, 14),
            ("USB-C Fast Charge Cable", "Durable cable for compatible USB-C devices.", 190m, 45),
            ("Portable Bluetooth Speaker", "Compact speaker for indoor and outdoor use.", 980m, 18),
            ("Power Bank 10000mAh", "Portable battery for charging on the go.", 790m, 24),
            ("Digital Interval Timer", "Easy-to-read timer for training intervals.", 650m, 17),
            ("Phone Tripod Stand", "Adjustable tripod for workout recording.", 480m, 25),
            ("Wireless Charging Pad", "Convenient wireless charging pad.", 570m, 20),
            ("Digital Body Scale", "Digital scale with a clear display.", 690m, 16),
            ("Clip-On Reading Light", "Rechargeable clip-on light for travel.", 260m, 28));

        Add(products, "Sports & Outdoors",
            ("Trekking Backpack 30L", "Lightweight backpack for day hikes and travel.", 1390m, 16),
            ("Compact Camping Lantern", "Rechargeable lantern for camping trips.", 520m, 22),
            ("Outdoor Football", "Durable football for recreational play.", 460m, 30),
            ("Basketball Size 7", "All-surface basketball for outdoor courts.", 590m, 24),
            ("Yoga Mat", "Non-slip mat for stretching and floor exercise.", 620m, 25),
            ("Running Hydration Belt", "Lightweight belt for carrying water on runs.", 410m, 20),
            ("Cycling Helmet", "Adjustable helmet for recreational cycling.", 1150m, 14),
            ("Foldable Picnic Blanket", "Easy-to-carry blanket for outdoor breaks.", 480m, 19),
            ("Portable Sports Cones", "Training cones for drills and field games.", 260m, 35),
            ("Insulated Travel Flask", "Insulated flask for outdoor activities.", 370m, 27));

        Add(products, "إكسسوارات رياضية",
            ("زجاجة مياه رياضية", "زجاجة قابلة لإعادة الاستخدام بسعة مناسبة للتمرين.", 240m, 75),
            ("حزام رفع أثقال", "حزام دعم لتمارين رفع الأثقال.", 590m, 20),
            ("قفازات رفع الأثقال", "قفازات مريحة لتحسين القبضة أثناء التدريب.", 390m, 32),
            ("أربطة دعم المعصم", "أربطة مرنة لدعم المعصم أثناء التمرين.", 260m, 28),
            ("حقيبة جيم متعددة الجيوب", "حقيبة واسعة لحمل ملابس ومعدات التمرين.", 890m, 16),
            ("واقيات ركبة رياضية", "واقيات مرنة لدعم الركبة أثناء الحركة.", 430m, 22),
            ("أشرطة رفع الأثقال", "أشرطة قبضة لتمارين السحب والرفع.", 210m, 40),
            ("منشفة جيم سريعة الجفاف", "منشفة خفيفة مناسبة للنادي والسفر.", 190m, 45),
            ("رباط رأس رياضي", "رباط مريح لامتصاص العرق أثناء التدريب.", 140m, 38),
            ("قفل خزانة رياضي", "قفل صغير لحفظ الأغراض في خزانة النادي.", 170m, 26));

        Add(products, "ملابس رياضية",
            ("قميص رياضي سريع الجفاف", "قميص خفيف ومريح للتمرين اليومي.", 450m, 40),
            ("بنطال تدريب مرن", "بنطال مرن مناسب لتمارين القوة والجري.", 720m, 25),
            ("ليغنز رياضي مرن", "ليغنز مرن مناسب للتمارين والحركة اليومية.", 680m, 26),
            ("شورت تدريب خفيف", "شورت مريح للتمرين والجري.", 390m, 35),
            ("هودي رياضي", "هودي مريح للإحماء والارتداء اليومي.", 850m, 18),
            ("طقم رياضي قطعتين", "طقم عملي للتمارين والأنشطة اليومية.", 1090m, 14),
            ("قميص ضغط رياضي", "قميص مرن مناسب لتمارين القوة.", 570m, 24),
            ("بنطال جوجر رياضي", "بنطال جوجر بقصة مريحة للحركة.", 740m, 20),
            ("تيشيرت بلا أكمام", "تيشيرت خفيف للتمرين في الأجواء الدافئة.", 360m, 30),
            ("جوارب تدريب مبطنة", "جوارب مريحة للاستخدام الرياضي اليومي.", 180m, 50));

        Add(products, "Clothing & Apparel",
            ("Classic Cotton T-Shirt", "Soft cotton tee for everyday wear.", 380m, 32),
            ("Lightweight Zip Jacket", "Light jacket for warmups and casual wear.", 920m, 17),
            ("Everyday Polo Shirt", "Classic polo shirt with a comfortable fit.", 640m, 21),
            ("Relaxed Fit Joggers", "Relaxed joggers for travel and daily use.", 710m, 19),
            ("Long Sleeve Base Layer", "Stretch base layer for cool-weather activity.", 560m, 24),
            ("Training Leggings", "Flexible leggings for training and everyday use.", 650m, 22),
            ("Sleeveless Training Top", "Light sleeveless top for active days.", 340m, 28),
            ("Everyday Hoodie", "Soft hoodie for casual layering.", 890m, 16),
            ("Classic Sports Cap", "Adjustable cap for outdoor activities.", 290m, 34),
            ("Crew Sport Socks", "Comfortable crew socks for daily wear.", 170m, 48));

        Add(products, "Toys & Games",
            ("Kids Mini Basketball Set", "Mini hoop set for indoor play.", 780m, 15),
            ("Family Frisbee", "Lightweight flying disc for outdoor games.", 160m, 40),
            ("Children Jump Rope", "Adjustable skipping rope for active play.", 130m, 35),
            ("Soft Play Football", "Soft football for casual family games.", 290m, 24),
            ("Balance Board Game", "Balance board for coordination practice.", 560m, 12),
            ("Foam Bowling Set", "Soft bowling pins and ball for indoor play.", 430m, 18),
            ("Outdoor Ring Toss", "Portable ring toss set for family fun.", 240m, 23),
            ("Kids Activity Mat", "Padded mat for indoor movement and play.", 690m, 14),
            ("Mini Table Tennis Set", "Portable net and paddles for table tennis.", 510m, 16),
            ("Colorful Cone Set", "Bright cones for simple movement games.", 210m, 30));

        Add(products, "معدات التمرين",
            ("دمبل قابل للتعديل", "دمبل عملي لتمارين القوة في المنزل.", 1850m, 12),
            ("حبل مقاومة", "حبل مقاومة متعدد الاستخدامات للإحماء والتمارين.", 320m, 60),
            ("بنش تمارين قابل للطي", "بنش ثابت قابل للطي لتمارين المنزل.", 2450m, 8),
            ("كيتل بيل حديد", "وزن كيتل بيل لتمارين القوة والتحمل.", 990m, 14),
            ("بار عقلة للباب", "بار تثبيت للباب لتمارين وزن الجسم.", 780m, 12),
            ("مجموعة أوزان حرة", "مجموعة أوزان لتمارين القوة المتنوعة.", 3200m, 6),
            ("لوح تمارين الضغط", "لوح متعدد الوضعيات لتمارين الجزء العلوي.", 540m, 18),
            ("عجلة تمارين البطن", "عجلة متينة لتمارين عضلات الوسط.", 390m, 25),
            ("كرة طبية تدريبية", "كرة طبية للتمارين الوظيفية واللياقة.", 720m, 13),
            ("رول مساج عضلي", "أسطوانة فوم للإحماء والاستشفاء العضلي.", 460m, 20));

        return products;
    }

    private static void Add(
        ICollection<ProductSeed> products,
        string category,
        params (string Name, string Description, decimal Price, int Stock)[] items)
    {
        foreach (var item in items)
            products.Add(new ProductSeed(item.Name, item.Description, item.Price, item.Stock, category));
    }

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        if (Categories.Length != 10 || Products.Count != 100 || Products.GroupBy(p => p.Category).Any(group => group.Count() != 10))
            throw new InvalidOperationException("The sample catalog must contain 10 categories and 10 products per category.");

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var categories = await db.Categories.ToDictionaryAsync(c => c.Name, cancellationToken);
        foreach (var seed in Categories)
        {
            if (categories.ContainsKey(seed.Name)) continue;
            var category = new Category(seed.Name, seed.Description);
            db.Categories.Add(category);
            categories.Add(seed.Name, category);
        }
        await db.SaveChangesAsync(cancellationToken);

        var existingProductNames = await db.Products.Select(p => p.Name).ToListAsync(cancellationToken);
        var existingProducts = existingProductNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var categoryImages = Categories.ToDictionary(c => c.Name, c => c.ImageUrl);
        foreach (var seed in Products)
        {
            if (existingProducts.Contains(seed.Name)) continue;
            var product = new Product(seed.Name, seed.Description, seed.Price, seed.Stock, categories[seed.Category].Id);
            product.AddImage(categoryImages[seed.Category], isMain: true);
            db.Products.Add(product);
            existingProducts.Add(seed.Name);
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
