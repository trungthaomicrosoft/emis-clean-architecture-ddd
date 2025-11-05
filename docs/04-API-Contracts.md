# API Contracts & Endpoints

## 🌐 API Gateway Configuration

### Base URL Structure
```
Production:   https://api.emis.com
Tenant:       https://{tenant}.api.emis.com
or            https://api.emis.com/{tenantId}
```

### API Versioning
```
/api/v1/{resource}
/api/v2/{resource}
```

### Authentication
```
Header: Authorization: Bearer {JWT_TOKEN}
Header: X-Tenant-Id: {tenant_id} (optional, can extract from JWT)
```

---

## 🔐 1. Identity Service API

### Base Path: `/api/v1/auth` & `/api/v1/users`

#### POST /api/v1/auth/login
**Request:**
```json
{
  "tenantId": "school-abc-xyz",
  "username": "0912345678",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "refresh_token_here",
    "expiresIn": 3600,
    "tokenType": "Bearer",
    "user": {
      "userId": "uuid",
      "username": "0912345678",
      "fullName": "Nguyễn Văn A",
      "email": "teacher@school.com",
      "roles": ["Teacher", "ClassTeacher"],
      "tenantId": "school-abc-xyz"
    }
  }
}
```

#### POST /api/v1/auth/refresh-token
**Request:**
```json
{
  "refreshToken": "refresh_token_here"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "accessToken": "new_access_token",
    "refreshToken": "new_refresh_token",
    "expiresIn": 3600
  }
}
```

#### POST /api/v1/auth/register
**Request:**
```json
{
  "tenantId": "school-abc-xyz",
  "username": "0987654321",
  "password": "SecurePassword123!",
  "email": "parent@email.com",
  "fullName": "Trần Thị B",
  "phoneNumber": "0987654321",
  "role": "Parent"
}
```

#### GET /api/v1/users/{userId}
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "userId": "uuid",
    "username": "0912345678",
    "fullName": "Nguyễn Văn A",
    "email": "teacher@school.com",
    "phoneNumber": "0912345678",
    "roles": ["Teacher"],
    "status": "Active",
    "createdAt": "2025-01-01T00:00:00Z",
    "lastLoginAt": "2025-11-05T10:30:00Z"
  }
}
```

#### PUT /api/v1/users/{userId}
**Request:**
```json
{
  "fullName": "Nguyễn Văn A Updated",
  "email": "newemail@school.com"
}
```

#### POST /api/v1/roles
**Request:**
```json
{
  "name": "AssistantTeacher",
  "description": "Giáo viên phụ trách phụ",
  "permissions": [
    "student.view",
    "attendance.create",
    "comment.create"
  ]
}
```

---

## 👶 2. Student Service API

### Base Path: `/api/v1/students` & `/api/v1/classes`

#### GET /api/v1/students
**Query Parameters:**
- `page` (default: 1)
- `pageSize` (default: 20)
- `classId` (optional)
- `status` (optional: Studying, Dropped, OnHold, Trial)
- `search` (search by name, code)

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "studentId": "uuid",
        "code": "2024-001",
        "fullName": "Bé Minh An",
        "gender": "Male",
        "dateOfBirth": "2021-05-15",
        "age": 4,
        "ethnicity": "Kinh",
        "status": "Studying",
        "class": {
          "classId": "uuid",
          "name": "Lớp Mầm",
          "grade": "3-4 tuổi"
        },
        "address": {
          "street": "123 Đường ABC",
          "ward": "Phường 1",
          "district": "Quận 1",
          "city": "TP. HCM"
        },
        "primaryParent": {
          "parentId": "uuid",
          "fullName": "Nguyễn Văn A",
          "phoneNumber": "0912345678",
          "relationship": "Father"
        }
      }
    ],
    "totalCount": 500,
    "page": 1,
    "pageSize": 20,
    "totalPages": 25
  }
}
```

