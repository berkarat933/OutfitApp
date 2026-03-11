## Plan: OutfitApp – Dolap & Kombin Uygulaması (Azure + .NET + React)

Kullanıcıların kıyafetlerini dijital bir dolaba yükleyip kategorize edebildiği, bu kıyafetlerden kombin oluşturup kaydedebildiği bir web uygulaması. Backend .NET 8 Web API, frontend React (TypeScript), altyapı Azure (App Service, SQL Database, Blob Storage) üzerinde çalışacak. Mimari, ileride mobil (React Native / MAUI) eklemeye uygun olacak şekilde API-first tasarlanacak.

### Steps

1. **Solution yapısını oluştur** – Boş olan `OutfitApp.sln` içine 3 proje ekle:
   - `OutfitApp.API` → ASP.NET Core 8 Web API (backend)
   - `OutfitApp.Core` → Class Library (entity, DTO, interface tanımları)
   - `OutfitApp.Infrastructure` → Class Library (EF Core DbContext, Azure servis entegrasyonları)
   - `OutfitApp.Web` → React + TypeScript frontend (solution dışı `/client` klasörü veya aynı repo içinde)

2. **Domain modellerini tasarla** (`OutfitApp.Core` içinde):
   - `User` → Id, Email, DisplayName, AvatarUrl
   - `ClothingItem` → Id, UserId, Name, Category (enum: Üst, Alt, Ayakkabı, Aksesuar, DışGiyim), Color, Season, ImageUrl, CreatedAt
   - `Outfit` → Id, UserId, Name, Occasion (enum: Günlük, İş, Özel), CreatedAt
   - `OutfitItem` → OutfitId, ClothingItemId (many-to-many join)
   - İlgili enum'lar: `ClothingCategory`, `Season`, `Occasion`

3. **Azure altyapısını kur**:
   - **Azure SQL Database** → EF Core ile kullanılacak (connection string `appsettings.json` + Azure Key Vault)
   - **Azure Blob Storage** → Kıyafet fotoğrafları için bir `clothing-images` container'ı; SAS token ile erişim
   - **Azure App Service** → API deployment (Linux plan, .NET 8)
   - **Azure Static Web Apps** veya ikinci bir App Service → React frontend hosting
   - Opsiyonel: **Azure AD B2C** veya **ASP.NET Identity** ile kimlik doğrulama

4. **API endpoint'lerini geliştir** (`OutfitApp.API` içinde):
   - `POST /api/auth/register` & `POST /api/auth/login` → JWT tabanlı auth
   - `GET/POST/PUT/DELETE /api/wardrobe/items` → Kıyafet CRUD (fotoğraf upload dahil → Blob Storage'a yükle)
   - `GET /api/wardrobe/items?category=&season=` → Filtreleme
   - `GET/POST/PUT/DELETE /api/outfits` → Kombin CRUD
   - `POST /api/outfits/{id}/items` → Kombine kıyafet ekleme/çıkarma

5. **React frontend'i geliştir** (`OutfitApp.Web` / `/client`):
   - **Sayfa yapısı**: Login/Register → Dashboard → Dolabım (grid görünümü, kategori filtresi) → Kombin Oluştur (drag & drop veya seçim ile) → Kombinlerim (kaydedilmiş kombinler listesi)
   - Kıyafet ekleme formu: Fotoğraf yükleme, kategori/renk/mevsim seçimi
   - Kombin oluşturma ekranı: Dolaptaki kıyafetlerden seçerek bir kombin kartı oluşturma
   - State management: React Context veya Zustand; API iletişimi: Axios/fetch + JWT interceptor

6. **Azure'a deploy et**:
   - API → Azure App Service'e CI/CD (GitHub Actions veya Azure DevOps)
   - Frontend → Azure Static Web Apps'e deploy
   - Veritabanı migration'larını Azure SQL'e uygula (`dotnet ef database update`)

### Further Considerations

1. **Kimlik doğrulama tercihi** → ASP.NET Identity (basit, hızlı kurulum) mi yoksa Azure AD B2C (kurumsal, sosyal login desteği) mi tercih edilecek? → İlk aşama için ASP.NET Identity + JWT öneriyorum.
2. **AI destekli kombin önerisi** → İleride Azure OpenAI veya Custom Vision ile "dolabındaki kıyafetlerden otomatik kombin öner" özelliği eklenebilir; şu an modelleri buna uygun tasarlamak yeterli.
3. **Mobil genişleme stratejisi** → API-first mimari sayesinde ileride React Native veya .NET MAUI ile aynı backend'i kullanarak mobil uygulama eklenebilir; bu yüzden API'yi RESTful ve iyi dökümante (Swagger/OpenAPI) tutmak kritik.
