# Microservices Design - Chi Tiết Từng Service

## 📐 Nguyên Tắc Thiết Kế

### Domain-Driven Design (DDD)
- Mỗi microservice tương ứng với 1 **Bounded Context**
- Áp dụng **Aggregate Pattern** để đảm bảo consistency
- **Domain Events** để communication giữa các service
- **Ubiquitous Language** trong team

### Microservices Principles
- **Single Responsibility:** Mỗi service có 1 trách nhiệm rõ ràng
- **Autonomous:** Service độc lập về deployment và scaling
- **Decentralized Data:** Mỗi service có database riêng
- **Resilient:** Circuit Breaker, Retry, Fallback
- **Observable:** Logging, Metrics, Tracing

---

## 🎯 Danh Sách Microservices

### 1. **Identity Service** 🔐
**Bounded Context:** Identity & Access Management

**Trách nhiệm:**
- Authentication (đăng nhập/đăng xuất)
- Authorization (phân quyền)
- User management
- Role & Permission management
- Multi-tenant user isolation
- Token management (JWT, Refresh Token)

**Domain Models:**
- **User:** UserId, Username, Email, PhoneNumber, PasswordHash, TenantId, Status
- **Role:** RoleId, Name, TenantId, Permissions
- **Permission:** PermissionId, Name, Resource, Action
- **RefreshToken:** TokenId, UserId, Token, ExpiresAt

**Technology Stack:**
- **Framework:** ASP.NET Core 8.0
- **Authentication:** IdentityServer / Duende IdentityServer hoặc Custom JWT
- **Database:** MySQL
- **Cache:** Redis (token blacklist, session)

**API Endpoints:**
```
POST   /api/v1/auth/login
POST   /api/v1/auth/logout
POST   /api/v1/auth/refresh-token
POST   /api/v1/auth/register
POST   /api/v1/auth/forgot-password
POST   /api/v1/auth/reset-password
GET    /api/v1/users/{userId}
PUT    /api/v1/users/{userId}
GET    /api/v1/roles
POST   /api/v1/roles
PUT    /api/v1/roles/{roleId}
DELETE /api/v1/roles/{roleId}
```

**Events Published:**
- `UserRegistered`
- `UserLoggedIn`
- `UserLoggedOut`
- `PasswordChanged`
- `RoleAssigned`

**Communication:**
- **Synchronous:** gRPC cho internal authentication check
- **Asynchronous:** RabbitMQ cho domain events

---

### 2. **Student Service** 👶
**Bounded Context:** Student Management

**Trách nhiệm:**
- Quản lý hồ sơ học sinh
- Quản lý phụ huynh
- Quản lý trạng thái học tập
- Phân lớp học sinh

**Domain Models:**
- **Student (Aggregate Root):**
  - StudentId, Code, FullName, Gender, DateOfBirth
  - Ethnicity, Address, TenantId, ClassId
  - Status (Studying, Dropped, OnHold, Trial)
  - Parents (List<Parent>)
  
- **Parent (Entity):**
  - ParentId, FullName, DateOfBirth, Username, PhoneNumber
  - Relationship (Father, Mother, Guardian)
  - StudentId (FK)

- **Class (Entity):**
  - ClassId, Name, Grade, AcademicYear, TenantId
  - TeacherId (primary teacher)
  - MaxStudents, CurrentStudents

**Value Objects:**
- Address (Street, Ward, District, City)
- StudentCode (format: SCHOOL_YEAR_SEQUENCE)

**Technology Stack:**
- **Framework:** ASP.NET Core 8.0 Web API
- **Database:** MySQL
- **ORM:** EF Core
- **Cache:** Redis

**API Endpoints:**
```
GET    /api/v1/students
POST   /api/v1/students
GET    /api/v1/students/{studentId}
PUT    /api/v1/students/{studentId}
DELETE /api/v1/students/{studentId}
PATCH  /api/v1/students/{studentId}/status
GET    /api/v1/students/{studentId}/parents
POST   /api/v1/students/{studentId}/parents
PUT    /api/v1/students/{studentId}/parents/{parentId}
DELETE /api/v1/students/{studentId}/parents/{parentId}
GET    /api/v1/classes
POST   /api/v1/classes
GET    /api/v1/classes/{classId}/students
POST   /api/v1/classes/{classId}/students/{studentId}
DELETE /api/v1/classes/{classId}/students/{studentId}
```

**Events Published:**
- `StudentCreated`
- `StudentUpdated`
- `StudentStatusChanged`
- `StudentAssignedToClass`
- `ParentAdded`

**Events Consumed:**
- `UserRegistered` (từ Identity Service - tạo link với Parent)