#### POST /api/v1/students
**Request:**
```json
{
  "code": "2024-001",
  "fullName": "Bé Minh An",
  "gender": "Male",
  "dateOfBirth": "2021-05-15",
  "ethnicity": "Kinh",
  "address": {
    "street": "123 Đường ABC",
    "ward": "Phường 1",
    "district": "Quận 1",
    "city": "TP. HCM"
  },
  "classId": "uuid",
  "parents": [
    {
      "fullName": "Nguyễn Văn A",
      "phoneNumber": "0912345678",
      "email": "parent1@email.com",
      "dateOfBirth": "1990-01-01",
      "relationship": "Father",
      "isPrimary": true
    },
    {
      "fullName": "Trần Thị B",
      "phoneNumber": "0987654321",
      "email": "parent2@email.com",
      "dateOfBirth": "1992-02-02",
      "relationship": "Mother",
      "isPrimary": false
    }
  ]
}
```

#### GET /api/v1/students/{studentId}
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "studentId": "uuid",
    "code": "2024-001",
    "fullName": "Bé Minh An",
    "gender": "Male",
    "dateOfBirth": "2021-05-15",
    "age": 4,
    "ethnicity": "Kinh",
    "status": "Studying",
    "enrollmentDate": "2024-09-01",
    "class": {
      "classId": "uuid",
      "name": "Lớp Mầm",
      "grade": "3-4 tuổi",
      "primaryTeacher": {
        "teacherId": "uuid",
        "fullName": "Cô Hương"
      }
    },
    "address": {
      "street": "123 Đường ABC",
      "ward": "Phường 1",
      "district": "Quận 1",
      "city": "TP. HCM"
    },
    "parents": [
      {
        "parentId": "uuid",
        "fullName": "Nguyễn Văn A",
        "phoneNumber": "0912345678",
        "email": "parent1@email.com",
        "relationship": "Father",
        "isPrimary": true,
        "userId": "user_uuid"
      }
    ],
    "createdAt": "2024-09-01T00:00:00Z",
    "updatedAt": "2024-11-05T10:00:00Z"
  }
}
```

#### PATCH /api/v1/students/{studentId}/status
**Request:**
```json
{
  "status": "OnHold",
  "reason": "Gia đình đi du lịch dài ngày"
}
```

#### GET /api/v1/classes
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "classId": "uuid",
        "name": "Lớp Mầm",
        "grade": "3-4 tuổi",
        "academicYear": "2024-2025",
        "currentStudents": 25,
        "maxStudents": 30,
        "primaryTeacher": {
          "teacherId": "uuid",
          "fullName": "Cô Hương"
        },
        "status": "Active"
      }
    ]
  }
}
```

#### GET /api/v1/classes/{classId}/students
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "class": {
      "classId": "uuid",
      "name": "Lớp Mầm",
      "currentStudents": 25
    },
    "students": [
      {
        "studentId": "uuid",
        "code": "2024-001",
        "fullName": "Bé Minh An",
        "gender": "Male",
        "dateOfBirth": "2021-05-15"
      }
    ]
  }
}
```

---

## 👨‍🏫 3. Teacher Service API

### Base Path: `/api/v1/teachers`

#### GET /api/v1/teachers
**Query Parameters:**
- `page`, `pageSize`
- `status` (Active, OnLeave, Resigned)
- `classId` (filter by assigned class)

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "teacherId": "uuid",
        "fullName": "Nguyễn Thị Hương",
        "gender": "Female",
        "phoneNumber": "0912345678",
        "email": "teacher@school.com",
        "hireDate": "2020-09-01",
        "status": "Active",
        "assignedClasses": [
          {
            "classId": "uuid",
            "className": "Lớp Mầm",
            "role": "Primary"
          }
        ]
      }
    ]
  }
}
```

#### POST /api/v1/teachers
**Request:**
```json
{
  "fullName": "Nguyễn Thị Hương",
  "gender": "Female",
  "dateOfBirth": "1995-03-15",
  "phoneNumber": "0912345678",
  "email": "teacher@school.com",
  "address": {
    "street": "456 Đường XYZ",
    "ward": "Phường 2",
    "district": "Quận 2",
    "city": "TP. HCM"
  },
  "hireDate": "2024-09-01"
}
```

#### POST /api/v1/teachers/{teacherId}/classes
**Request:**
```json
{
  "classId": "uuid",
  "role": "Primary",
  "startDate": "2024-09-01",
  "endDate": null
}
```

