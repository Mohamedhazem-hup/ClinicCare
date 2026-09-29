# Clinic Management System

Clinic management system built with ASP.NET Core MVC, Entity Framework Core 10, SQLite, and ASP.NET Core Identity. SQL Server is also supported through configuration.

## طريقة التشغيل

1. Install the .NET 10 SDK.
2. From the project directory, run:
   ```bash
   dotnet restore
   dotnet run
   ```
3. Browse to the URL printed by `dotnet run`.
4. On first launch, the initializer creates `clinicmanagement.dev.db`, the four roles, and the demo accounts.

## حسابات تجريبية (Seeded)

| الدور | Email | Password |
|---|---|---|
| Admin | admin@clinic.com | Admin123 |
| Receptionist | reception@clinic.com | Reception123 |
| Doctor | doctor@clinic.com | Doctor123 |
| Patient | patient@clinic.com | Patient123 |


## الصلاحيات (Roles & Permissions)

- **Admin**: يشوف الداشبورد، يدير الأطباء (إنشاء حساب دكتور بالكامل مع تسجيل دخول)، يدير الـ Receptionists، وله صلاحية كل حاجة.
- **Receptionist**: يسجل مرضى جدد، يحجز مواعيد (مع منع تعارض ميعاد نفس الدكتور)، يدير الفواتير.
- **Doctor**: يشوف مواعيده هو بس، يسجل تشخيص/علاج (Medical Record) ووصفة طبية (Prescription) لمرضاه.
- **Patient**: يعمل تسجيل بنفسه، يشوف مواعيده، تاريخه الطبي، وصفاته، وفواتيره فقط (My Appointments / My Medical Records / My Prescriptions / My Bills).

## الموديولات المنفذة

- **Patients** — CRUD كامل + صفحة تفاصيل فيها كل تاريخ المريض (مواعيد، سجلات طبية، فواتير).
- **Doctors** — CRUD (الإنشاء بيعمل حساب Login + بروفايل الدكتور مع بعض).
- **Appointments** — حجز/تعديل/حذف مع مدة للموعد ومنع تداخل المواعيد للطبيب أو المريض.
- **Medical Records** — تشخيص وعلاج وملاحظات مرتبطة بمريض ودكتور وميعاد (اختياري).
- **Prescriptions** — وصفات طبية مرتبطة بسجل طبي (اختياري) أو مباشرة بمريض ودكتور.
- **Bills** — فواتير + حالة الدفع (Pending/Paid/Cancelled) + زر "Mark as Paid" + إجمالي الإيرادات في الداشبورد.

## ملاحظات مهمة

- The default `DatabaseProvider` is `Sqlite`; its connection string points to `clinicmanagement.dev.db`.
- To use SQL Server, set `DatabaseProvider` to `SqlServer`, configure `DefaultConnection` for your SQL Server instance, then run `dotnet ef database update`.
- The prior `cliniccare.db` file is not used or deleted.