---

### 3. **Teacher Service** 👨‍🏫
**Bounded Context:** Teacher Management

**Trách nhiệm:**
- Quản lý hồ sơ giáo viên
- Phân công lớp học
- Quản lý lịch dạy
- Quản lý vai trò giáo viên

**Domain Models:**
- **Teacher (Aggregate Root):**
  - TeacherId, FullName, DateOfBirth, Gender
  - PhoneNumber (username), Email, Address
  - TenantId, Status, HireDate
  - Roles (List<TeacherRole>)
  - AssignedClasses (List<ClassAssignment>)

- **ClassAssignment (Entity):**
  - AssignmentId, TeacherId, ClassId
  - Role (Primary, Support, Substitute)
  - StartDate, EndDate

- **TeacherRole (Value Object):**
  - RoleType (Teacher, HeadTeacher, VicePrincipal, Principal)

**Technology Stack:**
- **Framework:** ASP.NET Core 8.0 Web API
- **Database:** MySQL
- **ORM:** EF Core
- **Cache:** Redis

**API Endpoints:**
```
GET    /api/v1/teachers
POST   /api/v1/teachers
GET    /api/v1/teachers/{teacherId}
PUT    /api/v1/teachers/{teacherId}
DELETE /api/v1/teachers/{teacherId}
GET    /api/v1/teachers/{teacherId}/classes
POST   /api/v1/teachers/{teacherId}/classes
DELETE /api/v1/teachers/{teacherId}/classes/{classId}
GET    /api/v1/teachers/{teacherId}/schedule
```

**Events Published:**
- `TeacherCreated`
- `TeacherUpdated`
- `TeacherAssignedToClass`
- `TeacherUnassignedFromClass`

---

### 4. **Attendance Service** ✅
**Bounded Context:** Attendance & Daily Check-in

**Trách nhiệm:**
- Điểm danh hàng ngày
- Nhận xét hàng ngày
- Tracking sự vắng mặt
- Báo cáo tỷ lệ đi học

**Domain Models:**
- **Attendance (Aggregate Root):**
  - AttendanceId, StudentId, ClassId, Date
  - Status (Present, Absent, Late, Excused)
  - CheckInTime, CheckOutTime
  - CheckedByTeacherId
  - Note, TenantId

- **DailyComment (Entity):**
  - CommentId, StudentId, Date, TeacherId
  - Content, Mood (Happy, Normal, Sad)
  - Activities (Eating, Sleeping, Playing)
  - HealthStatus
  - Media (List<MediaFile>)

**Technology Stack:**
- **Framework:** ASP.NET Core 8.0 Web API
- **Database:** MySQL
- **Cache:** Redis
- **Storage:** MinIO for media files

**API Endpoints:**
```
POST   /api/v1/attendance/check-in
POST   /api/v1/attendance/check-out
GET    /api/v1/attendance/class/{classId}/date/{date}
PUT    /api/v1/attendance/{attendanceId}
GET    /api/v1/attendance/student/{studentId}/month/{month}
POST   /api/v1/comments
GET    /api/v1/comments/student/{studentId}/date/{date}
PUT    /api/v1/comments/{commentId}
POST   /api/v1/comments/{commentId}/media
```

**Events Published:**
- `StudentCheckedIn`
- `StudentCheckedOut`
- `DailyCommentCreated`
- `AttendanceReportGenerated`

**Events Consumed:**
- `StudentAssignedToClass`
- `LeaveApproved` (từ Leave Service)

---

### 5. **Assessment Service** 📊
**Bounded Context:** Student Assessment & Evaluation

**Trách nhiệm:**
- Đánh giá học sinh theo tiêu chí
- Tracking sự phát triển
- Portfolio học sinh
- Báo cáo định kỳ

**Domain Models:**
- **Assessment (Aggregate Root):**
  - AssessmentId, StudentId, TeacherId
  - Period (Daily, Weekly, Monthly, Semester)
  - Date, TenantId
  - Criteria (List<AssessmentCriterion>)
  - OverallScore, Comment
  - Media (List<MediaFile>)

- **AssessmentCriterion (Entity):**
  - CriterionId, Name, Category
  - Score, MaxScore, Note

- **DevelopmentMilestone (Entity):**
  - MilestoneId, StudentId, Category
  - Description, AchievedDate
  - Evidence (media files)

**Categories:**
- Physical Development (Phát triển thể chất)
- Cognitive Development (Nhận thức)
- Language Development (Ngôn ngữ)
- Social-Emotional Development (Cảm xúc - xã hội)
- Creative Development (Sáng tạo)