#### GET /api/v1/teachers/{teacherId}/schedule
**Query Parameters:**
- `startDate`, `endDate`

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "teacher": {
      "teacherId": "uuid",
      "fullName": "Cô Hương"
    },
    "schedule": [
      {
        "date": "2025-11-05",
        "classes": [
          {
            "classId": "uuid",
            "className": "Lớp Mầm",
            "timeSlot": "7:30 - 11:30"
          }
        ]
      }
    ]
  }
}
```

---

## ✅ 4. Attendance Service API

### Base Path: `/api/v1/attendance` & `/api/v1/comments`

#### POST /api/v1/attendance/check-in
**Request:**
```json
{
  "studentId": "uuid",
  "classId": "uuid",
  "date": "2025-11-05",
  "checkInTime": "07:45:00",
  "status": "Present",
  "note": "Học sinh đến sớm"
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "data": {
    "attendanceId": "uuid",
    "studentId": "uuid",
    "date": "2025-11-05",
    "status": "Present",
    "checkInTime": "07:45:00"
  }
}
```

#### GET /api/v1/attendance/class/{classId}/date/{date}
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "classId": "uuid",
    "className": "Lớp Mầm",
    "date": "2025-11-05",
    "summary": {
      "totalStudents": 25,
      "present": 23,
      "absent": 1,
      "late": 1,
      "excused": 0
    },
    "attendances": [
      {
        "attendanceId": "uuid",
        "student": {
          "studentId": "uuid",
          "fullName": "Bé Minh An",
          "code": "2024-001"
        },
        "status": "Present",
        "checkInTime": "07:45:00",
        "checkOutTime": "16:30:00",
        "note": null
      }
    ]
  }
}
```

#### GET /api/v1/attendance/student/{studentId}/month/{month}
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "student": {
      "studentId": "uuid",
      "fullName": "Bé Minh An"
    },
    "month": "2025-11",
    "summary": {
      "totalDays": 20,
      "present": 18,
      "absent": 1,
      "late": 1,
      "attendanceRate": 90.0
    },
    "dailyRecords": [
      {
        "date": "2025-11-01",
        "status": "Present",
        "checkInTime": "07:30:00",
        "checkOutTime": "16:30:00"
      }
    ]
  }
}
```

#### POST /api/v1/comments
**Request:**
```json
{
  "studentId": "uuid",
  "date": "2025-11-05",
  "content": "Hôm nay bé ăn uống rất ngoan, ngủ đủ giấc. Bé tham gia các hoạt động vui vẻ.",
  "mood": "Happy",
  "eatingStatus": "Good",
  "sleepingStatus": "Good",
  "healthStatus": "Khỏe mạnh",
  "media": [
    {
      "type": "Image",
      "url": "https://storage.emis.com/comments/image1.jpg"
    }
  ]
}
```

#### GET /api/v1/comments/student/{studentId}/date/{date}
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "commentId": "uuid",
    "student": {
      "studentId": "uuid",
      "fullName": "Bé Minh An"
    },
    "date": "2025-11-05",
    "teacher": {
      "teacherId": "uuid",
      "fullName": "Cô Hương"
    },
    "content": "Hôm nay bé ăn uống rất ngoan...",
    "mood": "Happy",
    "eatingStatus": "Good",
    "sleepingStatus": "Good",
    "healthStatus": "Khỏe mạnh",
    "media": [
      {
        "mediaId": "uuid",
        "type": "Image",
        "url": "https://storage.emis.com/comments/image1.jpg",
        "thumbnailUrl": "https://storage.emis.com/comments/thumb_image1.jpg"
      }
    ],
    "createdAt": "2025-11-05T16:00:00Z"
  }
}
```

---

## 📊 5. Assessment Service API

### Base Path: `/api/v1/assessments` & `/api/v1/milestones`

