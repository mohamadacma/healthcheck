# 🩺 HealthCheck API — .NET 9 Learning Project

A **RESTful Web API** full-stack built with **.NET 9** and **Entity Framework Core**, Postgres, and React,designed as a solo hospital inventory management system prototype.
---

## 🎯 Project Purpose

This project serves as a hands-on learning experience to master modern .NET development technologies and design patterns while creating a functional hospital inventory management tool. The goal is to build a self-contained prototype that tracks medical supplies (e.g., medications, bandages, surgical tools) for hospitals, demonstrating value through features like usage tracking and stock management. This system is intended to showcase my skills to health industry professionals, potentially sparking collaboration to integrate advanced features (e.g., barcode scanning, EHR integration) in the future.

---

## ⚙️ Core Technologies & Concepts

## 🛠️ Stack
- ASP.NET Core (.NET 9)
- Entity Framework Core with Postgres
- RESTful API using Minimal APIs
- DTOs + Validation
- Azure-ready configuration
- HealthCheck endpoints
- CI/CD with GitHub Actions
- Deployed to Railway
- JWT authentication: Securing endpoints with role-based access (e.g., Admin, Nurse, SupplyChain).
- React frontend: Building an interactive UI for inventory search, usage logging, and basic analytics.     
- Search and Filtering: Query items by name, quantity, or category with pagination.      |

## 🚀 Features
- Create, read, update, delete inventory items: Manage stock levels for hospital supplies.
- Proper DTO layering: Standardizing data transfer between frontend and backend.
- Async database access: Ensuring non-blocking database operations.
- Structured error handling with logging: Capturing and reporting issues for debugging.
- Health endpoints: `/health`, `/health/ready`, `/health/live`
- Role-Based Authorization: Restricting actions (e.g., only Admins can delete items).
---

## 📚  Learnings

- Build scalable and maintainable API using **.NET 9 Minimal APIs**
- configure production-ready PostgreSQL DBs
- Deploy with Docker and Railway
- Implement API documentations with swagger
- Set up CI/CD pipelines with GitHub Actions.
- Apply **Clean Architecture** for separation of concerns
- Leverage **Entity Framework Core** for database interactions
- Understand and apply **Dependency Injection** in real-world projects
- Practice **best practices** in API versioning, error handling, and validation
- Develop a prototype hospital inventory system that addresses real-world challenges like stockouts and waste, preparing for potential industry collaboration.


### Future Scope
This project aims to evolve into a valuable tool for hospitals by:

Adding basic analytics (e.g., usage trends, waste reports) to support data-driven decisions.
Enhancing the UI with charts and mobile responsiveness.
Preparing for integration with external systems (e.g., EHRs, barcode scanners) through collaboration with health industry partners.
Expanding features like automated reorder alerts and expiration tracking based on feedback.
