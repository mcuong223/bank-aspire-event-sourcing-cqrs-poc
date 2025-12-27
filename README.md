# Banky.POC 🏦

> A High-Fidelity Proof of Concept designed for **Internal Knowledge Sharing** sessions on **Event-Driven Architecture Series: ES + CQRS**.

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Aspire](https://img.shields.io/badge/Orchestration-.NET%20Aspire-blueviolet)](https://learn.microsoft.com/en-us/dotnet/aspire/)
[![Broker](https://img.shields.io/badge/Broker-Apache%20Kafka-black?logo=apachekafka)](https://kafka.apache.org/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)]()

**Banky.POC** is a distributed banking system playground built to illustrate **Event Sourcing**, **Physical CQRS**, and **Streaming Architecture** concepts. Unlike typical "Hello World" examples, this project enforces **Strict Clean Architecture** and uses **Apache Kafka** to demonstrate true decoupling and autonomous replay capabilities.

## 📐 Architecture Overview

The system implements **Physical CQRS** with **Kafka** as the central nervous system. The Read Side is completely autonomous—it builds its state solely by consuming the immutable Event Log from Kafka.



```mermaid
graph TD
    User([User]) -->|POST| CmdAPI[Account.Command]
    User -->|GET| ReadAPI[Account.Read]
    
    subgraph "Write Side (Optimized)"
        CmdAPI --> Domain --> DIY_Repo
        DIY_Repo -.->|1. Load (Snapshot + Events)| PG_Write[(Write DB)]
        DIY_Repo -.->|2. Save Events| PG_Write
        DIY_Repo -.->|3. Save Snapshot (Every 5th)| PG_Write
        DIY_Repo -.->|4. Produce| Kafka{Kafka Topic}
    end

    subgraph "Read Side (Autonomous Consumers)"
        Kafka -.->|Group: Balance| C1[Balance Projector]
        Kafka -.->|Group: History| C2[History Projector]
        Kafka == New Group: Loyalty (Offset 0) ==> C3[Loyalty Projector]
        
        C1 -->|Upsert| T1[AccountView]
        C2 -->|Append| T2[TransactionHistory]
        C3 -->|Calc Time-Weight| T3[LoyaltyView]

        T1 --> PG_Read[(Read DB)]
        T2 --> PG_Read
        T3 --> PG_Read
        ReadAPI --> PG_Read
    end
```

## ✨ Key Features

### 1. Consumer-Driven Replay (Via Kafka)
This features demonstrates **Zero-Coupling Replay**:
- **The Problem:** How to build a new view (e.g., Loyalty) from past data without asking the Write Service?
- **The Solution:** We deploy the `LoyaltyProjector` with a fresh **Kafka Consumer Group** and set `AutoOffsetReset = Earliest`.
- **The Result:** Kafka automatically streams the entire event history to this new consumer. The Write Service is completely unaware.

### 2. Polyglot Projections (3 Distinct Patterns)
Demonstrating that "One Event Stream" can generate "Many Views":
* **View 1: Account Balance (Snapshot)** - Upsert logic.
* **View 2: Transaction History (Audit Log)** - Append-only logic.
* **View 3: VIP Loyalty Tracker (Derived Logic)**
    * Implements a **Time-Weighted Average Algorithm** to calculate VIP scores based on deposit duration, preventing "money churning" fraud.

### 3. Performance Optimization: Snapshotting
Addresses the "N+1 Problem" of Event Sourcing.
- **Mechanism:** Instead of replaying 1,000 events to load an account, the system automatically saves a JSON Snapshot of the Aggregate state every 5 events (configurable).
- **Impact:** `Load time = O(1)` instead of `O(N)`.

### 4. "DIY" Event Sourcing & Clean Architecture
Manual implementation using EF Core (Serialization, Versioning) and strict Physical Project Separation to enforce Dependency Inversion.

## 🛠️ Tech Stack

-   **.NET 9** & **.NET Aspire 9.0**
-   **Apache Kafka** (Message Broker)
-   **Kafka UI** (Visualization Dashboard)
-   **PostgreSQL** (Database)
-   **MassTransit** (Kafka Transport)
-   **xUnit**, **FluentAssertions**, **NSubstitute**

## ⚖️ Architectural Trade-offs (POC vs. Production)

| Feature | In this POC | In Production |
| :--- | :--- | :--- |
| **Consistency** | **Dual Write**: We save to Postgres and then Produce to Kafka. There is a slight risk of inconsistency if the process crashes between these two steps. | **Transactional Outbox Pattern**: Use a tool like Debezium or an Outbox table to guarantee "At-least-once" delivery from DB to Kafka. |
| **Snapshotting** | **Inline**: Done synchronously during the Save process. | **Async Worker**: Snapshots should be taken by a background process to avoid blocking the user request. |

## 🚀 Quick Start & Demo Script

### 1. Run the System
```bash
git clone https://github.com/mcuong223/bank-aspire-event-sourcing-cqrs-poc.git
cd src/Aspire/Banky.AppHost
dotnet run
```
Aspire will spin up containers for **Kafka**, **Zookeeper**, **Postgres**, and **Kafka UI**.

### 2. Visualizing Data
Open the **Kafka UI** link from the Aspire Dashboard (usually mapped to port `8080`).
- Go to **Consumers** tab to see the Lag (messages waiting to be processed).

### 3. The "Replay" Demo
1.  **Generate Data:** Create accounts and make transactions.
2.  **Observe:** See `Balance` and `History` consumers updating in real-time.
3.  **Deploy New Feature:** Uncomment/Enable the `LoyaltyProjector` (Simulating a new deploy).
4.  **Magic Moment:** Watch Kafka UI. You will see the new **Loyalty Consumer Group** appear with a high Lag, then rapidly drain to 0 as it replays history to build the VIP Tiers.

## 📂 Structure Overview

```text
src/
├── Aspire/                 # Orchestration (Kafka/Kafka-UI setup)
├── Shared/                 # Contracts
├── Services/
│   ├── Account.Command/    # WRITE: EF Core (Snapshots) -> Kafka Producer
│   └── Account.Read/       # READ: Kafka Consumers -> EF Core Projections
└── tests/                  # Unit Tests
```

## 📝 License

This project is licensed under the MIT License.