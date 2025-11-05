# Domain Models & Database Schema Design

## 🎯 Database Strategy

### Multi-Tenant Data Isolation
**Hybrid Approach:**
- **Shared Infrastructure:** Common services (Identity, Notification)
- **Isolated Tenant Data:** Student, Teacher, Attendance, Payment data
- **Connection String per Tenant:** Dynamic database selection

### Database Types
- **MySQL:** Transactional, relational data
- **MongoDB:** Chat history, Logs, News feed
- **Redis:** Cache, Session, Real-time data
- **MinIO/S3:** File storage

---

## 📊 Domain Models by Service

### 1. Identity Service - MySQL

#### Users Table
```sql
CREATE TABLE Users (
    UserId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    Username VARCHAR(100) NOT NULL,
    Email VARCHAR(255),
    PhoneNumber VARCHAR(20) NOT NULL,
    PasswordHash VARCHAR(500) NOT NULL,
    FullName NVARCHAR(255),
    Status ENUM('Active', 'Inactive', 'Locked') DEFAULT 'Active',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    LastLoginAt DATETIME,
    INDEX idx_tenant_username (TenantId, Username),
    INDEX idx_phone (PhoneNumber),
    UNIQUE KEY unique_tenant_username (TenantId, Username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### Roles Table
```sql
CREATE TABLE Roles (
    RoleId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    Name VARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    IsSystemRole BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_tenant (TenantId),
    UNIQUE KEY unique_tenant_role (TenantId, Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### Permissions Table
```sql
CREATE TABLE Permissions (
    PermissionId VARCHAR(36) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Resource VARCHAR(100) NOT NULL,
    Action VARCHAR(50) NOT NULL,
    Description NVARCHAR(500),
    UNIQUE KEY unique_permission (Resource, Action)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### UserRoles Table (Many-to-Many)
```sql
CREATE TABLE UserRoles (
    UserId VARCHAR(36) NOT NULL,
    RoleId VARCHAR(36) NOT NULL,
    AssignedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    AssignedBy VARCHAR(36),
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### RolePermissions Table (Many-to-Many)
```sql
CREATE TABLE RolePermissions (
    RoleId VARCHAR(36) NOT NULL,
    PermissionId VARCHAR(36) NOT NULL,
    PRIMARY KEY (RoleId, PermissionId),
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId) ON DELETE CASCADE,
    FOREIGN KEY (PermissionId) REFERENCES Permissions(PermissionId) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### RefreshTokens Table
```sql
CREATE TABLE RefreshTokens (
    TokenId VARCHAR(36) PRIMARY KEY,
    UserId VARCHAR(36) NOT NULL,
    Token VARCHAR(500) NOT NULL,
    ExpiresAt DATETIME NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    RevokedAt DATETIME,
    IsRevoked BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    INDEX idx_token (Token),
    INDEX idx_user_active (UserId, IsRevoked, ExpiresAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### Tenants Table (Shared - System Level)
```sql
CREATE TABLE Tenants (
    TenantId VARCHAR(36) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Subdomain VARCHAR(100) UNIQUE NOT NULL,
    ConnectionString VARCHAR(1000),
    Status ENUM('Active', 'Suspended', 'Inactive') DEFAULT 'Active',
    SubscriptionPlan VARCHAR(50),
    SubscriptionExpiresAt DATETIME,
    MaxUsers INT DEFAULT 1000,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_subdomain (Subdomain)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

### 2. Student Service - MySQL (Per Tenant)

#### Students Table
```sql
CREATE TABLE Students (
    StudentId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    Code VARCHAR(50) NOT NULL,
    FullName NVARCHAR(255) NOT NULL,
    Gender ENUM('Male', 'Female', 'Other') NOT NULL,
    DateOfBirth DATE NOT NULL,
    Ethnicity NVARCHAR(100),
    Street NVARCHAR(255),
    Ward NVARCHAR(100),
    District NVARCHAR(100),
    City NVARCHAR(100),
    Status ENUM('Studying', 'Dropped', 'OnHold', 'Trial') DEFAULT 'Studying',
    ClassId VARCHAR(36),
    EnrollmentDate DATE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_tenant (TenantId),
    INDEX idx_code (TenantId, Code),
    INDEX idx_class (ClassId),
    INDEX idx_status (Status),
    UNIQUE KEY unique_tenant_code (TenantId, Code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### Parents Table
```sql
CREATE TABLE Parents (
    ParentId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    StudentId VARCHAR(36) NOT NULL,
    UserId VARCHAR(36),
    FullName NVARCHAR(255) NOT NULL,
    DateOfBirth DATE,
    PhoneNumber VARCHAR(20) NOT NULL,
    Email VARCHAR(255),
    Relationship ENUM('Father', 'Mother', 'Guardian', 'Other') NOT NULL,
    IsPrimary BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (StudentId) REFERENCES Students(StudentId) ON DELETE CASCADE,
    INDEX idx_student (StudentId),
    INDEX idx_tenant (TenantId),
    INDEX idx_phone (PhoneNumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### Classes Table
```sql
CREATE TABLE Classes (
    ClassId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Grade VARCHAR(50),
    AcademicYear VARCHAR(20) NOT NULL,
    MaxStudents INT DEFAULT 30,
    CurrentStudents INT DEFAULT 0,
    PrimaryTeacherId VARCHAR(36),
    Status ENUM('Active', 'Inactive', 'Archived') DEFAULT 'Active',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_tenant (TenantId),
    INDEX idx_academic_year (AcademicYear),
    UNIQUE KEY unique_tenant_class (TenantId, Name, AcademicYear)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

### 3. Teacher Service - MySQL (Per Tenant)

#### Teachers Table
```sql
CREATE TABLE Teachers (
    TeacherId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    UserId VARCHAR(36) NOT NULL,
    FullName NVARCHAR(255) NOT NULL,
    DateOfBirth DATE,
    Gender ENUM('Male', 'Female', 'Other') NOT NULL,
    PhoneNumber VARCHAR(20) NOT NULL,
    Email VARCHAR(255),
    Street NVARCHAR(255),
    Ward NVARCHAR(100),
    District NVARCHAR(100),
    City NVARCHAR(100),
    HireDate DATE,
    Status ENUM('Active', 'OnLeave', 'Resigned', 'Terminated') DEFAULT 'Active',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    INDEX idx_tenant (TenantId),
    INDEX idx_user (UserId),
    UNIQUE KEY unique_tenant_user (TenantId, UserId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### TeacherClassAssignments Table
```sql
CREATE TABLE TeacherClassAssignments (
    AssignmentId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    TeacherId VARCHAR(36) NOT NULL,
    ClassId VARCHAR(36) NOT NULL,
    Role ENUM('Primary', 'Support', 'Substitute') DEFAULT 'Primary',
    StartDate DATE NOT NULL,
    EndDate DATE,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (TeacherId) REFERENCES Teachers(TeacherId) ON DELETE CASCADE,
    FOREIGN KEY (ClassId) REFERENCES Classes(ClassId) ON DELETE CASCADE,
    INDEX idx_teacher (TeacherId),
    INDEX idx_class (ClassId),
    INDEX idx_active (IsActive, EndDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

### 4. Attendance Service - MySQL (Per Tenant)

#### Attendances Table
```sql
CREATE TABLE Attendances (
    AttendanceId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    StudentId VARCHAR(36) NOT NULL,
    ClassId VARCHAR(36) NOT NULL,
    Date DATE NOT NULL,
    Status ENUM('Present', 'Absent', 'Late', 'Excused') NOT NULL,
    CheckInTime TIME,
    CheckOutTime TIME,
    CheckedByTeacherId VARCHAR(36),
    Note NVARCHAR(500),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_student_date (StudentId, Date),
    INDEX idx_class_date (ClassId, Date),
    INDEX idx_tenant_date (TenantId, Date),
    UNIQUE KEY unique_student_date (StudentId, Date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### DailyComments Table
```sql
CREATE TABLE DailyComments (
    CommentId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    StudentId VARCHAR(36) NOT NULL,
    Date DATE NOT NULL,
    TeacherId VARCHAR(36) NOT NULL,
    Content NVARCHAR(2000),
    Mood ENUM('Happy', 'Normal', 'Sad', 'Crying') DEFAULT 'Normal',
    EatingStatus ENUM('Good', 'Normal', 'Poor', 'Refused') DEFAULT 'Normal',
    SleepingStatus ENUM('Good', 'Normal', 'Poor', 'NoSleep') DEFAULT 'Normal',
    HealthStatus NVARCHAR(500),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_student_date (StudentId, Date),
    INDEX idx_teacher (TeacherId),
    UNIQUE KEY unique_student_date (StudentId, Date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### CommentMedia Table
```sql
CREATE TABLE CommentMedia (
    MediaId VARCHAR(36) PRIMARY KEY,
    CommentId VARCHAR(36) NOT NULL,
    MediaType ENUM('Image', 'Video') NOT NULL,
    Url VARCHAR(1000) NOT NULL,
    ThumbnailUrl VARCHAR(1000),
    FileSize BIGINT,
    UploadedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CommentId) REFERENCES DailyComments(CommentId) ON DELETE CASCADE,
    INDEX idx_comment (CommentId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

### 5. Assessment Service - MySQL (Per Tenant)

#### Assessments Table
```sql
CREATE TABLE Assessments (
    AssessmentId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    StudentId VARCHAR(36) NOT NULL,
    TeacherId VARCHAR(36) NOT NULL,
    Period ENUM('Daily', 'Weekly', 'Monthly', 'Semester', 'Yearly') NOT NULL,
    AssessmentDate DATE NOT NULL,
    OverallScore DECIMAL(5,2),
    Comment NVARCHAR(2000),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_student (StudentId),
    INDEX idx_teacher (TeacherId),
    INDEX idx_period_date (Period, AssessmentDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### AssessmentCriteria Table
```sql
CREATE TABLE AssessmentCriteria (
    CriterionId VARCHAR(36) PRIMARY KEY,
    AssessmentId VARCHAR(36) NOT NULL,
    Category ENUM('Physical', 'Cognitive', 'Language', 'SocialEmotional', 'Creative') NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Score DECIMAL(5,2) NOT NULL,
    MaxScore DECIMAL(5,2) NOT NULL,
    Note NVARCHAR(500),
    FOREIGN KEY (AssessmentId) REFERENCES Assessments(AssessmentId) ON DELETE CASCADE,
    INDEX idx_assessment (AssessmentId),
    INDEX idx_category (Category)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### DevelopmentMilestones Table
```sql
CREATE TABLE DevelopmentMilestones (
    MilestoneId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    StudentId VARCHAR(36) NOT NULL,
    Category ENUM('Physical', 'Cognitive', 'Language', 'SocialEmotional', 'Creative') NOT NULL,
    Title NVARCHAR(255) NOT NULL,
    Description NVARCHAR(1000),
    AchievedDate DATE NOT NULL,
    RecordedByTeacherId VARCHAR(36),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_student (StudentId),
    INDEX idx_category (Category),
    INDEX idx_date (AchievedDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### AssessmentMedia Table
```sql
CREATE TABLE AssessmentMedia (
    MediaId VARCHAR(36) PRIMARY KEY,
    AssessmentId VARCHAR(36),
    MilestoneId VARCHAR(36),
    MediaType ENUM('Image', 'Video', 'Document') NOT NULL,
    Url VARCHAR(1000) NOT NULL,
    ThumbnailUrl VARCHAR(1000),
    Description NVARCHAR(500),
    UploadedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_assessment (AssessmentId),
    INDEX idx_milestone (MilestoneId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

### 6. News Feed Service - MongoDB

#### Posts Collection
```javascript
{
  _id: ObjectId,
  tenantId: String,
  postId: String, // UUID
  title: String,
  content: String,
  authorId: String,
  authorName: String,
  authorRole: String, // Teacher, Admin
  scope: String, // School, Class, Custom
  targetAudience: {
    classIds: [String],
    userIds: [String],
    roles: [String]
  },
  priority: String, // Normal, Important, Urgent
  status: String, // Draft, Published, Archived
  media: [
    {
      mediaId: String,
      type: String, // Image, Video
      url: String,
      thumbnailUrl: String
    }
  ],
  interactions: {
    likesCount: Number,
    commentsCount: Number,
    likedBy: [String] // UserIds
  },
  createdAt: ISODate,
  updatedAt: ISODate,
  publishedAt: ISODate
}

// Indexes
db.posts.createIndex({ tenantId: 1, status: 1, publishedAt: -1 });
db.posts.createIndex({ "targetAudience.classIds": 1 });
db.posts.createIndex({ authorId: 1 });
```

#### Comments Collection
```javascript
{
  _id: ObjectId,
  commentId: String,
  postId: String,
  tenantId: String,
  userId: String,
  userName: String,
  content: String,
  parentCommentId: String, // null for top-level comments
  createdAt: ISODate,
  updatedAt: ISODate
}

// Indexes
db.comments.createIndex({ postId: 1, createdAt: 1 });
db.comments.createIndex({ parentCommentId: 1 });
```

---

### 7. Chat Service - MongoDB

#### Conversations Collection
```javascript
{
  _id: ObjectId,
  conversationId: String,
  tenantId: String,
  type: String, // OneToOne, StudentGroup, ClassGroup, CustomGroup
  name: String, // For group chats
  metadata: {
    studentId: String, // For StudentGroup
    classId: String,   // For ClassGroup
    studentName: String,
    className: String
  },
  participants: [
    {
      userId: String,
      userName: String,
      role: String, // Member, Admin
      joinedAt: ISODate,
      lastReadAt: ISODate
    }
  ],
  lastMessage: {
    messageId: String,
    content: String,
    senderId: String,
    senderName: String,
    sentAt: ISODate
  },
  createdAt: ISODate,
  updatedAt: ISODate
}

// Indexes
db.conversations.createIndex({ tenantId: 1, "participants.userId": 1 });
db.conversations.createIndex({ conversationId: 1 });
db.conversations.createIndex({ type: 1, "metadata.studentId": 1 });
```

#### Messages Collection
```javascript
{
  _id: ObjectId,
  messageId: String,
  conversationId: String,
  tenantId: String,
  senderId: String,
  senderName: String,
  content: String,
  type: String, // Text, Image, File, Video, Audio
  attachments: [
    {
      attachmentId: String,
      fileName: String,
      fileType: String,
      fileSize: Number,
      url: String,
      thumbnailUrl: String
    }
  ],
  replyTo: {
    messageId: String,
    content: String,
    senderName: String
  },
  status: String, // Sent, Delivered, Read
  deliveredTo: [
    {
      userId: String,
      deliveredAt: ISODate
    }
  ],
  readBy: [
    {
      userId: String,
      readAt: ISODate
    }
  ],
  sentAt: ISODate,
  editedAt: ISODate,
  isDeleted: Boolean
}

// Indexes
db.messages.createIndex({ conversationId: 1, sentAt: -1 });
db.messages.createIndex({ messageId: 1 });
db.messages.createIndex({ tenantId: 1, senderId: 1 });
```

---

### 8. Payment Service - MySQL (Per Tenant)

#### Invoices Table
```sql
CREATE TABLE Invoices (
    InvoiceId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    InvoiceNumber VARCHAR(50) NOT NULL,
    StudentId VARCHAR(36) NOT NULL,
    AcademicYear VARCHAR(20),
    Month INT,
    InvoiceDate DATE NOT NULL,
    DueDate DATE NOT NULL,
    TotalAmount DECIMAL(15,2) NOT NULL,
    PaidAmount DECIMAL(15,2) DEFAULT 0,
    RemainingAmount DECIMAL(15,2) NOT NULL,
    Status ENUM('Pending', 'PartiallyPaid', 'Paid', 'Overdue', 'Cancelled') DEFAULT 'Pending',
    Note NVARCHAR(500),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_tenant (TenantId),
    INDEX idx_student (StudentId),
    INDEX idx_status (Status),
    INDEX idx_due_date (DueDate),
    UNIQUE KEY unique_invoice_number (TenantId, InvoiceNumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### InvoiceItems Table
```sql
CREATE TABLE InvoiceItems (
    ItemId VARCHAR(36) PRIMARY KEY,
    InvoiceId VARCHAR(36) NOT NULL,
    FeeType ENUM('Tuition', 'Meal', 'Transportation', 'Activity', 'Material', 'Other') NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    Quantity DECIMAL(10,2) DEFAULT 1,
    UnitPrice DECIMAL(15,2) NOT NULL,
    Discount DECIMAL(15,2) DEFAULT 0,
    Amount DECIMAL(15,2) NOT NULL,
    FOREIGN KEY (InvoiceId) REFERENCES Invoices(InvoiceId) ON DELETE CASCADE,
    INDEX idx_invoice (InvoiceId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### Payments Table
```sql
CREATE TABLE Payments (
    PaymentId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    InvoiceId VARCHAR(36) NOT NULL,
    Amount DECIMAL(15,2) NOT NULL,
    PaymentMethod ENUM('Cash', 'BankTransfer', 'VNPay', 'MoMo', 'ZaloPay') NOT NULL,
    PaymentDate DATETIME NOT NULL,
    TransactionId VARCHAR(100),
    PaymentGateway VARCHAR(50),
    Status ENUM('Pending', 'Completed', 'Failed', 'Refunded') DEFAULT 'Pending',
    Note NVARCHAR(500),
    ProcessedBy VARCHAR(36),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (InvoiceId) REFERENCES Invoices(InvoiceId),
    INDEX idx_invoice (InvoiceId),
    INDEX idx_transaction (TransactionId),
    INDEX idx_status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

### 9. Menu Service - MySQL (Per Tenant)

#### Menus Table
```sql
CREATE TABLE Menus (
    MenuId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    Date DATE NOT NULL,
    ClassId VARCHAR(36), -- NULL = all classes
    Status ENUM('Draft', 'Published', 'Archived') DEFAULT 'Draft',
    CreatedBy VARCHAR(36),
    ApprovedBy VARCHAR(36),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    PublishedAt DATETIME,
    INDEX idx_tenant_date (TenantId, Date),
    INDEX idx_class (ClassId),
    UNIQUE KEY unique_tenant_class_date (TenantId, ClassId, Date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### MealSessions Table
```sql
CREATE TABLE MealSessions (
    SessionId VARCHAR(36) PRIMARY KEY,
    MenuId VARCHAR(36) NOT NULL,
    Type ENUM('Breakfast', 'MorningSnack', 'Lunch', 'AfternoonSnack', 'Dinner') NOT NULL,
    TotalCalories DECIMAL(8,2),
    FOREIGN KEY (MenuId) REFERENCES Menus(MenuId) ON DELETE CASCADE,
    INDEX idx_menu (MenuId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### Dishes Table
```sql
CREATE TABLE Dishes (
    DishId VARCHAR(36) PRIMARY KEY,
    SessionId VARCHAR(36) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(1000),
    Ingredients NVARCHAR(1000),
    Calories DECIMAL(8,2),
    Protein DECIMAL(8,2),
    Carbs DECIMAL(8,2),
    Fat DECIMAL(8,2),
    AllergenWarnings NVARCHAR(500),
    FOREIGN KEY (SessionId) REFERENCES MealSessions(SessionId) ON DELETE CASCADE,
    INDEX idx_session (SessionId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

### 10. Leave Service - MySQL (Per Tenant)

#### LeaveRequests Table
```sql
CREATE TABLE LeaveRequests (
    LeaveRequestId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    RequesterId VARCHAR(36) NOT NULL,
    RequesterType ENUM('Student', 'Teacher') NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Reason NVARCHAR(1000) NOT NULL,
    Status ENUM('Pending', 'Approved', 'Rejected', 'Cancelled') DEFAULT 'Pending',
    ApprovedBy VARCHAR(36),
    ApprovedAt DATETIME,
    RejectionReason NVARCHAR(500),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_requester (RequesterId, RequesterType),
    INDEX idx_status (Status),
    INDEX idx_dates (StartDate, EndDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

### 11. Camera Service - MySQL (Per Tenant)

#### Cameras Table
```sql
CREATE TABLE Cameras (
    CameraId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Location NVARCHAR(255),
    ClassId VARCHAR(36),
    RTSPUrl VARCHAR(500),
    StreamingUrl VARCHAR(500),
    Status ENUM('Online', 'Offline', 'Maintenance') DEFAULT 'Offline',
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_tenant (TenantId),
    INDEX idx_class (ClassId),
    INDEX idx_status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### CameraAccess Table
```sql
CREATE TABLE CameraAccess (
    AccessId VARCHAR(36) PRIMARY KEY,
    CameraId VARCHAR(36) NOT NULL,
    UserId VARCHAR(36) NOT NULL,
    AccessType ENUM('Live', 'Recording', 'Both') DEFAULT 'Live',
    GrantedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    ExpiresAt DATETIME,
    GrantedBy VARCHAR(36),
    FOREIGN KEY (CameraId) REFERENCES Cameras(CameraId) ON DELETE CASCADE,
    INDEX idx_camera (CameraId),
    INDEX idx_user (UserId),
    INDEX idx_expires (ExpiresAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### Recordings Table
```sql
CREATE TABLE Recordings (
    RecordingId VARCHAR(36) PRIMARY KEY,
    CameraId VARCHAR(36) NOT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    Duration INT, -- seconds
    StoragePath VARCHAR(1000),
    FileSize BIGINT,
    Status ENUM('Recording', 'Completed', 'Failed', 'Deleted') DEFAULT 'Recording',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CameraId) REFERENCES Cameras(CameraId) ON DELETE CASCADE,
    INDEX idx_camera_time (CameraId, StartTime),
    INDEX idx_status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

### 12. Notification Service - MongoDB

#### Notifications Collection
```javascript
{
  _id: ObjectId,
  notificationId: String,
  tenantId: String,
  userId: String,
  type: String, // Attendance, Payment, NewsFeed, Chat, Assessment, etc.
  title: String,
  content: String,
  priority: String, // Low, Normal, High, Urgent
  channels: [String], // Push, Email, SMS, InApp
  status: String, // Pending, Sent, Failed
  data: Object, // Additional context data
  createdAt: ISODate,
  sentAt: ISODate,
  readAt: ISODate,
  isRead: Boolean
}

// Indexes
db.notifications.createIndex({ tenantId: 1, userId: 1, createdAt: -1 });
db.notifications.createIndex({ userId: 1, isRead: 1 });
db.notifications.createIndex({ status: 1, createdAt: 1 });
```

---

## 🔄 Event Sourcing Tables (Optional - for Audit)

### DomainEvents Table
```sql
CREATE TABLE DomainEvents (
    EventId VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36),
    AggregateId VARCHAR(36) NOT NULL,
    AggregateType VARCHAR(100) NOT NULL,
    EventType VARCHAR(255) NOT NULL,
    EventData JSON NOT NULL,
    OccurredAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    ProcessedAt DATETIME,
    UserId VARCHAR(36),
    INDEX idx_aggregate (AggregateId, AggregateType),
    INDEX idx_occurred (OccurredAt),
    INDEX idx_tenant (TenantId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 🔍 Query Optimization

### Redis Cache Strategy
```
Key Patterns:
- tenant:{tenantId}:user:{userId}
- tenant:{tenantId}:student:{studentId}
- tenant:{tenantId}:class:{classId}:students
- tenant:{tenantId}:attendance:{date}
- session:{sessionId}
- token:blacklist:{token}
```

### Database Partitioning
- **Attendance:** Partition by date (monthly/yearly)
- **Payments:** Partition by date
- **Messages:** Partition by month
- **Logs:** Time-based partitioning

---

**Next:** [04-API-Contracts.md](./04-API-Contracts.md)
