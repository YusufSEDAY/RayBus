# RayBus - Advanced Train & Bus Ticket Reservation System

RayBus is an end-to-end ticket reservation and management ecosystem built with modern web technologies and advanced database programming techniques. The project focuses on handling complex business logic at the database level, high-performance reporting, and a secure, scalable data architecture.

## 🚀 Project Overview
RayBus digitizes the ticket reservation process for both bus and train travels. It features dedicated panels for Admins, Customers, and Companies, providing a seamless experience from search to seat selection and payment.

### Key Features:
* **Multi-Role Management**: Specialized dashboards for Admins, Customers, and Company users.
* **Dynamic Search & Filtering**: Real-time search functionality by city, date, and vehicle type.
* **Transactional Reservation**: A high-integrity ticketing system ensuring seat availability and consistent data state.
* **Automated Systems**: Features auto-cancellation of expired reservations and dynamic pricing based on occupancy rates.
* **Comprehensive Analytics**: Real-time data visualization for revenue, occupancy, and route performance.

## 🛠️ Technology Stack
The project follows a strict **Three-Tier Architecture**:
* **Frontend**: React.js.
* **Backend**: ASP.NET Core 8.0 Web API.
* **Database**: Microsoft SQL Server.
* **ORM**: Entity Framework Core 8.0.
* **Security**: JWT (Authentication) and BCrypt (Password Hashing).

## 📊 Database Engineering & Objects
The core of RayBus lies in its enterprise-grade database design, organized logically into specialized schemas.

### Schema Organization
Database objects are categorized into 5 primary schemas:
* **`app`**: Core application tables such as Users, Trips, and Reservations.
* **`log`**: Audit trails and system operation logs including Reservation and Payment logs.
* **`report`**: Reporting views optimized with complex JOINs for statistics.
* **`[proc]`**: Stored procedures containing the primary business logic.
* **`[func]`**: Scalar functions for statistical calculations like total expenditure.

### Database Object Metrics
* **29 Stored Procedures**: All critical operations (registration, reservation, cancellation) are managed at the database level.
* **13 Views**: Optimized data representations for Admin and Company dashboards.
* **8+ Triggers**: Automated seat status updates, notification queuing, and audit logging.
* **3 Scalar Functions**: Automatic calculation of user expenditure and travel frequency.

## ⚡ Performance & Scalability
* **Stress Testing**: The system was successfully tested against a massive dataset of **1 million records**, maintaining stable response times.
* **Query Optimization**: Using optimized views instead of multiple individual queries resulted in a **performance gain of up to 80%** for dashboard statistics.
* **Indexing Strategy**: Composite indexes on frequently searched criteria maximize retrieval speed.

## 🔐 Security
* **SQL Injection Protection**: All database interactions are performed via parameterized queries and Stored Procedures.
* **Data Privacy**: User passwords are encrypted using the BCrypt hashing algorithm.
* **Authorization**: Role-Based Access Control (RBAC) ensures users only perform authorized operations.

## 👥 Contributors
* **[Yusuf SEDAY]**
* **[Güven ZENCİR]**
* **[Yusuf Erdem GÜNGÖR]**
* 
---
*This project was developed as an academic study for the Database Programming course.*