#### POST /api/v1/assessments
**Request:**
```json
{
  "studentId": "uuid",
  "period": "Monthly",
  "assessmentDate": "2025-11-05",
  "overallScore": 8.5,
  "comment": "Bé có sự tiến bộ rõ rệt trong tháng này",
  "criteria": [
    {
      "category": "Physical",
      "name": "Vận động thô",
      "score": 9.0,
      "maxScore": 10.0,
      "note": "Bé chạy nhảy tốt, phối hợp chân tay tốt"
    },
    {
      "category": "Cognitive",
      "name": "Nhận biết màu sắc",
      "score": 8.5,
      "maxScore": 10.0,
      "note": "Bé nhận biết được 8/10 màu cơ bản"
    }
  ],
  "media": [
    {
      "type": "Video",
      "url": "https://storage.emis.com/assessments/video1.mp4",
      "description": "Video bé tham gia hoạt động thể chất"
    }
  ]
}
```

#### GET /api/v1/assessments/student/{studentId}
**Query Parameters:**
- `period` (Daily, Weekly, Monthly, Semester)
- `startDate`, `endDate`

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "student": {
      "studentId": "uuid",
      "fullName": "Bé Minh An"
    },
    "assessments": [
      {
        "assessmentId": "uuid",
        "period": "Monthly",
        "assessmentDate": "2025-11-05",
        "teacher": {
          "teacherId": "uuid",
          "fullName": "Cô Hương"
        },
        "overallScore": 8.5,
        "comment": "Bé có sự tiến bộ rõ rệt...",
        "categorySummary": {
          "Physical": 9.0,
          "Cognitive": 8.5,
          "Language": 8.0,
          "SocialEmotional": 9.0,
          "Creative": 8.0
        }
      }
    ]
  }
}
```

#### POST /api/v1/milestones
**Request:**
```json
{
  "studentId": "uuid",
  "category": "Language",
  "title": "Nói được câu hoàn chỉnh",
  "description": "Bé đã có thể nói được câu hoàn chỉnh với 5-6 từ",
  "achievedDate": "2025-11-05",
  "evidence": [
    {
      "type": "Video",
      "url": "https://storage.emis.com/milestones/video1.mp4"
    }
  ]
}
```

#### GET /api/v1/milestones/student/{studentId}
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "student": {
      "studentId": "uuid",
      "fullName": "Bé Minh An"
    },
    "milestones": [
      {
        "milestoneId": "uuid",
        "category": "Language",
        "title": "Nói được câu hoàn chỉnh",
        "description": "Bé đã có thể nói được câu...",
        "achievedDate": "2025-11-05",
        "recordedBy": {
          "teacherId": "uuid",
          "fullName": "Cô Hương"
        }
      }
    ]
  }
}
```

---

## 📰 6. News Feed Service API

### Base Path: `/api/v1/posts`

#### POST /api/v1/posts
**Request:**
```json
{
  "title": "Thông báo nghỉ lễ 30/4 - 1/5",
  "content": "Nhà trường thông báo lịch nghỉ lễ...",
  "scope": "School",
  "priority": "Important",
  "targetAudience": {
    "roles": ["Parent", "Teacher"]
  },
  "media": [
    {
      "type": "Image",
      "url": "https://storage.emis.com/posts/image1.jpg"
    }
  ]
}
```

#### GET /api/v1/posts
**Query Parameters:**
- `page`, `pageSize`
- `scope` (School, Class)
- `classId` (if scope=Class)
- `priority`

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "postId": "uuid",
        "title": "Thông báo nghỉ lễ 30/4 - 1/5",
        "content": "Nhà trường thông báo...",
        "author": {
          "userId": "uuid",
          "fullName": "Ban Giám Hiệu",
          "role": "Admin"
        },
        "scope": "School",
        "priority": "Important",
        "media": [
          {
            "mediaId": "uuid",
            "type": "Image",
            "url": "https://storage.emis.com/posts/image1.jpg"
          }
        ],
        "interactions": {
          "likesCount": 45,
          "commentsCount": 12,
          "isLikedByMe": true
        },
        "publishedAt": "2025-04-20T10:00:00Z"
      }
    ]
  }
}
```

#### POST /api/v1/posts/{postId}/like
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "isLiked": true,
    "likesCount": 46
  }
}
```

#### POST /api/v1/posts/{postId}/comments
**Request:**
```json
{
  "content": "Cảm ơn nhà trường đã thông báo",
  "parentCommentId": null
}
```

