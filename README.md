# 👗 OutfitApp — AI-Powered Digital Wardrobe

OutfitApp is a smart digital wardrobe application that uses **Azure OpenAI GPT-4o Vision** to automatically analyze and tag your clothing items. Upload a photo, let AI identify the category, color, material, style, and more — then save it to your wardrobe.

## 🚀 Features

- **📸 AI Clothing Analysis** — Upload a photo, GPT-4o Vision analyzes it and suggests tags (category, color, material, pattern, style, fit, occasion, brand)
- **🏷️ Editable Tags** — AI fills in the tags, you can edit/add before saving
- **📂 Digital Wardrobe** — Browse, filter, and manage your clothing collection
- **🔍 Smart Filters** — Filter by category, season, style
- **📊 Dashboard Stats** — See wardrobe statistics at a glance
- **📱 Responsive Design** — Works on desktop and mobile

## 🏗️ Architecture

```
OutfitApp.sln
├── OutfitApp.API            → ASP.NET Core Web API + Frontend (wwwroot)
├── OutfitApp.Core           → Entities, Enums, DTOs, Interfaces
└── OutfitApp.Infrastructure → DbContext, Azure Services (Blob, OpenAI)
```

## ☁️ Azure Services Used

| Service | Purpose |
|---|---|
| **Azure SQL Database** | Store clothing items, users, outfits |
| **Azure Blob Storage** | Store clothing images |
| **Azure OpenAI (GPT-4o)** | AI image analysis |

## 🛠️ Setup

### Prerequisites
- .NET 9 SDK
- Azure subscription with:
  - Azure SQL Database
  - Azure Blob Storage account
  - Azure OpenAI resource with GPT-4o deployment

### Configuration
1. Copy `OutfitApp.API/appsettings.Example.json` → `OutfitApp.API/appsettings.json`
2. Fill in your Azure credentials

### Run
```bash
# Restore packages
dotnet restore

# Apply database migrations
dotnet ef database update --project OutfitApp.Infrastructure --startup-project OutfitApp.API

# Run the app
dotnet run --project OutfitApp.API
```

Open `http://localhost:5000` in your browser.

## 📡 API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/wardrobe/analyze` | Upload photo → AI analysis (no DB save) |
| `POST` | `/api/wardrobe/save` | Save user-edited item to DB |
| `GET` | `/api/wardrobe/items` | List all wardrobe items (with filters) |
| `GET` | `/api/wardrobe/items/{id}` | Get single item |
| `DELETE` | `/api/wardrobe/items/{id}` | Delete item (DB + Blob) |
| `DELETE` | `/api/wardrobe/cancel/{blob}` | Cancel upload (delete blob only) |

## 🔜 Roadmap

- [ ] 👔 Outfit/Combination creator
- [ ] 🤖 AI outfit suggestions
- [ ] 📅 Outfit calendar
- [ ] 👤 User profiles & style preferences
- [ ] 🌤️ Weather-based suggestions
- [ ] 📱 Mobile app (MAUI/React Native)

## 📄 License

MIT