**Technology Stack:**
- **Framework:** ASP.NET Core 8.0 Web API
- **Database:** MySQL
- **Storage:** MinIO for media files
- **Cache:** Redis

**API Endpoints:**
```
POST   /api/v1/assessments
GET    /api/v1/assessments/{assessmentId}
PUT    /api/v1/assessments/{assessmentId}
GET    /api/v1/assessments/student/{studentId}
POST   /api/v1/assessments/{assessmentId}/media
GET    /api/v1/milestones/student/{studentId}
POST   /api/v1/milestones
```

**Events Published:**
- `AssessmentCreated`
- `MilestoneAchieved`
- `DevelopmentReportGenerated`

---

### 6. **News Feed Service** 📰
**Bounded Context:** School News & Announcements

**Trách nhiệm:**
- Đăng bảng tin
- Phân phối theo lớp/toàn trường
- Thông báo khẩn cấp
- Tương tác (like, comment)

**Domain Models:**
- **Post (Aggregate Root):**
  - PostId, Title, Content, AuthorId
  - TenantId, CreatedAt, UpdatedAt
  - Scope (School, Class, Custom)
  - TargetAudience (ClassIds[], UserIds[])
  - Priority (Normal, Important, Urgent)
  - Status (Draft, Published, Archived)
  - Media (List<MediaFile>)
  - Interactions (Likes, Comments)

- **Comment (Entity):**
  - CommentId, PostId, UserId, Content
  - CreatedAt, ParentCommentId (for nested comments)

**Technology Stack:**
- **Framework:** ASP.NET Core 8.0 Web API
- **Database:** MongoDB (better for social feed)
- **Cache:** Redis
- **Storage:** MinIO

**API Endpoints:**
```
POST   /api/v1/posts
GET    /api/v1/posts
GET    /api/v1/posts/{postId}
PUT    /api/v1/posts/{postId}
DELETE /api/v1/posts/{postId}
POST   /api/v1/posts/{postId}/publish
POST   /api/v1/posts/{postId}/like
POST   /api/v1/posts/{postId}/comments
GET    /api/v1/posts/{postId}/comments
GET    /api/v1/feed (personalized feed)
```

**Events Published:**
- `PostCreated`
- `PostPublished`
- `UrgentAnnouncementPosted`
- `CommentAdded`

---

### 7. **Chat Service** 💬
**Bounded Context:** Real-time Communication

**Trách nhiệm:**
- Chat 1-1
- Chat nhóm (theo học sinh, lớp, custom)
- Lưu trữ lịch sử chat
- File sharing
- Real-time message delivery

**Domain Models:**
- **Conversation (Aggregate Root):**
  - ConversationId, Type (OneToOne, StudentGroup, ClassGroup, CustomGroup)
  - TenantId, CreatedAt, LastMessageAt
  - Participants (List<Participant>)
  - Metadata (StudentId, ClassId cho group chats)

- **Message (Entity):**
  - MessageId, ConversationId, SenderId
  - Content, Type (Text, Image, File, Video)
  - SentAt, Status (Sent, Delivered, Read)
  - Attachments (List<Attachment>)
  - ReplyToMessageId

- **Participant (Value Object):**
  - UserId, JoinedAt, Role (Member, Admin)
  - LastReadAt

**Technology Stack:**
- **Framework:** ASP.NET Core 8.0 Web API + SignalR
- **Database:** MongoDB (chat history)
- **Cache:** Redis (online users, typing indicators)
- **Storage:** MinIO (file attachments)
- **Real-time:** SignalR

**API Endpoints:**
```
POST   /api/v1/conversations
GET    /api/v1/conversations
GET    /api/v1/conversations/{conversationId}
POST   /api/v1/conversations/{conversationId}/messages
GET    /api/v1/conversations/{conversationId}/messages
PUT    /api/v1/conversations/{conversationId}/messages/{messageId}/read
POST   /api/v1/conversations/{conversationId}/participants
DELETE /api/v1/conversations/{conversationId}/participants/{userId}
```

**SignalR Hubs:**
```
ChatHub:
  - SendMessage
  - JoinConversation
  - LeaveConversation
  - TypingIndicator
  - MarkAsRead
```

**Events Published:**
- `MessageSent`
- `MessageDelivered`
- `MessageRead`
- `ConversationCreated`

**Events Consumed:**
- `StudentCreated` → Auto-create student group
- `ClassCreated` → Auto-create class group

---

### 8. **Payment Service** 💰
**Bounded Context:** Fee & Payment Management

**Trách nhiệm:**
- Quản lý học phí
- Thanh toán online
- Lịch sử giao dịch
- Nhắc nợ tự động
- Hóa đơn/biên lai