#### GET /api/v1/feed
**Personalized feed based on user's classes and roles**

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "postId": "uuid",
        "title": "Hoạt động ngoại khóa lớp Mầm",
        "content": "...",
        "relevance": "Lớp của con bạn",
        "publishedAt": "2025-11-05T10:00:00Z"
      }
    ]
  }
}
```

---

## 💬 7. Chat Service API

### Base Path: `/api/v1/conversations` & `/api/v1/messages`

#### POST /api/v1/conversations
**Request (Create Student Group):**
```json
{
  "type": "StudentGroup",
  "name": "Nhóm chat - Bé Minh An",
  "metadata": {
    "studentId": "uuid",
    "studentName": "Bé Minh An"
  },
  "participants": [
    {
      "userId": "parent1_id",
      "role": "Member"
    },
    {
      "userId": "parent2_id",
      "role": "Member"
    },
    {
      "userId": "teacher_id",
      "role": "Admin"
    }
  ]
}
```

#### GET /api/v1/conversations
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "conversationId": "uuid",
        "type": "StudentGroup",
        "name": "Nhóm chat - Bé Minh An",
        "metadata": {
          "studentId": "uuid",
          "studentName": "Bé Minh An"
        },
        "participants": [
          {
            "userId": "uuid",
            "userName": "Nguyễn Văn A",
            "role": "Member"
          }
        ],
        "lastMessage": {
          "messageId": "uuid",
          "content": "Cảm ơn cô đã chăm sóc bé",
          "senderName": "Nguyễn Văn A",
          "sentAt": "2025-11-05T15:30:00Z"
        },
        "unreadCount": 2,
        "updatedAt": "2025-11-05T15:30:00Z"
      }
    ]
  }
}
```

#### POST /api/v1/conversations/{conversationId}/messages
**Request:**
```json
{
  "content": "Xin chào cô! Hôm nay bé có ăn ngon không ạ?",
  "type": "Text",
  "replyToMessageId": null
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "data": {
    "messageId": "uuid",
    "conversationId": "uuid",
    "senderId": "uuid",
    "senderName": "Nguyễn Văn A",
    "content": "Xin chào cô! Hôm nay bé có ăn ngon không ạ?",
    "type": "Text",
    "status": "Sent",
    "sentAt": "2025-11-05T15:30:00Z"
  }
}
```

#### GET /api/v1/conversations/{conversationId}/messages
**Query Parameters:**
- `page`, `pageSize`
- `before` (messageId - for pagination)

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "conversationId": "uuid",
    "messages": [
      {
        "messageId": "uuid",
        "senderId": "uuid",
        "senderName": "Cô Hương",
        "content": "Dạ, hôm nay bé ăn rất ngon ạ. Bé ăn hết 1 bát cơm.",
        "type": "Text",
        "status": "Read",
        "sentAt": "2025-11-05T15:35:00Z",
        "readBy": [
          {
            "userId": "uuid",
            "readAt": "2025-11-05T15:36:00Z"
          }
        ]
      }
    ],
    "hasMore": true
  }
}
```

#### POST /api/v1/conversations/{conversationId}/messages (with file)
**Request (multipart/form-data):**
```
content: "Gửi cô ảnh bé ở nhà"
type: "Image"
file: [binary file]
```

---

## 💰 8. Payment Service API

### Base Path: `/api/v1/invoices` & `/api/v1/payments`

#### POST /api/v1/invoices
**Request:**
```json
{
  "studentId": "uuid",
  "month": 11,
  "academicYear": "2024-2025",
  "dueDate": "2025-11-10",
  "items": [
    {
      "feeType": "Tuition",
      "description": "Học phí tháng 11",
      "quantity": 1,
      "unitPrice": 3000000,
      "discount": 0,
      "amount": 3000000
    },
    {
      "feeType": "Meal",
      "description": "Tiền ăn tháng 11",
      "quantity": 20,
      "unitPrice": 50000,
      "discount": 0,
      "amount": 1000000
    }
  ]
}
```

#### GET /api/v1/students/{studentId}/invoices
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "student": {
      "studentId": "uuid",
      "fullName": "Bé Minh An"
    },
    "invoices": [
      {
        "invoiceId": "uuid",
        "invoiceNumber": "INV-2025-11-001",
        "month": 11,
        "academicYear": "2024-2025",
        "invoiceDate": "2025-11-01",
        "dueDate": "2025-11-10",
        "totalAmount": 4000000,
        "paidAmount": 4000000,
        "remainingAmount": 0,
        "status": "Paid"
      }
    ]
  }
}
```

