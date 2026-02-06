# ScheduleCentral – Class Schedule Management System

## Overview

**ScheduleCentral** is a centralized class scheduling and timetable management system designed for higher education institutions. It focuses on conflict-free scheduling, structured academic data management, and clear role separation across administrative and academic stakeholders.

The system automates schedule generation, enforces institutional constraints, and provides clean schedule visualization for different academic sections. It is built to scale across departments while maintaining data consistency and scheduling accuracy.

---

## Core Objectives

ScheduleCentral aims to eliminate manual scheduling errors, reduce administrative workload, and ensure optimal use of academic resources such as instructors, rooms, and time slots. The system emphasizes correctness, transparency, and maintainability rather than instructor-owned or ad-hoc scheduling.

---

## Key Features

### User & Role Management

* Role-based access control with roles stored in a separate database table
* Supported roles include Admin, Program Officer, Department, Instructor, and Top Management
* No generic user role
* Account creation for departments and top management handled manually
* All users can update their own profiles

### Scheduling Engine

* Constraint Satisfaction Problem (CSP)–based scheduling algorithm
* Automatic conflict detection and prevention
* No schedule ownership by instructors
* Enforced constraints:

  * No two offerings in the same day, period, and section
  * No room conflicts across schedules
  * Course-hour mappings strictly enforced

### Section & Academic Structure

* Sections decomposed by department, year, number, and class type (REG / EXT)
* Parallel sections supported
* Courses shared across parallel sections
* Room types supported:

  * Normal classroom
  * Lecture hall
  * Computer lab
  * Architecture studio

### Schedule Formats

* REG: Monday–Friday × 8 periods
* EXT (Night): Monday–Friday × 2 periods
* EXT (Weekend): Saturday–Sunday × 8 periods
* Matrix-based timetable representation

### Schedule Interaction

* Students can search and view schedules by section
* Read-only access for students
* Broadcast-only notifications
* Filterable schedule tables

### Reporting & Analytics

* Administrative reports on scheduling usage
* System-level summaries for top management

---

## System Architecture

* **Backend:** ASP.NET / Laravel (depending on deployment)
* **Scheduling Logic:** CSP-based algorithm
* **Database:** Relational (normalized up to 3NF)
* **Authentication:** Framework-provided hashing and session management
* **Data Input:** CSV-based course offering uploads

---

## Database Design Highlights

* Fully normalized schemas at 1NF, 2NF, and 3NF
* Sections represent academic groups, not users
* Departments separated from department officials (users)
* Rooms modeled independently with type constraints
* Course offerings drive instructor constraints and scheduling logic

---

## Project Structure

```
ScheduleCentral/
├── Controllers/
├── Models/
│   ├── Section.cs
│   ├── Course.cs
│   ├── Room.cs
│   ├── Schedule.cs
│   └── User.cs
├── Views/
├── Services/
│   └── SchedulingEngine/
├── Data/
├── Imports/
│   └── CourseOfferings.csv
└── wwwroot/
```

---

## Setup & Installation

1. Clone the repository

   ```bash
   git clone https://github.com/your-username/schedulecentral.git
   ```

2. Configure environment variables and database connection

3. Import course offerings using CSV upload

4. Run migrations

5. Start the application

---

## Design Decisions

* No instructor-owned schedules to preserve institutional control
* No admin approval workflow for account creation
* Conflict prevention handled at algorithm and database levels
* Emphasis on correctness over manual overrides

---

## Future Enhancements

* Visual timetable editor
* What-if scheduling simulations
* Advanced analytics dashboards
* Mobile-friendly student views
* API exposure for external systems

---

## License

This project is intended for academic and institutional use. Licensing can be adapted as required.

---

## Author

**Micheás Aragaw**
Backend Developer | IoT Specialist