**Domain Models:**
- **Invoice (Aggregate Root):**
  - InvoiceId, StudentId, TenantId
  - InvoiceNumber, InvoiceDate, DueDate
  - TotalAmount, PaidAmount, RemainingAmount
  - Status (Pending, PartiallyPaid, Paid, Overdue, Cancelled)
  - InvoiceItems (List<InvoiceItem>)

- **InvoiceItem (Entity):**
  - ItemId, Description, FeeType
  - Amount, Quantity, Discount

- **Payment (Entity):**
  - PaymentId, InvoiceId, Amount
  - PaymentMethod (Cash, BankTransfer, Online)
  - PaymentDate, TransactionId
  - Status (Pending, Completed, Failed, Refunded)
  - PaymentGateway (VNPay, MoMo, ZaloPay)

**Technology Stack:**
- **Framework:** ASP.NET Core 8.0 Web API
- **Database:** MySQL
- **Cache:** Redis
- **Payment Gateway Integration:** VNPay, MoMo SDK

**API Endpoints:**
```
POST   /api/v1/invoices
GET    /api/v1/invoices
GET    /api/v1/invoices/{invoiceId}
PUT    /api/v1/invoices/{invoiceId}
POST   /api/v1/payments
GET    /api/v1/payments/{paymentId}
POST   /api/v1/payments/process (payment gateway)
GET    /api/v1/payments/callback (webhook from gateway)
GET    /api/v1/students/{studentId}/invoices
GET    /api/v1/students/{studentId}/payment-history
```

**Events Published:**
- `InvoiceCreated`
- `PaymentCompleted`
- `PaymentFailed`
- `InvoiceOverdue`
- `RemindersScheduled`

---

### 9. **Menu Service** 🍱
**Bounded Context:** Daily Menu Management

**Trách nhiệm:**
- Quản lý thực đơn hàng ngày/tuần
- Thông tin dinh dưỡng
- Thông báo cho phụ huynh

**Domain Models:**
- **Menu (Aggregate Root):**
  - MenuId, Date, TenantId, ClassId (null = all classes)
  - MealSessions (List<MealSession>)
  - CreatedBy, ApprovedBy

- **MealSession (Entity):**
  - SessionId, Type (Breakfast, Lunch, Snack)
  - Dishes (List<Dish>)
  - TotalCalories

- **Dish (Value Object):**
  - Name, Description, Ingredients
  - Calories, Nutrition (Protein, Carbs, Fat)
  - AllergenWarnings

**Technology Stack:**
- **Framework:** ASP.NET Core 8.0 Web API
- **Database:** MySQL
- **Cache:** Redis

**API Endpoints:**
```
POST   /api/v1/menus
GET    /api/v1/menus/date/{date}
GET    /api/v1/menus/week/{weekStart}
PUT    /api/v1/menus/{menuId}
DELETE /api/v1/menus/{menuId}
POST   /api/v1/menus/{menuId}/publish
```

**Events Published:**
- `MenuCreated`
- `MenuPublished`
- `WeeklyMenuAvailable`

---

### 10. **Leave Service** 📅
**Bounded Context:** Leave & Absence Management

**Trách nhiệm:**
- Xin nghỉ phép (học sinh, giáo viên)
- Phê duyệt nghỉ phép
- Lịch sử nghỉ phép

**Domain Models:**
- **LeaveRequest (Aggregate Root):**
  - LeaveRequestId, TenantId, RequesterId
  - RequesterType (Student, Teacher)
  - StartDate, EndDate, Reason
  - Status (Pending, Approved, Rejected)
  - ApprovedBy, ApprovedAt, RejectionReason

**Technology Stack:**
- **Framework:** ASP.NET Core 8.0 Web API
- **Database:** MySQL
- **Cache:** Redis

**API Endpoints:**
```
POST   /api/v1/leave-requests
GET    /api/v1/leave-requests
GET    /api/v1/leave-requests/{requestId}
PUT    /api/v1/leave-requests/{requestId}
POST   /api/v1/leave-requests/{requestId}/approve
POST   /api/v1/leave-requests/{requestId}/reject
GET    /api/v1/leave-requests/student/{studentId}
GET    /api/v1/leave-requests/teacher/{teacherId}
```

**Events Published:**
- `LeaveRequestCreated`
- `LeaveApproved`
- `LeaveRejected`

---

### 11. **Camera Service** 📹
**Bounded Context:** Surveillance & Live Streaming

**Trách nhiệm:**
- Camera streaming trực tiếp
- Recording & playback
- Access control
- Camera management