#### POST /api/v1/payments/process
**Request:**
```json
{
  "invoiceId": "uuid",
  "amount": 4000000,
  "paymentMethod": "VNPay",
  "returnUrl": "https://app.emis.com/payment/callback"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "paymentId": "uuid",
    "paymentUrl": "https://vnpay.vn/payment?token=...",
    "transactionId": "VNPAY_TXN_12345"
  }
}
```

#### GET /api/v1/payments/callback
**Query Parameters (from Payment Gateway):**
- `vnp_TxnRef`, `vnp_ResponseCode`, etc.

**Response:** Redirect to app with payment result

---

## 🍱 9. Menu Service API

### Base Path: `/api/v1/menus`

#### POST /api/v1/menus
**Request:**
```json
{
  "date": "2025-11-05",
  "classId": null,
  "mealSessions": [
    {
      "type": "Breakfast",
      "dishes": [
        {
          "name": "Bánh mì trứng",
          "description": "Bánh mì nướng + trứng ốp la",
          "ingredients": "Bánh mì, trứng, bơ",
          "calories": 250,
          "protein": 10,
          "carbs": 30,
          "fat": 8
        },
        {
          "name": "Sữa tươi",
          "calories": 120,
          "protein": 8,
          "carbs": 12,
          "fat": 5
        }
      ]
    },
    {
      "type": "Lunch",
      "dishes": [
        {
          "name": "Cơm",
          "calories": 200
        },
        {
          "name": "Thịt kho trứng",
          "ingredients": "Thịt ba chỉ, trứng, nước dừa",
          "calories": 300,
          "allergenWarnings": "Có chứa trứng"
        },
        {
          "name": "Canh chua cá",
          "calories": 150
        }
      ]
    }
  ]
}
```

#### GET /api/v1/menus/date/{date}
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "menuId": "uuid",
    "date": "2025-11-05",
    "status": "Published",
    "mealSessions": [
      {
        "sessionId": "uuid",
        "type": "Breakfast",
        "totalCalories": 370,
        "dishes": [
          {
            "dishId": "uuid",
            "name": "Bánh mì trứng",
            "description": "Bánh mì nướng + trứng ốp la",
            "calories": 250
          }
        ]
      }
    ]
  }
}
```

#### GET /api/v1/menus/week/{weekStart}
**Response:** Menu for entire week

---

## 📅 10. Leave Service API

### Base Path: `/api/v1/leave-requests`

#### POST /api/v1/leave-requests
**Request:**
```json
{
  "requesterId": "student_uuid",
  "requesterType": "Student",
  "startDate": "2025-11-10",
  "endDate": "2025-11-12",
  "reason": "Gia đình có việc riêng, xin cho bé nghỉ 3 ngày"
}
```

#### GET /api/v1/leave-requests/student/{studentId}
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "leaveRequestId": "uuid",
        "startDate": "2025-11-10",
        "endDate": "2025-11-12",
        "reason": "Gia đình có việc riêng...",
        "status": "Approved",
        "approvedBy": {
          "userId": "uuid",
          "fullName": "Cô Hương"
        },
        "approvedAt": "2025-11-05T10:00:00Z",
        "createdAt": "2025-11-04T15:00:00Z"
      }
    ]
  }
}
```

#### POST /api/v1/leave-requests/{requestId}/approve
**Request:**
```json
{
  "note": "Đã duyệt"
}
```

---

## 📹 11. Camera Service API

### Base Path: `/api/v1/cameras`

