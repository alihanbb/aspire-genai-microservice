# 🚀 Aspire GenAI Microservices

.NET Aspire ile orchestrate edilen, Generative AI özellikli modern bir microservices projesi. Ollama üzerinden çalışan **Semantic Search**, **AI Chat Support** ve **RAG** özellikleri içerir.

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![Aspire](https://img.shields.io/badge/Aspire-9.0-512BD4)
![Ollama](https://img.shields.io/badge/Ollama-LLM-black?logo=ollama)
![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)
![Redis](https://img.shields.io/badge/Redis-8-DC382D?logo=redis)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3-FF6600?logo=rabbitmq)

---

## 📋 İçindekiler

- [Mimari](#-mimari)
- [Proje Yapısı](#-proje-yapısı)
- [AI Özellikleri](#-ai-özellikleri)
- [Teknoloji Stack](#-teknoloji-stack)
- [Kurulum](#-kurulum)
- [API Endpoints](#-api-endpoints)

---

## 🏗 Mimari

```
┌─────────────────────────────────────────────────────────┐
│                    Aspire AppHost                        │
│                   (Orchestrator)                         │
├─────────────┬──────────────┬───────────────┬────────────┤
│  Catalog    │   Basket     │    Blazor     │   Ollama   │
│  Service    │   Service    │    Web App    │   + Models │
│  (API)      │   (API)      │  (Frontend)   │            │
├─────────────┼──────────────┼───────────────┼────────────┤
│ PostgreSQL  │    Redis     │               │  llama3.2  │
│ (Write+Read)│  (Sentinel)  │               │  all-minilm│
├─────────────┴──────────────┴───────────────┴────────────┤
│                      RabbitMQ                           │
│               (Async Messaging + Outbox)                │
└─────────────────────────────────────────────────────────┘
```

---

## 📁 Proje Yapısı

```
aspire-genai-microservices/
│
├── AspireApps/
│   ├── AspireApps.AppHost/          # 🎯 Aspire Orchestrator
│   │   ├── AppHost.cs               # Tüm servislerin konfigürasyonu
│   │   └── Extensions/              # AppInsights extension
│   │
│   ├── AspireApps.ApiService/       # 📦 Catalog Service (API)
│   │   ├── Endpoint/
│   │   │   └── ProductEndpoint.cs   # Minimal API endpoints
│   │   ├── Entity/
│   │   │   └── Product.cs           # Product entity
│   │   ├── Services/
│   │   │   ├── ProductService.cs    # CRUD + Search
│   │   │   └── ProductAIService.cs  # 🤖 AI Search + Chat Support
│   │   ├── Data/
│   │   │   ├── ProductDbContext.cs   # EF Core (Write)
│   │   │   ├── ProductReadDbContext.cs # EF Core (Read Replica)
│   │   │   ├── DataSeeder.cs        # Seed data
│   │   │   └── Migrations/          # EF Core migrations
│   │   └── Program.cs              # Service configuration
│   │
│   ├── AspireApps.Web/              # 🌐 Blazor Web App
│   │   ├── Components/Pages/
│   │   │   ├── Products.razor       # Ürün listesi
│   │   │   ├── Search.razor         # 🔍 Normal + AI Search
│   │   │   └── Support.razor        # 💬 AI Chat Support
│   │   └── ApiClients/
│   │       └── CatalogApiClient.cs  # Typed HTTP client
│   │
│   └── AspireApps.ServiceDefaults/  # 🔧 Shared Configuration
│       ├── Extensions.cs            # OpenTelemetry, Health Checks
│       └── Messaging/               # MassTransit config
│
├── AspireApps.BasketService/        # 🛒 Basket Service
│
└── docs/                            # 📚 Documentation
```

---

## 🤖 AI Özellikleri

### 1. Semantic Search (AI Search)
- **Model:** `all-minilm` (Embedding)
- Ürün açıklamalarından embedding vektörleri oluşturur
- InMemory Vector Store ile benzerlik araması yapar
- Doğal dilde arama: *"something for rainy days"* → ilgili ürünleri bulur

### 2. AI Chat Support
- **Model:** `llama3.2` (Chat/LLM)
- Outdoor ürünleri hakkında sorulara context-aware yanıtlar verir
- Ürün kataloğunu system prompt olarak kullanır
- Her yanıtta ilgili ürün önerisi sunar

### 3. Case-Insensitive Smart Search
- PostgreSQL `ILIKE` ile büyük/küçük harf duyarsız arama
- Hem ürün adı hem açıklama üzerinden arama

---

## 🛠 Teknoloji Stack

| Katman | Teknoloji |
|--------|-----------|
| **Orchestration** | .NET Aspire 9.0 |
| **Backend** | .NET 9.0 Minimal API |
| **Frontend** | Blazor Server (Interactive) |
| **AI/LLM** | Ollama + OllamaSharp |
| **AI Framework** | Microsoft.Extensions.AI |
| **Vector Store** | InMemory Vector Store |
| **Database** | PostgreSQL (Write + Read Replica) |
| **Cache** | Redis Sentinel (Master + Replicas) |
| **Messaging** | RabbitMQ + MassTransit |
| **Patterns** | CQRS, Outbox, Saga |
| **ORM** | Entity Framework Core |
| **Auth** | Keycloak (JWT Bearer) |
| **Monitoring** | OpenTelemetry, Application Insights |

---

## 🚀 Kurulum

### Gereksinimler
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [.NET Aspire Workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling)

### Çalıştırma

```bash
# 1. Repo'yu klonlayın
git clone https://github.com/alihanbb/aspire-genai-microservice.git
cd aspire-genai-microservice

# 2. Aspire workload yükleyin (henüz yüklemediyseniz)
dotnet workload install aspire

# 3. Projeyi çalıştırın (Docker otomatik başlar)
dotnet run --project AspireApps/AspireApps.AppHost/AspireApps.AppHost.csproj
```

> **Not:** İlk çalıştırmada Ollama modelleri (`llama3.2` ~2GB, `all-minilm` ~46MB) otomatik indirilir. Aspire Dashboard'dan indirme durumunu takip edebilirsiniz.

### Aspire Dashboard
Uygulama başladıktan sonra, terminaldeki URL'den Aspire Dashboard'a erişebilirsiniz:
```
https://localhost:17094
```

---

## 📡 API Endpoints

### Catalog Service (`/products`)

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| `GET` | `/products` | Tüm ürünleri listele |
| `GET` | `/products/{id}` | ID ile ürün getir |
| `POST` | `/products` | Yeni ürün ekle |
| `PUT` | `/products/{id}` | Ürün güncelle |
| `DELETE` | `/products/{id}` | Ürün sil |
| `GET` | `/products/search/{query}` | 🔍 Normal arama (case-insensitive) |
| `GET` | `/products/aisearch/{query}` | 🤖 AI Semantic Search |
| `GET` | `/products/support/{query}` | 💬 AI Chat Support |

---

## 📊 Design Patterns

- **CQRS:** Write ve Read veritabanı ayrımı (PostgreSQL replicas)
- **Outbox Pattern:** MassTransit ile reliable event publishing
- **Saga Pattern:** Distributed transaction yönetimi
- **Service Discovery:** Aspire resource references
- **Retry & Circuit Breaker:** Polly resilience policies
- **Keyed Services:** Multiple AI client DI registration

---

## 📄 Lisans

Bu proje eğitim ve öğrenme amaçlı oluşturulmuştur.
