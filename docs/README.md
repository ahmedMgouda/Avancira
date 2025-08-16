# Avancira Backend Project Documentation

## Overview

Avancira is a comprehensive online tutoring and lesson booking platform built with .NET 9 using Clean Architecture principles. The platform connects students with tutors, facilitating lesson scheduling, payments, messaging, and evaluation systems.

## 🏗️ Architecture Overview

The project follows **Clean Architecture** (Onion Architecture) with clear separation of concerns across multiple layers:

- **Domain Layer** - Core business logic and entities
- **Application Layer** - Use cases and business rules
- **Infrastructure Layer** - External concerns (database, email, payments)
- **API Layer** - Web API controllers and endpoints
- **Presentation Layers** - Admin panel (Blazor) and Angular frontend

## 🚀 Key Features

### Core Business Domains
- **Listings Management** - Tutors create and manage their service listings
- **Lesson Booking** - Students book lessons with tutors
- **Payment Processing** - Secure payments via Stripe and PayPal
- **Messaging System** - Real-time chat between students and tutors
- **Evaluation System** - Rating and review system
- **Wallet System** - Internal credit system for users
- **Subscription Management** - Premium features and billing
- **Notification System** - Multi-channel notifications (Email, SignalR)

### Technical Features
- **Real-time Communication** - SignalR for live messaging and notifications
- **Multi-tenant Architecture** - Support for different user roles and permissions
- **Caching** - Redis caching for performance optimization
- **Background Jobs** - Hangfire for scheduled tasks
- **File Storage** - Configurable file storage system
- **Rate Limiting** - API rate limiting for security
- **Comprehensive Logging** - Serilog integration
- **Health Checks** - Application health monitoring

## 📁 Project Structure

```
Avancira/
├── api/                          # Backend API
│   ├── Avancira.API/            # Web API layer
│   ├── Avancira.Application/    # Application services & use cases
│   ├── Avancira.Domain/         # Domain entities & business logic
│   ├── Avancira.Infrastructure/ # External integrations
│   ├── Avancira.Migrations/     # Database migrations
│   └── Avancira.Shared/         # Shared contracts
├── admin/                       # Admin panel (Blazor)
├── Frontend.Angular/            # Student/Tutor frontend
└── aspire/                      # .NET Aspire orchestration
```

## 🔧 Technology Stack

- **.NET 9** - Core framework
- **ASP.NET Core** - Web API
- **Entity Framework Core** - ORM with PostgreSQL
- **MediatR** - CQRS pattern implementation
- **FluentValidation** - Input validation
- **Mapster** - Object mapping
- **SignalR** - Real-time communication
- **Hangfire** - Background job processing
- **Serilog** - Structured logging
- **Redis** - Caching
- **Stripe & PayPal** - Payment processing
- **.NET Aspire** - Cloud-native orchestration

## 🎯 Getting Started

### Prerequisites
- .NET 9 SDK
- PostgreSQL database
- Redis (for caching)
- Node.js (for Angular frontend)

### Running the Application

#### Development with Aspire
```bash
cd aspire/Avancira.Host
dotnet run
```

#### Traditional Development
```bash
cd api/Avancira.API
dotnet run
```

## 📊 Business Flow

1. **Tutor Registration** → Create listings → Admin approval
2. **Student Discovery** → Browse listings → Book lessons
3. **Payment Processing** → Secure transaction → Lesson confirmation
4. **Lesson Delivery** → Real-time communication → Completion
5. **Evaluation** → Rating and reviews → Platform improvement

## 🔐 Security Features

- JWT-based authentication
- Role-based authorization
- Rate limiting
- Security headers
- Input validation
- CORS configuration
- Secure payment processing

## 📈 Monitoring & Observability

- Health checks for all services
- Structured logging with Serilog
- Performance monitoring
- Error tracking
- Background job monitoring

## 🔄 Development Workflow

The project supports both traditional development and modern cloud-native development with .NET Aspire for orchestration and service discovery.

---

For detailed architectural diagrams and component relationships, see the additional documentation files in this directory.
