# 🚀 Aspire GenAI Microservices

.NET Aspire ile orchestrate edilen, Generative AI özellikli modern bir microservices projesi. Ollama üzerinden çalışan **Semantic Search**, **AI Chat Support** ve **RAG** özellikleri içerir.

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![Aspire](https://img.shields.io/badge/Aspire-9.0-512BD4)
![Ollama](https://img.shields.io/badge/Ollama-LLM-black?logo=ollama)
![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)
![Redis](https://img.shields.io/badge/Redis-Sentinel-DC382D?logo=redis)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3-FF6600?logo=rabbitmq)
![Keycloak](https://img.shields.io/badge/Keycloak-25.0-4D8EB5?logo=keycloak)

---

## 📋 İçindekiler

- [Genel Mimari](#-genel-mimari)
- [Servis Haberleşme Akışı](#-servis-haberleşme-akışı)
- [PostgreSQL — CQRS & Read Replica](#-postgresql--cqrs--read-replica)
- [Redis Sentinel — High Availability](#-redis-sentinel--high-availability)
- [Basket (Shopping) Service](#-basket-shopping-service)
- [AI Özellikleri](#-ai-özellikleri)
- [Proje Yapısı](#-proje-yapısı)
- [Teknoloji Stack](#-teknoloji-stack)
- [Kurulum](#-kurulum)
- [API Endpoints](#-api-endpoints)
- [Design Patterns](#-design-patterns)

---

## 🏗 Genel Mimari

Tüm servisler **.NET Aspire AppHost** tarafından orchestrate edilir. Her servis Docker container olarak çalışır.

```mermaid
graph TB
    subgraph ORCHESTRATOR["🎯 .NET Aspire AppHost"]
        direction TB
        
        subgraph FRONTEND["🌐 Presentation Layer"]
            WEB["🖥 Blazor Web App<br/><i>Interactive Server</i>"]
        end
        
        subgraph SERVICES["⚙️ Application Services"]
            CATALOG["📦 Catalog Service<br/><i>.NET 9 Minimal API</i>"]
            BASKET["🛒 Basket Service<br/><i>.NET 9 Minimal API</i>"]
        end
        
        subgraph AI_LAYER["🤖 AI / LLM Layer"]
            OLLAMA["🧠 Ollama Server"]
            LLAMA["💬 llama3.2<br/><i>Chat Model</i>"]
            MINILM["🔍 all-minilm<br/><i>Embedding Model</i>"]
            WEBUI["🌐 Open WebUI"]
        end
        
        subgraph DATA_LAYER["💾 Data Layer"]
            subgraph PG["PostgreSQL Cluster"]
                PG_W["✏️ Write DB<br/><i>Primary</i>"]
                PG_R["📖 Read DB<br/><i>Replica</i>"]
            end
            
            subgraph REDIS["Redis Sentinel Cluster"]
                R_M["🔴 Master"]
                R_R1["🟡 Replica 1"]
                R_R2["🟡 Replica 2"]
                R_S1["👁 Sentinel 1"]
                R_S2["👁 Sentinel 2"]
                R_S3["👁 Sentinel 3"]
            end
        end
        
        subgraph MESSAGING["📨 Messaging"]
            RMQ["🐰 RabbitMQ<br/><i>+ Management Plugin</i>"]
        end
        
        subgraph AUTH["🔐 Security"]
            KC["🛡 Keycloak<br/><i>v25.0</i>"]
            KC_DB["🗄 Keycloak DB<br/><i>PostgreSQL</i>"]
        end
        
        subgraph MONITORING["📊 Monitoring"]
            INSIGHTS["📈 App Insights"]
            PGADMIN["🔧 pgAdmin"]
            REDIS_INSIGHT["🔧 Redis Insight"]
        end
    end

    WEB -->|HTTP| CATALOG
    WEB -->|HTTP| BASKET
    BASKET -->|HTTP| CATALOG
    CATALOG --> PG_W
    CATALOG --> PG_R
    CATALOG --> OLLAMA
    OLLAMA --> LLAMA
    OLLAMA --> MINILM
    BASKET --> R_M
    BASKET -.->|JWT Validation| KC
    CATALOG <-->|MassTransit| RMQ
    BASKET <-->|MassTransit| RMQ
    KC --> KC_DB
    R_M --> R_R1
    R_M --> R_R2
    R_S1 -.->|Monitor| R_M
    R_S2 -.->|Monitor| R_M
    R_S3 -.->|Monitor| R_M

    style ORCHESTRATOR fill:#1a1a2e,stroke:#e94560,stroke-width:2px,color:#fff
    style FRONTEND fill:#0f3460,stroke:#533483,stroke-width:2px,color:#fff
    style SERVICES fill:#16213e,stroke:#533483,stroke-width:2px,color:#fff
    style AI_LAYER fill:#1a1a40,stroke:#e94560,stroke-width:2px,color:#fff
    style DATA_LAYER fill:#0a1931,stroke:#185adb,stroke-width:2px,color:#fff
    style MESSAGING fill:#1b262c,stroke:#bbe1fa,stroke-width:2px,color:#fff
    style AUTH fill:#2d132c,stroke:#ee4540,stroke-width:2px,color:#fff
    style MONITORING fill:#1c1c3c,stroke:#6c63ff,stroke-width:2px,color:#fff
```

---

## 🔄 Servis Haberleşme Akışı

Servisler arası senkron (HTTP) ve asenkron (RabbitMQ) iletişim:

```mermaid
sequenceDiagram
    actor User
    participant Web as 🌐 Blazor Web
    participant Catalog as 📦 Catalog Service
    participant Basket as 🛒 Basket Service
    participant Ollama as 🧠 Ollama AI
    participant PG_W as ✏️ PostgreSQL Write
    participant PG_R as 📖 PostgreSQL Read
    participant Redis as 🔴 Redis Master
    participant RMQ as 🐰 RabbitMQ
    participant KC as 🛡 Keycloak

    Note over User,KC: 🔍 Normal Search Flow
    User->>Web: Search "hiking"
    Web->>Catalog: GET /products/search/hiking
    Catalog->>PG_R: ILIKE query (Read Replica)
    PG_R-->>Catalog: Results
    Catalog-->>Web: Product List
    Web-->>User: Display Results

    Note over User,KC: 🤖 AI Semantic Search Flow
    User->>Web: AI Search "rainy days"
    Web->>Catalog: GET /products/aisearch/rainy+days
    Catalog->>Ollama: Generate Embeddings (all-minilm)
    Ollama-->>Catalog: Vector Embeddings
    Catalog->>Catalog: Vector Similarity Search
    Catalog-->>Web: Ranked Products
    Web-->>User: AI Results

    Note over User,KC: 🛒 Shopping Cart Flow
    User->>Web: Add to Cart
    Web->>KC: Get JWT Token
    KC-->>Web: Access Token
    Web->>Basket: POST /basket (+ JWT)
    Basket->>KC: Validate Token
    KC-->>Basket: ✅ Valid
    Basket->>Catalog: GET /products/{id} (Price Check)
    Catalog->>PG_R: Get Product
    PG_R-->>Catalog: Product Data
    Catalog-->>Basket: Current Price
    Basket->>Redis: Cache Cart (JSON)
    Redis-->>Basket: ✅ Saved
    Basket-->>Web: Cart Updated

    Note over User,KC: 📨 Event-Driven Price Update
    Catalog->>PG_W: UPDATE product price
    Catalog->>RMQ: Publish ProductPriceChanged
    RMQ->>Basket: Consume Event
    Basket->>Redis: Update cached prices
```

---

## 🗄 PostgreSQL — CQRS & Read Replica

Projede **CQRS (Command Query Responsibility Segregation)** pattern'i uygulanmıştır. Yazma ve okuma işlemleri farklı veritabanı instance'larına yönlendirilir.

```mermaid
graph LR
    subgraph CATALOG_SERVICE["📦 Catalog Service"]
        CS["Minimal API<br/>Endpoints"]
        WR_CTX["✏️ ProductDbContext<br/><i>Write Operations</i>"]
        RD_CTX["📖 ProductReadDbContext<br/><i>Read Operations</i>"]
    end

    subgraph PG_PRIMARY["✏️ PostgreSQL Primary"]
        PW["catalogdb<br/><i>Write Database</i><br/>INSERT / UPDATE / DELETE"]
    end
    
    subgraph PG_REPLICA["📖 PostgreSQL Read Replica"]
        PR["catalogdb-read<br/><i>Read Database</i><br/>SELECT Queries"]
    end

    CS -->|"Create / Update / Delete"| WR_CTX
    CS -->|"Search / List / Get"| RD_CTX
    WR_CTX --> PW
    RD_CTX --> PR
    PW -.->|"Replication"| PR

    style PG_PRIMARY fill:#2d6a4f,stroke:#95d5b2,stroke-width:2px,color:#fff
    style PG_REPLICA fill:#1b4332,stroke:#74c69d,stroke-width:2px,color:#fff
    style CATALOG_SERVICE fill:#16213e,stroke:#533483,stroke-width:2px,color:#fff
```

### Yapılandırma Detayları

| Bileşen | Connection Name | Açıklama |
|---------|----------------|----------|
| **Write DB** | `catalogdb` | `ProductDbContext` — CRUD işlemleri, EF Core Migrations |
| **Read DB** | `catalogdb-read` | `ProductReadDbContext` — Search, List, Get sorguları |
| **pgAdmin** | — | Veritabanı yönetim paneli (otomatik başlar) |

```csharp
// AppHost.cs — CQRS Database Configuration
var postgres = builder.AddPostgres("postgres").WithPgAdmin();
var catalogDb = postgres.AddDatabase("catalogdb");              // Write

var postgresRead = builder.AddPostgres("postgres-read");
var catalogDbRead = postgresRead.AddDatabase("catalogdb-read"); // Read
```

---

## 🔴 Redis Sentinel — High Availability

Proje, **Redis Sentinel** yapısı ile yüksek erişilebilirlik (HA) sağlar. Master node çökerse, Sentinel'ler otomatik failover yapar.

```mermaid
graph TB
    subgraph SENTINEL_CLUSTER["👁 Sentinel Cluster (Quorum: 2)"]
        S1["Sentinel 1<br/><i>:26379</i>"]
        S2["Sentinel 2<br/><i>:26380</i>"]
        S3["Sentinel 3<br/><i>:26381</i>"]
    end

    subgraph REDIS_CLUSTER["🔴 Redis Data Cluster"]
        MASTER["🔴 Redis Master<br/><i>:6379</i><br/>READ + WRITE"]
        REPLICA1["🟡 Redis Replica 1<br/><i>:6380</i><br/>READ ONLY"]
        REPLICA2["🟡 Redis Replica 2<br/><i>:6381</i><br/>READ ONLY"]
    end
    
    subgraph CLIENTS["⚙️ Service Clients"]
        BASKET_SVC["🛒 Basket Service<br/><i>IDistributedCache</i>"]
        WEB_SVC["🌐 Blazor Web<br/><i>Session/Cache</i>"]
    end

    MASTER -->|"Async Replication"| REPLICA1
    MASTER -->|"Async Replication"| REPLICA2
    
    S1 -.->|"Monitor"| MASTER
    S1 -.->|"Monitor"| REPLICA1
    S1 -.->|"Monitor"| REPLICA2
    S2 -.->|"Monitor"| MASTER
    S3 -.->|"Monitor"| MASTER
    
    BASKET_SVC -->|"GetString / SetString"| MASTER
    WEB_SVC -->|"Session Cache"| MASTER

    style SENTINEL_CLUSTER fill:#4a1942,stroke:#e94560,stroke-width:2px,color:#fff
    style REDIS_CLUSTER fill:#6b2d5b,stroke:#f18c8e,stroke-width:2px,color:#fff
    style CLIENTS fill:#16213e,stroke:#533483,stroke-width:2px,color:#fff
    style MASTER fill:#c0392b,stroke:#e74c3c,stroke-width:2px,color:#fff
    style REPLICA1 fill:#d4ac0d,stroke:#f1c40f,stroke-width:2px,color:#000
    style REPLICA2 fill:#d4ac0d,stroke:#f1c40f,stroke-width:2px,color:#000
```

### Sentinel Yapılandırması

| Bileşen | Port | Rol |
|---------|------|-----|
| **Redis Master** | `6379` | Read/Write — ShoppingCart JSON verisi |
| **Redis Replica 1** | `6380` | Read-only — Master'dan async replicate |
| **Redis Replica 2** | `6381` | Read-only — Master'dan async replicate |
| **Sentinel 1** | `26379` | Master monitoring + Auto failover |
| **Sentinel 2** | `26380` | Master monitoring + Auto failover |
| **Sentinel 3** | `26381` | Master monitoring + Auto failover |
| **Redis Insight** | — | Redis yönetim paneli (otomatik başlar) |

**Failover Senaryosu:** Master çökerse → 3 Sentinel oylama yapar (quorum: 2) → Replica'lardan biri yeni Master olur → Otomatik geçiş.

---

## 🛒 Basket (Shopping) Service

Alışveriş sepeti yönetimi servisi. **Keycloak JWT** authentication ile korunan, **Redis** üzerinde cache'lenen sepet verilerini yönetir.

```mermaid
graph TB
    subgraph BASKET_SERVICE["🛒 Basket Service"]
        EP["🔌 Basket Endpoints<br/><i>Minimal API</i>"]
        SVC["⚙️ BasketServices<br/><i>Business Logic</i>"]
        CACHE["💾 IDistributedCache<br/><i>Redis Client</i>"]
        CATALOG_CLIENT["🔗 CatalogApiClient<br/><i>Typed HTTP Client</i>"]
        AUTH_MW["🔐 Auth Middleware<br/><i>JWT Bearer</i>"]
        EVT["📨 EventHandlers<br/><i>MassTransit Consumer</i>"]
    end

    subgraph EXTERNAL["External Dependencies"]
        REDIS_M["🔴 Redis Master"]
        KC["🛡 Keycloak"]
        CATALOG["📦 Catalog Service"]
        RMQ["🐰 RabbitMQ"]
    end
    
    subgraph ENTITIES["📋 Data Model"]
        CART["ShoppingCart<br/><i>UserName, Items[], TotalPrice</i>"]
        ITEM["ShoppingCartItem<br/><i>ProductId, ProductName,<br/>Price, Quantity</i>"]
    end

    EP -->|"Auth Check"| AUTH_MW
    AUTH_MW -->|"Validate JWT"| KC
    EP --> SVC
    SVC --> CACHE
    SVC --> CATALOG_CLIENT
    CACHE -->|"JSON Serialize"| REDIS_M
    CATALOG_CLIENT -->|"GET /products/{id}"| CATALOG
    EVT -->|"Consume Events"| RMQ
    EVT -->|"Update Prices"| CACHE
    CART --> ITEM

    style BASKET_SERVICE fill:#1b4332,stroke:#95d5b2,stroke-width:2px,color:#fff
    style EXTERNAL fill:#0a1931,stroke:#185adb,stroke-width:2px,color:#fff
    style ENTITIES fill:#2d132c,stroke:#ee4540,stroke-width:2px,color:#fff
```

### Basket Service Özellikleri

| Özellik | Açıklama |
|---------|----------|
| **Sepet CRUD** | `GetBasket`, `UpdateBasket`, `DeleteBasket` |
| **Fiyat Doğrulama** | Sepete eklerken CatalogService'den güncel fiyat çekilir |
| **Event-Driven Update** | RabbitMQ'dan `ProductPriceChanged` event'i ile sepetteki fiyatlar güncellenir |
| **JWT Authentication** | Keycloak ile Bearer token doğrulaması |
| **Redis Cache** | Sepet verileri JSON olarak Redis Master'da saklanır |

### Basket API Endpoints

| Method | Endpoint | Auth | Açıklama |
|--------|----------|------|----------|
| `GET` | `/basket/{userName}` | 🔐 JWT | Kullanıcının sepetini getir |
| `POST` | `/basket` | 🔐 JWT | Sepet oluştur/güncelle |
| `DELETE` | `/basket/{userName}` | 🔐 JWT | Sepeti sil |

---

## 🤖 AI Özellikleri

```mermaid
graph LR
    subgraph AI_FEATURES["🤖 AI Pipeline"]
        subgraph SEARCH["🔍 Semantic Search"]
            Q1["User Query"] --> EMB["all-minilm<br/><i>Embedding</i>"]
            EMB --> VEC["Vector Store<br/><i>InMemory</i>"]
            VEC --> RANK["Cosine Similarity<br/><i>Ranking</i>"]
            RANK --> RES1["Ranked Products"]
        end
        
        subgraph CHAT["💬 AI Chat Support"]
            Q2["User Question"] --> SYS["System Prompt<br/><i>+ Product Catalog</i>"]
            SYS --> LLM["llama3.2<br/><i>Chat Completion</i>"]
            LLM --> RES2["AI Response<br/><i>+ Recommendation</i>"]
        end
    end

    style AI_FEATURES fill:#1a1a40,stroke:#e94560,stroke-width:2px,color:#fff
    style SEARCH fill:#16213e,stroke:#0f3460,stroke-width:2px,color:#fff
    style CHAT fill:#2d132c,stroke:#ee4540,stroke-width:2px,color:#fff
```

### 1. Semantic Search (AI Search)
- **Model:** `all-minilm` (Embedding — 384 dimensions)
- Ürün açıklamalarından embedding vektörleri oluşturur
- InMemory Vector Store ile cosine similarity araması yapar
- Doğal dilde arama: *"something for rainy days"* → ilgili ürünleri bulur

### 2. AI Chat Support
- **Model:** `llama3.2` (3B parametreli Chat/LLM)
- Outdoor ürünleri hakkında sorulara context-aware yanıtlar verir
- Ürün kataloğunu system prompt olarak kullanır (grounding)
- Her yanıtta ilgili ürün önerisi sunar

### 3. Case-Insensitive Smart Search
- PostgreSQL `ILIKE` ile büyük/küçük harf duyarsız arama
- Hem ürün adı hem açıklama üzerinden arama

---

## 📁 Proje Yapısı

```mermaid
graph TB
    subgraph SOLUTION["📦 Solution: dotnet_generative_ai_aspire"]
        subgraph APPHOST["🎯 AspireApps.AppHost"]
            AH["AppHost.cs<br/><i>15+ Container Orchestration</i>"]
            EXT["Extensions/<br/><i>AppInsights Helper</i>"]
            CFG["config/<br/><i>sentinel.conf</i>"]
        end

        subgraph CATALOG["📦 AspireApps.CatalogService"]
            direction TB
            C_EP["Endpoint/<br/><i>ProductEndpoint.cs</i>"]
            C_SVC["Services/<br/><i>ProductService.cs<br/>ProductAIService.cs</i>"]
            C_DATA["Data/<br/><i>DbContext, Migrations,<br/>DataSeeder</i>"]
            C_ENT["Entity/<br/><i>Product.cs<br/>ProductVector.cs</i>"]
        end

        subgraph BASKET_SVC["🛒 AspireApps.BasketService"]
            direction TB
            B_EP["Endpoints/<br/><i>BasketEndpoint.cs</i>"]
            B_SVC["Services/<br/><i>BasketServices.cs</i>"]
            B_ENT["Entities/<br/><i>ShoppingCart.cs<br/>ShoppingCartItem.cs</i>"]
            B_EVT["EventHandlers/<br/><i>MassTransit Consumers</i>"]
            B_API["ApiClients/<br/><i>CatalogApiClient.cs</i>"]
        end

        subgraph WEB["🌐 AspireApps.Web"]
            direction TB
            W_PG["Pages/<br/><i>Products.razor<br/>Search.razor<br/>Support.razor</i>"]
            W_API["ApiClients/<br/><i>CatalogApiClient.cs</i>"]
        end

        subgraph DEFAULTS["🔧 AspireApps.ServiceDefaults"]
            D_EXT["Extensions.cs<br/><i>OpenTelemetry,<br/>Health Checks</i>"]
            D_MSG["Messaging/<br/><i>MassTransit Config</i>"]
        end
    end

    APPHOST --> CATALOG
    APPHOST --> BASKET_SVC
    APPHOST --> WEB
    CATALOG --> DEFAULTS
    BASKET_SVC --> DEFAULTS
    WEB --> DEFAULTS

    style SOLUTION fill:#0d1117,stroke:#30363d,stroke-width:2px,color:#fff
    style APPHOST fill:#e94560,stroke:#fff,stroke-width:2px,color:#fff
    style CATALOG fill:#533483,stroke:#fff,stroke-width:2px,color:#fff
    style BASKET_SVC fill:#2d6a4f,stroke:#fff,stroke-width:2px,color:#fff
    style WEB fill:#0f3460,stroke:#fff,stroke-width:2px,color:#fff
    style DEFAULTS fill:#4a4e69,stroke:#fff,stroke-width:2px,color:#fff
```

---

## 🛠 Teknoloji Stack

| Katman | Teknoloji | Açıklama |
|--------|-----------|----------|
| **Orchestration** | .NET Aspire 9.0 | Container orchestration, service discovery, health checks |
| **Backend** | .NET 9.0 Minimal API | Catalog Service, Basket Service |
| **Frontend** | Blazor Server (Interactive) | SSR + Interactive rendering |
| **AI/LLM** | Ollama + OllamaSharp | Local LLM inference |
| **AI Framework** | Microsoft.Extensions.AI | Unified AI abstractions |
| **Chat Model** | llama3.2 (3B) | Chat completion, support assistant |
| **Embedding Model** | all-minilm | 384-dim sentence embeddings |
| **Vector Store** | InMemory Vector Store | Semantic similarity search |
| **Database (Write)** | PostgreSQL 16 | Primary — CRUD operations |
| **Database (Read)** | PostgreSQL 16 | Replica — Read queries (CQRS) |
| **Cache** | Redis 7.4 Sentinel | 1 Master + 2 Replicas + 3 Sentinels |
| **Messaging** | RabbitMQ + MassTransit | Async messaging, Outbox pattern |
| **Auth** | Keycloak 25.0 | JWT Bearer, realm: eshop |
| **ORM** | Entity Framework Core | Code-first migrations |
| **Monitoring** | OpenTelemetry + App Insights | Distributed tracing, metrics |
| **UI Tools** | pgAdmin, Redis Insight, Open WebUI | Database & AI management |

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

> **📝 Not:** İlk çalıştırmada Ollama modelleri (`llama3.2` ~2GB, `all-minilm` ~46MB) otomatik indirilir. 15+ Docker container başlatılır. İlk açılış birkaç dakika sürebilir.

### Aspire Dashboard

Uygulama başladıktan sonra, terminaldeki URL'den Aspire Dashboard'a erişebilirsiniz:
```
https://localhost:17094
```
Dashboard üzerinden tüm servislerin durumunu, loglarını ve distributed trace'lerini izleyebilirsiniz.

---

## 📡 API Endpoints

### 📦 Catalog Service (`/products`)

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

### 🛒 Basket Service (`/basket`)

| Method | Endpoint | Auth | Açıklama |
|--------|----------|------|----------|
| `GET` | `/basket/{userName}` | 🔐 JWT | Sepeti getir |
| `POST` | `/basket` | 🔐 JWT | Sepet oluştur/güncelle |
| `DELETE` | `/basket/{userName}` | 🔐 JWT | Sepeti sil |

---

## 📊 Design Patterns

| Pattern | Kullanım |
|---------|----------|
| **CQRS** | Write ve Read veritabanı ayrımı (PostgreSQL Primary + Replica) |
| **Outbox Pattern** | MassTransit ile reliable event publishing |
| **Saga Pattern** | Distributed transaction yönetimi |
| **Service Discovery** | Aspire resource references ile dinamik endpoint çözümleme |
| **Retry & Circuit Breaker** | Polly resilience policies |
| **Keyed Services** | Multiple AI client DI registration |
| **Typed HTTP Clients** | Service-to-service communication |
| **Event-Driven Architecture** | RabbitMQ + MassTransit consumers |
| **Sentinel HA** | Redis otomatik failover — 3 Sentinel quorum |

---

## 📄 Lisans

Bu proje eğitim ve öğrenme amaçlı oluşturulmuştur.