**Domain Models:**
- **Camera (Aggregate Root):**
  - CameraId, Name, Location, ClassId
  - TenantId, RTSPUrl, Status
  - IsOnline, StreamingUrl

- **CameraAccess (Entity):**
  - AccessId, CameraId, UserId
  - AccessType (Live, Recording)
  - GrantedAt, ExpiresAt

- **Recording (Entity):**
  - RecordingId, CameraId, StartTime, EndTime
  - StoragePath, Duration, FileSize

**Technology Stack:**
- **Framework:** ASP.NET Core 8.0 Web API
- **Streaming:** WebRTC / HLS
- **Media Server:** Kurento / Janus / Wowza
- **Database:** MySQL (metadata)
- **Storage:** MinIO / NAS for recordings

**API Endpoints:**
```
GET    /api/v1/cameras
POST   /api/v1/cameras
GET    /api/v1/cameras/{cameraId}
GET    /api/v1/cameras/{cameraId}/stream-url
GET    /api/v1/cameras/{cameraId}/recordings
GET    /api/v1/cameras/{cameraId}/recordings/{recordingId}/playback
POST   /api/v1/cameras/{cameraId}/access
```

**Events Published:**
- `CameraOnline`
- `CameraOffline`
- `RecordingStarted`
- `RecordingCompleted`

---

### 12. **Report Service** 📈
**Bounded Context:** Reporting & Analytics

**Trách nhiệm:**
- Báo cáo điểm danh
- Báo cáo học phí
- Báo cáo phát triển học sinh
- Dashboard analytics

**Domain Models:**
- **Report (Aggregate Root):**
  - ReportId, Type, TenantId, GeneratedBy
  - Parameters, GeneratedAt
  - Format (PDF, Excel, JSON)
  - StoragePath, Status

**Technology Stack:**
- **Framework:** ASP.NET Core 8.0 Web API
- **Database:** MySQL + MongoDB (aggregated data)
- **Reporting:** FastReport / Crystal Reports
- **Cache:** Redis
- **Background Jobs:** Hangfire

**API Endpoints:**
```
POST   /api/v1/reports/generate
GET    /api/v1/reports/{reportId}
GET    /api/v1/reports/{reportId}/download
GET    /api/v1/dashboard/attendance
GET    /api/v1/dashboard/payment
GET    /api/v1/dashboard/overview
```

**Events Consumed:**
- All major events from other services for analytics

---

### 13. **Notification Service** 🔔
**Bounded Context:** Notification & Alert

**Trách nhiệm:**
- Push notifications
- Email notifications
- SMS notifications
- In-app notifications
- Notification preferences

**Domain Models:**
- **Notification (Aggregate Root):**
  - NotificationId, TenantId, UserId
  - Type, Title, Content, Priority
  - Channels (Push, Email, SMS, InApp)
  - Status (Pending, Sent, Failed)
  - CreatedAt, SentAt, ReadAt

**Technology Stack:**
- **Framework:** ASP.NET Core 8.0 Web API + SignalR
- **Database:** MongoDB
- **Queue:** RabbitMQ
- **Push:** Firebase Cloud Messaging (FCM)
- **Email:** SendGrid / SMTP
- **SMS:** Twilio / ESMS

**API Endpoints:**
```
GET    /api/v1/notifications
GET    /api/v1/notifications/{notificationId}
PUT    /api/v1/notifications/{notificationId}/read
PUT    /api/v1/notifications/read-all
GET    /api/v1/notifications/preferences
PUT    /api/v1/notifications/preferences
```

**SignalR Hub:**
```
NotificationHub:
  - ReceiveNotification
```

**Events Consumed:**
- All major events requiring user notification

---

## 🔄 Inter-Service Communication

### Synchronous Communication (gRPC)
- **Identity Service:** Authentication validation
- **Student/Teacher Service:** Get entity details

### Asynchronous Communication (RabbitMQ/Kafka)
- **Domain Events:** All business events
- **Integration Events:** Cross-service events
- **Event Bus Pattern:** Pub/Sub model

### API Gateway (YARP / Ocelot)
- Request routing
- Load balancing
- Rate limiting
- Authentication/Authorization
- API aggregation

---

## 📦 Shared Libraries

### EMIS.SharedKernel
- Base Entity, Aggregate Root
- Value Object base
- Domain Event base
- Repository interfaces

### EMIS.BuildingBlocks
- Multi-tenant utilities
- API response wrapper
- Exception handling
- Validation
- Logging helpers
- Event bus abstraction

---

**Next:** [03-Domain-Models.md](./03-Domain-Models.md)