#### GET /api/v1/cameras
**Query Parameters:**
- `classId` (filter by class)

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "cameraId": "uuid",
        "name": "Camera lớp Mầm - Góc 1",
        "location": "Phòng học lớp Mầm",
        "class": {
          "classId": "uuid",
          "name": "Lớp Mầm"
        },
        "status": "Online",
        "hasAccess": true
      }
    ]
  }
}
```

#### GET /api/v1/cameras/{cameraId}/stream-url
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "cameraId": "uuid",
    "streamingUrl": "https://stream.emis.com/live/camera_uuid",
    "protocol": "HLS",
    "expiresAt": "2025-11-05T17:00:00Z"
  }
}
```

#### GET /api/v1/cameras/{cameraId}/recordings
**Query Parameters:**
- `startDate`, `endDate`

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "recordings": [
      {
        "recordingId": "uuid",
        "startTime": "2025-11-05T07:30:00Z",
        "endTime": "2025-11-05T16:30:00Z",
        "duration": 32400,
        "fileSize": 5242880000
      }
    ]
  }
}
```

---

## 📈 12. Report Service API

### Base Path: `/api/v1/reports` & `/api/v1/dashboard`

#### POST /api/v1/reports/generate
**Request:**
```json
{
  "type": "AttendanceReport",
  "parameters": {
    "classId": "uuid",
    "startDate": "2025-11-01",
    "endDate": "2025-11-30"
  },
  "format": "PDF"
}
```

**Response (202 Accepted):**
```json
{
  "success": true,
  "data": {
    "reportId": "uuid",
    "status": "Generating",
    "estimatedCompletionTime": "2025-11-05T10:05:00Z"
  }
}
```

#### GET /api/v1/reports/{reportId}/download
**Response:** File download (PDF/Excel)

#### GET /api/v1/dashboard/overview
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "totalStudents": 500,
    "activeStudents": 485,
    "totalTeachers": 30,
    "totalClasses": 20,
    "todayAttendance": {
      "present": 480,
      "absent": 5,
      "rate": 96.0
    },
    "thisMonthRevenue": 120000000,
    "pendingPayments": 25
  }
}
```

---

## 🔔 13. Notification Service API

### Base Path: `/api/v1/notifications`

#### GET /api/v1/notifications
**Query Parameters:**
- `page`, `pageSize`
- `isRead` (true/false)

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "notificationId": "uuid",
        "type": "Attendance",
        "title": "Bé Minh An đã đến trường",
        "content": "Bé đã điểm danh vào lúc 07:45",
        "priority": "Normal",
        "isRead": false,
        "data": {
          "studentId": "uuid",
          "attendanceId": "uuid"
        },
        "createdAt": "2025-11-05T07:45:00Z"
      }
    ],
    "unreadCount": 5
  }
}
```

#### PUT /api/v1/notifications/{notificationId}/read
#### PUT /api/v1/notifications/read-all

#### GET /api/v1/notifications/preferences
**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "channels": {
      "push": true,
      "email": true,
      "sms": false,
      "inApp": true
    },
    "types": {
      "Attendance": true,
      "Payment": true,
      "NewsFeed": true,
      "Chat": true,
      "Assessment": true
    }
  }
}
```

---

## 🔄 Common Response Format

### Success Response
```json
{
  "success": true,
  "data": { ... },
  "metadata": {
    "timestamp": "2025-11-05T10:00:00Z",
    "requestId": "uuid"
  }
}
```

### Error Response
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Dữ liệu không hợp lệ",
    "details": [
      {
        "field": "email",
        "message": "Email không đúng định dạng"
      }
    ]
  },
  "metadata": {
    "timestamp": "2025-11-05T10:00:00Z",
    "requestId": "uuid"
  }
}
```

### Pagination
```json
{
  "success": true,
  "data": {
    "items": [...],
    "totalCount": 500,
    "page": 1,
    "pageSize": 20,
    "totalPages": 25
  }
}
```

---

## 📱 SignalR Hubs

### ChatHub
**Methods:**
- `SendMessage(conversationId, content)`
- `JoinConversation(conversationId)`
- `TypingIndicator(conversationId, isTyping)`
- `MarkAsRead(conversationId, messageId)`

### NotificationHub
**Methods:**
- `ReceiveNotification(notification)`

---

**Next:** [05-Technology-Stack.md](./05-Technology-Stack.md)
